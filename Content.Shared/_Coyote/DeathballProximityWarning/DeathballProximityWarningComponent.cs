using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.DeathballProximityWarning;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DeathballProximityWarningComponent : Component
{
    /// <summary>
    /// Is it actually on?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isActive")]
    public bool IsActive = true;

    /// <summary>
    /// Last time the warning system checked for deathballs
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastCheckTime")]
    public TimeSpan LastCheckTime = TimeSpan.Zero;

    /// <summary>
    /// The sound played when the deathball is no longer detected
    /// </summary>
    [DataField("safeOnceMoreSound")]
    public SoundSpecifier SafeOnceMoreSound = new SoundPathSpecifier("/Audio/Effects/Deathball/safe.ogg");

    #region Detected Range
    /// <summary>
    /// Range at which the warning starts to trigger
    /// </summary>
    [DataField("detectedRange")]
    public float DetectedRange = 500f; // meters, radious

    /// <summary>
    /// Sound played when the deathball was detected earlier, but is now out of range
    /// </summary>
    [DataField("lostSound")]
    public SoundSpecifier LostSound = new SoundPathSpecifier("/Audio/Effects/Deathball/lost.ogg");

    /// <summary>
    /// Sound played when the deathball is detected within warning range,
    /// And the warning level has increased
    /// </summary>
    [DataField("detectedSoundUp")]
    public SoundSpecifier DetectedSoundUp = new SoundPathSpecifier("/Audio/Effects/Deathball/detected_up.ogg");

    /// <summary>
    /// Sound played when the deathball is detected within warning range,
    /// And the warning level has decreased
    /// </summary>
    [DataField("detectedSoundDown")]
    public SoundSpecifier DetectedSoundDown = new SoundPathSpecifier("/Audio/Effects/Deathball/detected_down.ogg");
    #endregion

    #region Close Range
    /// <summary>
    /// Range at which the deathball is considered "close"
    /// </summary>
    [DataField("closeRange")]
    public float CloseRange = 200f; // meters, radious

    /// <summary>
    /// Sound played when the deathball is detected within close range,
    /// And the warning level has increased
    /// </summary>
    [DataField("closeSoundUp")]
    public SoundSpecifier CloseSoundUp = new SoundPathSpecifier("/Audio/Effects/Deathball/close_up.ogg");

    /// <summary>
    /// Sound played when the deathball is detected within close range,
    /// And the warning level has decreased
    /// </summary>
    [DataField("closeSoundDown")]
    public SoundSpecifier CloseSoundDown = new SoundPathSpecifier("/Audio/Effects/Deathball/close_down.ogg");
    #endregion

    #region Panic Range
    /// <summary>
    /// Range at which the deathball is considered "panic"
    /// </summary>
    [DataField("panicRange")]
    public float PanicRange = 100f; // meters, radious

    /// <summary>
    /// Sound played when the deathball is detected within panic range,
    /// And the warning level has increased
    /// </summary>
    [DataField("panicSoundUp")]
    public SoundSpecifier PanicSoundUp = new SoundPathSpecifier("/Audio/Effects/Deathball/panic_up.ogg");

    /// <summary>
    /// Sound played when the deathball is detected within panic range,
    /// And the warning level has decreased
    /// </summary>
    [DataField("panicSoundDown")]
    public SoundSpecifier PanicSoundDown = new SoundPathSpecifier("/Audio/Effects/Deathball/panic_down.ogg");
    #endregion

    /// <summary>
    /// Supported deathball types
    /// </summary>
    [DataField("supportedTypes")]
    public List<DeathballType> SupportedTypes = new()
    {
        DeathballType.Singulo,
        DeathballType.Tesla,
        DeathballType.Other,
    };

    /// <summary>
    /// Current state of the warning system
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("warningState")]
    public DeathballProximityWarningState WarningState = DeathballProximityWarningState.Inactive;
}

/// <summary>
/// Different states of the deathball proximity warning system
/// </summary>
public enum DeathballProximityWarningState : byte
{
    Inactive = 0,
    Safe     = 1,
    Detected = 2,
    Close    = 3,
    Panic    = 4,
}

public enum DeathballType
{
    Singulo,
    Tesla,
    Other,
}
