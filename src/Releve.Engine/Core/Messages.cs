namespace Releve.Engine.Core;

/// <summary>Messages neutres du moteur (pas de texte de piece : ils sont dans chaque classe de piece).</summary>
public static class Messages
{
    public const string UnknownVerb = "Cette commande n’est pas comprise.";
    public const string MissingObject = "Précisez l’objet de la commande.";
    public const string UnknownObject = "Vous ne voyez pas cela ici.";
    public const string AmbiguousPrefix = "Précisez : ";
    public const string CannotGo = "Vous ne pouvez pas aller par là.";
    public const string NotCarried = "Vous ne l’avez pas sur vous.";
    public const string Taken = "Pris.";
    public const string CannotTake = "Vous ne pouvez pas prendre cela.";
    public const string Dropped = "Posé.";
    public const string CannotDrop = "Vous ne portez pas cela.";
    public const string NothingHappens = "Rien ne se passe.";
    public const string EmptyInventory = "Vous ne portez rien.";
    public const string UseWithoutInstrument = "Précisez sur quoi l’utiliser.";
    public const string NothingToRead = "Il n’y a rien à lire ici.";
}

