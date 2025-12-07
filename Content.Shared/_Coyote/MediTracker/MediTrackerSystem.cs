using Content.Shared.Implants;
using Content.Shared.Mobs;

namespace Content.Shared._Coyote.MediTracker;

/// <summary>
/// This handles...
/// </summary>
public sealed class MediTrackerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MediTrackerComponent, ComponentInit>(OnMediTrackerInit);
        SubscribeLocalEvent<MediTrackerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MediTrackerComponent, ReTriggerRattleImplantEvent>(ForceSendCurrentMessage);
    }

    private void OnMobStateChanged(EntityUid uid, MediTrackerComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == component.LastMobState)
            return;

        var prevState = component.LastMobState;
        var newState = args.NewMobState;
        component.LastMobState = newState;

        // flow control for mob state changes
        // based on previous state and new state
        switch (prevState)
        {
            // Alive -> Critical, Trigger Crit state
            case MobState.Alive when newState is MobState.Critical:
                HandleMobBecameCritical(
                    uid,
                    component,
                    args);
                break;
            // Critical -> Alive, do nothing, its fine
            case MobState.Critical when newState is MobState.Alive:
                break;
            // Critical -> Dead, Trigger Died state
            // Alive -> Dead, Trigger Died state
            case MobState.Alive when newState is MobState.Dead:
            case MobState.Critical when newState is MobState.Dead:
                HandleMobDied(
                    uid,
                    component,
                    args);
                break;
            // Dead -> Alive, Trigger Revived state
            // Dead -> Critical, Trigger Revived state
            case MobState.Dead when newState is MobState.Alive:
            case MobState.Dead when newState is MobState.Critical:
                HandleMobRevived(
                    uid,
                    component,
                    args);
                break;
            // Default case, do nothing
            case MobState.Invalid:
            default:
                return;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MediTrackerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateMediTracker(
                uid,
                component,
                frameTime);
        }
    }

    /// <summary>
    /// Handles the passive update stuff for the meditracker
    /// If someones been dead for a while, resend the message
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="frameTime"></param>
    public void UpdateMediTracker(EntityUid uid, MediTrackerComponent component, float frameTime)
    {

    }

}
