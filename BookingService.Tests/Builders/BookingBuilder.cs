using BookingService.Entities;

namespace BookingService.Tests.Builders;

public sealed class BookingBuilder
{
    private long _userId = 1;
    private long _resourceId = 1;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateOnly? _bookedFrom;
    private DateOnly? _bookedTo;
    private Guid? _catalogRequestId = Guid.NewGuid();

    private BookingBuilder()
    {
    }

    public static BookingBuilder Create() => new();

    public BookingBuilder WithUserId(long userId)
    {
        _userId = userId;
        return this;
    }

    public BookingBuilder WithResourceId(long resourceId)
    {
        _resourceId = resourceId;
        return this;
    }

    public BookingBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public BookingBuilder WithDates(DateOnly bookedFrom, DateOnly bookedTo)
    {
        _bookedFrom = bookedFrom;
        _bookedTo = bookedTo;
        return this;
    }

    public BookingBuilder WithCatalogRequestId(Guid catalogRequestId)
    {
        _catalogRequestId = catalogRequestId;
        return this;
    }

    public BookingBuilder WithoutCatalogRequestId()
    {
        _catalogRequestId = null;
        return this;
    }

    public Booking Build()
    {
        var bookedFrom = _bookedFrom?? DateOnly.FromDateTime(_createdAt.AddDays(7).UtcDateTime);
        var bookedTo = _bookedTo ?? bookedFrom.AddDays(3);

        var booking = Booking.Create(
            _userId,
            _resourceId,
            bookedFrom,
            bookedTo,
            _createdAt);

        if (_catalogRequestId.HasValue)
            booking.SetCatalogRequestId(_catalogRequestId.Value);

        return booking;
    }
}
