using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.Id).ValueGeneratedNever();
        builder.Property(booking => booking.BookedBy).IsRequired();
        builder.Property(booking => booking.Status).HasConversion<string>().IsConcurrencyToken();
        builder.Property(booking => booking.StartUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.Property(booking => booking.EndUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasIndex(booking => booking.RoomId);
    }
}
