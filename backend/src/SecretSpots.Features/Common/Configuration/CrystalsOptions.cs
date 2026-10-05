namespace SecretSpots.Features.Common.Configuration;

public class CrystalsOptions
{
    public int StartingBalance { get; set; } = 0;
    public int CheckInReward { get; set; } = 10;
    public int OwnerRewardMin { get; set; } = 1;
    public int OwnerRewardMax { get; set; } = 3;
}
