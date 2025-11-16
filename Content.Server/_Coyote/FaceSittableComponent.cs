namespace Content.Server._Coyote;

/// <summary>
/// This is used for having people sit on my face.
/// 100% a kink thing
/// </summary>
[RegisterComponent]
public sealed partial class FaceSittingComponent : Component
{
    /// <summary>
    /// The entity that is sitting on my face.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Sitter;

    /// <summary>
    /// Effects have been applied.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool EffectsApplied = false;

    /// <summary>
    /// Time between effect reapplications.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EffectReapplicationInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time to reapply effects.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DateTime NextEffectReapplicationTime = DateTime.MinValue;
}

/// <summary>
/// Event raised when someone starts sitting on a face.
/// </summary>
public sealed class FaceSitEvent(EntityUid sitter) : EntityEventArgs
{
    public EntityUid Sitter = sitter;
}

