namespace Game.Core.Services.Reward;

public sealed class RewardState
{
    public int Gold { get; set; }

    public int Tech { get; set; }

    public int Units { get; set; }

    public RewardState Clone()
    {
        return new RewardState
        {
            Gold = Gold,
            Tech = Tech,
            Units = Units,
        };
    }
}
