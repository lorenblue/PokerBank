using System.Net;
using System.Net.Http.Json;
using PokerBank.Domain;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Events;

[Collection(ApiTestCollection.Name)]
public sealed class EventsApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateEvent_ReturnsCreatedEvent()
    {
        using var client = factory.CreateHttpsClient();
        var scheduledAtUtc = DateTimeOffset.UtcNow.AddDays(3);

        var response = await client.PostAsJsonAsync(
            "/events",
            new { Title = " Friday poker ", ScheduledAtUtc = scheduledAtUtc });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var pokerEvent = await response.Content.ReadFromJsonAsync<EventResponse>();

        Assert.NotNull(pokerEvent);
        Assert.NotEqual(Guid.Empty, pokerEvent.Id);
        Assert.Equal("Friday poker", pokerEvent.Title);
        AssertCloseTo(scheduledAtUtc, pokerEvent.ScheduledAtUtc);
        Assert.Equal("Scheduled", pokerEvent.Status);
        Assert.Null(pokerEvent.CancelledAtUtc);
        Assert.Null(pokerEvent.GameId);
        Assert.Equal(0, pokerEvent.GoingCount);
        Assert.Equal(0, pokerEvent.MaybeCount);
        Assert.Equal(0, pokerEvent.NotGoingCount);
        Assert.Null(pokerEvent.MyRsvpStatus);
    }

    [Fact]
    public async Task CreateEvent_ReturnsBadRequest_WhenTitleIsMissing()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/events",
            new { Title = " ", ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(3) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListEvents_ReturnsEventsForCurrentGroup()
    {
        using var client = factory.CreateHttpsClient();
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        var laterEvent = await client.CreateEventAsync("Later", DateTimeOffset.UtcNow.AddDays(7));
        var earlierEvent = await client.CreateEventAsync("Earlier", DateTimeOffset.UtcNow.AddDays(2));
        await otherGroupClient.CreateEventAsync("Other group", DateTimeOffset.UtcNow.AddDays(1));

        var response = await client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<EventResponse[]>();

        Assert.NotNull(events);
        Assert.Collection(
            events,
            pokerEvent => Assert.Equal(earlierEvent.Id, pokerEvent.Id),
            pokerEvent => Assert.Equal(laterEvent.Id, pokerEvent.Id));
    }

    [Fact]
    public async Task GetEvent_ReturnsRsvpsAndCounts()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await ownerClient.CreatePlayerAsync("Lorenzo");
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();
        await memberClient.SetEventRsvpAsync(pokerEvent.Id, "Going");

        var response = await memberClient.GetAsync($"/events/{pokerEvent.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<EventDetailsResponse>();

        Assert.NotNull(details);
        Assert.Equal(pokerEvent.Id, details.Id);
        Assert.Equal(1, details.GoingCount);
        Assert.Equal(0, details.MaybeCount);
        Assert.Equal(0, details.NotGoingCount);
        Assert.Equal("Going", details.MyRsvpStatus);
        var rsvp = Assert.Single(details.Rsvps);
        Assert.Equal(lorenzo.Id, rsvp.PlayerId);
        Assert.Equal("Lorenzo", rsvp.PlayerName);
        Assert.Equal("Going", rsvp.Status);
    }

    [Fact]
    public async Task UpdateEvent_ChangesTitleAndScheduledTime()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync();
        var rescheduledAtUtc = DateTimeOffset.UtcNow.AddDays(14);

        var updated = await client.UpdateEventAsync(pokerEvent.Id, "Saturday poker", rescheduledAtUtc);

        Assert.Equal(pokerEvent.Id, updated.Id);
        Assert.Equal("Saturday poker", updated.Title);
        AssertCloseTo(rescheduledAtUtc, updated.ScheduledAtUtc);
        Assert.Equal("Scheduled", updated.Status);
    }

    [Fact]
    public async Task CancelEvent_CancelsEvent()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync();

        var cancelled = await client.CancelEventAsync(pokerEvent.Id);

        Assert.Equal(pokerEvent.Id, cancelled.Id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.NotNull(cancelled.CancelledAtUtc);
    }

    [Fact]
    public async Task SetMyEventRsvp_CreatesAndUpdatesRsvp()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var player = await ownerClient.CreatePlayerAsync("Lorenzo");
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.LinkDefaultAdminToPlayerAsync(player.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var going = await memberClient.SetEventRsvpAsync(pokerEvent.Id, "Going");
        var notGoing = await memberClient.SetEventRsvpAsync(pokerEvent.Id, "NotGoing");
        var eventsResponse = await memberClient.GetAsync("/events");

        Assert.Equal(player.Id, going.PlayerId);
        Assert.Equal("Lorenzo", going.PlayerName);
        Assert.Equal("Going", going.Status);
        Assert.Equal(player.Id, notGoing.PlayerId);
        Assert.Equal("NotGoing", notGoing.Status);

        var events = await eventsResponse.Content.ReadFromJsonAsync<EventResponse[]>();

        Assert.NotNull(events);
        var updatedEvent = Assert.Single(events);
        Assert.Equal(0, updatedEvent.GoingCount);
        Assert.Equal(0, updatedEvent.MaybeCount);
        Assert.Equal(1, updatedEvent.NotGoingCount);
        Assert.Equal("NotGoing", updatedEvent.MyRsvpStatus);
    }

    [Fact]
    public async Task SetMyEventRsvp_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        using var ownerClient = factory.CreateHttpsClient();
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.PostAsJsonAsync(
            $"/events/{pokerEvent.Id}/rsvp",
            new { Status = "Going" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetMyEventRsvp_ReturnsConflict_WhenEventIsCancelled()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var player = await ownerClient.CreatePlayerAsync("Lorenzo");
        var pokerEvent = await ownerClient.CreateEventAsync();
        await ownerClient.CancelEventAsync(pokerEvent.Id);

        await factory.LinkDefaultAdminToPlayerAsync(player.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.PostAsJsonAsync(
            $"/events/{pokerEvent.Id}/rsvp",
            new { Status = "Going" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_CreatesGameLinkedToEvent()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        var game = await client.StartEventGameAsync(pokerEvent.Id);
        var eventsResponse = await client.GetAsync("/events");

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(pokerEvent.Id, game.PokerEventId);
        Assert.Equal("Open", game.Status);

        var events = await eventsResponse.Content.ReadFromJsonAsync<EventResponse[]>();

        Assert.NotNull(events);
        var updatedEvent = Assert.Single(events);
        Assert.Equal(game.Id, updatedEvent.GameId);
    }

    [Fact]
    public async Task StartEventGame_ReturnsConflict_WhenEventHasNotStarted()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddDays(1));

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_ReturnsConflict_WhenEventIsCancelled()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync();
        await client.CancelEventAsync(pokerEvent.Id);

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_ReturnsConflict_WhenEventAlreadyHasGame()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        await client.StartEventGameAsync(pokerEvent.Id);

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_ReturnsConflict_WhenAnotherGameIsOpen()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        await client.CreateGameAsync();

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_ReturnsNotFound_WhenEventIsInDifferentPokerGroup()
    {
        using var client = factory.CreateHttpsClient();
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);
        var pokerEvent = await otherGroupClient.CreateEventAsync();

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ReturnsConflict_WhenEventHasLinkedGame()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        await client.StartEventGameAsync(pokerEvent.Id);

        var response = await client.PutAsJsonAsync(
            $"/events/{pokerEvent.Id}",
            new { Title = "Saturday poker", ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(3) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelEvent_ReturnsConflict_WhenEventHasLinkedGame()
    {
        using var client = factory.CreateHttpsClient();
        var pokerEvent = await client.CreateEventAsync(scheduledAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        await client.StartEventGameAsync(pokerEvent.Id);

        var response = await client.PostAsync($"/events/{pokerEvent.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/events",
            new { Title = "Friday poker", ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(3) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ReturnsForbidden_ForMember()
    {
        using var ownerClient = factory.CreateHttpsClient();
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.PutAsJsonAsync(
            $"/events/{pokerEvent.Id}",
            new { Title = "Saturday poker", ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(3) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEvent_ReturnsForbidden_ForMember()
    {
        using var ownerClient = factory.CreateHttpsClient();
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.PostAsync($"/events/{pokerEvent.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StartEventGame_ReturnsForbidden_ForMember()
    {
        using var ownerClient = factory.CreateHttpsClient();
        var pokerEvent = await ownerClient.CreateEventAsync();

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.PostAsync($"/events/{pokerEvent.Id}/game", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static void AssertCloseTo(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }
}
