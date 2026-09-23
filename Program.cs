using System;
using System.Collections.Generic;
using System.Linq;

namespace AirportSystem3
{
    public enum FlightType { Domestic = 1, International = 2 }
    public enum FlightStatus { Scheduled = 1, Delayed = 2, Boarding = 3, Departed = 4, Cancelled = 5 }
    public enum PassengerCategory { Standard = 1, VIP = 2, ReducedMobility = 3 }

    public class Gate
    {
        public string GateNumber { get; set; }
        public bool SupportsInternational { get; set; }

        public Gate(string gateNumber, bool supportsInternational)
        {
            GateNumber = gateNumber;
            SupportsInternational = supportsInternational;
        }
    }

    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public Person(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class Passenger : Person
    {
        public PassengerCategory Category { get; set; }
        public Flight? ConnectingFlight { get; set; }

        public Passenger(string id, string name, PassengerCategory category, Flight? connectingFlight = null)
            : base(id, name)
        {
            Category = category;
            ConnectingFlight = connectingFlight;
        }

        public double GetBaggageAllowance() => Category switch
        {
            PassengerCategory.VIP => 50.0,
            PassengerCategory.ReducedMobility => 40.0,
            _ => 30.0 // Standard
        };
    }

    public class GroundStaff : Person
    {
        public const double MaxShiftHours = 8.0;
        public double TotalDutyHours { get; set; } = 0.0;
        public List<string> Assignments { get; set; } = new List<string>();

        public GroundStaff(string id, string name) : base(id, name) { }
    }

    public class Baggage
    {
        public string BaggageTag { get; set; }
        public Passenger Owner { get; set; }
        public Flight Flight { get; set; }
        public double WeightKg { get; set; }

        public Baggage(string baggageTag, Passenger owner, Flight flight, double weightKg)
        {
            BaggageTag = baggageTag;
            Owner = owner;
            Flight = flight;
            WeightKg = weightKg;
        }
    }

    public class Booking
    {
        public Passenger Passenger { get; set; }
        public Flight Flight { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Booking(Passenger passenger, Flight flight)
        {
            Passenger = passenger;
            Flight = flight;
        }
    }

    public class Flight
    {
        public const int GateTurnaroundMinutes = 45;

        public string FlightNumber { get; set; }
        public FlightType Type { get; set; }
        public DateTime ScheduledDeparture { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int SeatCapacity { get; set; }
        public Gate? AssignedGate { get; set; }
        public FlightStatus Status { get; set; }

        public List<Booking> ConfirmedBookings { get; set; } = new List<Booking>();
        public List<Booking> StandbyList { get; set; } = new List<Booking>();
        public List<Passenger> BoardedPassengers { get; set; } = new List<Passenger>();

        public Flight(string flightNumber, FlightType type, DateTime scheduledDeparture, int seatCapacity, DateTime? arrivalTime = null)
        {
            FlightNumber = flightNumber;
            Type = type;
            ScheduledDeparture = scheduledDeparture;
            SeatCapacity = seatCapacity;
            ArrivalTime = arrivalTime;
            Status = FlightStatus.Scheduled;
        }

        // source of truth for Gate Occupancy
        public (DateTime Start, DateTime End) GateOccupancyWindow =>
            (ArrivalTime ?? ScheduledDeparture.AddMinutes(-GateTurnaroundMinutes), ScheduledDeparture);

        public bool OverlapsGateWindow(Flight other)
        {
            var (startA, endA) = GateOccupancyWindow;
            var (startB, endB) = other.GateOccupancyWindow;
            return startA < endB && startB < endA;
        }
    }

    internal class Program
    {
        static List<Flight> flights = new List<Flight>();
        static List<Gate> gates = new List<Gate>();
        static List<Passenger> passengers = new List<Passenger>();
        static List<GroundStaff> staffMembers = new List<GroundStaff>();
        static List<Baggage> baggageList = new List<Baggage>();

        const int MinimumConnectionMinutes = 45;
        const int MaxStandbyCapacity = 5;

        static void Main(string[] args)
        {
            bool isRunning = true;
            while (isRunning)
            {
                Console.WriteLine("\n==============================================");
                Console.WriteLine("=== Meridian Terminal Ground Ops System ===");
                Console.WriteLine("==============================================");
                Console.WriteLine("1.  Register Gate");
                Console.WriteLine("2.  Register Flight");
                Console.WriteLine("3.  Assign Gate to Flight");
                Console.WriteLine("4.  Update Flight Status");
                Console.WriteLine("5.  Register Passenger");
                Console.WriteLine("6.  Book Passenger / Manage Standby");
                Console.WriteLine("7.  View Flight Standby List & Manifest");
                Console.WriteLine("8.  Cancel Passenger Booking");
                Console.WriteLine("9.  Register Baggage");
                Console.WriteLine("10. View Passenger Cumulative Baggage Weight");
                Console.WriteLine("11. Check Boarding Eligibility & Process Boarding");
                Console.WriteLine("12. Register Staff Member");
                Console.WriteLine("13. Assign Staff & Track Shift Hours");
                Console.WriteLine("14. Exit");
                Console.WriteLine("----------------------------------------------");
                Console.Write("Select an option (1-14): ");

                string? choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1": RegisterGate(); break;
                        case "2": RegisterFlight(); break;
                        case "3": AssignGate(); break;
                        case "4": UpdateFlightStatus(); break;
                        case "5": RegisterPassenger(); break;
                        case "6": BookPassenger(); break;
                        case "7": ViewStandbyList(); break;
                        case "8": CancelBooking(); break;
                        case "9": RegisterBaggage(); break;
                        case "10": ViewCumulativeBaggage(); break;
                        case "11": BoardingOperations(); break;
                        case "12": RegisterStaff(); break;
                        case "13": ManageStaff(); break;
                        case "14":
                            isRunning = false;
                            Console.WriteLine("Exiting Ground Ops System. Shift ended.");
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please enter a number between 1 and 14.");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Result: OPERATION FAILED");
                    Console.WriteLine("Reason: Invalid numeric or time format entered. Please try again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Result: OPERATION REJECTED");
                    Console.WriteLine($"Reason: {ex.Message}");
                }
            }
        }

        static void RegisterGate()
        {
            Console.Write("Enter Gate Number (e.g. G1): ");
            string gateNo = Console.ReadLine()!.Trim().ToUpper();

            if (gates.Any(g => g.GateNumber == gateNo))
            {
                Console.WriteLine("Result: Gate already exists.");
                return;
            }

            Console.Write("Does this gate support International flights? (y/n): ");
            bool isInternational = Console.ReadLine()?.Trim().ToLower() == "y";

            gates.Add(new Gate(gateNo, isInternational));
            Console.WriteLine($"Result: Gate {gateNo} registered successfully (International: {(isInternational ? "Yes" : "No")}).");
        }

        static void RegisterFlight()
        {
            Console.Write("Enter Flight Number (e.g. MT-111): ");
            string flightNo = Console.ReadLine()!.Trim().ToUpper();

            if (flights.Any(f => f.FlightNumber == flightNo))
            {
                Console.WriteLine("Result: Flight already registered.");
                return;
            }

            Console.Write("Flight Type (1 for Domestic, 2 for International): ");
            int typeInput = int.Parse(Console.ReadLine()!);
            if (typeInput != 1 && typeInput != 2)
            {
                Console.WriteLine("Result: Invalid flight type selected.");
                return;
            }

            Console.Write("Enter Scheduled Departure Time (HH:mm, e.g. 14:30): ");
            DateTime departure = DateTime.Today.Add(TimeSpan.Parse(Console.ReadLine()!.Trim()));

            Console.Write("Is this an inbound connecting leg arriving at the terminal? (y/n): ");
            DateTime? arrival = null;
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                Console.Write("Enter Inbound Arrival Time (HH:mm): ");
                arrival = DateTime.Today.Add(TimeSpan.Parse(Console.ReadLine()!.Trim()));
            }

            Console.Write("Enter Confirmed Seat Capacity: ");
            int capacity = int.Parse(Console.ReadLine()!);
            if (capacity <= 0)
            {
                Console.WriteLine("Result: Seat capacity must be greater than zero.");
                return;
            }

            flights.Add(new Flight(flightNo, (FlightType)typeInput, departure, capacity, arrival));
            Console.WriteLine($"Result: Flight {flightNo} successfully registered for departure at {departure:HH:mm}.");
        }

        static void AssignGate()
        {
            Console.Write("Enter Flight Number: ");
            string flightNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == flightNo);

            if (flight == null)
            {
                Console.WriteLine($"Result: Flight {flightNo} does not exist.");
                return;
            }

            Console.Write("Enter Gate Number: ");
            string gateNo = Console.ReadLine()!.Trim().ToUpper();
            Gate? gate = gates.FirstOrDefault(g => g.GateNumber == gateNo);

            if (gate == null)
            {
                Console.WriteLine($"Result: Gate {gateNo} not found. Register the gate first.");
                return;
            }

            if (flight.Type == FlightType.International && !gate.SupportsInternational)
            {
                Console.WriteLine($"Result: GATE REJECTED. Gate {gateNo} does not support International flights.");
                return;
            }

            // gate window check
            var conflict = flights.FirstOrDefault(f =>
                f.FlightNumber != flight.FlightNumber &&
                f.AssignedGate == gate &&
                f.Status != FlightStatus.Departed &&
                f.Status != FlightStatus.Cancelled &&
                f.OverlapsGateWindow(flight));

            if (conflict != null)
            {
                var (cStart, cEnd) = conflict.GateOccupancyWindow;
                Console.WriteLine("Result: GATE ASSIGNMENT REJECTED");
                Console.WriteLine($"Reason: Gate {gateNo} is already occupied by flight {conflict.FlightNumber} ({cStart:HH:mm} - {cEnd:HH:mm}).");
                return;
            }

            flight.AssignedGate = gate;
            Console.WriteLine($"Result: Flight {flightNo} successfully assigned to Gate {gateNo}.");
        }

