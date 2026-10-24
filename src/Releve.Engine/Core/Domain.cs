using Releve.Engine.World;

namespace Releve.Engine.Core;

/// <summary>Actions reconnues par le moteur.</summary>
public enum GameAction { Look, Take, Drop, Open, Close, Use, Talk, Read, Go, Inventory }

/// <summary>Directions cardinales des sorties.</summary>
public enum Direction { North, South, East, West }

/// <summary>Etat d'un objet (serrure, coffre, mecanisme).</summary>
public enum ObjectState { None, Closed, Open, Locked, Unlocked, Activated }

/// <summary>Etat d'affichage d'une direction : calcule, jamais stocke.</summary>
public enum ExitState { Absent, Blocked, Open }

/// <summary>Nature de l'emplacement d'un objet.</summary>
public enum LocationKind { Room, Inventory, Nowhere }

/// <summary>
/// Commande normalisee. Convention pour Use avec deux complements :
/// le premier nom est l'instrument, le second est la cible.
/// </summary>
public sealed record Command(
    GameAction Action,
    ObjectId? Target = null,
    ObjectId? Instrument = null,
    Direction? Direction = null);

/// <summary>Emplacement courant d'un objet.</summary>
public readonly record struct Location(LocationKind Kind, RoomId? Room = null)
{
    public static Location InRoom(RoomId r) => new(LocationKind.Room, r);
    public static Location Inventory => new(LocationKind.Inventory);
    public static Location Nowhere => new(LocationKind.Nowhere);
}

/// <summary>Objet defini une seule fois : definition immuable.</summary>
public sealed record ObjectDef(
    ObjectId Id,
    string Name,
    string[] Aliases,
    bool Portable,
    Location InitialLocation,
    ObjectState InitialState,
    Func<GameState, string> Describe,
    Func<GameState, bool>? VisibleWhen = null);

/// <summary>
/// Sortie appartenant a la piece de depart. <see cref="To"/> nul = fausse piste (toujours bloquee).
/// <see cref="Words"/> liste optionnelle de mots de sortie (normalisés) franchissant cette sortie.
/// </summary>
public sealed record Exit(
    Direction Dir,
    RoomId? To,
    Func<GameState, bool>? IsOpen = null,
    string? BlockedMessage = null,
    bool OneWay = false,
    IReadOnlyList<string>? Words = null);

/// <summary>Resultat d'une commande : messages a afficher, avec indication de changement d'etat.</summary>
public sealed record CommandResult(IReadOnlyList<string> Messages, bool StateChanged = false)
{
    public static CommandResult Say(params string[] m) => new(m);
    public static CommandResult Changed(params string[] m) => new(m, true);
}

/// <summary>Ligne d'inventaire.</summary>
public sealed record InventoryItem(ObjectId Id, string Name);

/// <summary>Zone d'image : rectangle source [X1,X2) x [Y1,Y2) et condition d'activation.</summary>
public sealed record ImagePatch(int X1, int Y1, int X2, int Y2, Func<GameState, bool> IsActive);

/// <summary>Rectangle d'image (coordonnées source).</summary>
public readonly record struct ImageRect(int X1, int Y1, int X2, int Y2);

/// <summary>Instantane de l'etat a afficher, relu par la couche Game a chaque image.</summary>
public sealed record RoomView(
    RoomId Room,
    string Title,
    string ImageKey,
    IReadOnlyDictionary<Direction, ExitState> Exits,
    IReadOnlyList<InventoryItem> Inventory,
    IReadOnlyList<ImageRect> ActivePatches)
{
    // Constructeur de compatibilité pour les appels existants sans ActivePatches
    public RoomView(RoomId room, string title, string imageKey,
        IReadOnlyDictionary<Direction, ExitState> exits, IReadOnlyList<InventoryItem> inventory)
        : this(room, title, imageKey, exits, inventory, Array.Empty<ImageRect>())
    {
    }
}
