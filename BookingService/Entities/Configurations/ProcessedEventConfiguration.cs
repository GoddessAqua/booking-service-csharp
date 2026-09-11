using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Entities.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable(DbTableNames.ProcessedEvent);

        builder.HasKey(@event => @event.EventId);
        
        builder
            .Property(@event => @event.EventId)
            .HasColumnName("event_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(@event => @event.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}