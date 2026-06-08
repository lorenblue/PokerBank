using System.Net.Http.Json;

namespace PokerBank.Tests.TestSupport;

internal static class ApiTestClientExtensions
{
    public static async Task<PlayerResponse> CreatePlayerAsync(
        this HttpClient client,
        string name,
        string? emailAddress = null)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name, EmailAddress = emailAddress });
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<PlayerResponse>(response, "Create player response was empty.");
    }

    public static async Task ArchivePlayerAsync(this HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<InvitePlayerResponse> InvitePlayerAsync(this HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/invite", content: null);
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<InvitePlayerResponse>(response, "Invite player response was empty.");
    }

    public static async Task<EventResponse> CreateEventAsync(
        this HttpClient client,
        string title = "Friday poker",
        DateTimeOffset? scheduledAtUtc = null)
    {
        var response = await client.PostAsJsonAsync(
            "/events",
            new
            {
                Title = title,
                ScheduledAtUtc = scheduledAtUtc ?? DateTimeOffset.UtcNow.AddDays(7)
            });
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<EventResponse>(response, "Create event response was empty.");
    }

    public static async Task<EventResponse> UpdateEventAsync(
        this HttpClient client,
        Guid eventId,
        string title,
        DateTimeOffset scheduledAtUtc)
    {
        var response = await client.PutAsJsonAsync(
            $"/events/{eventId}",
            new { Title = title, ScheduledAtUtc = scheduledAtUtc });
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<EventResponse>(response, "Update event response was empty.");
    }

    public static async Task<EventResponse> CancelEventAsync(this HttpClient client, Guid eventId)
    {
        var response = await client.PostAsync($"/events/{eventId}/cancel", content: null);
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<EventResponse>(response, "Cancel event response was empty.");
    }

    public static async Task<EventRsvpResponse> SetEventRsvpAsync(
        this HttpClient client,
        Guid eventId,
        string status)
    {
        var response = await client.PostAsJsonAsync($"/events/{eventId}/rsvp", new { Status = status });
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<EventRsvpResponse>(response, "Set event RSVP response was empty.");
    }

    public static async Task<StartEventGameResponse> StartEventGameAsync(this HttpClient client, Guid eventId)
    {
        var response = await client.PostAsync($"/events/{eventId}/game", content: null);
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<StartEventGameResponse>(response, "Start event game response was empty.");
    }

    public static async Task<GameResponse> CreateGameAsync(this HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<GameResponse>(response, "Create game response was empty.");
    }

    public static async Task AddBuyInAsync(
        this HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/buy-ins",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    public static async Task AddCashOutAsync(
        this HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/cash-outs",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    public static async Task CloseGameAsync(this HttpClient client, Guid gameId)
    {
        var response = await client.PostAsync($"/games/{gameId}/close", content: null);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<PaymentResponse> RecordPaymentAsync(
        this HttpClient client,
        Guid playerId,
        decimal amount,
        string direction,
        string method = "ETransfer")
    {
        var response = await client.PostAsJsonAsync(
            PaymentUrl(playerId, direction),
            new { Amount = amount, Method = method });
        response.EnsureSuccessStatusCode();

        return await ReadRequiredAsync<PaymentResponse>(response, "Create payment response was empty.");
    }

    public static Task<PaymentResponse> RecordPaymentMadeByPlayerAsync(
        this HttpClient client,
        Guid playerId,
        decimal amount) =>
        client.RecordPaymentAsync(playerId, amount, "MadeByPlayer");

    public static Task<PaymentResponse> RecordPaymentReceivedByPlayerAsync(
        this HttpClient client,
        Guid playerId,
        decimal amount) =>
        client.RecordPaymentAsync(playerId, amount, "ReceivedByPlayer");

    private static string PaymentUrl(Guid playerId, string direction) =>
        direction switch
        {
            "MadeByPlayer" => $"/players/{playerId}/payments/made",
            "ReceivedByPlayer" => $"/players/{playerId}/payments/received",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown payment direction.")
        };

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, string message)
    {
        var value = await response.Content.ReadFromJsonAsync<T>();

        return value ?? throw new InvalidOperationException(message);
    }
}
