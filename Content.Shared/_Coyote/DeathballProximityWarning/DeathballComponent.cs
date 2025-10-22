namespace Content.Shared._Coyote.DeathballProximityWarning;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DeathballComponent : Component
{
    /// <summary>
    /// The kind of deathball this is
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deathballType")]
    public DeathballType DeathballType = DeathballType.Other;
}
