using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Coyote.VesselConsent;

/// <summary>
/// Contains the current description of a vessel's conditions
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VesselConditionsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Description = "";

    /// <summary>
    /// Random number representing this description on its specific entity, re-randomized
    /// whenever the description changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Id;
}
