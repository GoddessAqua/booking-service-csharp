namespace BookingService.Entities;

/// <summary>
/// Entity для хранения истории изменений статуса бронирования
/// </summary>
public class BookingStatusHistory
{
    public long Id { get; private set; }
    public long BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!; // навигационное свойство
    public BookingStatus? StatusFrom { get; private set; }
    public BookingStatus StatusTo { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }

    private BookingStatusHistory() { }

    public static BookingStatusHistory Create(
        Booking booking,
        BookingStatus? statusFrom,
        BookingStatus statusTo,
        DateTimeOffset changedAt)
        => new()
        {
            Booking = booking,
            StatusFrom = statusFrom,
            StatusTo = statusTo,
            ChangedAt = changedAt
        };
}
