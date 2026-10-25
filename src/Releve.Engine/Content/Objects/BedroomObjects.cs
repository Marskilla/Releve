using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Objects;

/// <summary>Objets de la chambre de réveil (PoC §3.1).</summary>
public static class BedroomObjects
{
    // Textes partagés pour éviter la duplication (J2.4)
    public const string BedroomMachinesDescription =
        "Dossier partiel affiché : maladie inconnue évolutive, traitement adaptatif expérimental. "
        + "Statut : réveil terminé, constantes stables. Le reste du dossier est indisponible.";

    public static IReadOnlyDictionary<ObjectId, ObjectDef> Create() => new Dictionary<ObjectId, ObjectDef>
    {
        [ObjectId.BedroomBed] = new ObjectDef(
            Id: ObjectId.BedroomBed,
            Name: "le lit",
            Aliases: new[] { "lit" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => "Un lit médicalisé."),

        [ObjectId.BedroomMachines] = new ObjectDef(
            Id: ObjectId.BedroomMachines,
            Name: "les machines",
            Aliases: new[] { "machine", "machines", "moniteur", "appareil", "appareils", "materiel", "materiel medical" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => BedroomMachinesDescription),

        [ObjectId.BedroomWardrobe] = new ObjectDef(
            Id: ObjectId.BedroomWardrobe,
            Name: "l'armoire",
            Aliases: new[] { "armoire", "placard" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.Closed,
            Describe: DescribeWardrobe),

        [ObjectId.ChildClothes] = new ObjectDef(
            Id: ObjectId.ChildClothes,
            Name: "les vêtements",
            Aliases: new[] { "vetements", "vetement", "habits" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => "Des vetements d'enfant, bien trop petits pour vous.",
            VisibleWhen: s => s.StateOf(ObjectId.BedroomWardrobe) == ObjectState.Open),

        [ObjectId.Badge] = new ObjectDef(
            Id: ObjectId.Badge,
            Name: "le badge",
            Aliases: new[] { "badge", "carte", "badge identification" },
            Portable: true,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => "Un badge d'identification. La photo est celle d'un enfant d'environ huit ans.",
            VisibleWhen: s => s.StateOf(ObjectId.BedroomWardrobe) == ObjectState.Open),

        [ObjectId.BedroomReader] = new ObjectDef(
            Id: ObjectId.BedroomReader,
            Name: "le lecteur de badge",
            Aliases: new[] { "lecteur", "lecteur badge" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.Locked,
            Describe: DescribeReader),

        [ObjectId.BedroomDoor] = new ObjectDef(
            Id: ObjectId.BedroomDoor,
            Name: "la porte",
            Aliases: new[] { "porte" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => "Une porte pleine, equipee d'un lecteur de badge."),

        [ObjectId.BedroomBin] = new ObjectDef(
            Id: ObjectId.BedroomBin,
            Name: "la corbeille",
            Aliases: new[] { "corbeille", "poubelle" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: DescribeBin),

        [ObjectId.GarbageBag] = new ObjectDef(
            Id: ObjectId.GarbageBag,
            Name: "le sac poubelle",
            Aliases: new[] { "sac", "sac poubelle" },
            Portable: true,
            InitialLocation: Location.InRoom(RoomId.Bedroom),
            InitialState: ObjectState.None,
            Describe: _ => "Un sac poubelle, vide."),
    };

    private static string DescribeWardrobe(GameState state) =>
        state.StateOf(ObjectId.BedroomWardrobe) == ObjectState.Open
            ? "L'armoire est ouverte : elle contient des vetements et un badge."
            : "Une armoire en contreplaqué, de modele hospitalier standard.";

    private static string DescribeReader(GameState state) =>
        state.StateOf(ObjectId.BedroomReader) == ObjectState.Unlocked
            ? "Un lecteur de badge est fixé près de la porte. Son voyant est vert."
            : "Un lecteur de badge est fixé près de la porte. Son voyant est rouge.";

    private static string DescribeBin(GameState state) =>
        state.Locations.TryGetValue(ObjectId.GarbageBag, out var location)
        && location.Kind == LocationKind.Room
        && location.Room == RoomId.Bedroom
            ? "Une corbeille. Elle ne contient qu'un sac poubelle vide."
            : "Une corbeille, vide.";
}
