using Robust.Shared.Serialization;

namespace Content.Shared._Coyote.VesselConsent;

/// <summary>
/// This event is fired by the client when they want to change the vessel conditions of the vessel they are on.
/// </summary>
/// <param name="newDescription">The new conditions of the vessel</param>
[Serializable, NetSerializable]
public sealed class UpdateCurrentVesselConditionsEvent(string newDescription) : EntityEventArgs
{
    public readonly string NewDescription = newDescription;
}
