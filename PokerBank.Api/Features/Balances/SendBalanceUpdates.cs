using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;
using PokerBank.Api.Email;

namespace PokerBank.Api.Features.Balances;

public static class SendBalanceUpdates
{
    public static IEndpointRouteBuilder MapSendBalanceUpdates(this IEndpointRouteBuilder app)
    {
        app.MapPost("/balances/updates/send", Handle)
            .WithName("SendBalanceUpdates")
            .WithTags("Balances")
            .WithSummary("Send balance update emails.");

        return app;
    }

    private static async Task<Ok<Response>> Handle(
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        IEmailSender emailSender,
        CancellationToken cancellationToken)
    {
        var balances = await BalanceQuery.ListAsync(
            dbContext,
            groupContext.Id,
            playerId: null,
            activeOnly: true,
            cancellationToken);

        var skipped = new List<SkippedPlayer>();
        var sent = 0;

        foreach (var balance in balances)
        {
            if (string.IsNullOrWhiteSpace(balance.EmailAddress))
            {
                skipped.Add(new SkippedPlayer(
                    balance.PlayerId,
                    balance.PlayerName,
                    "Missing email address"));
                continue;
            }

            await emailSender.SendAsync(
                new EmailMessage(
                    balance.EmailAddress,
                    "PokerBank balance update",
                    BuildBody(
                        balance.PlayerName,
                        balance.BalanceAmount,
                        balance.GameNetAmount,
                        balance.PaymentNetAmount)),
                cancellationToken);

            sent++;
        }

        return TypedResults.Ok(new Response(sent, skipped.ToArray()));
    }

    private static string BuildBody(
        string playerName,
        decimal balanceAmount,
        decimal gameNetAmount,
        decimal paymentNetAmount)
    {
        var balanceLine = balanceAmount switch
        {
            > 0 => $"You should receive {Money(balanceAmount)}.",
            < 0 => $"You owe {Money(Math.Abs(balanceAmount))}.",
            _ => "You are settled."
        };

        return $"""
            Hi {playerName},

            Current PokerBank balance: {balanceLine}

            Game net: {SignedMoney(gameNetAmount)}
            Payment net: {SignedMoney(paymentNetAmount)}
            """;
    }

    private static string Money(decimal amount) =>
        amount.ToString("$0.00", CultureInfo.InvariantCulture);

    private static string SignedMoney(decimal amount)
    {
        var sign = amount > 0 ? "+" : amount < 0 ? "-" : "";

        return $"{sign}{Money(Math.Abs(amount))}";
    }

    private sealed record Response(int Sent, SkippedPlayer[] Skipped);

    private sealed record SkippedPlayer(Guid PlayerId, string PlayerName, string Reason);
}
