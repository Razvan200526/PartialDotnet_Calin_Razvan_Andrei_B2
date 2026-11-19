using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings.CancelBooking;

public sealed class CancelBookingHandler(IBookingRepository bookings, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelBookingCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(CancelBookingCommand command, CancellationToken ct = default)
    {
        var booking = await bookings.GetByIdAsync(command.Id, ct);
        if (booking is null)
            return Result<Unit>.Failure(Error.NotFound("BookingNotFound", "The booking does not exist."));

        var result = booking.Cancel();
        if (!result.IsSuccess)
            return Result<Unit>.Failure(result.Error!);

        var saved = await unitOfWork.SaveChangesAsync(ct);
        return saved.IsSuccess ? Result<Unit>.Success(Unit.Value) : Result<Unit>.Failure(saved.Error!);
    }
}