        static void UpdateFlightStatus()
        {
            Console.Write("Enter Flight Number: ");
            string flightNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == flightNo);

            if (flight == null)
            {
                Console.WriteLine("Result: Flight not found.");
                return;
            }

            Console.WriteLine("1. Scheduled | 2. Delayed | 3. Boarding | 4. Departed | 5. Cancelled");
            Console.Write("Choose new status (1-5): ");
            int statusVal = int.Parse(Console.ReadLine()!);

            if (statusVal < 1 || statusVal > 5)
            {
                Console.WriteLine("Result: Invalid status selected.");
                return;
            }

            flight.Status = (FlightStatus)statusVal;
            Console.WriteLine($"Result: Flight {flightNo} status updated to {flight.Status}.");
        }

        static void RegisterPassenger()
        {
            Console.Write("Enter Passenger ID (e.g. P-20): ");
            string pId = Console.ReadLine()!.Trim().ToUpper();

            if (passengers.Any(p => p.Id == pId))
            {
                Console.WriteLine("Result: Passenger already exists.");
                return;
            }

            Console.Write("Enter Full Name: ");
            string name = Console.ReadLine()!.Trim();

            Console.WriteLine("1. Standard (30kg) | 2. VIP (50kg) | 3. ReducedMobility (40kg)");
            Console.Write("Select category (1-3): ");
            int catVal = int.Parse(Console.ReadLine()!);
            if (catVal < 1 || catVal > 3)
            {
                Console.WriteLine("Result: Invalid category choice.");
                return;
            }

            Console.Write("Enter Inbound Connecting Flight Number (leave empty if none): ");
            string? connectingNo = Console.ReadLine()?.Trim().ToUpper();

            Flight? connectingFlight = null;
            if (!string.IsNullOrEmpty(connectingNo))
            {
                connectingFlight = flights.FirstOrDefault(f => f.FlightNumber == connectingNo);
                if (connectingFlight == null)
                {
                    Console.WriteLine($"Result: Connecting flight {connectingNo} does not exist.");
                    return;
                }
            }

            passengers.Add(new Passenger(pId, name, (PassengerCategory)catVal, connectingFlight));
            Console.WriteLine($"Result: Passenger {pId} ({name}) registered.");
        }

        static void BookPassenger()
        {
            Console.Write("Enter Passenger ID: ");
            string pId = Console.ReadLine()!.Trim().ToUpper();
            Passenger? passenger = passengers.FirstOrDefault(p => p.Id == pId);

            if (passenger == null)
            {
                Console.WriteLine("Result: Passenger must be registered before booking.");
                return;
            }

            Console.Write("Enter Flight Number to Book: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (flight == null)
            {
                Console.WriteLine("Result: Flight not found.");
                return;
            }

            if (flight.Status == FlightStatus.Departed || flight.Status == FlightStatus.Cancelled)
            {
                Console.WriteLine($"Result: Cannot book. Flight {fNo} is {flight.Status}.");
                return;
            }

            if (flight.ConfirmedBookings.Any(b => b.Passenger == passenger) || flight.StandbyList.Any(b => b.Passenger == passenger))
            {
                Console.WriteLine("Result: Passenger already holds a booking or standby slot on this flight.");
                return;
            }

            if (flight.ConfirmedBookings.Count < flight.SeatCapacity)
            {
                flight.ConfirmedBookings.Add(new Booking(passenger, flight));
                Console.WriteLine($"Result: CONFIRMED. Seat assigned ({flight.ConfirmedBookings.Count}/{flight.SeatCapacity}).");
            }
            else if (flight.StandbyList.Count < MaxStandbyCapacity)
            {
                flight.StandbyList.Add(new Booking(passenger, flight));
                Console.WriteLine($"Result: FLIGHT FULL. Placed on STANDBY (Position #{flight.StandbyList.Count}).");
            }
            else
            {
                Console.WriteLine("Result: BOOKING REJECTED. Flight is sold out and Standby list is full.");
            }
        }

        static void ViewStandbyList()
        {
            Console.Write("Enter Flight Number: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (flight == null)
            {
                Console.WriteLine("Result: Flight not found.");
                return;
            }

            Console.WriteLine($"\n=== Manifest & Standby for Flight {flight.FlightNumber} ===");
            Console.WriteLine($"Status: {flight.Status} | Gate: {(flight.AssignedGate != null ? flight.AssignedGate.GateNumber : "Unassigned")} | Departure: {flight.ScheduledDeparture:HH:mm}");
            Console.WriteLine($"Confirmed Seats ({flight.ConfirmedBookings.Count}/{flight.SeatCapacity}):");

            if (flight.ConfirmedBookings.Count > 0)
            {
                for (int i = 0; i < flight.ConfirmedBookings.Count; i++)
                {
                    var p = flight.ConfirmedBookings[i].Passenger;
                    Console.WriteLine($"  Seat {i + 1}: {p.Id} - {p.Name} ({p.Category})");
                }
            }
            else
            {
                Console.WriteLine("  No confirmed passengers.");
            }

            Console.WriteLine($"\nStandby Queue ({flight.StandbyList.Count}/{MaxStandbyCapacity}):");
            if (flight.StandbyList.Count > 0)
            {
                var sortedStandby = flight.StandbyList.OrderBy(b => b.CreatedAt).ToList();
                for (int i = 0; i < sortedStandby.Count; i++)
                {
                    var p = sortedStandby[i].Passenger;
                    Console.WriteLine($"  Standby #{i + 1}: {p.Id} - {p.Name} (Queued at {sortedStandby[i].CreatedAt:HH:mm:ss})");
                }
            }
            else
            {
                Console.WriteLine("  Standby queue is empty.");
            }
        }

        static void CancelBooking()
        {
            Console.Write("Enter Passenger ID: ");
            string pId = Console.ReadLine()!.Trim().ToUpper();
            Passenger? passenger = passengers.FirstOrDefault(p => p.Id == pId);

            Console.Write("Enter Flight Number: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (passenger == null || flight == null)
            {
                Console.WriteLine("Result: Invalid Passenger ID or Flight Number.");
                return;
            }

            var confirmed = flight.ConfirmedBookings.FirstOrDefault(b => b.Passenger == passenger);
            if (confirmed != null)
            {
                flight.ConfirmedBookings.Remove(confirmed);
                Console.WriteLine($"Result: Booking for {pId} cancelled.");

                // Auto-promote earliest standby (FIFO)
                if (flight.StandbyList.Count > 0)
                {
                    var promoted = flight.StandbyList.OrderBy(b => b.CreatedAt).First();
                    flight.StandbyList.Remove(promoted);
                    flight.ConfirmedBookings.Add(promoted);
                    Console.WriteLine($"[AUTO-PROMOTION] Standby passenger {promoted.Passenger.Id} ({promoted.Passenger.Name}) promoted to CONFIRMED seat!");
                }
                return;
            }

            var standby = flight.StandbyList.FirstOrDefault(b => b.Passenger == passenger);
            if (standby != null)
            {
                flight.StandbyList.Remove(standby);
                Console.WriteLine($"Result: Passenger {pId} removed from Standby queue.");
                return;
            }

            Console.WriteLine("Result: Passenger does not hold an active booking on this flight.");
        }

        static void RegisterBaggage()
        {
            Console.Write("Enter Passenger ID: ");
            string pId = Console.ReadLine()!.Trim().ToUpper();
            Passenger? passenger = passengers.FirstOrDefault(p => p.Id == pId);

            Console.Write("Enter Flight Number: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (passenger == null || flight == null)
            {
                Console.WriteLine("Result: Passenger or Flight not found.");
                return;
            }

            bool isConfirmed = flight.ConfirmedBookings.Any(b => b.Passenger == passenger);
            if (!isConfirmed)
            {
                Console.WriteLine("\nResult: BAGGAGE REJECTED");
                Console.WriteLine($"Reason: Passenger {pId} does not hold a confirmed booking on flight {fNo}.");
                return;
            }

            if (flight.Status == FlightStatus.Departed || flight.Status == FlightStatus.Cancelled)
            {
                Console.WriteLine($"\nResult: BAGGAGE REJECTED");
                Console.WriteLine($"Reason: Flight {fNo} is {flight.Status}.");
                return;
            }

            Console.Write("Enter Baggage Weight (kg): ");
            double weight = double.Parse(Console.ReadLine()!);

            if (weight <= 0)
            {
                Console.WriteLine("Result: Baggage weight must be greater than zero.");
                return;
            }

            double currentWeight = baggageList
                .Where(b => b.Owner == passenger && b.Flight == flight)
                .Sum(b => b.WeightKg);

            double allowance = passenger.GetBaggageAllowance();

            if (currentWeight + weight > allowance)
            {
                Console.WriteLine("\nResult: BAGGAGE REJECTED");
                Console.WriteLine($"Reason: This bag would bring the passenger's total checked baggage to {currentWeight + weight:F1}kg, exceeding the {allowance:F1}kg allowance for {passenger.Category} passengers.");
                return;
            }

            string tag = $"BAG-{fNo}-{pId}-{baggageList.Count + 1}";
            baggageList.Add(new Baggage(tag, passenger, flight, weight));
            Console.WriteLine($"\nResult: BAGGAGE ACCEPTED. (Tag: {tag})");
            Console.WriteLine($"Current total checked weight: {currentWeight + weight:F1}kg / {allowance:F1}kg.");
        }

        static void ViewCumulativeBaggage()
        {
            Console.Write("Enter Passenger ID: ");
            string pId = Console.ReadLine()!.Trim().ToUpper();
            Passenger? passenger = passengers.FirstOrDefault(p => p.Id == pId);

            Console.Write("Enter Flight Number: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (passenger == null || flight == null)
            {
                Console.WriteLine("Result: Passenger or Flight not found.");
                return;
            }

            var bags = baggageList.Where(b => b.Owner == passenger && b.Flight == flight).ToList();
            double totalWeight = bags.Sum(b => b.WeightKg);
            double allowance = passenger.GetBaggageAllowance();

            Console.WriteLine($"\n=== Cumulative Baggage Report ===");
            Console.WriteLine($"Passenger: {passenger.Name} (ID: {passenger.Id})");
            Console.WriteLine($"Category: {passenger.Category} | Flight: {fNo}");
            Console.WriteLine($"Total Bags Checked: {bags.Count}");
            Console.WriteLine($"Total Weight: {totalWeight:F1}kg / Allowance: {allowance:F1}kg (Remaining: {Math.Max(0, allowance - totalWeight):F1}kg)");

            if (bags.Count > 0)
            {
                Console.WriteLine("Individual Bags:");
                for (int i = 0; i < bags.Count; i++)
                {
                    Console.WriteLine($"  Bag #{i + 1}: Tag: {bags[i].BaggageTag} | Weight: {bags[i].WeightKg:F1}kg");
                }
            }
            else
            {
                Console.WriteLine("  No bags registered for this flight.");
            }
        }

        static void BoardingOperations()
        {
            Console.WriteLine("1. Check Boarding Eligibility Only | 2. Process Gate Boarding Scan");
            Console.Write("Choice: ");
            string? subChoice = Console.ReadLine()?.Trim();

            Console.Write("Enter Passenger ID: ");
            string pId = Console.ReadLine()!.Trim().ToUpper();
            Passenger? passenger = passengers.FirstOrDefault(p => p.Id == pId);

            Console.Write("Enter Flight Number: ");
            string fNo = Console.ReadLine()!.Trim().ToUpper();
            Flight? flight = flights.FirstOrDefault(f => f.FlightNumber == fNo);

            if (passenger == null || flight == null)
            {
                Console.WriteLine("Result: Invalid Passenger ID or Flight Number.");
                return;
            }

            
            if (flight.Status == FlightStatus.Departed || flight.Status == FlightStatus.Cancelled)
            {
                Console.WriteLine("\nResult: BOARDING DENIED");
                Console.WriteLine($"Reason: Flight is already {flight.Status}.");
                return;
            }

            
            bool isConfirmed = flight.ConfirmedBookings.Any(b => b.Passenger == passenger);
            bool isStandby = flight.StandbyList.Any(b => b.Passenger == passenger);

            if (!isConfirmed)
            {
                Console.WriteLine("\nResult: BOARDING DENIED");
                Console.WriteLine(isStandby
                    ? "Reason: Passenger is on Standby and does not hold a confirmed seat."
                    : "Reason: Passenger has no booking on this flight.");
                return;
            }

            //  here connection time check (compares next departure with inbound arrival)
            if (passenger.ConnectingFlight != null)
            {
                DateTime inboundArrival = passenger.ConnectingFlight.ArrivalTime ?? passenger.ConnectingFlight.ScheduledDeparture;
                double diffMinutes = (flight.ScheduledDeparture - inboundArrival).TotalMinutes;
                if (diffMinutes < MinimumConnectionMinutes)
                {
                    Console.WriteLine("\nResult: BOARDING DENIED");
                    Console.WriteLine($"Reason: Only {diffMinutes:F0} minutes remain since the connecting flight's arrival; the minimum connection time is {MinimumConnectionMinutes} minutes.");
                    return;
                }
            }

            
            if (subChoice == "1")
            {
                Console.WriteLine("\nResult: BOARDING ELIGIBLE");
                Console.WriteLine($"Passenger {passenger.Name} meets all connection and booking requirements for flight {flight.FlightNumber}.");
                return;
            }

            // actual boarding gate scan:  requires Flight to be in Boarding status
            if (flight.Status != FlightStatus.Boarding)
            {
                Console.WriteLine("\nResult: BOARDING DENIED");
                Console.WriteLine($"Reason: Flight {flight.FlightNumber} is currently '{flight.Status}'. Status must be 'Boarding' to scan passengers.");
                return;
            }

            if (!flight.BoardedPassengers.Contains(passenger))
            {
                flight.BoardedPassengers.Add(passenger);
                Console.WriteLine($"\nResult: BOARDING SUCCESSFUL");
                Console.WriteLine($"Passenger {pId} ({passenger.Name}) has boarded flight {fNo}.");
            }
            else
            {
                Console.WriteLine($"Result: Passenger {pId} is already marked as boarded.");
            }
        }

        static void RegisterStaff()
        {
            Console.Write("Enter Staff ID (e.g. ST-101): ");
            string sId = Console.ReadLine()!.Trim().ToUpper();

            if (staffMembers.Any(s => s.Id == sId))
            {
                Console.WriteLine("Result: Staff member with this ID already exists.");
                return;
            }

            Console.Write("Enter Staff Full Name: ");
            string name = Console.ReadLine()!.Trim();

            staffMembers.Add(new GroundStaff(sId, name));
            Console.WriteLine($"Result: Staff member {name} (ID: {sId}) registered successfully.");
        }

        static void ManageStaff()
        {
            Console.WriteLine("1. Assign Staff to Flight / Gate | 2. View Staff Hours & Assignments");
            Console.Write("Choice: ");
            string? opt = Console.ReadLine()?.Trim();

            if (opt == "1")
            {
                Console.Write("Enter Staff ID (e.g. ST-101): ");
                string sId = Console.ReadLine()!.Trim().ToUpper();
                GroundStaff? staff = staffMembers.FirstOrDefault(s => s.Id == sId);

                if (staff == null)
                {
                    Console.WriteLine("Result: Staff member not found. Register them first.");
                    return;
                }

                Console.Write("Enter Flight Number to assign (leave blank if none): ");
                string? fNo = Console.ReadLine()?.Trim().ToUpper();
                if (!string.IsNullOrEmpty(fNo) && !flights.Any(f => f.FlightNumber == fNo))
                {
                    Console.WriteLine($"Result: Flight {fNo} does not exist.");
                    return;
                }

                Console.Write("Enter Gate Number to assign (leave blank if none): ");
                string? gNo = Console.ReadLine()?.Trim().ToUpper();
                if (!string.IsNullOrEmpty(gNo) && !gates.Any(g => g.GateNumber == gNo))
                {
                    Console.WriteLine($"Result: Gate {gNo} does not exist.");
                    return;
                }

                Console.Write("Enter Assignment Duration in Hours (e.g. 2.5): ");
                double hours = double.Parse(Console.ReadLine()!);

                if (staff.TotalDutyHours + hours > GroundStaff.MaxShiftHours)
                {
                    Console.WriteLine("\nResult: ASSIGNMENT REJECTED");
                    Console.WriteLine($"Reason: Adding {hours:F1} hrs would bring {staff.Name} to {staff.TotalDutyHours + hours:F1} hrs, exceeding the {GroundStaff.MaxShiftHours:F1} hrs shift limit.");
                    return;
                }

                string target = "General Terminal Duty";
                if (!string.IsNullOrEmpty(fNo) && !string.IsNullOrEmpty(gNo))
                    target = $"Flight {fNo} at Gate {gNo}";
                else if (!string.IsNullOrEmpty(fNo))
                    target = $"Flight {fNo}";
                else if (!string.IsNullOrEmpty(gNo))
                    target = $"Gate {gNo}";

                staff.TotalDutyHours += hours;
                staff.Assignments.Add($"{target} ({hours:F1} hrs)");

                Console.WriteLine($"\nResult: ASSIGNMENT CONFIRMED");
                Console.WriteLine($"Staff member {staff.Name} assigned to {target}. Total logged hours: {staff.TotalDutyHours:F1}/{GroundStaff.MaxShiftHours:F1} hrs.");
            }
            else if (opt == "2")
            {
                Console.WriteLine("\n--- Ground Staff Roster & Duty Logs ---");
                if (staffMembers.Count == 0)
                {
                    Console.WriteLine("No staff members registered.");
                    return;
                }

                foreach (var s in staffMembers)
                {
                    Console.WriteLine($"ID: {s.Id} | Name: {s.Name} | Logged: {s.TotalDutyHours:F1}/{GroundStaff.MaxShiftHours:F1} hrs");
                    if (s.Assignments.Count > 0)
                    {
                        foreach (var task in s.Assignments)
                        {
                            Console.WriteLine($"   -> {task}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   -> No active assignments recorded.");
                    }
                }
            }
        }
    }
}