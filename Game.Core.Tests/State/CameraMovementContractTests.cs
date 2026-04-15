using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.State;

public class CameraMovementContractTests
{
    private static readonly CameraMovementConfig DefaultConfig = new(
        ScrollSpeed: 10f,
        EdgeMargin: 24f,
        MinX: 0f,
        MaxX: 100f,
        MinY: 0f,
        MaxY: 100f);

    // ACC:T22.11
    [Fact]
    public void ShouldMoveByConfiguredScrollSpeed_WhenKeyboardScrollIsRequested()
    {
        var sut = CreateSut();
        var initialState = new CameraMovementState(10f, 10f, IsLocked: false);
        var input = CameraMovementInput.CreateCentered(200f, 200f) with { KeyboardDirectionX = 1 };

        var result = sut.Step(initialState, input);

        result.X.Should().Be(20f);
        result.Y.Should().Be(10f);
    }

    // ACC:T22.3
    [Fact]
    public void ShouldScaleMovementByDeltaSeconds_WhenKeyboardScrollIsRequested()
    {
        var sut = CreateSut();
        var initialState = new CameraMovementState(10f, 10f, IsLocked: false);
        var input = CameraMovementInput.CreateCentered(200f, 200f) with { KeyboardDirectionX = 1 };

        var resultAtDeltaHalf = sut.Step(initialState, input, 0.5f);
        var resultAtDeltaOne = sut.Step(initialState, input, 1.0f);

        resultAtDeltaHalf.X.Should().Be(15f);
        resultAtDeltaOne.X.Should().Be(20f);
        resultAtDeltaOne.X.Should().BeGreaterThan(resultAtDeltaHalf.X);
    }

    // ACC:T22.11
    [Theory]
    [InlineData(24f, -10f)]
    [InlineData(25f, 0f)]
    public void ShouldApplyEdgeScrollingOnlyAtOrInsideMargin_WhenPointerIsNearLeftEdge(float pointerX, float expectedDeltaX)
    {
        var sut = CreateSut();
        var initialState = new CameraMovementState(50f, 50f, IsLocked: false);
        var input = CameraMovementInput.CreateCentered(200f, 200f) with { PointerX = pointerX };

        var result = sut.Step(initialState, input);

        result.X.Should().Be(50f + expectedDeltaX);
        result.Y.Should().Be(50f);
    }

    // ACC:T22.11
    [Fact]
    public void ShouldClampPositionToConfiguredBounds_WhenMovementWouldExceedMapLimits()
    {
        var sut = CreateSut();
        var initialState = new CameraMovementState(96f, 50f, IsLocked: false);
        var input = CameraMovementInput.CreateCentered(200f, 200f) with { KeyboardDirectionX = 1 };

        var result = sut.Step(initialState, input);

        result.X.Should().Be(100f);
        result.Y.Should().Be(50f);
    }

    // ACC:T22.11
    [Fact]
    public void ShouldKeepPositionUnchanged_WhenMovementIsRequestedWhileCameraIsLocked()
    {
        var sut = CreateSut();
        var initialState = new CameraMovementState(40f, 70f, IsLocked: true);
        var input = CameraMovementInput.CreateCentered(200f, 200f) with
        {
            KeyboardDirectionX = 1,
            KeyboardDirectionY = -1
        };

        var result = sut.Step(initialState, input);

        result.Should().Be(initialState);
    }

    private static CameraMovementRuntime CreateSut(CameraMovementConfig? config = null)
    {
        return new CameraMovementRuntime(config ?? DefaultConfig);
    }
}
