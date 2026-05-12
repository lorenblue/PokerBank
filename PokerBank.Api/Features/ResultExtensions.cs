using FluentResults;
using PokerBank.Domain;

namespace PokerBank.Api.Features;

internal static class ResultExtensions
{
    public static IResult ToApiError(this ResultBase result)
    {
        var pokerGameError = result.Errors.OfType<PokerGameError>().FirstOrDefault();

        if (pokerGameError is not null)
        {
            return pokerGameError.Code switch
            {
                PokerGameErrorCode.InvalidAmount or PokerGameErrorCode.InvalidPlayerId =>
                    Results.BadRequest(new ErrorResponse(pokerGameError.Message)),
                PokerGameErrorCode.GameClosed or
                    PokerGameErrorCode.CashOutsExceedBuyIns or
                    PokerGameErrorCode.BuyInsMustEqualCashOuts =>
                    Results.Conflict(new ErrorResponse(pokerGameError.Message)),
                _ => Results.BadRequest(new ErrorResponse(pokerGameError.Message))
            };
        }

        return Results.BadRequest(new ErrorResponse(result.Errors[0].Message));
    }

    private sealed record ErrorResponse(string Error);
}
