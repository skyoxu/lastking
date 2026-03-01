using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Dispatches user-facing feedback payloads from core to presentation adapters.
/// </summary>
public interface IFeedbackDispatcher
{
    /// <summary>
    /// Publish one feedback payload.
    /// </summary>
    /// <param name="feedback">Feedback payload.</param>
    /// <returns>The payload accepted for dispatch.</returns>
    UiFeedbackDto Publish(UiFeedbackDto feedback);
}
