using Releve.Engine.Content.Objects;
using Releve.Engine.Content.Rooms;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content;

/// <summary>
/// Assemblage du monde de jeu : quatre pièces du PoC, la chambre de réveil comme départ.
/// Point de construction unique.
/// </summary>
public static class GameWorld
{
    public static WorldModel Create() => new(
        startRoom: RoomId.Bedroom,
        rooms: new Dictionary<RoomId, RoomBase>
        {
            [RoomId.Bedroom] = new Bedroom(),
            [RoomId.TechCorridor] = new TechCorridor(),
            [RoomId.HospitalLobby] = new HospitalLobby(),
            [RoomId.Street] = new Street(),
        },
        objects: CreateObjects());

    private static IReadOnlyDictionary<ObjectId, ObjectDef> CreateObjects()
    {
        var objects = new Dictionary<ObjectId, ObjectDef>();
        foreach (var part in new[]
        {
            BedroomObjects.Create(),
            TechCorridorObjects.Create(),
            HospitalLobbyObjects.Create(),
        })
        {
            foreach (var (id, def) in part)
            {
                objects[id] = def;
            }
        }

        return objects;
    }
}
