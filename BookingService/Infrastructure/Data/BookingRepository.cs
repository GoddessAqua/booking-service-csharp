using BookingService.Dto.Response;
using BookingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Data;

/// <summary>
/// Репозиторий для работы с бронированиями через EF Core
/// </summary>
public class BookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> FindByIdAsync(long id)
        => await _context.Bookings.FindAsync(id);

    public async Task<Booking?> FindByCatalogRequestIdAsync(Guid catalogRequestId)
        => await _context.Bookings.FirstOrDefaultAsync(b => b.CatalogRequestId == catalogRequestId);

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
        var query = _context.Bookings.AsQueryable();

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
        => await _context.Bookings
            .Where(b => b.Id == id)
            .Select(b => (BookingStatus?)b.Status)
            .FirstOrDefaultAsync();

    public async Task SaveAsync(Booking booking)
    {
        if (booking.Id == 0)
            _context.Bookings.Add(booking);

        await _context.SaveChangesAsync();
    }

    public Task ReloadAsync(Booking booking)
        => _context.Entry(booking).ReloadAsync();

    /// <summary>Получить количество бронирований по статусам и наиболее популярным ресурсам.</summary>
    public async Task<StatisticsResponse> GetStatisticsAsync(CancellationToken ct = default)
    {
        var totalBookings = await _context.Bookings
            .CountAsync(ct); 
        
        var statisticByStatus = await _context.Bookings
            .GroupBy(g => g.Status)
            .Select(s => new StatusCount
            {
                Status = s.Key,
                Count = s.Count(),
            })
            .ToListAsync(ct);
        
        var topResources = await _context.Bookings
            .GroupBy(g => g.ResourceId)
            .Select(s => new ResourceCount
            {
                ResourceId = s.Key,
                BookingCount = s.Count(),
            })
            .OrderByDescending(o => o.BookingCount)
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
        return await _context.Bookings
            .AsNoTracking()
            .Where(b 
                => b.Status == BookingStatus.CancellationPending && 
                   b.CancellationRequestedAt != null &&
                   b.CancellationRequestedAt <= cancellationRequestedBefore)
            .ToListAsync(ct);
    }
}
