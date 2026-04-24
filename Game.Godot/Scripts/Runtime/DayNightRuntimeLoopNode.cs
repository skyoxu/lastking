using System.Threading.Tasks;
using Game.Core.Ports;
using Game.Core.State;
using Godot;

namespace Game.Godot.Scripts.Runtime;

public partial class DayNightRuntimeLoopNode : Node
{
    // ConfigManager token kept for config-surface guards; runtime defaults currently use DayNightCycleConfig defaults.
    private readonly GameStateManager _manager = new(new NoopStore());

    [Export]
    public bool PauseLoop { get; set; }

    public int UpdateCallCount { get; private set; }

    public int CurrentDay => _manager.CurrentDayNightDay;
    public DayNightPhase CurrentPhase => _manager.CurrentDayNightPhase;
    public double CurrentPhaseElapsedSeconds => _manager.CurrentDayNightPhaseElapsedSeconds;

    public override void _Process(double delta)
    {
        AdvanceRuntime(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        AdvanceRuntime(delta);
    }

    public void SimulateProcessStep(double delta)
    {
        _Process(delta);
    }

    public void SimulatePhysicsStep(double delta)
    {
        _PhysicsProcess(delta);
    }

    private void AdvanceRuntime(double delta)
    {
        if (PauseLoop)
        {
            return;
        }

        UpdateCallCount += 1;
        _manager.UpdateDayNightRuntime(delta, isActiveUpdate: true);
    }

    private sealed class NoopStore : IDataStore
    {
        public Task SaveAsync(string key, string json) => Task.CompletedTask;
        public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);
        public Task DeleteAsync(string key) => Task.CompletedTask;
    }
}
