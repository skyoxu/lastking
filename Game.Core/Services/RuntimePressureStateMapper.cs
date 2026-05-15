namespace Game.Core.Services;

public sealed class RuntimePressureStateMapper
{
    public string MapCastleHp(int hp)
    {
        if (hp <= 20)
        {
            return "critical";
        }

        if (hp <= 40)
        {
            return "danger";
        }

        if (hp <= 60)
        {
            return "warning";
        }

        return "stable";
    }
}
