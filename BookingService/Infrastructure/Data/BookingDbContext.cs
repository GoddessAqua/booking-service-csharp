using BookingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var result = ChangeTracker
            .Entries<Booking>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e =>
            {
                var status = e.Property(p => p.Status);

                return e.State switch
                {
                    EntityState.Added
                        => BookingStatusHistory.Create(e.Entity, null, status.CurrentValue),

                    EntityState.Modified when status.CurrentValue != status.OriginalValue && status.IsModified
                        => BookingStatusHistory.Create(e.Entity.Id, status.OriginalValue, status.CurrentValue),

                    _ => null
                };
            })
            .OfType<BookingStatusHistory>()
            .ToList();
        
        Set<BookingStatusHistory>().AddRange(result);

        return await base.SaveChangesAsync(cancellationToken);
    }
}
