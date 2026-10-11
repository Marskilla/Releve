using Releve.Engine.Core;

namespace Releve.Engine.World;

/// <summary>Pieces du monde. Les membres marques « test » servent uniquement aux tests (§11).</summary>
public enum RoomId
{
    // Monde d'amorcage (§12.2), a supprimer a l'arrivee du contenu reel :
    SandboxA,
    SandboxB,

    // Pieces utilisees uniquement par le monde de test :
    TestRoomStart,
    TestRoomVault,
    TestRoomWall,
}

/// <summary>Objets du monde. Les membres marques « test » servent uniquement aux tests (§11).</summary>
public enum ObjectId
{
    // Monde d'amorcage (§12.2) :
    SandboxItem,
    SandboxSwitch,

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
    SandboxSwitchUnlocked,
    SandboxAnnouncePlayed,
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
