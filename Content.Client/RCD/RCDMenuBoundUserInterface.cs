using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.RCD;

[UsedImplicitly]
public sealed partial class RCDMenuBoundUserInterface : BoundUserInterface
{
    private const string TopLevelActionCategory = "Main";

    // Starlight-edit: the trailing Tool field groups categories for devices that carry more than one toolset.
    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite, string Tool)> PrototypesGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite, string Tool)>
        {
            ["WallsAndFlooring"] = ("rcd-component-walls-and-flooring", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RCD/walls_and_flooring.png")), "Construction"),
            ["WindowsAndGrilles"] = ("rcd-component-windows-and-grilles", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RCD/windows_and_grilles.png")), "Construction"),
            ["Airlocks"] = ("rcd-component-airlocks", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RCD/airlocks.png")), "Construction"),
            ["Electrical"] = ("rcd-component-electrical", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/multicoil.png")), "Construction"),
            ["Lighting"] = ("rcd-component-lighting", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/lighting.png")), "Construction"),
            // Starlight Start: RPD
            ["Piping"] = ("rpd-component-piping", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/fourway.png")), "Atmospherics"),
            ["AtmosphericUtility"] = ("rpd-component-atmospheric-utility", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/v_gas_mixer.png")), "Atmospherics"),
            ["PumpsValves"] = ("rpd-component-pumps", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/pump_volume.png")), "Atmospherics"),
            ["Vents"] = ("rpd-component-vents", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/vent_passive.png")), "Atmospherics"),
            ["SensorsMonitors"] = ("rpd-component-sensors-monitors", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/airalarm.png")), "Atmospherics"),
            ["InterfacesStorage"] = ("rpd-component-interfaces-storage", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPD/port.png")), "Atmospherics"),
            // Starlight End: RPD
            // Starlight Start: RPLD
            ["PlumbingDucts"] = ("rpld-component-ducts", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPLD/category_ducts.png")), "Plumbing"),
            ["PlumbingSupply"] = ("rpld-component-supply", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPLD/tank.png")), "Plumbing"),
            ["PlumbingProduction"] = ("rpld-component-production", new SpriteSpecifier.Texture(new ResPath("/Textures/_Starlight/Interface/Radial/RPLD/reaction_chamber.png")), "Plumbing"),
            // Starlight End: RPLD
        };

    // Starlight-start
    // Icon and label for each tool ring. Only rendered when a device spans several tools.
    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)> ToolGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)>
        {
            ["Construction"] = ("rcd-component-tool-group-construction", new SpriteSpecifier.Rsi(new ResPath("Objects/Tools/rcd.rsi"), "icon")),
            ["Atmospherics"] = ("rcd-component-tool-group-atmospherics", new SpriteSpecifier.Rsi(new ResPath("_Starlight/Objects/Tools/rpd.rsi"), "icon")),
            ["Plumbing"] = ("rcd-component-tool-group-plumbing", new SpriteSpecifier.Rsi(new ResPath("_Starlight/Objects/Tools/rpld.rsi"), "icon")),
        };
    // Starlight-end

    private bool IsRpd => EntMan.TryGetComponent<RCDComponent>(Owner, out var rcd) && rcd.IsRpd; // Starlight: RPD

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    private SimpleRadialMenu? _menu;

    public RCDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<RCDComponent>(Owner, out var rcd))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var models = ConvertToButtons(rcd.AvailablePrototypes);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(HashSet<ProtoId<RCDPrototype>> prototypes)
    {
        Dictionary<string, List<RadialMenuActionOptionBase>> buttonsByCategory = new();
        ValueList<RadialMenuActionOptionBase> topLevelActions = new();
        foreach (var protoId in prototypes)
        {
            var prototype = _prototypeManager.Index(protoId);
            if (prototype.Category == TopLevelActionCategory)
            {
                var topLevelActionOption = new RadialMenuActionOption<RCDPrototype>(HandleMenuOptionClick, prototype)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                    ToolTip = GetTooltip(prototype)
                };
                topLevelActions.Add(topLevelActionOption);
                continue;
            }

            if (!PrototypesGroupingInfo.TryGetValue(prototype.Category, out var groupInfo))
                continue;

            if (!buttonsByCategory.TryGetValue(prototype.Category, out var list))
            {
                list = new List<RadialMenuActionOptionBase>();
                buttonsByCategory.Add(prototype.Category, list);
            }

            var actionOption = new RadialMenuActionOption<RCDPrototype>(HandleMenuOptionClick, prototype)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                ToolTip = GetTooltip(prototype)
            };
            list.Add(actionOption);
        }

        // Starlight-start
        // Group the category rings by tool, and only insert the extra
        // tool-selection ring when this device actually spans more than one group.
        var categoryOptionsByGroup = new Dictionary<string, List<RadialMenuOptionBase>>();

        foreach (var (key, list) in buttonsByCategory)
        {
            var groupInfo = PrototypesGroupingInfo[key];
            var categoryOption = new RadialMenuNestedLayerOption(list)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(groupInfo.Sprite),
                ToolTip = Loc.GetString(groupInfo.Tooltip)
            };

            if (!categoryOptionsByGroup.TryGetValue(groupInfo.Tool, out var groupList))
            {
                groupList = new List<RadialMenuOptionBase>();
                categoryOptionsByGroup.Add(groupInfo.Tool, groupList);
            }

            groupList.Add(categoryOption);
        }

        var models = new List<RadialMenuOptionBase>();
        var wrapInToolGroups = categoryOptionsByGroup.Count > 1;

        foreach (var (toolGroup, categoryOptions) in categoryOptionsByGroup)
        {
            if (!wrapInToolGroups || !ToolGroupingInfo.TryGetValue(toolGroup, out var toolInfo))
            {
                models.AddRange(categoryOptions);
                continue;
            }

            // Top level actions (deconstruct) sit inside each tool's ring rather than above them
            // intentionally to preserve muscle memory from the RCD and RPD.
            var groupContents = new List<RadialMenuOptionBase>(categoryOptions);

            foreach (var action in topLevelActions)
                groupContents.Add(action);

            models.Add(new RadialMenuNestedLayerOption(groupContents)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(toolInfo.Sprite),
                ToolTip = Loc.GetString(toolInfo.Tooltip)
            });
        }

        // Single toolset devices keep their top level actions at the root.
        if (!wrapInToolGroups)
        {
            foreach (var action in topLevelActions)
                models.Add(action);
        }
        // Starlight-end

        return models;
    }

    private void HandleMenuOptionClick(RCDPrototype proto)
    {
        // A predicted message cannot be used here as the RCD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new RCDSystemMessage(proto.ID));


        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        // Starlight-start
        // Equivalent to EntitySystem.Name(uid), which is unavailable here as this is not a system.
        var device = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        // Starlight-end

        var msg = Loc.GetString("rcd-component-change-mode", ("device", device), ("mode", Loc.GetString(proto.SetName))); // Starlight-edit: name the actual device

        if (proto.Mode is RcdMode.ConstructTile or RcdMode.ConstructObject)
        {
            var name = Loc.GetString(proto.SetName);

            if (proto.Prototype != null &&
                _prototypeManager.TryIndex(proto.Prototype, out var entProto)) // don't use Resolve because this can be a tile
            {
                name = entProto.Name;
            }

            msg = Loc.GetString("rcd-component-change-build-mode", ("device", device), ("name", name)); // Starlight-edit: name the actual device
        }

        // Popup message
        var popup = EntMan.System<PopupSystem>();
        popup.PopupClient(msg, Owner, _playerManager.LocalSession.AttachedEntity);
    }

    private string GetTooltip(RCDPrototype proto)
    {
        string tooltip;

        if (proto.Mode is RcdMode.ConstructTile or RcdMode.ConstructObject
            && proto.Prototype != null
            && _prototypeManager.TryIndex(proto.Prototype, out var entProto)) // don't use Resolve because this can be a tile
        {
            tooltip = entProto.Name; //Starlight: no name field?
        }
        else
        {
            tooltip = Loc.GetString(proto.SetName); //Starlight comment: Has name field?
        }

        tooltip = OopsConcat(char.ToUpper(tooltip[0]).ToString(), tooltip.Remove(0, 1));

        return tooltip;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }
}
