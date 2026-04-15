namespace Game.Core.Services;

public sealed class CameraMovementRuntime
{
    private readonly CameraMovementConfig _config;

    public CameraMovementRuntime(CameraMovementConfig config)
    {
        _config = config;
    }

    public CameraMovementState Step(CameraMovementState state, CameraMovementInput input, float deltaSeconds = 1f)
    {
        if (state.IsLocked)
        {
            return state;
        }
        var deltaScale = deltaSeconds <= 0f ? 0f : deltaSeconds;

        var deltaX = 0f;
        var deltaY = 0f;

        if (input.KeyboardDirectionX != 0)
        {
            deltaX += input.KeyboardDirectionX * _config.ScrollSpeed * deltaScale;
        }

        if (input.KeyboardDirectionY != 0)
        {
            deltaY += input.KeyboardDirectionY * _config.ScrollSpeed * deltaScale;
        }

        if (input.PointerX <= _config.EdgeMargin)
        {
            deltaX -= _config.ScrollSpeed * deltaScale;
        }
        else if (input.PointerX >= input.ViewportWidth - _config.EdgeMargin)
        {
            deltaX += _config.ScrollSpeed * deltaScale;
        }

        if (input.PointerY <= _config.EdgeMargin)
        {
            deltaY -= _config.ScrollSpeed * deltaScale;
        }
        else if (input.PointerY >= input.ViewportHeight - _config.EdgeMargin)
        {
            deltaY += _config.ScrollSpeed * deltaScale;
        }

        var nextX = Clamp(state.X + deltaX, _config.MinX, _config.MaxX);
        var nextY = Clamp(state.Y + deltaY, _config.MinY, _config.MaxY);

        return state with { X = nextX, Y = nextY };
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}

public readonly record struct CameraMovementState(float X, float Y, bool IsLocked);

public readonly record struct CameraMovementConfig(
    float ScrollSpeed,
    float EdgeMargin,
    float MinX,
    float MaxX,
    float MinY,
    float MaxY);

public readonly record struct CameraMovementInput(
    float PointerX,
    float PointerY,
    float ViewportWidth,
    float ViewportHeight,
    int KeyboardDirectionX,
    int KeyboardDirectionY)
{
    public static CameraMovementInput CreateCentered(float viewportWidth, float viewportHeight)
    {
        return new CameraMovementInput(
            PointerX: viewportWidth / 2f,
            PointerY: viewportHeight / 2f,
            ViewportWidth: viewportWidth,
            ViewportHeight: viewportHeight,
            KeyboardDirectionX: 0,
            KeyboardDirectionY: 0);
    }
}
