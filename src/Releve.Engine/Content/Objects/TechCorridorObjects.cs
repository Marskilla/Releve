using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Objects;

/// <summary>Objets du couloir technique (PoC §3.2).</summary>
public static class TechCorridorObjects
{
    public static IReadOnlyDictionary<ObjectId, ObjectDef> Create() => new Dictionary<ObjectId, ObjectDef>
    {
        [ObjectId.StairsDoor] = new ObjectDef(
            Id: ObjectId.StairsDoor,
            Name: "la porte de l’escalier",
            Aliases: new[] { "porte escalier", "porte fond", "escalier" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TechCorridor),
            InitialState: ObjectState.None,
            Describe: _ =>
                "Une porte pleine percée d’un grand vitrage en verre armé. On distingue les premières marches, "
                + "qui montent vers une lumière plus claire."),

        [ObjectId.StairsReader] = new ObjectDef(
            Id: ObjectId.StairsReader,
            Name: "le lecteur de l’escalier",
            Aliases: new[] { "lecteur escalier", "lecteur fond" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TechCorridor),
            InitialState: ObjectState.Locked,
            Describe: DescribeReader),

        [ObjectId.TechDoor] = new ObjectDef(
            Id: ObjectId.TechDoor,
            Name: "la porte du local technique",
            Aliases: new[] { "porte local", "porte technique", "hublot" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TechCorridor),
            InitialState: ObjectState.None,
            Describe: _ =>
                "Une porte pleine à hublot. À travers, on distingue les éléments d’une chaufferie, "
                + "une rangée de lave-linge à l’arrêt depuis longtemps et des compteurs électriques."),

        [ObjectId.TechReader] = new ObjectDef(
            Id: ObjectId.TechReader,
            Name: "le lecteur du local technique",
            Aliases: new[] { "lecteur local", "lecteur technique" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TechCorridor),
            InitialState: ObjectState.Locked,
            Describe: _ => "Le voyant du lecteur est rouge."),
    };

    private static string DescribeReader(GameState state) =>
        state.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked
            ? "Le voyant du lecteur est vert."
            : "Le voyant du lecteur est rouge.";
}
