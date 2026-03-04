using Godot;
using Game.Godot.Adapters;
using System.Text.Json;

namespace Game.Godot.Scripts.UI;

public partial class HUD : Control
{
    private static readonly JsonDocumentOptions EventJsonOptions = new() { MaxDepth = 16 };

    private EventBusAdapter? _bus;
    private Label _score = default!;
    private Label _health = default!;

    public override void _Ready()
    {
        _score = GetNode<Label>("TopBar/HBox/ScoreLabel");
        _health = GetNode<Label>("TopBar/HBox/HealthLabel");

        _bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        if (_bus != null)
        {
            _bus.Connect(EventBusAdapter.SignalName.DomainEventEmitted, new Callable(this, nameof(OnDomainEventEmitted)));
        }
    }

    public override void _ExitTree()
    {
        if (_bus != null)
        {
            var callable = new Callable(this, nameof(OnDomainEventEmitted));
            if (_bus.IsConnected(EventBusAdapter.SignalName.DomainEventEmitted, callable))
            {
                _bus.Disconnect(EventBusAdapter.SignalName.DomainEventEmitted, callable);
            }
        }
    }

    private void OnDomainEventEmitted(string type, string source, string dataJson, string id, string specVersion, string dataContentType, string timestampIso)
    {
        if (source.Length == 0 || dataJson.Length > 2048)
        {
            return;
        }

        if (type == "core.score.updated" || type == "score.changed")
        {
            try
            {
                if (dataJson.Length > 2048)
                {
                    return;
                }

                using var doc = JsonDocument.Parse(dataJson, EventJsonOptions);
                int v = 0;
                if (doc.RootElement.TryGetProperty("value", out var val)) v = val.GetInt32();
                else if (doc.RootElement.TryGetProperty("score", out var sc)) v = sc.GetInt32();
                _score.Text = $"Score: {v}";
            }
            catch { }
        }
        else if (type == "core.health.updated" || type == "player.health.changed")
        {
            try
            {
                if (dataJson.Length > 2048)
                {
                    return;
                }

                using var doc = JsonDocument.Parse(dataJson, EventJsonOptions);
                int v = 0;
                if (doc.RootElement.TryGetProperty("value", out var val)) v = val.GetInt32();
                else if (doc.RootElement.TryGetProperty("health", out var hp)) v = hp.GetInt32();
                _health.Text = $"HP: {v}";
            }
            catch { }
        }
    }

    public void SetScore(int v) => _score.Text = $"Score: {v}";
    public void SetHealth(int v) => _health.Text = $"HP: {v}";
}
