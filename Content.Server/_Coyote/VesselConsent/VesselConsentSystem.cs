using Content.Server.Administration.Logs;
using Content.Shared._COYOTE;
using Content.Shared._Coyote.VesselConsent;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;

namespace Content.Server._Coyote.VesselConsent;

/// <summary>
/// This handles updating the conditions on vessels and informing clients
/// when a vessel is fully consenting or not.
/// </summary>
public sealed class VesselConsentSystem : SharedVesselConsentSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VesselConditionsComponent, ComponentInit>(InitConditionsComponent);

        // We need to add the ConsentsToVesselConditionsComponent to all entities with a mind,
        // so we subscribe to this event for all entities with MindContainerComponent
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdd);
        SubscribeLocalEvent<ConsentsToVesselConditionsComponent, MindRemovedMessage>(OnMindRemove);

        SubscribeLocalEvent<ConsentsToVesselConditionsComponent, MoveEvent>(OnPlayerMove);

        SubscribeNetworkEvent<UpdateOwnVesselConsentEvent>(OnPlayerConsent);
        SubscribeNetworkEvent<UpdateCurrentVesselConditionsEvent>(OnVesselConditionsUpdate);
    }

    public void AddPlayer(Entity<VesselConditionsComponent, ConsentingEntitiesComponent> grid, Entity<ConsentsToVesselConditionsComponent> player)
    {
        var oldState = grid.Comp2.AllConsenting;
        _adminLog.Add(LogType.Consent, LogImpact.Medium, $"{ToPrettyString(player)} entered vessel {ToPrettyString(grid)}");
        if (player.Comp.Consents.Contains(grid.Comp1.Id) || grid.Comp1.Description.Length == 0)
        {
            grid.Comp2.ConsentingEntities.Add(player);
        }
        else
        {
            grid.Comp2.NonConsentingEntities.Add(player);
        }
        var newState = grid.Comp2.AllConsenting;
        if (oldState != newState)
        {
            _adminLog.Add(LogType.Consent, LogImpact.Medium, $"{ToPrettyString(grid)} changed consent state to {newState}");
            ForEachEntityAboard(grid.Swap(), ent => RaiseNetworkEvent(new CurrentVesselConsentChangeEvent(newState), ent));
        }
        else
        {
            // Let the newly entering user know the overall state
            RaiseNetworkEvent(new CurrentVesselConsentChangeEvent(newState), player);
        }
    }

    public void RemovePlayer(Entity<VesselConditionsComponent, ConsentingEntitiesComponent> grid,
        EntityUid player)
    {
        var oldState = grid.Comp2.AllConsenting;
        grid.Comp2.ConsentingEntities.Remove(player);
        grid.Comp2.NonConsentingEntities.Remove(player);
        var newState = grid.Comp2.AllConsenting;

        if (oldState != newState)
        {
            _adminLog.Add(LogType.Consent, LogImpact.Low, $"{ToPrettyString(grid)} changed consent state to {newState}");
            ForEachEntityAboard(grid.Swap(), ent => RaiseNetworkEvent(new CurrentVesselConsentChangeEvent(newState), ent));
        }
    }

    private void InitConditionsComponent(Entity<VesselConditionsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Id = Random.Shared.Next();
    }

    private void OnMindAdd(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        // Overwrites in case we didn't remove it after the entity went mindless
        AddComp(ent.Owner, new ConsentsToVesselConditionsComponent(), true);
        if (TryComp(ent.Owner, out ConsentsToVesselConditionsComponent? consents)
            && TryComp(ent.Owner, out TransformComponent? transformComp))
        {
            MaybeAddPlayer(new Entity<TransformComponent?, ConsentsToVesselConditionsComponent>(ent.Owner, transformComp, consents));
        }
    }

    private void OnMindRemove(Entity<ConsentsToVesselConditionsComponent> ent, ref MindRemovedMessage args)
    {
        if (TryComp(ent, out TransformComponent? transformComp))
        {
            MaybeRemovePlayer(ent, _transform.GetGrid(new Entity<TransformComponent?>(ent, transformComp)));
        }
        RemComp<ConsentsToVesselConditionsComponent>(ent);
    }

    private void OnPlayerMove(Entity<ConsentsToVesselConditionsComponent> ent, ref MoveEvent args)
    {
        // If the entity has moved to a new grid, they need to be removed from the previous
        // one and added to the new one.
        var oldGrid = _transform.GetGrid(args.OldPosition);
        var newGrid = _transform.GetGrid(args.NewPosition);
        if (oldGrid == newGrid)
            return;
        MaybeRemovePlayer(ent, oldGrid);
        MaybeAddPlayer(ent, newGrid);
    }

    private void OnPlayerConsent(UpdateOwnVesselConsentEvent ev, EntitySessionEventArgs args)
    {
        Log.Debug($"{ToPrettyString(args.SenderSession.AttachedEntity)} changed consent state to {ev.NewConsentState} wrt {ev.ConditionsId}");
        var mind = args.SenderSession.GetMind();
        if (mind == null)
            return;
        if (!TryComp(mind, out MindComponent? mindComp))
            return;
        if (mindComp.CurrentEntity is not { } entity)
            return;
        if (!TryComp(entity, out ConsentsToVesselConditionsComponent? consents))
            return;

        if (ev.NewConsentState)
        {
            consents.Consents.Add(ev.ConditionsId);
        }
        else
        {
            consents.Consents.Remove(ev.ConditionsId);
        }

        Dirty(entity, consents);

        // Refresh this player by removing and re-adding them to their current grid
        if (!TryComp(entity, out TransformComponent? transformComp))
            return;

        // TODO: Doing the remove-add thing might induce unnecessary events and updates
        MaybeRemovePlayer(entity, transformComp.GridUid);
        MaybeAddPlayer(new Entity<ConsentsToVesselConditionsComponent>(entity, consents), transformComp.GridUid);
    }

    private void OnVesselConditionsUpdate(UpdateCurrentVesselConditionsEvent ev, EntitySessionEventArgs args)
    {
        // Check that the player actually has permission to change the description of the vessel they're on
        var mind = args.SenderSession.GetMind();
        if (mind == null)
            return;
        if (!TryComp(mind, out MindComponent? mindComp))
            return;

        if (!PlayerOwnsCurrentVessel(mindComp.CurrentEntity))
            return;

        if (!TryComp(mindComp.CurrentEntity, out TransformComponent? trans))
            return;

        if (!TryComp(trans.GridUid, out VesselConditionsComponent? cond))
            return;

        if (trans.GridUid is not {} nonNullGrid)
            return;

        cond.Description = ev.NewDescription;
        cond.Id = Random.Shared.Next();
        Dirty(nonNullGrid, cond);
    }

    private void MaybeAddPlayer(Entity<TransformComponent?, ConsentsToVesselConditionsComponent> ent)
    {
        var grid = _transform.GetGrid(ent);
        MaybeAddPlayer(ent.Swap(), grid);
    }

    private void MaybeAddPlayer(Entity<ConsentsToVesselConditionsComponent> ent, EntityUid? grid)
    {
        if (!TryComp(grid, out VesselConditionsComponent? conditions))
        {
            // Let the player know they are not aboard a vessel where the consent system is applicable
            RaiseNetworkEvent(new CurrentVesselConsentChangeEvent(null), ent);
            return;
        }
        if (!TryComp(grid, out ConsentingEntitiesComponent? consentingEntities))
            return;
        if (grid is {} nonNullGrid)
        {
            AddPlayer(new Entity<VesselConditionsComponent, ConsentingEntitiesComponent>(nonNullGrid, conditions, consentingEntities),
                ent);
        }
    }

    private void MaybeRemovePlayer(EntityUid ent, EntityUid? grid)
    {
        if (!TryComp(grid, out VesselConditionsComponent? conditions))
            return;
        if (!TryComp(grid, out ConsentingEntitiesComponent? consentingEntities))
            return;
        if (grid is {} nonNullGrid)
        {
            RemovePlayer(new Entity<VesselConditionsComponent, ConsentingEntitiesComponent>(nonNullGrid, conditions, consentingEntities),
                ent);
        }
    }

    private static void ForEachEntityAboard(Entity<ConsentingEntitiesComponent> grid, Action<EntityUid> func)
    {
        foreach (var ent in grid.Comp.ConsentingEntities)
        {
            func(ent);
        }

        foreach (var ent in grid.Comp.NonConsentingEntities)
        {
            func(ent);
        }
    }
}
