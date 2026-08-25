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

    // TODO: Task 02 — реализовать агрегирующий SQL-запрос статистики
    public async Task<StatisticsResponse> GetStatisticsAsync()
    {
        var totalBookings = await _context.Bookings
            .AsQueryable()
            .CountAsync(); 
        
        var statisticByStatus = await _context.Bookings
            .GroupBy(g => g.Status)
            .Select(s => new StatusCount
            {
                Status = s.Key,
                Count = s.Count(),
            })
            .AsQueryable()
            .ToListAsync();
        
        var topResources = await _context.Bookings
            .GroupBy(g => g.ResourceId)
            .Select(s => new ResourceCount
            {
                ResourceId = s.Key,
                BookingCount = s.Count(),
            })
            .OrderByDescending(o => o.BookingCount)
            .Take(5)
            .AsQueryable()
            .ToListAsync();
        
        var statistics = new StatisticsResponse
        {
            TotalCount = totalBookings,
            ByStatus = statisticByStatus,
            TopResources = topResources
        };

        return statistics;
    }

    // TODO: Task 03 — найти бронирования, застрявшие в CancellationPending
    public Task<List<Booking>> FindStuckCancellationsAsync(DateTimeOffset cancellationRequestedBefore)
        => throw new NotImplementedException();
}
