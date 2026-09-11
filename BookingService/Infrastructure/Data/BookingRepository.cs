using System.Data;
using BookingService.Dto.Response;
using BookingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookingService.Infrastructure.Data;

/// <summary>
/// Репозиторий для работы с бронированиями через EF Core
/// </summary>
public class BookingRepository(BookingDbContext context)
{
    public async Task<Booking?> FindByIdAsync(long id)
        => await context.Bookings.FindAsync(id);

    public async Task<Booking?> FindByCatalogRequestIdAsync(Guid catalogRequestId)
        => await context.Bookings.FirstOrDefaultAsync(b => b.CatalogRequestId == catalogRequestId);

    /// <summary>
    /// Найти бронирования по опциональным фильтрам с пагинацией
    /// </summary>
    public async Task<List<Booking>> FindByFilterAsync(
        long? userId,
        long? resourceId,
        BookingStatus? status,
        int pageNumber,
        int pageSize)
    {
        var query = context.Bookings.AsQueryable();

        if (userId.HasValue)
            query = query.Where(b => b.UserId == userId.Value);

        if (resourceId.HasValue)
            query = query.Where(b => b.ResourceId == resourceId.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Получить только статус бронирования по ID
    /// </summary>
    public async Task<BookingStatus?> FindStatusByIdAsync(long id)
        => await context.Bookings
            .Where(b => b.Id == id)
            .Select(b => (BookingStatus?)b.Status)
            .FirstOrDefaultAsync();

    public async Task SaveAsync(Booking booking)
    {
        if (booking.Id == 0)
            context.Bookings.Add(booking);

        await context.SaveChangesAsync();
    }

    public Task ReloadAsync(Booking booking)
        => context.Entry(booking).ReloadAsync();

    /// <summary>Получить количество бронирований по статусам и наиболее популярным ресурсам.</summary>
    public async Task<StatisticsResponse> GetStatisticsAsync(CancellationToken ct = default)
    {
        var totalBookings = await context.Bookings
            .CountAsync(ct); 
        
        var statisticByStatus = await context.Bookings
            .GroupBy(b => b.Status)
            .Select(grp => new StatusCount
            {
                Status = grp.Key,
                Count = grp.Count(),
            })
            .ToListAsync(ct);
        
        var topResources = await context.Bookings
            .GroupBy(b => b.ResourceId)
            .Select(grp => new ResourceCount
            {
                ResourceId = grp.Key,
                BookingCount = grp.Count(),
            })
            .OrderByDescending(resource => resource.BookingCount)
            .Take(5)
            .ToListAsync(ct);
        
        var statistics = new StatisticsResponse
        {
            TotalCount = totalBookings,
            ByStatus = statisticByStatus,
            TopResources = topResources
        };

        return statistics;
    }

    /// <summary>Найти отмены, оставшиеся в CancellationPending дольше заданного срока.</summary>
    public async Task<List<Booking>> FindStuckCancellationsAsync(DateTimeOffset cancellationRequestedBefore, CancellationToken ct = default)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b 
                => b.Status == BookingStatus.CancellationPending && 
                   b.CancellationRequestedAt != null &&
                   b.CancellationRequestedAt <= cancellationRequestedBefore)
            .ToListAsync(ct);
    }
    
    /// <summary>
    /// Получить историю изменений статусов бронирования
    /// </summary>
    /// <returns></returns>
    public async Task<List<BookingStatusHistory>> GetBookingHistoryAsync(
        long bookingId,
        CancellationToken ct)
    {
        return await context.BookingStatusHistories
            .AsNoTracking()
            .Where(b => b.BookingId == bookingId)
            .OrderBy(x => x.ChangedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
    
    public async Task<bool> IsEventProcessedAsync(Guid eventId)
        => await context.ProcessedEvents.AnyAsync(b => b.EventId == eventId);

    public async Task<IDbContextTransaction> BeginTransactionAsync() 
        => await context.Database.BeginTransactionAsync();

    public async Task AddProcessedEventAsync(ProcessedEvent newProcessedEvent)
        => await context.ProcessedEvents.AddAsync(newProcessedEvent);

    public async Task SaveChangesAsync()
        => await context.SaveChangesAsync();

    public void DetachProcessedEvent(ProcessedEvent newProcessedEvent)
        => context.Entry(newProcessedEvent).State = EntityState.Detached;
}