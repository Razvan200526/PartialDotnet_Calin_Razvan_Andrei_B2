# RoomHub implementation plan

## Goal and agreed conventions

Refactor the .NET 10 starter in the style of InternHub: five vertical slices, rich domain objects, direct CQRS handler injection, EF Core SQLite repositories, and an explicit Unit of Work. Preserve all assignment routes and response shapes.

Use extensions for application setup, feature registration, route mapping, request validation, and database initialization. Keep each class, record, enum, or interface in its own file. Use a focused repository with unit and integration tests.

## Domain and request flow

- Keep domain entities, errors, results, policies, repository contracts, and Unit of Work contracts independent of ASP.NET Core and EF Core.
- Implement CreateRoom, GetRoom, CreateBooking, GetBooking, and CancelBooking in separate feature folders.
- Use ICommandHandler for writes and IQueryHandler for reads, with asynchronous Result responses and cancellation tokens.
- Keep endpoints responsible for binding, dispatch, and response mapping. Use route GUID constraints and request-validation filters that reuse domain validation.
- Preserve missing-room 404 precedence before booking business validation.
- Enforce R1 in Room, R2–R6 in Booking, R7 through Room's private schedule, and R8 through Booking.Cancel.
- Supply the current instant through injected TimeProvider and preserve UTC timestamps through database reads.
- Map validation, missing resources, and conflicts to ProblemDetails with 400, 404, and 409.

## Capacity-dependent booking duration

- Extend Booking.Validate to choose a maximum of two hours when the room capacity exceeds 10, otherwise four hours as in R6.
- Accept durations exactly equal to the applicable maximum. Return the existing BookingTooLong validation error with the correct limit in its message for longer durations.
- Reuse the capacity already supplied by Room, CreateBookingRequestValidator, and CreateBookingHandler; keep the rule in the domain.
- Apply the rule to newly created bookings, with no schema migration or changes to existing bookings.
- Test capacities below, at, and above 10; exact limits and one tick beyond; a large room with one attendee; direct handler validation before the daily quota; missing-room 404 precedence; and 400 responses without writes through both route styles.
- Update README and HTTP examples, run the tests and formatting checks, and record the change in a separate commit.

## API versioning and persistence

- Expose explicit v1 routes under /api/v1 while retaining /api as the assignment-compatible v1 aliases.
- Return Location headers matching the route style used by the caller.
- Use scoped repositories and a scoped AppDbContext. Stage changes in repositories and save through IUnitOfWork.
- Map private entity state and the private room booking collection. Use concurrency tokens for room schedule changes and cancellation.
- Resolve relative database paths against the application content root and apply checked-in migrations at startup.
- Keep databases and build output out of Git. Verify the migration matches the model.

## R9: participant limit across rooms

R9 is the additional requirement supplied after the original plan: a participant may have at most two active bookings with the same UTC start date, across all rooms.

- Add ParticipantBookingPolicy in the domain. Its maximum is two; an existing count of two or more produces the DailyBookingLimitReached conflict.
- Extend IBookingRepository with CountActiveForParticipantOnDayAsync(bookedBy, utcDay, ct). EF filters by participant, Active status, and the UTC date of StartUtc; it does not restrict RoomId.
- Preserve the existing name normalization: trim bookedBy, then compare case-sensitively. Determine the date after converting request timestamps to UTC.
- In CreateBookingHandler, begin a serializable transaction, find the room, validate booking inputs, query the participant's count, and apply the domain policy before staging a booking.
- Save and commit only after all checks succeed. SQLite's immediate write reservation keeps the count and insertion atomic across application instances. Dispose unsuccessful transactions without committing.
- Cancellation already changes Status to Cancelled, so subsequent count queries automatically release that participant's allowance.
- Keep R9 out of endpoints and route validators. This extension uses the existing schema and requires no data backfill.

## Verification and delivery

- Test all five HTTP operations, exact payload fields, Location headers, UTC serialization, errors, and both route styles.
- Test R1–R8 boundaries, touching intervals, cancellation, missing-room precedence, failed writes, restart persistence, and stale writes against real SQLite.
- Test the capacity-dependent two-hour/four-hour duration limits at their exact boundaries in the domain and over HTTP.
- Test R9 for the third booking in one room and across different rooms; independent participants; trimmed names; cancelled bookings; different UTC days; midnight crossings; offset timestamps; persisted counts; and concurrent requests across application instances.
- Verify route validation stops invalid requests before handler dispatch.
- Run a Release build and tests, formatting checks, migration consistency checks, dependency auditing, and a live HTTP smoke check.
- Maintain at least three meaningful commits. Public repository publication and submission follow review.
- Update README and roomhub.http. Leave DECISIONS.md, BUGS.md, and AI-LOG.md for the student to complete personally, as required by the assignment.
