# Airport System : Assignment 2 
# Meridian Terminal — Ground Operations System

A console application for managing a single shift of ground operations at Meridian Terminal:
flight registration, gate assignment, passenger booking and standby, baggage tracking, boarding,
and ground staff duty-hour tracking.

## How to run

1. Open the project in Visual Studio 2026 (or any environment with the .NET SDK installed).
2. Build the solution.
3. Run the project — the console menu appears immediately. No seed data is preloaded; use the
   menu to register gates, flights, passengers, and staff before booking or boarding anyone.
4. Data exists only for the current run — nothing is saved between sessions.

## Design decisions

The assignment leaves several values undefined on purpose. The choices made here, and the
reasoning behind them:

| Decision | Value chosen | Reasoning |
|---|---|---|
| Minimum connection time | 45 minutes | Enough time for a passenger to deplane, move terminal-side, and reach a new gate at a mid-sized terminal, without being so long it strands genuinely workable connections. |
| Gate turnaround window (no arrival leg) | 45 minutes before scheduled departure | Covers boarding, baggage loading, and pushback prep for a flight that originates at this terminal (no inbound leg to anchor the window to). Kept as a separate constant from the connection-time minimum even though both are currently 45 — they govern unrelated rules and could diverge later. |
| Standby list capacity | 5 passengers | Keeps the standby queue small enough that gate staff can realistically track and call it manually during a shift. |
| Baggage allowance — Standard | 30 kg | Typical single-class checked-bag allowance. |
| Baggage allowance — VIP | 50 kg | Meaningfully higher allowance as a service perk for VIP passengers. |
| Baggage allowance — Reduced Mobility | 40 kg | Accounts for mobility equipment and medical supplies often checked alongside a passenger's normal luggage. |
| Gate occupancy window | `ArrivalTime ?? (ScheduledDeparture − 45 min)` through `ScheduledDeparture` | A flight occupies its gate from when it lands (if it's someone's connecting leg) — or 45 minutes before departure otherwise — until it departs. Every gate-conflict check reads this single computed property (`Flight.GateOccupancyWindow`), so the rule is defined in exactly one place. |
| Baggage requires confirmed booking | Yes (Mandatory) | Prevents checking bags for passengers without a confirmed seat reservation on that flight. |

## Architecture

- **`Flight`, `Gate`, `Passenger` (`: Person`), `GroundStaff` (`: Person`), `Baggage`, `Booking`** —
  domain model classes. `Booking` links a `Passenger` to a `Flight` with a creation timestamp,
  used to keep the standby queue in first-in-first-out order.
- **`Flight.GateOccupancyWindow`** — the single source of truth for gate-conflict checks, backed by
  an explicit `ArrivalTime` (nullable, for connecting legs) kept separate from `ScheduledDeparture`.
- **`Program`** — the menu loop, input handling, and all business-rule checks for this build.
  Rules are enforced with early-return checks and printed `Result:` / `Reason:` messages rather
  than custom exception types, all funneled through one `catch (Exception ex)` for unexpected
  failures and a dedicated `catch (FormatException)` for bad numeric/time input.

## Menu reference

The system runs as a continuous loop — pick an option, complete it, and you're back at the menu.
Nothing exits until you choose option 14.

