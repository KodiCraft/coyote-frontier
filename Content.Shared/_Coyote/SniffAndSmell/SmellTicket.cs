using Robust.Shared.Map;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This is a ticket for a pending smell.
/// </summary>
[DataDefinition]
public sealed partial class SmellTicket(
    EntityUid sourceEntity,
    Scent scent,
    MapCoordinates origin,
    TimeSpan createdTime,
    bool isLewd = false
)
{
    /// <summary>
    /// The scent prototype ID
    /// </summary>
    [DataField]
    public Scent Smell = scent;

    /// <summary>
    /// The entity that this smell came from
    /// </summary>
    [DataField]
    public EntityUid SourceEntity = sourceEntity;

    /// <summary>
    /// The prioroity of this smell ticket
    /// Higher priority tickets get processed first.
    /// Based on proximity and other factors.
    /// </summary>
    [DataField]
    public double Priority = 0.1;

    /// <summary>
    /// Map coords of where the ticket was made
    /// </summary>
    [DataField]
    public MapCoordinates OriginCoordinates = origin;

    /// <summary>
    /// Time when this ticket was created
    /// </summary>
    [DataField]
    public TimeSpan CreatedTime = createdTime;

    /// <summary>
    /// Is it lewd?
    /// </summary>
    [DataField]
    public bool IsLewd = isLewd;

    /// <summary>
    /// Require LOS?
    /// </summary>
    [DataField]
    public bool RequireLoS = true;

    /// <summary>
    /// Rolled random cooldown for this ticket
    /// </summary>
    [DataField]
    public TimeSpan CooldownTime = TimeSpan.Zero;

    /// <summary>
    /// Determines if this ticket's priority is higher than another ticket's priority.
    /// Used for sorting tickets.
    /// </summary>
    public int ComparePriority(SmellTicket other)
    {
        return other.Priority.CompareTo(Priority);
    }

    /// <summary>
    /// Gets the priority value for this ticket.
    /// </summary>
    public double GetPriority()
    {
        return Priority * Smell.ScentProto.PriorityMultiplier;
    }

    /// <summary>
    /// Sets the priority value for this ticket.
    /// </summary>
    public void SetPriority(double priority)
    {
        Priority = priority;
    }
}
