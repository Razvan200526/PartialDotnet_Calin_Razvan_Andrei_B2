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
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var room = await rooms.GetByIdAsync(command.RoomId, ct);
        if (room is null)
            return Result<CreateBookingResponse>.Failure(Error.NotFound("RoomNotFound", "The room does not exist."));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var validation = Booking.Validate(command.BookedBy, command.StartUtc, command.EndUtc,
            command.Attendees, room.Capacity, now);
        if (!validation.IsSuccess)
            return Result<CreateBookingResponse>.Failure(validation.Error!);

        var activeCount = await bookings.CountActiveForParticipantOnDayAsync(
            command.BookedBy!.Trim(), DateOnly.FromDateTime(command.StartUtc), ct);
        var dailyLimit = ParticipantBookingPolicy.ValidateDailyLimit(activeCount);
        if (!dailyLimit.IsSuccess)
            return Result<CreateBookingResponse>.Failure(dailyLimit.Error!);

        var result = room.Book(command.BookedBy, command.StartUtc, command.EndUtc, command.Attendees, now);
        if (!result.IsSuccess)
            return Result<CreateBookingResponse>.Failure(result.Error!);

        bookings.Add(result.Value!);
        var saved = await unitOfWork.SaveChangesAsync(ct);
        if (!saved.IsSuccess)
            return Result<CreateBookingResponse>.Failure(saved.Error!);

        await transaction.CommitAsync(ct);
        return Result<CreateBookingResponse>.Success(CreateBookingResponse.From(result.Value!));
    }
}
