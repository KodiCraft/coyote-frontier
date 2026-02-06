using Robust.Shared.Serialization;

namespace Content.Shared._Coyote.VesselConsent;

/// <summary>
/// Indicates that the vessel the current player is on has changed its overall consent value.
/// </summary>
/// <param name="newValue">True if everyone aboard the vessel consents to its conditions, false otherwise. Null if
/// the player is not aboard a vessel where the consent system is applicable, such as if they are on a public
/// vessel or in space.</param>
[Serializable, NetSerializable]
public sealed class CurrentVesselConsentChangeEvent(bool? newValue) : EntityEventArgs
{
    public readonly bool? NewValue = newValue;
}
