using Releve.Engine.Core;

namespace Releve.Engine.Parsing;

/// <summary>Tables du moteur : verbes et directions reconnus (formes deja normalisees).</summary>
public static class Vocabulary
{
    public static readonly IReadOnlyDictionary<string, GameAction> Verbs = new Dictionary<string, GameAction>(StringComparer.Ordinal)
    {
        // Look
        ["regarde"] = GameAction.Look,
        ["regarder"] = GameAction.Look,
        ["examine"] = GameAction.Look,
        ["examiner"] = GameAction.Look,
        ["observe"] = GameAction.Look,
        ["observer"] = GameAction.Look,
        ["inspecte"] = GameAction.Look,
        ["inspecter"] = GameAction.Look,
        // Take
        ["prends"] = GameAction.Take,
        ["prend"] = GameAction.Take,
        ["prendre"] = GameAction.Take,
        ["prenez"] = GameAction.Take,
        ["prennez"] = GameAction.Take,
        ["ramasse"] = GameAction.Take,
        ["ramasser"] = GameAction.Take,
        ["saisis"] = GameAction.Take,
        ["saisir"] = GameAction.Take,
        // Drop
        ["pose"] = GameAction.Drop,
        ["poser"] = GameAction.Drop,
        ["depose"] = GameAction.Drop,
        ["deposer"] = GameAction.Drop,
        ["lache"] = GameAction.Drop,
        ["lacher"] = GameAction.Drop,
        ["jette"] = GameAction.Drop,
        ["jeter"] = GameAction.Drop,
        // Open
        ["ouvre"] = GameAction.Open,
        ["ouvrir"] = GameAction.Open,
        ["ouvrez"] = GameAction.Open,
        // Close
        ["ferme"] = GameAction.Close,
        ["fermer"] = GameAction.Close,
        ["fermez"] = GameAction.Close,
        // Use
        ["utilise"] = GameAction.Use,
        ["utiliser"] = GameAction.Use,
        ["utilisez"] = GameAction.Use,
        ["passe"] = GameAction.Use,
        ["passer"] = GameAction.Use,
        ["applique"] = GameAction.Use,
        ["appliquer"] = GameAction.Use,
        ["appuie"] = GameAction.Use,
        ["appuyer"] = GameAction.Use,
        ["pousse"] = GameAction.Use,
        ["pousser"] = GameAction.Use,
        ["actionne"] = GameAction.Use,
        ["actionner"] = GameAction.Use,
        // Talk
        ["parle"] = GameAction.Talk,
        ["parler"] = GameAction.Talk,
        ["discute"] = GameAction.Talk,
        ["discuter"] = GameAction.Talk,
        ["interroge"] = GameAction.Talk,
        ["interroger"] = GameAction.Talk,
        ["demande"] = GameAction.Talk,
        ["demander"] = GameAction.Talk,
        // Read
        ["lis"] = GameAction.Read,
        ["lire"] = GameAction.Read,
        ["lisez"] = GameAction.Read,
        // Go
        ["va"] = GameAction.Go,
        ["vas"] = GameAction.Go,
        ["aller"] = GameAction.Go,
        ["allez"] = GameAction.Go,
        ["avance"] = GameAction.Go,
        ["avancer"] = GameAction.Go,
        ["sors"] = GameAction.Go,
        ["sortir"] = GameAction.Go,
        ["monte"] = GameAction.Go,
        ["monter"] = GameAction.Go,
        ["descends"] = GameAction.Go,
        ["descendre"] = GameAction.Go,
        ["entre"] = GameAction.Go,
        ["entrer"] = GameAction.Go,
        ["rentre"] = GameAction.Go,
        ["rentrer"] = GameAction.Go,
        ["retourne"] = GameAction.Go,
        ["retourner"] = GameAction.Go,
        // Inventory
        ["inventaire"] = GameAction.Inventory,
        ["inv"] = GameAction.Inventory,
        ["i"] = GameAction.Inventory,
    };

    public static readonly IReadOnlyDictionary<string, Direction> Directions = new Dictionary<string, Direction>(StringComparer.Ordinal)
    {
        ["nord"] = Direction.North,
        ["n"] = Direction.North,
        ["sud"] = Direction.South,
        ["s"] = Direction.South,
        ["est"] = Direction.East,
        ["e"] = Direction.East,
        ["ouest"] = Direction.West,
        ["o"] = Direction.West,
    };

    /// <summary>Mots qui declenchent la fermeture du jeu (interceptes par la couche Game).</summary>
    public static readonly IReadOnlySet<string> QuitWords = new HashSet<string>(StringComparer.Ordinal)
    {
        "quitter",
        "quitte",
        "quittes",
    };
}
