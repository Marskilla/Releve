using Releve.Engine.Core;

namespace Releve.Engine.World;

/// <summary>Pièces du monde. Les membres marqués « test » servent uniquement aux tests.</summary>
public enum RoomId
{
    // Contenu réel :
    Bedroom,
    TechCorridor,
    HospitalLobby,
    Street,

    // Pieces utilisees uniquement par le monde de test :
    TestRoomStart,
    TestRoomVault,
    TestRoomWall,
}

/// <summary>Objets du monde. Les membres marqués « test » servent uniquement aux tests.</summary>
public enum ObjectId
{
    // Chambre de réveil
    BedroomBed,
    BedroomMachines,
    BedroomWardrobe,
    ChildClothes,
    Badge,
    BedroomReader,
    BedroomDoor,
    BedroomBin,
    GarbageBag,

    // Couloir technique
    StairsDoor,
    StairsReader,
    TechDoor,
    TechReader,

    // Accueil de l'hôpital
    Android,
    Terminal,
    Display,
    Wheelchairs,
    GlassDoor,
    Plants,
    Elevator,
    ElevatorReader,
    ElevatorButton,

    // Objets utilises uniquement par le monde de test :
    TestKey,
    TestCabinet,
    TestChest,
    TestBook,
    TestAccountBook,
    TestHiddenNote,
}

/// <summary>Evenements uniques (annonce deja jouee, mecanisme deja actionne...).</summary>
public enum GameEvent
{
    // Contenu réel
    WakeAnnouncementDone,

    TestAnnouncePlayed,
}

/// <summary>Courants d'affichage inverse.</summary>
public static class Directions
{
    public static Direction Opposite(this Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(dir)),
    };
}

/// <summary>Identifiants des choix de dialogue.</summary>
public static class DialogIds
{
    public const string Close = "Close";
    public const string PatientAccept = "PatientAccept";
    public const string PatientRefuse = "PatientRefuse";
}
