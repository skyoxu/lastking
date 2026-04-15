using Game.Core.Services;
using Godot;

namespace Game.Godot.Scripts.Camera;

public partial class CameraController : Node
{
    [Signal]
    public delegate void ScrollRequestedEventHandler(Vector2 delta);

    [Signal]
    public delegate void ModeChangedEventHandler(string mode);

    [Export]
    public NodePath CameraPath { get; set; } = new(string.Empty);

    [Export]
    public float ScrollSpeed { get; set; } = 500f;

    [Export]
    public float EdgeMargin { get; set; } = 20f;

    [Export]
    public bool Locked { get; set; }

    [Export]
    public bool Paused { get; set; }

    [Export]
    public bool ProcessInputInReady { get; set; } = true;

    private Camera2D? _camera;
    private CameraMovementRuntime _runtime = new(new CameraMovementConfig(500f, 20f, float.MinValue, float.MaxValue, float.MinValue, float.MaxValue));
    private string _lastMode = "idle";
    private bool _manualKeyboardOverrideEnabled;
    private Vector2 _manualKeyboardAxis = Vector2.Zero;
    private bool _manualMouseOverrideEnabled;
    private Vector2 _manualMousePosition = Vector2.Zero;
    private Vector2 _lastAppliedDelta = Vector2.Zero;

    public override void _Ready()
    {
        ResolveCamera();
        RefreshRuntime();
        if (ProcessInputInReady && _camera is not null)
        {
            ApplyFromCurrentInput(0.016f);
        }
    }

    public override void _Process(double delta)
    {
        ApplyFromCurrentInput((float)delta);
    }

    public bool HasActiveCamera()
    {
        return _camera is not null;
    }

    public Vector2 GetCameraPosition()
    {
        return _camera?.GlobalPosition ?? Vector2.Zero;
    }

    public void SetLocked(bool value)
    {
        Locked = value;
        if (Locked)
        {
            EmitModeChanged("locked");
        }
    }

    public void SetPaused(bool value)
    {
        Paused = value;
    }

    public string CurrentMode()
    {
        return _lastMode;
    }

    public Vector2 LastAppliedDelta()
    {
        return _lastAppliedDelta;
    }

    public void SetManualKeyboardAxis(Vector2 axis)
    {
        _manualKeyboardOverrideEnabled = true;
        _manualKeyboardAxis = axis;
    }

    public void ClearManualKeyboardAxis()
    {
        _manualKeyboardOverrideEnabled = false;
        _manualKeyboardAxis = Vector2.Zero;
    }

    public void SetManualMousePosition(Vector2 mousePosition)
    {
        _manualMouseOverrideEnabled = true;
        _manualMousePosition = mousePosition;
    }

    public void ClearManualMousePosition()
    {
        _manualMouseOverrideEnabled = false;
        _manualMousePosition = Vector2.Zero;
    }

    public Vector2 PreviewStep(Vector2 keyboardAxis, Vector2 mousePosition, Vector2 viewportSize, float deltaSeconds)
    {
        ResolveCamera();
        if (_camera is null)
        {
            return Vector2.Zero;
        }

        RefreshRuntime();
        var input = BuildInput(keyboardAxis, mousePosition, viewportSize);
        var current = new CameraMovementState(_camera.GlobalPosition.X, _camera.GlobalPosition.Y, Locked || Paused);
        var next = _runtime.Step(current, input, deltaSeconds);
        return new Vector2(next.X, next.Y) - _camera.GlobalPosition;
    }

    public void ApplyManualStep(float deltaSeconds)
    {
        ApplyFromCurrentInput(deltaSeconds);
    }

