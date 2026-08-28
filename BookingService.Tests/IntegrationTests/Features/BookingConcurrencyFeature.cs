using BookingService.Entities;
using BookingService.Configuration;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;
using BookingService.Tests.Builders;
using BookingService.Tests.Fixtures;
using FluentAssertions;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingService.Tests.IntegrationTests.Features;

[FeatureDescription("Для предотвращения потери изменений система использует xmin PostgreSQL как токен конкурентности.")]
public class BookingConcurrencyFeature : FeatureFixtureBase
{
    private BookingDbContext? _firstContext;
    private BookingDbContext? _secondContext;
    private Booking? _firstBooking;
    private Booking? _secondBooking;
    private Exception? _saveException;
    private Services.BookingService? _confirmationService;

    //Имитация ситуации гонки для проверки оптимистичной блокировки
    
    // EF/PostgreSQL обнаруживает конфликт через xmin
    [Scenario]
    [Label("TK-1-1")]
    public async Task Concurrent_update_is_detected()
    {
        await Runner.AddAsyncSteps(
                _ => Given_the_database_is_migrated(),
                _ => Given_a_booking(),
                _ => Given_two_independent_processes(),
                _ => When_two_processes_load_the_same_booking(),
                _ => Then_the_loaded_versions_should_match(),
                _ => When_the_first_process_updates_the_booking(),
                _ => Then_the_first_process_should_have_a_new_version(),
                _ => When_the_second_process_saves_its_outdated_booking(),
                _ => Then_a_concurrency_exception_should_be_thrown(),
                _ => Then_the_database_should_contain_the_first_process_change())
            .RunAsync();
    }

    // BookingService ловит конфликт и перезагружает сущность 
    [Scenario]
    [Label("TK-1-2")]
    public async Task Concurrent_confirmation_is_handled()
    {
        await Runner.AddAsyncSteps(
                _ => Given_the_database_is_migrated(),
                _ => Given_a_booking(),
                _ => Given_two_independent_processes(),
                _ => Given_the_confirmation_service_uses_the_second_process(),
                _ => When_two_processes_load_the_same_booking(),
                _ => Then_the_loaded_versions_should_match(),
                _ => When_the_first_process_updates_the_booking(),
                _ => Then_the_first_process_should_have_a_new_version(),
                _ => When_confirmation_is_processed_by_the_second_process(),
                _ => Then_the_second_process_should_reload_the_booking())
            .RunAsync();
    }

    #region given

    private async Task Given_a_booking()
    {
        const int daysFromNow = 7;
        const int durationDays = 3;

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromNow));
        var to = from.AddDays(durationDays);

        Booking = BookingBuilder.Create()
            .WithUserId(1)
            .WithResourceId(1)
            .WithCatalogRequestId(Guid.NewGuid())
            .WithCreatedAt(DateTimeOffset.UtcNow)
            .WithDates(from, to)
            .Build();

        await SaveBookingAsync();
    }

    private Task Given_two_independent_processes()
    {
        _firstContext = CreateScenarioDbContext();
        _secondContext = CreateScenarioDbContext();
        return Task.CompletedTask;
    }

    private Task Given_the_confirmation_service_uses_the_second_process()
    {
        var publisher = new BookingEventPublisher(
            BusMock,
            NullLogger<BookingEventPublisher>.Instance);

        _confirmationService = new Services.BookingService(
            new BookingRepository(_secondContext!),
            publisher,
            new CurrentDateTimeProvider(),
            NullLogger<Services.BookingService>.Instance);

        return Task.CompletedTask;
    }

    #endregion

    #region when

    private async Task When_two_processes_load_the_same_booking()
    {
        _firstBooking = await _firstContext!.Bookings
            .SingleAsync(b => b.Id == Booking.Id);

        _secondBooking = await _secondContext!.Bookings
            .SingleAsync(b => b.Id == Booking.Id);
    }

    private async Task When_the_first_process_updates_the_booking()
    {
        _firstBooking!.Confirm();
        await _firstContext!.SaveChangesAsync();
    }

    private async Task When_the_second_process_saves_its_outdated_booking()
    {
        _secondBooking!.Cancel(DateTimeOffset.UtcNow);
        _saveException = await CaptureSaveExceptionAsync(_secondContext!);
    }

    private async Task When_confirmation_is_processed_by_the_second_process()
    {
        await _confirmationService!
            .HandleBookingJobConfirmed(Booking.CatalogRequestId!.Value);
    }

    #endregion

    #region then

    private Task Then_the_loaded_versions_should_match()
    {
        _firstBooking!.Version.Should().Be(_secondBooking!.Version);
        return Task.CompletedTask;
    }

    private Task Then_the_first_process_should_have_a_new_version()
    {
        _firstBooking!.Version.Should().NotBe(_secondBooking!.Version);
        return Task.CompletedTask;
    }

    private Task Then_a_concurrency_exception_should_be_thrown()
    {
        _saveException.Should().BeOfType<DbUpdateConcurrencyException>();
        return Task.CompletedTask;
    }

    private Task Then_the_second_process_should_reload_the_booking()
    {
        _secondBooking!.Status.Should().Be(BookingStatus.Confirmed);
        _secondBooking.Version.Should().Be(_firstBooking!.Version);
        return Task.CompletedTask;
    }

    private async Task Then_the_database_should_contain_the_first_process_change()
    {
        await using var verificationContext = CreateDbContext();

        var actualBooking = await verificationContext.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == Booking.Id);

        actualBooking.Status.Should().Be(BookingStatus.Confirmed);
        actualBooking.Version.Should().Be(_firstBooking!.Version);
    }

    #endregion

    private static async Task<Exception?> CaptureSaveExceptionAsync(BookingDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

}
