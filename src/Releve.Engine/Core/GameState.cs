using Releve.Engine.World;

namespace Releve.Engine.Core;

/// <summary>Etat mutable du jeu : seul element sauvegarde.</summary>
public sealed class GameState
{
    public RoomId CurrentRoom { get; set; }

    public Dictionary<ObjectId, Location> Locations { get; } = new();

    public Dictionary<ObjectId, ObjectState> States { get; } = new();

    public HashSet<GameEvent> Events { get; } = new();

    public ObjectState StateOf(ObjectId id) => States.GetValueOrDefault(id, ObjectState.None);

    public bool IsInInventory(ObjectId id) => Locations.GetValueOrDefault(id).Kind == LocationKind.Inventory;
}
