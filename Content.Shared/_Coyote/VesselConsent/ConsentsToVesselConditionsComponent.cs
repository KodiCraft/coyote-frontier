using Robust.Shared.GameStates;

namespace Content.Shared._Coyote.VesselConsent;

/// <summary>
/// Stores which vessel conditions this entity consents to
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsentsToVesselConditionsComponent : Component
{
    // Corresponds to the ID in VesselConditionsComponent
    [DataField, AutoNetworkedField]
    public HashSet<int> Consents = [];
}
