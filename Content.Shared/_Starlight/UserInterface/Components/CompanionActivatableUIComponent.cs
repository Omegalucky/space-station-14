using Content.Shared.UserInterface;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.UserInterface.Components;

/// <summary>
/// Opens extra user interfaces alongside the entity's <see cref="ActivatableUIComponent"/> one, so a
/// device with several screens shows all of them from a single interaction.
/// </summary>
/// <remarks>
/// <see cref="ActivatableUIComponent"/> holds a single key, so screens past the first have no vanilla
/// way to open. These ride along with the primary screen rather than opening on their own, which means
/// every rule the primary screen already enforces -- power cell charge, reach, hands, admin-only --
/// applies to them for free. Nothing is re-implemented here.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class CompanionActivatableUIComponent : Component
{
    /// <summary>
    /// UI keys opened and closed together with the primary one. Each must also be listed on the
    /// entity's UserInterface component.
    /// </summary>
    [DataField(required: true)]
    public List<Enum> Keys = new();
}
