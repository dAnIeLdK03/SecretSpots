namespace SecretSpots.Domain;

// How many questions a user asked the spot assistant on one (UTC) day — enforces the per-user and
// app-wide daily limits. Kept in the database so the limits survive restarts and scale-out.
public class AssistantUsage
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public int QuestionCount { get; set; }
}
