namespace Releve.Engine.Core;

/// <summary>Textes de saveur pour les réponses variées (J4).</summary>
public static class FlavourText
{
    /// <summary>Réponses neutres à une commande incomprise (8).</summary>
    public static readonly string[] UnknownNeutral =
    {
        "Commande non comprise.",
        "Je ne saisis pas.",
        "Pas compris.",
        "Je ne comprends pas cette commande.",
        "Hein ? Pardon ?",
        "Ça ne me dit rien.",
        "Essayez de formuler ça autrement.",
        "Cette commande m’est inconnue."
    };

    /// <summary>Réponses humoristiques à une commande incomprise (6).</summary>
    public static readonly string[] UnknownHumour =
    {
        "Vous avez un problème ? Vous voulez qu’on en parle ?",
        "Mais que me dites-vous là…",
        "On se calme, hein ? Essayez de formuler ça autrement.",
        "Mes circuits ont beau chercher, rien ne vient.",
        "Désolé, mon vocabulaire a des limites… et vous venez de les trouver.",
        "Intéressant. Incompréhensible, mais intéressant."
    };

    /// <summary>Réponses aux grossièretés (10).</summary>
    public static readonly string[] ProfanityReplies =
    {
        "Surveillez votre langage !",
        "Si votre maman savait ça…",
        "Tsk, tsk, tsk… que c’est vulgaire.",
        "Jean-Marie Bigard, sors de ce corps !",
        "Hou là là, quel vilain langage.",
        "Voilà qui n’est pas très distingué.",
        "Allons, allons. Un peu de tenue !",
        "Je suis un programme bien élevé : je ne répondrai pas sur ce ton.",
        "Cette remarque sera transmise à qui de droit.",
        "Il y a peut-être des enfants qui jouent. Un peu de retenue !"
    };

    /// <summary>Probabilité d'utiliser une réponse humoristique (25%).</summary>
    public const double HumourProbability = 0.25;
}