using Content.Shared.RCD.Systems;
using Content.Shared.Atmos.Components; // Starlight-edit: RPLD/RPD layered placement support
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization; // Starlight

namespace Content.Shared.RCD.Components;

/// <summary>
/// Main component for the RCD
/// Optionally uses LimitedChargesComponent.
/// Charges can be refilled with RCD ammo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RCDSystem))]
public sealed partial class RCDComponent : Component
{
    /// <summary>
    /// List of RCD prototypes that the device comes loaded with
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RCDPrototype>> AvailablePrototypes { get; set; } = new();

    // Starlight-start
    /// <summary>
    /// Other RCD-like devices whose recipes get folded into <see cref="AvailablePrototypes"/> on map init.
    /// Lets a combined device (e.g. the CE's rapid prototyper) track the tools it stands in for instead of
    /// carrying a hand-maintained copy of their recipes. Leave empty for a normal, self-contained device.
    /// </summary>
    [DataField]
    public List<EntProtoId> ComposeFrom = new();

    /// <summary>
    /// True when this device merges several toolsets, which relaxes tool-specific restrictions.
    /// </summary>
    public bool IsComposite => ComposeFrom.Count > 0;
    // Starlight-end

    /// <summary>
    /// Sound that plays when a RCD operation successfully completes
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessSound { get; set; } = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// The ProtoId of the currently selected RCD prototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RCDPrototype> ProtoId { get; set; } = "Invalid";

    // Starlight Start
    /// <summary>
    /// A cached copy of currently selected RCD prototype
    /// </summary>
    /// <remarks>
    /// If the ProtoId is changed, make sure to update the CachedPrototype as well
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public RCDPrototype CachedPrototype { get; set; } = default!;


    /// <summary>
    /// When true the RCD will use the prototype's MirrorPrototype (if available) for placement/validation.
    /// This is networked so the server can validate/finalize mirror placement.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool UseMirrorPrototype = false;

    /// <summary>
    /// Indicates whether this is an RCD or an RPD
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsRpd { get; set; } = false;
    // Starlight End

    // Starlight Start: RPLD support
    /// <summary>
    /// Indicates whether this is an RPLD (plumbing)
    /// </summary>
    [DataField("isRPLD"), AutoNetworkedField]
    public bool IsRPLD { get; set; } = false;
    // Starlight End: RPLD support

    // Starlight-start
    /// <summary>
    /// Pipe colours this device can paint what it builds with, keyed by the name shown in the UI.
    /// Devices that do not paint leave this empty, which is what keeps the colour strip off the
    /// RCD and the RPLD.
    /// </summary>
    /// <remarks>
    /// Not networked: it never changes at runtime, so the client reads it straight off the
    /// prototype. Same arrangement as the spray painter's palette.
    /// </remarks>
    [DataField]
    public Dictionary<string, Color> PipeColorPalette = new();

    /// <summary>
    /// Key into <see cref="PipeColorPalette"/> for the colour applied to whatever gets built next,
    /// or null to leave it whatever colour its prototype ships with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PipeColor;
    // Starlight-end

    /// <summary>
    /// The direction constructed entities will face upon spawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction ConstructionDirection
    {
        get => _constructionDirection;
        set
        {
            _constructionDirection = value;
            ConstructionTransform = new Transform(new(), _constructionDirection.ToAngle());
        }
    }

    private Direction _constructionDirection = Direction.South;

    /// <summary>
    /// Returns a rotated transform based on the specified ConstructionDirection
    /// </summary>
    /// <remarks>
    /// Contains no position data
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Transform ConstructionTransform { get; private set; }

    // Starlight Start
    /// <summary>
    /// Last free-mode layer selected on the client.
    /// Used by the server as the authoritative layer when placing layered pipes in Free mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AtmosPipeLayer? LastSelectedLayer { get; set; }

    /// <summary>
    /// Current pipe layer / build mode for RPD
    /// </summary>
    [DataField, AutoNetworkedField]
    public RpdMode CurrentMode { get; set; } = RpdMode.Free;

    [DataField]
    public SoundSpecifier SoundSwitchMode { get; set; } = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg");
}

[Serializable, NetSerializable]
public enum RpdMode : byte
{
    Primary = 0,
    Secondary = 1,
    Tertiary = 2,
    Quaternary = 3,
    Quinary = 4,
    Free = 5,
    // Starlight End
}
