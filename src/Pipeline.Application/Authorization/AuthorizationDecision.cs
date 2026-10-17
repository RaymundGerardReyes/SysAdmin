namespace Pipeline.Application.Authorization;

public enum DecisionOutcome
{
    Allow,
    Deny,
    Challenge,
    PendingApproval
}

public sealed record AuthorizationDecision(
    DecisionOutcome Outcome,
    string ReasonCode,
    string? PathId,
    AuthorizationTicket? Ticket = null)
{
    public bool IsAllowed => Outcome == DecisionOutcome.Allow;

    public static AuthorizationDecision Allow(AuthorizationTicket ticket, string pathId = "AUTHZ_P10") =>
        new(DecisionOutcome.Allow, "OK", pathId, ticket);

    public static AuthorizationDecision Deny(string reasonCode, string pathId) =>
        new(DecisionOutcome.Deny, reasonCode, pathId, null);

    public static AuthorizationDecision Challenge(string reasonCode, string pathId) =>
        new(DecisionOutcome.Challenge, reasonCode, pathId, null);

    public static AuthorizationDecision PendingApproval(string reasonCode, string pathId) =>
        new(DecisionOutcome.PendingApproval, reasonCode, pathId, null);
}
