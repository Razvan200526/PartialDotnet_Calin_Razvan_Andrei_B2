# DECISIONS

Answer **yourself**, in your own words, in 3–5 sentences per question.
Refer to concrete files and classes from your own repo (for example `Features/Bookings/CreateBooking/...`).
An unanswered question gives 0 points.

## 1. A rule you moved
Pick one business rule (R1–R8). In which file did it live in the starter and in which file does it live now? Why there?

_(your answer here)_

## 2. Result instead of an exception
Why is `Result` a better choice than an exception for "the room is occupied"? What happens in your code when an `Error` is returned?

Result pattern is a better choice since it is an expeceted failure rather than a problem in the server. Exceptions should be thrown only when somthing really goes wrong and handled by use case with different methods (retries etc...)

## 3. Who knows about EF Core?
Which classes in your project know about EF Core and which do not? What do you gain and what do you pay with Repository + Unit of Work?

The repository classes. Only then know about EF Core since we want to keep the responsabilities separate from the domain classes or other ones that have nothing to do with the database. Also AppDbContext, since it literally holds the database context. The repository patterns is used to hide the implementation of actual operations on the database from the service classes.

Fake-based tests also cannot verify SQL translation, database constraints, or real transaction behavior, so integration tests remain necessary.


## 4. The flow of a request
Draw in text (ASCII arrows) the path of a `POST /api/rooms/{roomId}/bookings` request through your project, with the **real** names of files and classes, from the endpoint to the database.

`POST /api/rooms/{roomId}/bookings -> RequestValidationFilter.cs -> CreateBookingRequestValidator.cs -> CreateBookingEndpoint.cs -> CreateBooingCommand.cs -> CreateBookingHandler.cs -> EfRoomReposiory.cs ->  Room.Book() -> EfBookingRepository.Add() -> EfUnitOfWork.SaveChangesAsync() -> AppDbContext.SaveChangesAsync() -> SQLite (roomhub.db)`


## 5. What you rejected from what the AI gave you
Give a concrete example of code or an idea proposed by the AI that you corrected or rejected, and say why.

For using the repository pattern, the AI wanted to create the IRepository pattern simply and use it to implement all of the repositories themselves. That was not a good idea since we want one interface for each repo so we maintain structure and separation of concerns

## 6. If you had 30 more minutes
What would you improve and what risk did you leave in the project?

I would check again for thread conccurency and more in depth pattern checking. As for the functionality, i m pretty happy with it
