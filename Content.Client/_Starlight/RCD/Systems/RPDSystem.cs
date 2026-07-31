using Content.Client.Items;
using Content.Client.Message;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Starlight.RCD.Systems;

public sealed class RPDSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<RCDComponent>(OnItemStatus);
    }

    private Control OnItemStatus(Entity<RCDComponent> entity)
        => new RPDModeStatusControl(entity);

    private sealed class RPDModeStatusControl : Control
    {
        private readonly RichTextLabel _label = new()
        {
            StyleClasses = { "ItemStatus" }
        };

        private readonly EntityUid _uid;
        private readonly RCDSystem _rcdSystem;

        public RPDModeStatusControl(Entity<RCDComponent> entity)
        {
            _uid = entity.Owner;
            _rcdSystem = Get<RCDSystem>();
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            // Re-checked every frame so the readout follows the selected recipe rather than being
            // fixed at construction. The layer mode is meaningless for unlayered recipes.
            var show = _rcdSystem.ShowsPipeMode(_uid);
            _label.Visible = show;

            if (!show)
                return;

            var currentMode = _rcdSystem.GetCurrentRpdMode(_uid);

            var modeKey = $"rcd-rpd-mode-{currentMode.ToString().ToLowerInvariant()}";
            var modeName = Robust.Shared.Localization.Loc.GetString(modeKey);

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("rcd-item-status-mode",
                ("mode", $"[color=cyan]{modeName}[/color]")));
        }
    }
}
