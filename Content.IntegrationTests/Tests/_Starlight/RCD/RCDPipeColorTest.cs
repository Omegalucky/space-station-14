using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Atmos.Piping.Components;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Starlight.RCD;

/// <summary>
/// Guards the RPD's pipe colour selection: the palettes the swatch strip is built from, the
/// validation that stops a client asking for a colour its device does not have, and the server-side
/// hop that actually paints what gets built.
/// </summary>
[TestFixture]
public sealed class RCDPipeColorTest : GameTest
{
    [Test]
    public async Task PaletteIsPresentOnPaintingDevicesOnly()
    {
        var server = Pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entityManager.System<SharedMapSystem>();

        MapId mapId = default;
        await server.WaitPost(() => mapSys.CreateMap(out mapId));

        await server.WaitAssertion(() =>
        {
            var coords = new MapCoordinates(Vector2.Zero, mapId);

            RCDComponent Spawn(string proto) =>
                entityManager.GetComponent<RCDComponent>(entityManager.SpawnEntity(proto, coords));

            var rpd = Spawn("RPD");
            var prototyper = Spawn("RCDChiefEngineerPrototyper");
            var rcd = Spawn("RCD");
            var rpld = Spawn("RPLD");

            Assert.Multiple(() =>
            {
                Assert.That(rpd.PipeColorPalette, Is.Not.Empty,
                    "RPD needs a palette or its colour strip will not appear");

                // The prototyper cannot inherit the palette - composeFrom only folds in recipes -
                // so the two are hand-kept copies. This is what catches them drifting apart.
                Assert.That(prototyper.PipeColorPalette, Is.EqualTo(rpd.PipeColorPalette),
                    "prototyper's palette has drifted from the RPD's; update both in tools.yml");

                // An empty palette is how the strip stays off devices that do not paint.
                Assert.That(rcd.PipeColorPalette, Is.Empty, "plain RCD should not offer pipe colours");
                Assert.That(rpld.PipeColorPalette, Is.Empty, "RPLD should not offer pipe colours");

                // Nothing is painted until the player picks a swatch.
                Assert.That(rpd.PipeColor, Is.Null);
                Assert.That(prototyper.PipeColor, Is.Null);
            });
        });
    }

    [Test]
    public async Task OnlyColoursFromTheDevicesOwnPaletteAreAccepted()
    {
        var server = Pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entityManager.System<SharedMapSystem>();

        MapId mapId = default;
        await server.WaitPost(() => mapSys.CreateMap(out mapId));

        await server.WaitAssertion(() =>
        {
            var uid = entityManager.SpawnEntity("RPD", new MapCoordinates(Vector2.Zero, mapId));
            var rpd = entityManager.GetComponent<RCDComponent>(uid);

            var known = rpd.PipeColorPalette.Keys.First();

            entityManager.EventBus.RaiseLocalEvent(uid, new RCDSetPipeColorMessage(known));
            Assert.That(rpd.PipeColor, Is.EqualTo(known), "a colour from the palette should be accepted");

            entityManager.EventBus.RaiseLocalEvent(uid, new RCDSetPipeColorMessage("not-a-real-colour"));
            Assert.That(rpd.PipeColor, Is.EqualTo(known),
                "a colour outside the palette should be rejected and leave the selection alone");

            entityManager.EventBus.RaiseLocalEvent(uid, new RCDSetPipeColorMessage(null));
            Assert.That(rpd.PipeColor, Is.Null, "the unpainted swatch should clear the selection");
        });
    }

    [Test]
    public async Task BuiltPipeTakesTheSelectedColour()
    {
        var server = Pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entityManager.System<SharedMapSystem>();

        MapId mapId = default;
        await server.WaitPost(() => mapSys.CreateMap(out mapId));

        await server.WaitAssertion(() =>
        {
            var pipe = entityManager.SpawnEntity("GasPipeStraight", new MapCoordinates(Vector2.Zero, mapId));
            var color = entityManager.GetComponent<AtmosPipeColorComponent>(pipe);

            Assert.That(color.Color, Is.EqualTo(Color.White), "pipes should start unpainted");

            // Stands in for the hop RCDSystem makes after spawning: shared code raises this and the
            // server system applies it, because the colour component only exists on the server.
            var ev = new RCDPipeColorEvent(Color.Red);
            entityManager.EventBus.RaiseLocalEvent(pipe, ref ev);

            Assert.That(color.Color, Is.EqualTo(Color.Red),
                "the colour should land on the component, not just on appearance data");
        });
    }
}
