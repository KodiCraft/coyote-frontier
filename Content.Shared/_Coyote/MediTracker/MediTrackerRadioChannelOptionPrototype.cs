using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Coyote.MediTracker;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class MediTrackerRadioChannelOptionPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(PrototypeIdSerializer<MediTrackerRadioChannelOptionPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [DataField]
    public bool Abstract { get; } = false;

    /// <summary>
    /// The radio channel this is associated with
    /// </summary>
    [DataField("radioChannel")]
    [AlwaysPushInheritance]
    public ProtoId<RadioChannelPrototype> Channel = "Medical";

    /// <summary>
    /// Is it a Primary channel?
    /// </summary>
    [DataField("isPrimary")]
    [AlwaysPushInheritance]
    public bool IsPrimary = false;

    /// <summary>
    /// Is it a Fallback channel?
    /// </summary>
    [DataField("isFallback")]
    [AlwaysPushInheritance]
    public bool IsFallback = false;

    /// <summary>
    /// The message that the implant will send when crit
    /// </summary>
    [DataField("critMessage")]
    [AlwaysPushInheritance]
    public string CritMessage = "medi-tracker-crit-default";

    /// <summary>
    /// The message that the implant will send when dead
    /// </summary>
    [DataField("deathMessage")]
    [AlwaysPushInheritance]
    public string DeathMessage = "medi-tracker-death-default";

    /// <summary>
    /// The message that the implant will send when crit still
    /// </summary>
    [DataField("critRetryMessage")]
    [AlwaysPushInheritance]
    public string CritRetryMessage = "medi-tracker-crit-retry-default";

    /// <summary>
    /// The message that the implant will send when dead still
    /// </summary>
    [DataField("deathRetryMessage")]
    [AlwaysPushInheritance]
    public string DeathRetryMessage = "medi-tracker-death-retry-default";

    /// <summary>
    /// The message that the implant will send when revived
    /// </summary>
    [DataField("revivalMessage")]
    [AlwaysPushInheritance]
    public string RevivalMessage = "medi-tracker-revival-default";

    /// <summary>
    /// The message that the implant will send when they're no longer recoverable
    /// </summary>
    [DataField("nonRecoverableMessage")]
    [AlwaysPushInheritance]
    public string NonRecoverableMessage = "medi-tracker-non-recoverable-default";
}
