namespace SecretSpots.Domain;

// Separate from User (1:1 by UserId, not its own Guid Id) so the wallet concern — and its xmin
// concurrency token — doesn't sit on the same row every unrelated User update (email, display
// name, ...) would otherwise also be guarded by.
public class CrystalWallet
{
    public Guid UserId { get; set; }
    public int Balance { get; set; }
}
