using Robust.Shared.Serialization;

namespace Content.Shared._Coyote.VesselConsent;

/// <summary>
/// Fired by the client when they consent or revoke consent to a given set of conditions
/// </summary>
/// <param name="newConsentState">True if the player is now consenting, false if they are no longer consenting</param>
/// <param name="conditionsId">The ID of the relevant conditions</param>
[Serializable, NetSerializable]
public sealed class UpdateOwnVesselConsentEvent(bool newConsentState, int conditionsId) : EntityEventArgs
{
    public readonly bool NewConsentState = newConsentState;
    public readonly int ConditionsId = conditionsId;
}
