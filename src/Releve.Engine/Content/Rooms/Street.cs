using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Rue provisoire : fin du PoC (PoC §3.4, aucune valeur de lore).</summary>
public sealed class Street : RoomBase
{
    private const string RoomTitle = "La rue (provisoire)";
    private const string RoomDescription =
        "Une rue vide, gagnée par la végétation. (Lieu provisoire : fin du PoC.)";

    public override RoomId Id => RoomId.Street;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "Street";

    public override string Description(GameState s) => RoomDescription;

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.East, RoomId.HospitalLobby,
            Words: new[] { "entre", "entrer", "rentre", "rentrer", "hopital" }),
    };
}
