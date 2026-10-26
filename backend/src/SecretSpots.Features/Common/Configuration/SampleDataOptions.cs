namespace SecretSpots.Features.Common.Configuration;

// Development only — SampleDataSeeder is never registered outside IsDevelopment().
public class SampleDataOptions
{
    public bool Enabled { get; set; } = true;

    // Shared by every sample account so they're easy to log into locally.
    public string Password { get; set; } = "Sample123!";
}
