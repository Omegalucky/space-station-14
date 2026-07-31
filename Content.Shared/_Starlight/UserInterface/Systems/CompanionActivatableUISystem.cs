using Content.Shared._Starlight.UserInterface.Components;
using Content.Shared.UserInterface;

namespace Content.Shared._Starlight.UserInterface.Systems;

/// <summary>
/// Drives <see cref="CompanionActivatableUIComponent"/>. Companion screens open when the primary screen
/// opens and close when it closes, so the whole device behaves as one interaction.
/// </summary>
public sealed partial class CompanionActivatableUISystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CompanionActivatableUIComponent, AfterActivatableUIOpenEvent>(OnPrimaryOpened);
        SubscribeLocalEvent<CompanionActivatableUIComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    /// <summary>
    /// The primary screen just opened for someone. ActivatableUISystem ran every check before raising
    /// this -- reach, hands, single user, and the power cell gate -- so the companion screens inherit
    /// all of it simply by opening here.
    /// </summary>
    private void OnPrimaryOpened(EntityUid uid, CompanionActivatableUIComponent component, AfterActivatableUIOpenEvent args)
    {
        foreach (var key in component.Keys)
        {
            if (_ui.HasUi(uid, key))
                _ui.OpenUi(uid, key, args.User);
        }
    }

    /// <summary>
    /// Close the companion screens whenever the primary one closes, whatever caused it: the user
    /// closing the window, dropping the device, or the power cell running flat.
    /// </summary>
    private void OnUiClosed(EntityUid uid, CompanionActivatableUIComponent component, BoundUIClosedEvent args)
    {
        if (!TryComp<ActivatableUIComponent>(uid, out var activatable)
            || !Equals(args.UiKey, activatable.Key))
            return;

        foreach (var key in component.Keys)
        {
            _ui.CloseUi(uid, key, args.Actor);
        }
    }
}
