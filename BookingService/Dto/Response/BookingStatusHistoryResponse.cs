using BookingService.Entities;

namespace BookingService.Dto.Response;

/// <summary>
/// DTO записи истории изменения статуса бронирования.
/// </summary>
public sealed record BookingStatusHistoryResponse(
    long BookingId,
    BookingStatus? StatusFrom,
    BookingStatus StatusTo,
    DateTimeOffset ChangedAt);
