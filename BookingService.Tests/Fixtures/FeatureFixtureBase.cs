using BookingService.Entities;
using BookingService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Tests.Fixtures;

public abstract class FeatureFixtureBase : IntegrationTestBase
{
    private readonly List<BookingDbContext> _scenarioContexts = [];

	    protected Booking Booking { get; set; } = null!;

    protected BookingDbContext CreateScenarioDbContext()
    {
        var context = CreateDbContext();
        _scenarioContexts.Add(context);
        return context;
    }

    protected async Task Given_the_database_is_migrated()
    {
        var canConnect = await Context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        var pendingMigrations = await Context.Database
            .GetPendingMigrationsAsync();

        pendingMigrations.Should().BeEmpty();
    }

    protected async Task SaveBookingAsync()
    {
        await using var seedContext = CreateDbContext();
        seedContext.Bookings.Add(Booking);
        await seedContext.SaveChangesAsync();
    }

    public override async Task DisposeAsync()
    {
        foreach (var context in _scenarioContexts)
        {
            await context.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
