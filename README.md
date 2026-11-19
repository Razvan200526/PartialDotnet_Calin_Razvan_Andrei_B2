# RoomHub

A study room booking API built with .NET 10, vertical slices, rich domain objects, hand-written CQRS handlers, and EF Core SQLite.

## Run

Install a .NET 10 SDK, then run from the repository root:

```sh
dotnet restore RoomHub.slnx
dotnet run --project src/RoomHub.Api
```

The launch profile listens on http://localhost:5090. Startup applies the checked-in EF migrations automatically. The default database is src/RoomHub.Api/roomhub.db and survives restarts.

Override the database with ConnectionStrings:Default in application configuration or the ConnectionStrings__Default environment variable. Relative file paths resolve against the application's content root.

## API

The original assignment routes remain available. Explicit v1 routes provide the same contract:

| Operation | Assignment route | Versioned route |
|---|---|---|
| Create room | POST /api/rooms | POST /api/v1/rooms |
| Get room | GET /api/rooms/{id} | GET /api/v1/rooms/{id} |
| Create booking | POST /api/rooms/{roomId}/bookings | POST /api/v1/rooms/{roomId}/bookings |
| Get booking | GET /api/bookings/{id} | GET /api/v1/bookings/{id} |
| Cancel booking | POST /api/bookings/{id}/cancel | POST /api/v1/bookings/{id}/cancel |

Creation returns 201 with a Location header matching the requested route style. Successful cancellation returns 204. Errors use application/problem+json: 400 for validation, 404 for missing resources, and 409 for conflicts.

Open roomhub.http in Rider or a REST Client extension to exercise the API. Set apiPath to /api to run the same requests through the assignment routes.

## Structure

- src/RoomHub.Api/Features: the five use cases, each with its own contracts, handler, and endpoint.
- src/RoomHub.Api/Domain: entities with private state, business rules, Result/Error, repository interfaces, and Unit of Work contracts.
- src/RoomHub.Api/Infrastructure: SQLite repositories, EF mappings and migrations, and transaction handling.
- src/RoomHub.Api/Extensions: HTTP setup and API versioning.
- tests: separate unit and integration test projects, using fixed TimeProvider instances and temporary SQLite files.

Each declared type has its own file. Request-validation filters reuse domain validation, and handlers enforce rules when invoked directly.

## Booking duration by room capacity

Rooms with capacity greater than 10 allow bookings lasting at most two hours. Rooms with capacity 10 or less keep the four-hour limit from R6. A booking lasting exactly the applicable limit is accepted; a longer one returns 400 with the title BookingTooLong.

The limit uses the room's capacity, even when only one person attends. Booking.Validate enforces it for both route validation and direct handler calls. The rule applies when creating bookings; existing bookings retain their stored data and follow the cancellation rules below.

## Cancellation window

An active booking can be cancelled only when its start is at least 60 minutes after the current time. Exactly 60 minutes is allowed. A shorter interval, including a booking that has already started or ended, returns 409 with the title CancellationWindowClosed.

CancelBookingHandler reads the current UTC time from the injected TimeProvider and passes it to Booking.Cancel(nowUtc), where the rule is enforced. A rejected cancellation leaves the booking active, so it still occupies the room and counts toward the participant's daily limit.

A missing booking returns 404. Cancelling an already cancelled booking keeps the existing 409 BookingAlreadyCancelled response.

## Participant daily limit (R9)

A participant may hold at most two active bookings per UTC start date across all rooms. A third returns 409 with the title DailyBookingLimitReached.

Names are trimmed and compared case-sensitively. A booking spanning midnight counts only on its UTC start date. Cancellation releases the daily allowance. The handler queries the global count and applies ParticipantBookingPolicy inside a serializable SQLite transaction, so competing requests cannot bypass the limit.

The complete implementation plan, including the capacity-based duration limit, cancellation window, and R9, is in [IMPLEMENTATION-PLAN.md](IMPLEMENTATION-PLAN.md).

## Verify

```sh
dotnet build RoomHub.slnx --configuration Release
dotnet test RoomHub.slnx --configuration Release
dotnet format RoomHub.slnx --no-restore --verify-no-changes
```

Integration tests use real SQLite and apply the same migrations as application startup. They cover HTTP contracts, capacity-based duration limits, validation, cancellation boundaries using an adjustable TimeProvider, UTC boundaries, restart persistence, stale writes, and R9 across rooms and application instances.

To add a database schema change:

```sh
dotnet tool restore
dotnet ef migrations add DescribeChange --project src/RoomHub.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project src/RoomHub.Api
```

Review and commit the migration, designer, and model snapshot together. Ordinary application startup does not require the EF CLI.

## Submission

Read ASSIGNMENT.md for the full grading and submission rules. Complete DECISIONS.md, BUGS.md, and AI-LOG.md yourself in your own words. Those templates have intentionally been preserved. Submit a public repository link after reviewing the implementation.
