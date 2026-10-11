using Releve.Engine.Core;

namespace Releve.Engine.World;

/// <summary>Pieces du monde. Les membres marques « test » servent uniquement aux tests (§11).</summary>
public enum RoomId
{
    // Contenu reel (document PoC §2) :
    Bedroom,
    TechCorridor,
    HospitalLobby,
    Street,

    // Pieces utilisees uniquement par le monde de test :
    TestRoomStart,
    TestRoomVault,
    TestRoomWall,
}

/// <summary>Objets du monde. Les membres marques « test » servent uniquement aux tests (§11).</summary>
public enum ObjectId
{
    // Chambre de reveil (PoC §3.1)
    BedroomBed,
    BedroomMachines,
    BedroomWardrobe,
    ChildClothes,
    Badge,
    BedroomReader,
    BedroomDoor,
    BedroomBin,
    GarbageBag,

    // Couloir technique (PoC §3.2)
    StairsDoor,
    StairsReader,
    TechDoor,
    TechReader,

    // Accueil de l'hopital (PoC §3.3)
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
    // Contenu reel (PoC §2)
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
