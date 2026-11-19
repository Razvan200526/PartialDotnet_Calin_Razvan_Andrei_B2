# BUGS

Reviewed starter commit `a77f4cb`. Paths below are relative to `src/RoomHub.Api/`, and line numbers refer to the starter.

| Which rule from ASSIGNMENT.md is violated (R1–R8) | Where the problem was in the starter (file, approximate line) | How you discovered it (test, reading, AI) | What you changed |
|---|---|---|---|
| R5: bookings cannot start in the past. | `Services/BookingService.cs`, `Create`, around line 8: no check for past dates. | AI review and a test: the starter accepted a booking in the past. | `Booking.Validate` rejects past starts using the time from `TimeProvider` (HTTP 400). |
| R6: bookings cannot exceed four hours. | `Services/BookingService.cs`, `Create`, around line 8: no duration limit. | AI review and a test: a five-hour booking was accepted. | `Booking.Validate` rejects bookings over the limit. The later requirement lowers it to two hours for rooms with capacity above 10. |
| R7: bookings can touch without overlapping. | `Services/BookingService.cs`, line 28: the overlap check used `<=`. | AI review and tests: bookings right before or after another were rejected. | `Booking.Overlaps` uses `<` instead of `<=`, so touching intervals are allowed. |
| R7/R8: cancelled bookings should free the interval. | `Services/BookingService.cs`, around line 26, and `Repositories/BookingRepository.cs`, line 31: cancelled bookings still blocked the room. | AI review and a test: rebooking a cancelled interval still returned 409. | `Booking.Overlaps` only checks active bookings. |

The starter already handled R1–R4 and checked for a missing room first.

It also lost data on restart. EF Core and SQLite now handle persistence.
