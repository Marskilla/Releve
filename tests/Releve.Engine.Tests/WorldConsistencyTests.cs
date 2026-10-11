using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Coherence du monde : les tests remplacent la validation de schema (§11).</summary>
public class WorldConsistencyTests
{
    // Pieces et objets reserves aux tests : ils vivent dans les enums du moteur mais pas dans le monde reel.
    private static readonly RoomId[] TestRoomIds =
    {
        RoomId.TestRoomStart, RoomId.TestRoomVault, RoomId.TestRoomWall,
    };

    private static readonly ObjectId[] TestObjectIds =
    {
        ObjectId.TestKey, ObjectId.TestCabinet, ObjectId.TestChest,
        ObjectId.TestBook, ObjectId.TestAccountBook, ObjectId.TestHiddenNote,
    };

    [Fact]
    public void Game_World_Is_Consistent()
    {
        var world = GameWorld.Create();

        AssertConsistent(world, TestRoomIds, TestObjectIds, isGameWorld: true);

        Assert.Equal(RoomId.Bedroom, world.StartRoom);
        Assert.Equal(4, world.Rooms.Count);
        Assert.Equal(22, world.Objects.Count);
    }

    [Fact]
    public void Test_World_Is_Consistent()
    {
        var world = Releve.Engine.Tests.World.TestWorldFactory.Create();

        AssertConsistent(world, TestRoomIds, TestObjectIds, isGameWorld: false);

        Assert.Equal(RoomId.TestRoomStart, world.StartRoom);
    }

    private static void AssertConsistent(WorldModel world, RoomId[] testRooms, ObjectId[] testObjects, bool isGameWorld)
    {
        if (isGameWorld)
        {
            // Chaque piece hors monde de test a une classe de piece.
            foreach (var roomId in Enum.GetValues<RoomId>().Except(testRooms))
            {
                Assert.True(world.Rooms.ContainsKey(roomId), $"Aucune pièce pour {roomId}.");
            }

            // Chaque objet hors monde de test a une definition.
            foreach (var objectId in Enum.GetValues<ObjectId>().Except(testObjects))
            {
                Assert.True(world.Objects.ContainsKey(objectId), $"Aucune définition pour {objectId}.");
            }
        }
        else
        {
            // Le monde de test n'utilise que ses propres pieces et objets.
            foreach (var roomId in world.Rooms.Keys)
            {
                Assert.Contains(roomId, testRooms);
            }

            foreach (var objectId in world.Objects.Keys)
            {
                Assert.Contains(objectId, testObjects);
            }
        }

        foreach (var (roomId, room) in world.Rooms)
        {
            var seenDirections = new HashSet<Direction>();

            foreach (var exit in room.Exits)
            {
                Assert.True(seenDirections.Add(exit.Dir), $"Sortie en double dans {roomId} vers {exit.Dir}.");

                if (exit.To is not null)
                {
                    // La destination existe.
                    Assert.True(world.Rooms.ContainsKey(exit.To.Value),
                        $"{roomId} vers {exit.Dir} pointe vers une pièce inconnue ({exit.To}).");

                    // Retour present cote destination, sauf sortie a sens unique.
                    if (!exit.OneWay)
                    {
                        var back = world.Rooms[exit.To.Value].Exits
                            .FirstOrDefault(e => e.Dir == exit.Dir.Opposite());
                        Assert.True(back is not null,
                            $"Aucun retour depuis {exit.To} vers {exit.Dir.Opposite()}.");
                        Assert.Equal(roomId, back!.To);
                    }
                }
                else
                {
                    // Sortie sans destination : toujours bloquee, message obligatoire.
                    Assert.False(exit.OneWay);
                    Assert.False(string.IsNullOrWhiteSpace(exit.BlockedMessage));
                }
            }
        }

        // Pas de collision d'alias entre objets presents dans la meme piece.
        foreach (var (roomId, _) in world.Rooms)
        {
            var aliases = world.Objects.Values
                .Where(def => def.InitialLocation.Kind == LocationKind.Room
                    && def.InitialLocation.Room == roomId)
                .SelectMany(def => def.Aliases.Select(alias => string.Join(" ", TextNormalizer.Normalize(alias))))
                .ToList();

            Assert.Equal(aliases.Distinct(StringComparer.Ordinal).Count(), aliases.Count);
        }
    }
}
