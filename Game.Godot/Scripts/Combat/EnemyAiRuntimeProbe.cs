using Godot;
using Godot.Collections;
using Game.Core.Services;
using GodotArray = Godot.Collections.Array;

namespace Lastking.Game.Godot.Scripts.Combat;

public partial class EnemyAiRuntimeProbe : EnemyAi
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    public new Dictionary ProbeNavigationPath(Vector2 origin, Vector2 destination)
    {
        return base.ProbeNavigationPath(origin, destination);
    }

    public new Dictionary SelectTargetWithNavigation(GodotArray candidates, Vector2 origin)
    {
        return base.SelectTargetWithNavigation(candidates, origin);
    }

    public new Dictionary SimulateBlockedMapFallbackWithNavigation(
        int enemyCount,
        int timeoutTicks,
        int attackReadyTick,
        Vector2 origin,
        Vector2 destination)
    {
        return base.SimulateBlockedMapFallbackWithNavigation(
            enemyCount,
            timeoutTicks,
            attackReadyTick,
            origin,
            destination);
    }
}
