using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Entities.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(b => b.BookedFrom)
            .HasColumnName("booked_from")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.BookedTo)
            .HasColumnName("booked_to")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.CatalogRequestId)
            .HasColumnName("catalog_request_id")
            .HasColumnType("uuid");
        
        builder.Property(b => b.CancellationRequestedAt)                                                                                                                                                                                   
            .HasColumnName("cancellation_requested_at")                                                                                                                                                                                   
            .HasColumnType("timestamp with time zone");

        builder.Property(b => b.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("idx_bookings_status");

        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("idx_bookings_user_id");

        builder.HasIndex(b => b.ResourceId)
            .HasDatabaseName("idx_bookings_resource_id");
        
        builder.HasIndex(b => b.CancellationRequestedAt)
            .HasDatabaseName("idx_bookings_cancellation_pending")
            .HasFilter("status = 4");
    }
}