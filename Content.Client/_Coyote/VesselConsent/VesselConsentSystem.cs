using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Coyote.VesselConsent;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._COYOTE.VesselConsent;

/// <summary>
/// This handles updating the menu button and further reporting when the consent state of the current vessel changes.
/// </summary>
public sealed class VesselConsentSystem : SharedVesselConsentSystem
{
    public bool? VesselConsentState => _vesselConsentState;
    public string? CurrentVesselDescription => GetCurrentVesselConditions()?.Description;
    public int? CurrentVesselConditionsId => GetCurrentVesselConditions()?.Id;
    public event Action? OnConsentStatusChange;
    public event Action? OnVesselConditionsChange;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private MenuButton? VesselConsentButton => _uiManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.VesselConsentButton;

    private bool? _vesselConsentState;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CurrentVesselConsentChangeEvent>(VesselConsentChanged);
        SubscribeLocalEvent<VesselConditionsComponent, AfterAutoHandleStateEvent>(VesselConditionsChanged);
        // TODO: Add an event to fire when vessel ownership changes, notably if the player's ID is removed
    }

    public bool OwnsCurrentVessel()
    {
        return PlayerOwnsCurrentVessel(_playerManager.LocalEntity);
    }

    public bool ConsentsToCurrentVessel()
    {
        if (!TryComp(_playerManager.LocalEntity, out TransformComponent? trans))
            // Does the player consent to the vessel they are on if they are nowhere??
            return false;
        if (!TryComp(_playerManager.LocalEntity, out ConsentsToVesselConditionsComponent? consents))
            return false;

        var grid = _transform.GetGrid(trans.Coordinates);
        // Consent is implicit in public spaces or on vessels with no description
        return !TryComp(grid, out VesselConditionsComponent? cond) ||
               cond.Description.Length == 0 ||
               consents.Consents.Contains(cond.Id);
    }

    public void UpdateCurrentVesselConditions(string description)
    {
        RaiseNetworkEvent(new UpdateCurrentVesselConditionsEvent(description));
    }

    public void UpdateOwnVesselConsent(bool newConsentState, int conditionsId)
    {
        RaiseNetworkEvent(new UpdateOwnVesselConsentEvent(newConsentState, conditionsId));
    }

    private void VesselConsentChanged(CurrentVesselConsentChangeEvent args)
    {
        Log.Info($"Got consent change event: {args.NewValue?.ToString() ?? "null"}");
        _vesselConsentState = args.NewValue;
        UpdateVcButton();
        OnConsentStatusChange?.Invoke();
    }

    private void VesselConditionsChanged(Entity<VesselConditionsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Log.Info($"Current vessel changed its description: {CurrentVesselDescription}");
        UpdateVcButton();
        OnVesselConditionsChange?.Invoke();
    }

    private void UpdateVcButton()
    {
        if (VesselConsentButton == null)
            return;
        // Somewhat awkward way to reset the color depending on the new state, not sure if there's a cleaner way
        VesselConsentButton.RemoveStyleClass(MenuButton.StyleClassRedTopButton);
        VesselConsentButton.RemoveStyleClass(MenuButton.StyleClassGreenTopButton);
        switch (_vesselConsentState)
        {
            case true:
                VesselConsentButton.AddStyleClass(MenuButton.StyleClassGreenTopButton);
                break;
            case false:
                VesselConsentButton.AddStyleClass(MenuButton.StyleClassRedTopButton);
                break;
            case null:
                break;
        }
    }

    private VesselConditionsComponent? GetCurrentVesselConditions()
    {
        if (!TryComp(_playerManager.LocalEntity, out TransformComponent? comp))
            return null;
        var grid = _transform.GetGrid(comp.Coordinates);
        return !TryComp(grid, out VesselConditionsComponent? cond) ? null : cond;
    }
}
