using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public static class CreateEvent
{
    public static IEndpointRouteBuilder MapCreateEvent(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events", Handle)
            .WithName("CreateEvent")
            .WithTags("Events")
            .WithSummary("Create an event.");

        return app;
    }

    private static async Task<Results<Created<EventResponse>, BadRequest<ErrorResponse>>> Handle(
        Request request,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var result = PokerEvent.Create(groupContext.Id, request.Title, request.ScheduledAtUtc);

        if (result.IsFailed)
        {
            return TypedResults.BadRequest(EventResultMapper.ToError(result));
        }

        var pokerEvent = result.Value;

        dbContext.Events.Add(pokerEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/events/{pokerEvent.Id}",
            new EventResponse(
                pokerEvent.Id,
                pokerEvent.Title,
                pokerEvent.ScheduledAtUtc,
                pokerEvent.Status,
                pokerEvent.CreatedAtUtc,
                pokerEvent.CancelledAtUtc,
                GameId: null,
                GoingCount: 0,
                MaybeCount: 0,
                NotGoingCount: 0,
                MyRsvpStatus: null));
    }

    private sealed record Request(string? Title, DateTimeOffset ScheduledAtUtc);
}
