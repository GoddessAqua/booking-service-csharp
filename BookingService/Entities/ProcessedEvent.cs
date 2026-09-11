namespace BookingService.Entities;

/// <summary>
/// Entity для обеспечения Idempotency Protection для событий, класс с инкапсулированной бизнес-логикой
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedEvent()
    {
    }

    public static ProcessedEvent Create(Guid eventId, DateTimeOffset processedAt)
    {
        return new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAt = processedAt
        };
    }
}