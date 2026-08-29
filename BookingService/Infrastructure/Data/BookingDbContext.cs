using BookingService.Configuration;
using BookingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Data;

public class BookingDbContext(
    DbContextOptions<BookingDbContext> options,
    ICurrentDateTimeProvider dateTimeProvider) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        CollectStatusHistory();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        CollectStatusHistory();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void CollectStatusHistory()
    {
        ChangeTracker.DetectChanges();

        var pendingHistoryEntries = ChangeTracker
            .Entries<BookingStatusHistory>()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        foreach (var entry in pendingHistoryEntries)
        {
            entry.State = EntityState.Detached;
        }

        var result = ChangeTracker
            .Entries<Booking>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e =>
            {
                var status = e.Property(p => p.Status);

                return e.State switch
                {
                    EntityState.Added
                        => BookingStatusHistory.Create(e.Entity, null, status.CurrentValue, dateTimeProvider.UtcNow()),

                    EntityState.Modified when status.CurrentValue != status.OriginalValue && status.IsModified
                        => BookingStatusHistory.Create(
                            e.Entity,
                            status.OriginalValue,
                            status.CurrentValue,
                            dateTimeProvider.UtcNow()),

                    _ => null
                };
            })
            .OfType<BookingStatusHistory>()
            .ToList();
        
        Set<BookingStatusHistory>().AddRange(result);
    }
}
