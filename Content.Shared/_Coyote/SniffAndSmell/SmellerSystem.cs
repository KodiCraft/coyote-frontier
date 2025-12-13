using System.Numerics;
using Content.Shared.Chat;
using Content.Shared.Consent;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This handles...
/// </summary>
public sealed class SmellerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem    _transform      = default!;
    [Dependency] private readonly SharedInteractionSystem  _interact       = default!;
    [Dependency] private readonly IGameTiming              _time           = default!;
    [Dependency] private readonly IRobustRandom            _rng            = default!;
    [Dependency] private readonly SharedPopupSystem        _popupSystem    = default!;
    [Dependency] private readonly SharedChatSystem         _chatsys        = default!;
    [Dependency] private readonly ISharedChatManager       _chatManager    = default!;
    [Dependency] private readonly ISharedChatManager       _playerManager  = default!;
    [Dependency] private readonly SharedConsentSystem      _consent        = default!;
    [Dependency] private readonly PrototypeManager         _proto          = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ScentComponent, ComponentStartup>(OnScentStartup);
        SubscribeLocalEvent<SmellerComponent, GetVerbsEvent<InteractionVerb>>(GetSmellVerbs);
    }

    private void OnScentStartup(EntityUid uid, ScentComponent component, ComponentStartup args)
    {
        // load scents from prototypes
        foreach (var scentProtoId in component.ScentPrototypesToAdd)
        {
            if (_proto.TryIndex(scentProtoId, out var scentProto))
            {
                var scentInstance = new Scent(scentProto, Guid.NewGuid().ToString());
                scentInstance.RerollCooldownTime(_rng);
                component.Scents.Add(scentInstance);
            }
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// Does two things:
    /// Gather surrounding scents to be processed later
    /// Process pending smells at set intervals
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SmellerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            DetectSmells(uid, component);
            ProcessPendingSmells(uid, component);
        }
    }

    private void DetectSmells(EntityUid uid, SmellerComponent component)
    {
        if (component.NextSmellDetectionTime > _time.CurTime)
            return;
        component.NextSmellDetectionTime = _time.CurTime + component.SmellDetectionInterval;

        List<(Scent, float)> potentialScents = new();
        var smellerPos = _transform.GetWorldPosition(uid);
        var query = EntityQueryEnumerator<ScentComponent>();
        while (query.MoveNext(out var scentUid, out var scentComp))
        {
            var scentEntityPos = _transform.GetWorldPosition(scentUid);
            var distance = Vector2.Distance(smellerPos, scentEntityPos);
            foreach (var scent in scentComp.Scents)
            {
                if (distance > scent.FarRange)
                    continue;
                if (scentComp.DisabledScentIds.Contains(scentProto.ID))
                    continue;
                var scentInstance = new Scent(scentProto, scentComp.ScentInstanceId);
                potentialScents.Add((scentInstance, distance));
            }
        }
        // filter out scents that are out of range / line of sight / on cooldown / failed RNG
        foreach (var scent in potentialScents)
        {
            double priority = 1.0;
            if (scent.Item2 < scent.Item1.ScentProto.CloseRange)
            {
                // close range scents are higher priority, scaled up by closeness
                // the highers the priority value, the higher priority it is
                // provides a 2.0 addition flat
                // then up to an additional 3.0 based on distance
                var distanceFactor = scent.Item1.ScentProto.CloseRange - scent.Item2;
                if (distanceFactor < 0.1f)
                    distanceFactor = 0.1f;
                priority += 2.0 + (3.0 * distanceFactor / scent.Item1.ScentProto.CloseRange);
            }
            else
            {
                // far range scents are lower priority, scaled up by closeness
                // provuidees an additional 2.0 based on distance
                var distanceFactor = scent.Item1.ScentProto.FarRange - scent.Item2;
                if (distanceFactor < 0.1f)
                    distanceFactor = 0.1f;
                priority += (2.0 * distanceFactor / scent.Item1.ScentProto.FarRange);
            }
            var newTicket = new SmellTicket(
                scent.Item1,
                uid,
                priority,
                _transform.GetWorldCoordinates(uid),
                requireLoS: true);
            if (CanSmellScent(
                    uid,
                    component,
                    newTicket,
                    out _,
                    true))
            {
                // add to pending smells if not already present
                if (!component.PendingSmells.Exists(x => x.Smell.ScentInstanceId == scent.ScentInstanceId))
                {
                    component.PendingSmells.Add(newTicket);
                }
            }
        }
        // then update existing tickets to reflect new positions / sources
        foreach (var ticket in component.PendingSmells)
        {
            if (ticket.SourceEntity == uid)
                continue; // dont update self
            if (!_proto.TryIndex<ScentPrototype>(ticket.Smell.ScentProto.ID, out var scentProto))
                continue;
            var scentEntityPos = _transform.GetWorldPosition(ticket.SourceEntity);
            var distance = Vector2.Distance(smellerPos, scentEntityPos);
            // update priority based on distance
            ticket.Priority = 1.0 / (distance + 0.1); // closer is higher priority
            // update origin coords
            ticket.OriginCoordinates = _transform.GetWorldCoordinates(ticket.SourceEntity);
        }
    }

    private void ProcessPendingSmells(EntityUid uid, SmellerComponent component)
    {
        if (component.NextSmellProcessingTime > _time.CurTime)
            return;
        var interval = _rng.Next(
            component.SmellProcessingTickIntervalRange.X,
            component.SmellProcessingTickIntervalRange.Y);
        component.NextSmellProcessingTime = _time.CurTime + TimeSpan.FromSeconds(interval);

        if (component.PendingSmells.Count == 0)
            return;

        component.PendingSmells.Sort((a, b) => a.ComparePriority(b));

        foreach (var ticket in component.PendingSmells.ToArray())
        {
            bool smelledSomething = false;
            if (CanSmellScent(
                    uid,
                    component,
                    ticket,
                    out var remove))
            {
                SmellScent(
                    uid,
                    component,
                    ticket);
                smelledSomething = true;
            }
            if (remove)
            {
                component.PendingSmells.RemoveAt(0);
            }
            if (smelledSomething)
            {
                break; // Only process one smell per tick
            }
        }
    }

    #region Smellability Checks
    /// <summary>
    /// Determines if the entity can smell the scent based on:
    /// Range,
    /// Line of Sight,
    /// Cooldowns,
    /// and good old RNG.
    /// </summary>
    private bool CanSmellScent(
        EntityUid uid,
        SmellerComponent component,
        SmellTicket scent,
        out bool removeTicket,
        bool detecting = false
        )
    {
        removeTicket = true;
        var maxRange = scent.Smell.ScentProto.FarRange;
        var smellerPos = _transform.GetWorldPosition(uid);
        var scentPos = scent.OriginCoordinates.Position;
        var distance = Vector2.Distance(scentPos, smellerPos);
        if (distance > maxRange)
            return false;
        if (scent.RequireLoS)
        {
            if (!_interact.InRangeUnobstructed(
                    uid,
                    scent.OriginCoordinates,
                    maxRange,
                    CollisionGroup.InteractImpassable))
            {
                return false;
            }
        }

        removeTicket = false;
        // Check cooldown
        if (SmellIsOnCooldown(
                component,
                scent.Smell,
                detecting))
        {
            return false;
        }

        // Check RNG
        if (detecting)
        {
            var chance = _rng.Next(1, 101);
            if (chance > scent.Smell.ScentProto.DetectionChance)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if the scent is on cooldown for the smeller.
    /// </summary>
    private bool SmellIsOnCooldown(SmellerComponent component, Scent scent, bool detecting)
    {
        // Check if WE have smelled this scent before, and if we are still on cooldown
        if (component.SmelledScentsCooldowns.TryGetValue(scent.ScentInstanceId, out var nextSmellTime)
            && nextSmellTime >= _time.CurTime)
            return true;
        // // Check if the SCENT'S cooldown has expired
        // if (detecting)
        // {
        //     if (scent.CooldownTime >= _time.CurTime)
        //         return true;
        // }
        return false;
    }
    #endregion

    #region Smelling

    /// <summary>
    /// Actually smells a thing.
    /// Throws messages, sets cooldowns, etc.
    /// </summary>
    private void SmellScent(
        EntityUid uid,
        SmellerComponent component,
        SmellTicket ticket,
        bool direct = false)
    {
        if (LewdGuard(uid, ticket))
            return;

        // first, remember that we smelled this scent, and set our personal cooldown
        // doesnt factor in here, its for the pending smell tickets
        // you can totally sniff someone any time tho
        var cooldownSeconds = _rng.Next(
            ticket.Smell.ScentProto.MinCooldown,
            ticket.Smell.ScentProto.MaxCooldown);
        var nextSmellTime = _time.CurTime + TimeSpan.FromSeconds(cooldownSeconds);
        component.SmelledScentsCooldowns[ticket.Smell.ScentInstanceId] = nextSmellTime;

        // and, if they have a pending smell ticket for this scent, remove it
        // chances are it'll be readded with a new cooldown later
        component.PendingSmells.RemoveAll(x => x.Smell.ScentInstanceId == ticket.Smell.ScentInstanceId);

        // Get the appropriate message list based on range
        List<string>? messages = null;
        if (direct && ticket.Smell.ScentProto.ScentsDirect.Count > 0)
        {
            messages = ticket.Smell.ScentProto.ScentsDirect;
        }
        else
        {
            var distance = Vector2.Distance(
                _transform.GetWorldPosition(uid),
                ticket.OriginCoordinates.Position);
            if (distance <= ticket.Smell.ScentProto.CloseRange
                && ticket.Smell.ScentProto.ScentsClose.Count > 0)
            {
                messages = ticket.Smell.ScentProto.ScentsClose;
            }
            else if (distance <= ticket.Smell.ScentProto.FarRange
                     && ticket.Smell.ScentProto.ScentsFar.Count > 0)
            {
                messages = ticket.Smell.ScentProto.ScentsFar;
            }
        }

        if (messages == null
            || messages.Count == 0)
            return;
        // The actual message!

        var smellKind = PopupType.Medium;
        if (ticket.Smell.ScentProto.Stinky)
        {
            smellKind = PopupType.MediumCaution;
        }
        var locmsg = Loc.GetString(
            _rng.Pick(messages),
            ("src", Identity.Name(ticket.SourceEntity, EntityManager)));
        _popupSystem.PopupEntity(
            locmsg,
            uid,
            uid,
            smellKind,
            false);
    }

    /// <summary>
    /// Lewd guard: prevents smelling lewd scents if the user has no business doing so
    /// If the scent isnt lewd, then, its allowed i guess
    /// If smeller is Aghost, its allowed (admins are made to be prefbroken)
    /// Otherwise, checl consents
    /// </summary>
    private bool LewdGuard(EntityUid uid, SmellTicket ticket)
    {
        if (!ticket.Smell.ScentProto.Lewd)
            return false;
        if (HasComp<AdminGhostComponent>(uid))
            return false;
        return _consent.HasConsent(uid, "CanSmellLewdScents");
    }









    #endregion

}
