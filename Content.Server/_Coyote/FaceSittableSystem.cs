namespace Content.Server._Coyote;

/// <summary>
/// This handles people sitting on faces.
/// 100% a kink thing
///
/// What it does:
/// Knocks down the target
/// Stuns them for the duration
/// Blinds them
/// buckles the sitter to the sittee
///
/// Does this by an update loop
/// - checks if the sitter is within range of the sittee
/// - reapplies effects if needed
/// - removes the component if the sitter leaves
/// - if it is interrupted,
/// </summary>
public sealed class FaceSittableSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }
}
