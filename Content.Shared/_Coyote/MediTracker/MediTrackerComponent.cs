using Content.Shared.Mobs;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.MediTracker;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class MediTrackerComponent : Component
{
    /// <summary>
    /// The last recorded mob state
    /// </summary>
    [DataField]
    public MobState LastMobState = MobState.Alive;

    /// <summary>
    /// The list of possible radio channel options the message can be sent to
    /// </summary>
    [DataField("initialRadioChannelOptions")]
    public HashSet<MediTrackerRadioChannelOptionPrototype> InitOptionSet = new();

    /// <summary>
    /// The list of possible other radio channels the message can be sent to simultaneously
    /// </summary>
    [DataField]
    public HashSet<MediTrackerRadioChannelOption> RadioChannels = new();

    /// <summary>
    /// Use fallback at all?
    /// </summary>
    [DataField]
    public bool UseFallback = true;

    /// <summary>
    /// The number of retries attempted
    /// </summary>
    [DataField]
    public int RetryCount = 0;

    /// <summary>
    /// The number of retries after which to use the fallback channels
    /// </summary>
    [DataField]
    public int RetriesBeforeFallback = 3;

    /// <summary>
    /// How often to resend the message if no response
    /// </summary>
    [DataField]
    public TimeSpan RetryInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long to wait before switching to the fallback channel
    /// </summary>
    [DataField]
    public TimeSpan FallbackDelay = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Time of death, if applicable
    /// </summary>
    [DataField]
    public TimeSpan? TimeOfDeath = null;

    /// <summary>
    /// Time of crit, if applicable
    /// </summary>
    [DataField]
    public TimeSpan? TimeOfCrit = null;

    /// <summary>
    /// Last time a message was sent, if applicable
    /// </summary>
    [DataField]
    public TimeSpan? LastMessageTime = null;

    /// <summary>
    /// Requires a mind to operate
    /// Makes it so people who released the body from their ghost can't use it
    /// Cus like, respawns and stuff
    /// Can be set to false for special NPCs like cappy and clippy and clarpy and whatever
    /// </summary>
    [DataField]
    public bool RequireMind = true;

    /// <summary>
    /// Update cooldown timer
    /// </summary>
    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Last time an update was sent
    /// </summary>
    [DataField]
    public TimeSpan LastUpdateTime = TimeSpan.Zero;

}

/// <summary>
/// Radio Channel Option pro meta ultra for this channel selection
/// </summary>
[DataDefinition]
public sealed partial class MediTrackerRadioChannelOption
{
    /// <summary>
    /// The radio channel prototype ID
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = default!;

    /// <summary>
    /// This channel is enabled
    /// </summary>
    [DataField]
    public bool Enabled = default!;

    /// <summary>
    /// If this channel is part of the first group to try sending to
    /// </summary>
    [DataField]
    public bool IsPrimary = default!;

    /// <summary>
    /// If this channel is part of the fallback group to try sending to
    /// </summary>
    [DataField]
    public bool IsFallback = default!;

    /// <summary>
    /// The message that the implant will send when crit
    /// </summary>
    [DataField]
    public string CritMessage = default!;

    /// <summary>
    /// The message that the implant will send when dead
    /// </summary>
    [DataField]
    public string DeathMessage = default!;

    /// <summary>
    /// The message that the implant will send when crit still
    /// </summary>
    [DataField]
    public string CritRetryMessage = default!;

    /// <summary>
    /// The message that the implant will send when dead still
    /// </summary>
    [DataField]
    public string DeathRetryMessage = default!;

    /// <summary>
    /// The message that the implant will send when revived
    /// </summary>
    [DataField]
    public string RevivalMessage = default!;

    /// <summary>
    /// Message that the implant will send when no longer recoverable
    /// </summary>
    [DataField]
    public string NonRecoverableMessage = default!;

    public MediTrackerRadioChannelOption(ProtoId<MediTrackerRadioChannelOptionPrototype> protoId)
    {
        var proto = IoCManager.Resolve<IPrototypeManager>().Index(protoId);
        Channel = proto.Channel;
        Enabled = true;
        IsPrimary = proto.IsPrimary;
        IsFallback = proto.IsFallback;
        CritMessage = proto.CritMessage;
        DeathMessage = proto.DeathMessage;
        CritRetryMessage = proto.CritRetryMessage;
        DeathRetryMessage = proto.DeathRetryMessage;
        RevivalMessage = proto.RevivalMessage;
        NonRecoverableMessage = proto.NonRecoverableMessage;
    }
}
