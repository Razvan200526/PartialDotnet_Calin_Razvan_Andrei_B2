using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed class CreateBookingHandler(
    IRoomRepository rooms, IBookingRepository bookings, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<Result<CreateBookingResponse>> HandleAsync(CreateBookingCommand command, CancellationToken ct = default)
    {
        var room = await rooms.GetByIdAsync(command.RoomId, ct);
        if (room is null)
            return Result<CreateBookingResponse>.Failure(Error.NotFound("RoomNotFound", "The room does not exist."));

        var result = room.Book(command.BookedBy, command.StartUtc, command.EndUtc,
            command.Attendees, timeProvider.GetUtcNow().UtcDateTime);
        if (!result.IsSuccess)
            return Result<CreateBookingResponse>.Failure(result.Error!);

        bookings.Add(result.Value!);
        var saved = await unitOfWork.SaveChangesAsync(ct);
        return saved.IsSuccess
            ? Result<CreateBookingResponse>.Success(CreateBookingResponse.From(result.Value!))
            : Result<CreateBookingResponse>.Failure(saved.Error!);
    }
}
