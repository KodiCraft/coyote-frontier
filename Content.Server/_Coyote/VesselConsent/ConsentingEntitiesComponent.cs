namespace Content.Server._Coyote.VesselConsent;

/// <summary>
/// This keeps track of who is aboard a vessel and whether they are consenting or not.
/// </summary>
[RegisterComponent]
public sealed partial class ConsentingEntitiesComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly HashSet<EntityUid> ConsentingEntities = [];
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly HashSet<EntityUid> NonConsentingEntities = [];

    public bool AllConsenting => NonConsentingEntities.Count == 0;
}
