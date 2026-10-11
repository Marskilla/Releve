using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Objects;

/// <summary>Objets de l'accueil de l'hopital (PoC §3.3). Aucun n'est portable.</summary>
public static class HospitalLobbyObjects
{
    public static IReadOnlyDictionary<ObjectId, ObjectDef> Create() => new Dictionary<ObjectId, ObjectDef>
    {
        [ObjectId.Android] = new ObjectDef(
            Id: ObjectId.Android,
            Name: "l’androïde",
            Aliases: new[] { "androide", "android", "robot" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ =>
                "Un androïde à l’allure humanoïde sobre, immobile derrière le comptoir, en position d’attente."),

        [ObjectId.Terminal] = new ObjectDef(
            Id: ObjectId.Terminal,
            Name: "la borne",
            Aliases: new[] { "borne", "borne interactive", "terminal" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ => "Une borne interactive équipée d’un lecteur de badge."),

        [ObjectId.Display] = new ObjectDef(
            Id: ObjectId.Display,
            Name: "l’afficheur",
            Aliases: new[] { "afficheur", "date", "affichage" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ => "L’afficheur indique : 9 octobre 2173."),

        [ObjectId.Wheelchairs] = new ObjectDef(
            Id: ObjectId.Wheelchairs,
            Name: "les fauteuils et brancards",
            Aliases: new[] { "fauteuil", "fauteuils", "fauteuil roulant", "brancard", "brancards" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ =>
                "Des fauteuils roulants et des brancards sont alignés à leurs bornes de recharge, tous présents, "
                + "comme prêts à accueillir des patients."),

        [ObjectId.GlassDoor] = new ObjectDef(
            Id: ObjectId.GlassDoor,
            Name: "la porte vitrée",
            Aliases: new[] { "porte", "porte vitree", "vitre", "rue" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ =>
                "À travers la porte vitrée, on voit une rue sans véhicule ni passant, gagnée par une végétation spontanée."),

        [ObjectId.Plants] = new ObjectDef(
            Id: ObjectId.Plants,
            Name: "les plantes",
            Aliases: new[] { "plante", "plantes" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ => "Quelques plantes en pot, vertes et soignées."),

        [ObjectId.Elevator] = new ObjectDef(
            Id: ObjectId.Elevator,
            Name: "l’ascenseur",
            Aliases: new[] { "ascenseur" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ =>
                "Les portes métalliques d’un ascenseur, avec un lecteur de badge et un unique bouton."),

        [ObjectId.ElevatorReader] = new ObjectDef(
            Id: ObjectId.ElevatorReader,
            Name: "le lecteur de l’ascenseur",
            Aliases: new[] { "lecteur", "lecteur ascenseur" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.Locked,
            Describe: _ => "Le voyant du lecteur est rouge."),

        [ObjectId.ElevatorButton] = new ObjectDef(
            Id: ObjectId.ElevatorButton,
            Name: "le bouton",
            Aliases: new[] { "bouton" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.HospitalLobby),
            InitialState: ObjectState.None,
            Describe: _ => "Un bouton « monter »."),
    };
}
