using Releve.Engine.Core;

namespace Releve.Engine.World;

/// <summary>
/// Classe de piece sans etat propre : toutes les methodes recoivent l'etat en parametre.
/// Les comportements specifiques au lieu vivent dans <see cref="Handle"/>.
/// </summary>
public abstract class RoomBase
{
    public abstract RoomId Id { get; }

    public abstract string Title { get; }

    /// <summary>Cle d'image (variante possible selon l'etat).</summary>
    public abstract string ImageKey(GameState s);

    /// <summary>Texte affiche a l'entree.</summary>
    public abstract string Description(GameState s);

    /// <summary>Messages d'introduction (uniques, avant la description).</summary>
    public virtual IEnumerable<string> IntroMessages(GameState s) => Array.Empty<string>();

    /// <summary>Description detaillee (pour « regarde » sans cible). Null = pas de description detaillee.</summary>
    public virtual string? DetailedDescription(GameState s) => null;

    /// <summary>Zones d'image actives selon l'etat.</summary>
    public virtual IReadOnlyList<ImagePatch> ImagePatches => Array.Empty<ImagePatch>();

    /// <summary>Messages d'entree supplementaires (apres description).</summary>
    public virtual IEnumerable<string> OnEnter(GameState s) => Array.Empty<string>();

    public abstract IReadOnlyList<Exit> Exits { get; }

    /// <summary>Resultat propre a la piece, ou null = comportements par defaut.</summary>
    public virtual CommandResult? Handle(Command c, GameState s) => null;

    /// <summary>Reaction a un choix de dialogue modal (J8). Renvoie un resultat ou null (inconnu/sans effet).</summary>
    public virtual CommandResult? OnDialogChoice(string choiceId, GameState s) => null;
}

/// <summary>Point de construction unique : assemble toutes les pieces et tous les objets.</summary>
public sealed class World
{
    public World(RoomId startRoom, IReadOnlyDictionary<RoomId, RoomBase> rooms, IReadOnlyDictionary<ObjectId, ObjectDef> objects)
    {
        StartRoom = startRoom;
        Rooms = rooms;
        Objects = objects;
    }

    public RoomId StartRoom { get; }

    public IReadOnlyDictionary<RoomId, RoomBase> Rooms { get; }

    public IReadOnlyDictionary<ObjectId, ObjectDef> Objects { get; }

    /// <summary>Etat initial : piece de depart, emplacement et etat initiaux de chaque objet.</summary>
    public GameState CreateInitialState()
    {
        var state = new GameState { CurrentRoom = StartRoom };
        foreach (var (id, def) in Objects)
        {
            state.Locations[id] = def.InitialLocation;
            if (def.InitialState != ObjectState.None)
            {
                state.States[id] = def.InitialState;
            }
        }

        return state;
    }
}
