namespace PokerBank.Api.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body);
