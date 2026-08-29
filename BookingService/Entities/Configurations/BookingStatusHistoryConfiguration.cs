using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Entities.Configurations;

public class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("booking_status_history");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(b => b.BookingId)
            .HasColumnName("booking_id")
            .IsRequired();
        
        builder.Property(b => b.StatusFrom)
            .HasColumnName("status_from")
            .HasConversion<int>()
            .IsRequired(false); 

        builder.Property(b => b.StatusTo)
            .HasConversion<int>()
            .HasColumnName("status_to")
            .IsRequired();

        builder.Property(b => b.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        
        builder.HasOne(b => b.Booking)
            .WithMany()
            .HasForeignKey(b => b.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new {x.BookingId, x.ChangedAt})
            .HasDatabaseName("idx_booking_status_history_booking_id_changed_at");
    }
}