    private void ApplyFromCurrentInput(float deltaSeconds)
    {
        ResolveCamera();
        if (_camera is null)
        {
            return;
        }

        RefreshRuntime();
        var keyboardAxis = _manualKeyboardOverrideEnabled ? _manualKeyboardAxis : ReadKeyboardAxis();
        var viewport = GetViewport();
        var viewportSize = viewport is null ? new Vector2(1920f, 1080f) : viewport.GetVisibleRect().Size;
        var mousePosition = _manualMouseOverrideEnabled ? _manualMousePosition : GetViewport().GetMousePosition();
        var input = BuildInput(keyboardAxis, mousePosition, viewportSize);

        var current = new CameraMovementState(_camera.GlobalPosition.X, _camera.GlobalPosition.Y, Locked || Paused);
        var next = _runtime.Step(current, input, deltaSeconds);
        var nextPosition = new Vector2(next.X, next.Y);
        _lastAppliedDelta = nextPosition - _camera.GlobalPosition;
        if (_lastAppliedDelta != Vector2.Zero)
        {
            _camera.GlobalPosition = nextPosition;
            EmitSignal(SignalName.ScrollRequested, _lastAppliedDelta);
        }

        EmitModeChanged(DetermineMode(keyboardAxis, mousePosition, viewportSize));
    }

    private CameraMovementInput BuildInput(Vector2 keyboardAxis, Vector2 mousePosition, Vector2 viewportSize)
    {
        return new CameraMovementInput(
            PointerX: mousePosition.X,
            PointerY: mousePosition.Y,
            ViewportWidth: viewportSize.X,
            ViewportHeight: viewportSize.Y,
            KeyboardDirectionX: (int)Mathf.Clamp(Mathf.RoundToInt(keyboardAxis.X), -1, 1),
            KeyboardDirectionY: (int)Mathf.Clamp(Mathf.RoundToInt(keyboardAxis.Y), -1, 1));
    }

    private void ResolveCamera()
    {
        if (_camera is not null && IsInstanceValid(_camera))
        {
            return;
        }

        if (!CameraPath.IsEmpty)
        {
            _camera = GetNodeOrNull<Camera2D>(CameraPath);
        }

        _camera ??= GetNodeOrNull<Camera2D>("Camera2D");
        _camera ??= GetNodeOrNull<Camera2D>("../Camera2D");
    }

    private void RefreshRuntime()
    {
        var minX = _camera?.LimitLeft ?? int.MinValue;
        var maxX = _camera?.LimitRight ?? int.MaxValue;
        var minY = _camera?.LimitTop ?? int.MinValue;
        var maxY = _camera?.LimitBottom ?? int.MaxValue;
        _runtime = new CameraMovementRuntime(new CameraMovementConfig(ScrollSpeed, EdgeMargin, minX, maxX, minY, maxY));
    }

    private Vector2 ReadKeyboardAxis()
    {
        var axis = Vector2.Zero;
        if (Input.IsActionPressed("ui_right"))
        {
            axis.X += 1f;
        }
        if (Input.IsActionPressed("ui_left"))
        {
            axis.X -= 1f;
        }
        if (Input.IsActionPressed("ui_down"))
        {
            axis.Y += 1f;
        }
        if (Input.IsActionPressed("ui_up"))
        {
            axis.Y -= 1f;
        }
        return axis;
    }

    private string DetermineMode(Vector2 keyboardAxis, Vector2 mousePosition, Vector2 viewportSize)
    {
        if (Locked || Paused)
        {
            return "locked";
        }

        var hasKeyboard = keyboardAxis != Vector2.Zero;
        var hasEdge = IsWithinEdge(mousePosition, viewportSize);
        if (hasKeyboard && hasEdge)
        {
            return "combined";
        }
        if (hasKeyboard)
        {
            return "keyboard";
        }
        if (hasEdge)
        {
            return "edge";
        }
        return "idle";
    }

    private bool IsWithinEdge(Vector2 mousePosition, Vector2 viewportSize)
    {
        return mousePosition.X <= EdgeMargin
               || mousePosition.X >= viewportSize.X - EdgeMargin
               || mousePosition.Y <= EdgeMargin
               || mousePosition.Y >= viewportSize.Y - EdgeMargin;
    }

    private void EmitModeChanged(string mode)
    {
        if (_lastMode == mode)
        {
            return;
        }
        _lastMode = mode;
        EmitSignal(SignalName.ModeChanged, mode);
    }
}
