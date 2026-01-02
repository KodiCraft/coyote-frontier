using Content.Shared.Consent;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExport
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 1;

    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = default!;

    /// <summary>
    /// Character-specific OOC consent freetext.
    /// </summary>
    [DataField]
    public string? CharacterConsentFreetext;

    /// <summary>
    /// Account-level OOC consent freetext.
    /// </summary>
    [DataField]
    public string? AccountConsentFreetext;

    /// <summary>
    /// OOC consent toggles.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ConsentTogglePrototype>, string>? ConsentToggles;
}
