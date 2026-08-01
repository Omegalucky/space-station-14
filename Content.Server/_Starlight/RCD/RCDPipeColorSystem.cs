using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.RCD;

namespace Content.Server._Starlight.RCD;

/// <summary>
/// Applies the pipe colour selected on an RPD to the pipes and devices it builds.
/// </summary>
/// <remarks>
/// This lives on the server because <see cref="AtmosPipeColorComponent"/> does, while the RCD
/// system that spawns the entity is shared - hence the hop through <see cref="RCDPipeColorEvent"/>.
/// Going through <see cref="AtmosPipeColorSystem"/> rather than setting appearance data directly
/// keeps the colour on the component, which is the copy that gets written into map saves and read
/// back by the spray painter and the atmos monitoring console.
/// </remarks>
public sealed partial class RCDPipeColorSystem : EntitySystem
{
    [Dependency] private AtmosPipeColorSystem _pipeColor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeColorComponent, RCDPipeColorEvent>(OnPipeColor);
    }

    /// <remarks>
    /// Subscribing against the component means the things an RPD builds that have no colour to
    /// set - air alarms, sensors - never reach this handler and need no special casing.
    /// </remarks>
    private void OnPipeColor(Entity<AtmosPipeColorComponent> ent, ref RCDPipeColorEvent args)
    {
        _pipeColor.SetColor(ent, ent.Comp, args.Color);
    }
}
