using FluentResults;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

internal static class EventResultMapper
{
    public static ErrorResponse ToError(ResultBase result)
    {
        var error = result.Errors.OfType<PokerEventError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        return new ErrorResponse(message);
    }

    public static bool IsConflict(ResultBase result)
    {
        var error = result.Errors.OfType<PokerEventError>().FirstOrDefault();

        return error?.Code == PokerEventErrorCode.EventCancelled;
    }
}
