using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Starlight.RCD;

/// <summary>
/// Guards RCDComponent.ComposeFrom, which the chief engineer's rapid prototyper uses to inherit the
/// recipes of the RCD and the RPD instead of carrying its own copy. If someone adds a recipe to either
/// source device and the prototyper silently fails to pick it up, this test is what catches it.
/// </summary>
[TestFixture]
public sealed class RCDComposeFromTest : GameTest
{
    private static ProtoId<RCDPrototype> Recipe(string id) => new(id);

    [Test]
    public async Task PrototyperInheritsRecipesFromBothSourceDevices()
    {
        var server = Pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entityManager.System<SharedMapSystem>();

        MapId mapId = default;
        await server.WaitPost(() => mapSys.CreateMap(out mapId));

        await server.WaitAssertion(() =>
        {
            var coords = new MapCoordinates(Vector2.Zero, mapId);

            var prototyper = entityManager.GetComponent<RCDComponent>(
                entityManager.SpawnEntity("RCDChiefEngineerPrototyper", coords));
            var rcd = entityManager.GetComponent<RCDComponent>(
                entityManager.SpawnEntity("RCD", coords));
            var rpd = entityManager.GetComponent<RCDComponent>(
                entityManager.SpawnEntity("RPD", coords));

            Assert.Multiple(() =>
            {
                Assert.That(prototyper.IsComposite, Is.True,
                    "prototyper should declare composeFrom and report itself composite");

                Assert.That(rcd.AvailablePrototypes.Except(prototyper.AvailablePrototypes), Is.Empty,
                    "prototyper is missing recipes the plain RCD has");
                Assert.That(rpd.AvailablePrototypes.Except(prototyper.AvailablePrototypes), Is.Empty,
                    "prototyper is missing recipes the plain RPD has");

                // Composition must not leak back into the single-toolset devices.
                Assert.That(rpd.AvailablePrototypes, Does.Not.Contain(Recipe("WallSolid")),
                    "plain RPD should not have gained construction recipes");
                Assert.That(rcd.AvailablePrototypes, Does.Not.Contain(Recipe("PipeStraight")),
                    "plain RCD should not have gained piping recipes");
                Assert.That(rpd.IsComposite, Is.False);
                Assert.That(rcd.IsComposite, Is.False);
            });
        });
    }
}
