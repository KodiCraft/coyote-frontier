using Robust.Shared.Random;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This defines a discrete scent that can be detected.
/// </summary>
[DataDefinition]
public sealed partial class Scent(
    ScentPrototype scentProto,
    string scentGuid)
{
    /// <summary>
    /// The proto for this scent
    /// </summary>
    [DataField]
    public ScentPrototype ScentProto = scentProto;

    /// <summary>
    /// The unique-ish ID for this scent instance
    /// </summary>
    [DataField]
    public string ScentInstanceId = scentGuid;

    /// <summary>
    /// Rolled cooldown time for this scent instance
    /// In seconds
    /// </summary>
    [DataField]
    public TimeSpan CooldownTime = TimeSpan.Zero;

    public void RerollCooldownTime(IRobustRandom random)
    {
        // Roll a new cooldown time between min and max
        var minSeconds = ScentProto.MinCooldown;
        var maxSeconds = ScentProto.MaxCooldown;
        var rolledSeconds = random.NextDouble() * (maxSeconds - minSeconds) + minSeconds;
        CooldownTime = TimeSpan.FromSeconds((long) rolledSeconds); // important cast, it truncates the decimal and lets me use a long for once
    }
}
