namespace SecretSpots.Features.Assistant;

// SpotIds: the spots the answer recommends, in the order mentioned, each checked to be a visible
// Approved spot — the frontend renders them as cards from its own data, never from model output.
public record AssistantAnswerResponse(string Answer, IReadOnlyList<Guid> SpotIds);
