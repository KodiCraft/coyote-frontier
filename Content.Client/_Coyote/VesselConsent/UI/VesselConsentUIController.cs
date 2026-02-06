using Content.Client._COYOTE.VesselConsent.UI.Windows;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client._COYOTE.VesselConsent.UI;

[UsedImplicitly]
public sealed class VesselConsentUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly GameplayStateLoadController _gameplayStateLoad = default!;

    private VesselConsentUI? _window;
    private MenuButton? VesselConsentButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.VesselConsentButton;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<VesselConsentUI>();
        _window.OnOpen += () =>
        {
            if (VesselConsentButton is not null)
                VesselConsentButton.Pressed = true;
        };
        _window.OnClose += () =>
        {
            if (VesselConsentButton is not null)
                VesselConsentButton.Pressed = false;
            _window.UpdateUi(); // Discard unsaved changes
        };
    }

    // Both called in GameMenuTopBarUIController.cs as the top bar buttons are being loaded
    public void LoadButton()
    {
        if (VesselConsentButton == null)
            return;

        VesselConsentButton.OnPressed += ButtonPressed;
    }

    public void UnloadButton()
    {
        if (VesselConsentButton == null)
            return;

        VesselConsentButton.OnPressed -= ButtonPressed;
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
        {
            if (VesselConsentButton != null)
                VesselConsentButton.Pressed = false;
            _window.Close();
        }
        else
        {
            if (VesselConsentButton != null)
                VesselConsentButton.Pressed = true;
            _window.Open();
        }
    }

    private void ButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenVesselConsentWindow, InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<VesselConsentUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<VesselConsentUIController>();
    }
}
