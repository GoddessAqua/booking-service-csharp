using BookingService.Catalog.Async.Api.Contracts.Requests;
using BookingService.Configuration;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;

namespace BookingService.Infrastructure.BackgroundJobs;

/// <summary>
/// Фоновая задача для повторной отправки команд отмены
/// по бронированиям, зависшим в статусе CancellationPending.
/// </summary>
public sealed class StuckCancellationsJob( //Синглтон
    IServiceScopeFactory scopeFactory,
    ICurrentDateTimeProvider dateTimeProvider,
    ILogger<StuckCancellationsJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan ExecutionInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CancellationTimeout = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Запуск джобы {jobName}", nameof(StuckCancellationsJob));
        
        using var timer = new PeriodicTimer(ExecutionInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await TryProcessAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogInformation(                                                                                                                                                                                                        
                "Окончание работы джобы {JobName}",                                                                                                                                                                                       
                nameof(StuckCancellationsJob));
        }
        
        logger.LogInformation("Окончание работы джобы {JobName}", nameof(StuckCancellationsJob));
    }

    private async Task TryProcessAsync(CancellationToken ct)
    {
        try
        {
            await ProcessAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ошибка обработки зависших отмен");
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        
        //scoped-сервисы внутри синглтона через scopeFactory
        var bookingRepo = scope.ServiceProvider.GetRequiredService<BookingRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<BookingEventPublisher>();

        var cancellationRequestedBefore = dateTimeProvider.UtcNow() - CancellationTimeout;
        var filteredBookings = await bookingRepo.FindStuckCancellationsAsync(cancellationRequestedBefore, ct);

        foreach (var booking in filteredBookings)
        {
            if (booking.CatalogRequestId is null)
            {
                logger.LogWarning(
                    "Повторная отправка отмены пропущена: " +
                    "bookingId={BookingId}, CatalogRequestId отсутствует",
                    booking.Id);

                continue;
            }

            try
            {
                var command = new CancelBookingJobByRequestIdRequest
                {
                    EventId = Guid.NewGuid(),
                    RequestId = booking.CatalogRequestId.Value
                };

                await publisher.PublishCancelBookingJob(command);

                logger.LogInformation(
                    "Повторно отправлена команда отмены: " +
                    "bookingId={BookingId}, requestId={RequestId}",
                    booking.Id,
                    booking.CatalogRequestId);
            }
            catch (Exception exception) when (!ct.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Ошибка повторной отправки отмены: " +
                    "bookingId={BookingId}, requestId={RequestId}",
                    booking.Id,
                    booking.CatalogRequestId);
            }
        }
    }
}
