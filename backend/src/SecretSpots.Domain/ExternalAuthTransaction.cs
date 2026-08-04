namespace SecretSpots.Domain;

public enum ExternalAuthTransactionStatus
{
    Pending,
    Completed,
    Consumed,
}

public class ExternalAuthTransaction
{
    public required string Id { get; set; }
    public required ExternalAuthProvider Provider { get; set; }
    public ExternalAuthTransactionStatus Status { get; set; } = ExternalAuthTransactionStatus.Pending;
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }

}