| # | Option | What it asks for | What it does |
|---|---|---|---|
| 1 | Register Gate | Gate number, whether it supports international flights (y/n) | Adds a new gate. Do this before assigning any flight to it. |
| 2 | Register Flight | Flight number, type (1 Domestic / 2 International), scheduled departure (HH:mm), whether it's an inbound connecting leg (y/n → arrival time), seat capacity | Adds a new flight. If you mark it as a connecting leg, its arrival time becomes what later boarding checks measure connection time against. |
| 3 | Assign Gate to Flight | Flight number, gate number | Assigns the flight to the gate, unless the gate can't take an international flight or another flight's gate-occupancy window overlaps this one's — in which case it tells you which flight is blocking it and its window. |
| 4 | Update Flight Status | Flight number, new status (1–5) | Moves a flight to Scheduled / Delayed / Boarding / Departed / Cancelled. Boarding and baggage operations check this status before proceeding. |
| 5 | Register Passenger | Passenger ID, name, category (1–3), optional connecting flight number | Adds a passenger record. This only *registers* them — it does not book them onto anything yet. |
| 6 | Book Passenger / Manage Standby | Passenger ID, flight number | Books the passenger: a confirmed seat if capacity remains, otherwise a standby slot — or an outright rejection once both the flight and its standby list are full. |
| 7 | View Flight Standby List & Manifest | Flight number | Prints the flight's status, gate, and departure time, its full list of confirmed seat-holders, and its standby queue in wait order. Read-only — doesn't change anything. |
| 8 | Cancel Passenger Booking | Passenger ID, flight number | Cancels a confirmed or standby booking. Cancelling a confirmed seat automatically promotes the earliest standby passenger, and tells you who was promoted. |
| 9 | Register Baggage | Passenger ID, flight number, weight (kg) | Checks a bag against the passenger's confirmed booking on that flight, rejecting it if it would push their cumulative weight over their category's allowance. Prints a baggage tag on success. |
| 10 | View Passenger Cumulative Baggage Weight | Passenger ID, flight number | Prints an itemized report: every bag tag and weight, the running total, the allowance, and how much is left. Read-only. |
| 11 | Check Boarding Eligibility & Process Boarding | Sub-choice (1 Check only / 2 Process scan), then passenger ID, flight number | Runs the same eligibility checks either way (flight not Departed/Cancelled, passenger holds a confirmed seat, sufficient connection time). Choice 1 stops there and reports eligible/denied. Choice 2 goes further and also requires the flight's status to already be "Boarding" before marking the passenger boarded. |
| 12 | Register Staff Member | Staff ID, name | Adds a ground staff member with zero logged hours. |
| 13 | Assign Staff & Track Shift Hours | Sub-choice (1 Assign / 2 View roster). Assign asks for staff ID, optional flight number, optional gate number, duration in hours | Sub-option 1 logs a duty assignment against the staff member, rejecting it if it would push their cumulative shift hours over 8. Sub-option 2 prints every staff member's total hours and their full assignment history. |
| 14 | Exit | — | Ends the session. Nothing is saved — all data is gone on the next run. |

A few sequencing notes that follow from how the menu is built:
- Register a gate (1) and a flight (2) before trying to assign one to the other (3).
- Register a passenger (5) before booking them (6) — booking an unregistered ID fails.
- A passenger needs a **confirmed** booking (6) before baggage can be registered against them (9).
- Boarding (11, option 2) won't succeed until the flight's status has been moved to "Boarding" via option 4 — checking eligibility (11, option 1) will still say eligible beforehand, since eligibility and "cleared to physically board" are treated as separate questions.

## Implemented features

**Flight & Gate Management**
- Register a gate, including whether it supports international flights
- Register a flight (domestic/international, scheduled departure, optional inbound arrival time
  for connecting legs, seat capacity)
- Assign a flight to a gate — rejects international flights at domestic-only gates, and rejects
  any assignment that overlaps another flight's gate-occupancy window
- Update a flight's status (Scheduled / Delayed / Boarding / Departed / Cancelled)

**Passengers & Boarding**
- Register a passenger (Standard / VIP / Reduced Mobility), with an optional link to an earlier
  connecting flight
- Book a passenger onto a flight — confirms a seat if capacity remains, otherwise places them on
  the standby list (rejecting outright once the standby list is also full)
- View a flight's full manifest: confirmed seats and the standby queue in wait order
- Cancel a confirmed or standby booking — cancelling a confirmed seat automatically promotes the
  earliest standby passenger
- Check boarding eligibility on its own, or process an actual gate boarding scan — both paths
  verify the flight isn't Departed/Cancelled, the passenger holds a confirmed seat, and (for
  connecting passengers) enough time remains since their inbound flight's arrival; the boarding
  scan additionally requires the flight's status to already be "Boarding"

**Baggage**
- Register a bag against a passenger's confirmed booking on a flight, rejecting any bag that
  would push their cumulative checked weight over their category's allowance — even if that bag
  alone is within a normal limit
- View a passenger's full baggage report for a flight: bag count, itemized tags and weights,
  total weight, and remaining allowance

**Staff**
- Register a ground staff member
- Assign a staff member to a flight, a gate, both, or general terminal duty for a given duration,
  rejecting any assignment that would push their cumulative shift hours over the 8-hour cap
- View the full staff roster with logged hours and a per-person assignment history

**Error handling**
- Invalid menu choices, unknown flight/gate/passenger/staff IDs, and non-numeric or malformed
  time input are all caught and reported with a specific reason rather than a generic error
