using Releve.Engine;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Tests.World;

/// <summary>Petit monde de test, independant du contenu reel (§11).</summary>
public static class TestWorldFactory
{
    public static WorldModel Create() => new(
        startRoom: RoomId.TestRoomStart,
        rooms: new Dictionary<RoomId, RoomBase>
        {
            [RoomId.TestRoomStart] = new TestRoomStart(),
            [RoomId.TestRoomVault] = new TestRoomVault(),
            [RoomId.TestRoomWall] = new TestRoomWall(),
        },
        objects: TestObjects.Create());

    public static GameEngine CreateEngine()
    {
        var world = Create();
        return new GameEngine(world, world.CreateInitialState());
    }
}

/// <summary>Objets du monde de test.</summary>
public static class TestObjects
{
    public static IReadOnlyDictionary<ObjectId, ObjectDef> Create() => new Dictionary<ObjectId, ObjectDef>
    {
        [ObjectId.TestKey] = new ObjectDef(
            Id: ObjectId.TestKey,
            Name: "petite clé",
            Aliases: new[] { "clef", "cle", "petite clef" },
            Portable: true,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.None,
            Describe: _ => "Une petite clé de fer, à panse ronde."),

        [ObjectId.TestCabinet] = new ObjectDef(
            Id: ObjectId.TestCabinet,
            Name: "armoire",
            Aliases: new[] { "armoire", "placard" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.Locked,
            Describe: s => s.StateOf(ObjectId.TestCabinet) == ObjectState.Locked
                ? "L’armoire est fermée à clé. Sa serrure est en métal clair."
                : "L’armoire est déverrouillée. Le battant reste clos."),

        [ObjectId.TestChest] = new ObjectDef(
            Id: ObjectId.TestChest,
            Name: "coffre",
            Aliases: new[] { "coffre" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.Closed,
            Describe: s => s.StateOf(ObjectId.TestChest) == ObjectState.Open
                ? "Le coffre est ouvert sur un fond de velours."
                : "Le coffre est fermé."),

        [ObjectId.TestBook] = new ObjectDef(
            Id: ObjectId.TestBook,
            Name: "livre ancien",
            Aliases: new[] { "livre ancien" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.None,
            Describe: _ => "Un livre ancien à la couverture de toile noire."),

        [ObjectId.TestAccountBook] = new ObjectDef(
            Id: ObjectId.TestAccountBook,
            Name: "livre de comptes",
            Aliases: new[] { "livre de comptes" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.None,
            Describe: _ => "Un registre relié, couvert d’une écriture fine."),

        [ObjectId.TestHiddenNote] = new ObjectDef(
            Id: ObjectId.TestHiddenNote,
            Name: "carnet",
            Aliases: new[] { "carnet" },
            Portable: true,
            InitialLocation: Location.InRoom(RoomId.TestRoomStart),
            InitialState: ObjectState.None,
            Describe: _ => "Un carnet de poche, glissé au fond du coffre.",
            VisibleWhen: s => s.StateOf(ObjectId.TestChest) == ObjectState.Open),
    };
}

/// <summary>Piece de depart du monde de test.</summary>
public sealed class TestRoomStart : RoomBase
{
    private const string TitleText = "Salle de départ";
    private const string DescriptionText =
        "Une salle carrée. Une armoire est posée contre le mur nord, un coffre repose au pied d’un banc. À l’est, un couloir effondré.";
    private const string NorthBlockedMessage =
        "Une porte de pierre bloque le passage nord. Sa serrure est bien fermée.";
    private const string EastBlockedMessage =
        "Le couloir est bouché par un éboulis.";
    private const string KeyUsedOnCabinet = "La clé tourne : la serrure de l’armoire cède.";
    private const string KeyUsedOnOpenCabinet = "La serrure est déjà ouverte.";
    private const string OtherUsedOnCabinet = "Rien ne se produit.";

    public override RoomId Id => RoomId.TestRoomStart;

    public override string Title => TitleText;

    public override string ImageKey(GameState s) => "test_start";

    public override string Description(GameState s) => DescriptionText;

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.North, RoomId.TestRoomVault,
            IsOpen: s => s.StateOf(ObjectId.TestCabinet) != ObjectState.Locked,
            BlockedMessage: NorthBlockedMessage),
        new Exit(Direction.East, null, BlockedMessage: EastBlockedMessage),
    };

    public override CommandResult? Handle(Command c, GameState s)
    {
        if (c.Action != GameAction.Use || c.Target != ObjectId.TestCabinet)
        {
            return null;
        }

        if (c.Instrument != ObjectId.TestKey)
        {
            return CommandResult.Say(OtherUsedOnCabinet);
        }

        if (s.StateOf(ObjectId.TestCabinet) != ObjectState.Locked)
        {
            return CommandResult.Say(KeyUsedOnOpenCabinet);
        }

        s.States[ObjectId.TestCabinet] = ObjectState.Unlocked;
        return CommandResult.Changed(KeyUsedOnCabinet);
    }
}

/// <summary>Piece au nord, avec un retour et une sortie a sens unique.</summary>
public sealed class TestRoomVault : RoomBase
{
    private const string TitleText = "Chambre forte";
    private const string DescriptionText = "Une petite chambre forte aux parois de métal.";

    public override RoomId Id => RoomId.TestRoomVault;

    public override string Title => TitleText;

    public override string ImageKey(GameState s) => "test_vault";

    public override string Description(GameState s) => DescriptionText;

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.South, RoomId.TestRoomStart),
        // Sortie sans retour : volontairement absente cote destination.
        new Exit(Direction.East, RoomId.TestRoomWall, OneWay: true),
    };
}

/// <summary>Piece sans aucune sortie.</summary>
public sealed class TestRoomWall : RoomBase
{
    private const string TitleText = "Cul-de-sac";
    private const string DescriptionText = "Un cul-de-sac. Le béton reprend des deux côtés.";

    public override RoomId Id => RoomId.TestRoomWall;

    public override string Title => TitleText;

    public override string ImageKey(GameState s) => "test_wall";

    public override string Description(GameState s) => DescriptionText;

    public override IReadOnlyList<Exit> Exits { get; } = Array.Empty<Exit>();
}
