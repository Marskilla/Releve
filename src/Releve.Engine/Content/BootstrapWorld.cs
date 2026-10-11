using Releve.Engine.Content.Objects;
using Releve.Engine.Content.Rooms;
using Releve.Engine.World;

namespace Releve.Engine.Content;

/// <summary>
/// Assemblage du monde d'amorcage (§12.2). Point de construction unique, a remplacer
/// par le contenu reel.
/// </summary>
public static class BootstrapWorld
{
    public static WorldModel Create() => new(
        startRoom: RoomId.SandboxA,
        rooms: new Dictionary<RoomId, RoomBase>
        {
            [RoomId.SandboxA] = new SandboxA(),
            [RoomId.SandboxB] = new SandboxB(),
        },
        objects: SandboxObjects.Create());
}
