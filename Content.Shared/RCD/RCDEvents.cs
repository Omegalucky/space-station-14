using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.RCD;

[Serializable, NetSerializable]
public sealed class RCDSystemMessage(ProtoId<RCDPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<RCDPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public sealed class RCDConstructionGhostRotationEvent(NetEntity netEntity, Direction direction) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
    public readonly Direction Direction = direction;
}

// Starlight Start: RPD/RPLD
[Serializable, NetSerializable]
public sealed class RCDConstructionGhostFlipEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly bool UseMirrorPrototype;
    public RCDConstructionGhostFlipEvent(NetEntity netEntity, bool useMirrorPrototype)
    {
        NetEntity = netEntity;
        UseMirrorPrototype = useMirrorPrototype;
    }
}

[Serializable, NetSerializable]
public sealed class RPDSelectedLayerEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly byte Layer;

    public RPDSelectedLayerEvent(NetEntity netEntity, byte layer)
    {
        NetEntity = netEntity;
        Layer = layer;
    }
}
// Starlight End: RPD/RPLD

// Starlight-start
/// <summary>
/// Sent when the player picks a swatch from the pipe colour strip under the RPD's radial menu.
/// </summary>
/// <remarks>
/// Carries the palette key rather than the colour itself, so the server resolves the colour
/// against its own copy of the palette and a modified client cannot ask for an arbitrary one.
/// A null key is the "leave it unpainted" swatch.
/// </remarks>
[Serializable, NetSerializable]
public sealed class RCDSetPipeColorMessage(string? key) : BoundUserInterfaceMessage
{
    public string? Key = key;
}

/// <summary>
/// Raised on an entity an RCD-family device has just built, when that device has a pipe colour
/// selected. Handled on the server, where the pipe colour component lives.
/// </summary>
[ByRefEvent]
public record struct RCDPipeColorEvent(Color Color);
// Starlight-end

[Serializable, NetSerializable]
public enum RcdUiKey : byte
{
    Key
}
