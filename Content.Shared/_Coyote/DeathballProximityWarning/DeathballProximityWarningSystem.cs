using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Coyote.DeathballProximityWarning;

/// <summary>
/// This handles the deathball proximity warning system logic
/// Suck my deathball.
/// </summary>
public sealed class DeathballProximityWarningSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _soundSystem = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    // Timers for the proximity warning system, different intervals based on warning state
    private TimeSpan BaseCheckInterval = TimeSpan.FromSeconds(5);
    private TimeSpan DetectedCheckInterval = TimeSpan.FromSeconds(4);
    private TimeSpan CloseCheckInterval = TimeSpan.FromSeconds(3);
    private TimeSpan PanicCheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _lastCheck = TimeSpan.Zero;
    private TimeSpan _checkInterval = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeathballProximityWarningComponent, GetVerbsEvent<Verb>>(GetVerbs);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_gameTiming.CurTime - _lastCheck < _checkInterval)
            return;
        _lastCheck = _gameTiming.CurTime;
        Dictionary<DeathballType, List<MapCoordinates>> deathballsByType = new();
        // query all deathballs in the game
        var deathballQuery = EntityQueryEnumerator<DeathballComponent>();
        while (deathballQuery.MoveNext(out var uid, out var deathballComp))
        {
            var transform = Transform(uid);
            var mapCoords = _transformSystem.GetMapCoordinates(uid, transform);
            if (!deathballsByType.ContainsKey(deathballComp.DeathballType))
                deathballsByType[deathballComp.DeathballType] = new List<MapCoordinates>();
            deathballsByType[deathballComp.DeathballType].Add(mapCoords);
        }

        // now iterate over all warning systems
        var warningQuery = EntityQueryEnumerator<DeathballProximityWarningComponent>();
        while (warningQuery.MoveNext(out var uid, out var warningComp))
        {
            if (!warningComp.IsActive)
                continue;
            // timers! cooldowns based on state!
            var timeSinceLastCheck = _gameTiming.CurTime - warningComp.LastCheckTime;
            switch (warningComp.WarningState)
            {
                case DeathballProximityWarningState.Detected:
                    if (timeSinceLastCheck < DetectedCheckInterval)
                        continue;
                    break;
                case DeathballProximityWarningState.Close:
                    if (timeSinceLastCheck < CloseCheckInterval)
                        continue;
                    break;
                case DeathballProximityWarningState.Panic:
                    if (timeSinceLastCheck < PanicCheckInterval)
                        continue;
                    break;
                case DeathballProximityWarningState.Safe:
                case DeathballProximityWarningState.Inactive:
                default:
                    if (timeSinceLastCheck < BaseCheckInterval)
                        continue;
                    break;
            }
            warningComp.LastCheckTime = _gameTiming.CurTime;
            // get our own coords
            var transform = Transform(uid);
            var myMapCoords = _transformSystem.GetMapCoordinates(uid, transform);
            // now we do a lot of distance checking
            // we can short-circuit as soon as we find the highest warning level
            // we can also make this super fast through lazy, imprecise evaluation
            // we dont have to be perfectly accurate here, just good enough to warn players
            var previousState = warningComp.WarningState;
            var newState = DeathballProximityWarningState.Safe;
            var kindFound = DeathballType.Other;
            MapCoordinates? dbCoords = null;
            var panicRangeSq =    warningComp.PanicRange    * warningComp.PanicRange;
            var closeRangeSq =    warningComp.CloseRange * warningComp.CloseRange;
            var detectedRangeSq = warningComp.DetectedRange * warningComp.DetectedRange;
            foreach (var deathKind in deathballsByType.Where(
                         deathKind => warningComp.SupportedTypes.Contains(deathKind.Key)))
            {
                foreach (var distance in
                         from deathballCoords in deathKind.Value
                         where myMapCoords.MapId == deathballCoords.MapId
                         select (myMapCoords.Position - deathballCoords.Position).LengthSquared()
                         into distance
                         where !(distance > detectedRangeSq)
                         select distance) // what a mouthful
                {
                    if (distance <= panicRangeSq)
                    {
                        newState = DeathballProximityWarningState.Panic;
                        kindFound = deathKind.Key;
                        break; // highest alert, break
                    }

                    if (distance <= closeRangeSq)
                    {
                        newState = DeathballProximityWarningState.Close;
                        kindFound = deathKind.Key;
                        continue; // continue checking for panic
                    }

                    newState = DeathballProximityWarningState.Detected;
                    kindFound = deathKind.Key;
                }

                if (newState == DeathballProximityWarningState.Panic)
                    break; // highest alert, break
            }

            warningComp.WarningState = newState;
            // did our state change?
            if (previousState == newState)
            {
                continue; // no change, next
            }
            // did we go up? otherwise down
            var wentUp = newState > previousState;
            DoWarningText(
                uid,
                warningComp,
                kindFound,
                newState,
                wentUp,
                myMapCoords,
                dbCoords ?? myMapCoords); // placeholder for db coords
            switch (newState)
            {
                case DeathballProximityWarningState.Detected:
                    PlayWarningSound(uid, wentUp ? warningComp.DetectedSoundUp : warningComp.DetectedSoundDown);
                    break;
                case DeathballProximityWarningState.Close:
                    PlayWarningSound(uid, wentUp ? warningComp.CloseSoundUp : warningComp.CloseSoundDown);
                    break;
                case DeathballProximityWarningState.Panic:
                    PlayWarningSound(uid, wentUp ? warningComp.PanicSoundUp : warningComp.PanicSoundDown);
                    break;
                case DeathballProximityWarningState.Safe:
                    if (!wentUp) // probably shouldn't happen but whatever
                    {
                        // play safe sound down
                        PlayWarningSound(uid, warningComp.SafeOnceMoreSound);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // NEXT
        }
    }

    private void PlayWarningSound(EntityUid uid, SoundSpecifier sound)
    {
        var cords = Transform(uid).Coordinates;
        _soundSystem.PlayPvs(
            sound,
            cords,
            AudioParams.Default.WithVolume(1f));
    }

    private void DoWarningText(
        EntityUid uid,
        DeathballProximityWarningComponent warningComp,
        DeathballType deathballType,
        DeathballProximityWarningState newState,
        bool wentUp,
        MapCoordinates myCoords,
        MapCoordinates dbCoords)
    {
        var typeStr = deathballType.ToString();
        var stateStr = newState.ToString();
        var upDownStr = wentUp ? "UP" : "DOWN";
        var localizationKey = $"deathball-warning-{typeStr}-{stateStr}-{upDownStr}";

        var dist = (myCoords.Position - dbCoords.Position).Length();
        var bearing = Angle.FromWorldVec(myCoords.Position - dbCoords.Position).Degrees;
        // normalize to -180 to 180
        var bearingJustNumber = (float) ((bearing + 180) % 360) - 180;

        var message = Loc.GetString(localizationKey,
            ("distance", MathF.Round(dist, 1)),
            ("bearing", MathF.Round(bearingJustNumber, 1)));
        _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(advertisements.Values)), InGameICChatType.Speak, hideChat: true);

}
