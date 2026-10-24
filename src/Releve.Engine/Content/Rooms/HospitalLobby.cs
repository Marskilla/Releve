using Releve.Engine.Content.Objects;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Accueil de l’hôpital (lore §5.3, PoC §3.3).</summary>
public sealed class HospitalLobby : RoomBase
{
    private const string RoomTitle = "Accueil de l’hôpital";
    private const string RoomDescription =
        "Le hall d’accueil est vaste, lumineux et parfaitement entretenu. Derrière le comptoir, un androïde se tient immobile.";
    private const string WestBlockedMessage = "La porte vitrée de l’entrée reste fermée.";
    private const string NorthBlockedMessage =
        "Les portes de l’ascenseur restent closes. Le lecteur de badge affiche un rouge fixe.";
    private const string AndroidAnswer =
        "L’androïde s’anime : « Veuillez passer votre badge sur la borne pour sortir. »";
    private const string TerminalUnlockedMessage = "La porte vitrée de l’entrée se déverrouille.";
    private const string ElevatorReaderRefuses = "Le lecteur refuse le badge.";
    private const string ButtonDoesNothing = "Le bouton ne produit aucun effet.";
    // OpenGlassDoorLocked réutilise WestBlockedMessage (J1.2)
    // OpenGlassDoorUnlocked réutilise TerminalUnlockedMessage (J1.2)
    private const string OpenElevator = NorthBlockedMessage; // réutilise la constante (J1.2)

    // Coordonnées zones d'image (J5) - image 1774x887
    private const int GlassDoorX1 = 88, GlassDoorY1 = 150, GlassDoorX2 = 272, GlassDoorY2 = 668;

    // Fiche patient : texte du lore, repris a l'identique (PoC §3.3).
    private static readonly string[] PatientRecord =
    {
        "FICHE PATIENT",
        "Nom : Marchand",
        "Prénom : Alex",
        "Date d’entrée : 14 mars 2113",
        "Motif d’admission : maladie inconnue évolutive nécessitant un traitement adaptatif",
        "Conditions de sortie : maladie en rémission complète",
        "Adresse de sortie : Alex Marchand, chez M. et Mme Marchand, 12 rue des Tilleuls",
    };

    public override RoomId Id => RoomId.HospitalLobby;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "HospitalLobby";

    public override string Description(GameState s) => RoomDescription;

    /// <summary>Zones d'image actives selon l'etat (J5).</summary>
    public override IReadOnlyList<ImagePatch> ImagePatches => new[]
    {
        // Porte vitrée de sortie ouverte
        new ImagePatch(GlassDoorX1, GlassDoorY1, GlassDoorX2, GlassDoorY2,
            s => s.StateOf(ObjectId.Terminal) == ObjectState.Activated),
    };

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.South, RoomId.TechCorridor, Words: new[] { "descends", "descendre" }),
        new Exit(Direction.West, RoomId.Street, IsOpen: IsWestOpen, BlockedMessage: WestBlockedMessage,
            Words: new[] { "sors", "sortir", "rue" }),
        // Fausse piste : l'ascenseur n'ouvre sur aucune zone de jeu.
        new Exit(Direction.North, null, BlockedMessage: NorthBlockedMessage,
            Words: new[] { "monte", "monter", "ascenseur" }),
    };

    public override CommandResult? Handle(Command c, GameState s)
    {
        // Seule reponse de l'androïde, quelle que soit la forme de la demande.
        if (c.Action == GameAction.Talk && c.Target == ObjectId.Android)
        {
            return CommandResult.Say(AndroidAnswer);
        }

        if (c.Action == GameAction.Use)
        {
            if (c.Target == ObjectId.Terminal && c.Instrument == ObjectId.Badge)
            {
                var first = s.StateOf(ObjectId.Terminal) != ObjectState.Activated;
                s.States[ObjectId.Terminal] = ObjectState.Activated;

                // Repeter l'action reaffiche la fiche, sans le message de deverrouillage.
                return first
                    ? CommandResult.Changed(WithUnlockMessage())
                    : CommandResult.Say(PatientRecord);
            }

            if (c.Target == ObjectId.ElevatorReader && c.Instrument == ObjectId.Badge)
            {
                return CommandResult.Say(ElevatorReaderRefuses);
            }

            if (c.Target == ObjectId.ElevatorButton)
            {
                return CommandResult.Say(ButtonDoesNothing);
            }
        }

        // « ouvre porte vitree ».
        if (c.Action == GameAction.Open && c.Target == ObjectId.GlassDoor)
        {
            if (s.StateOf(ObjectId.Terminal) != ObjectState.Activated)
            {
                return CommandResult.Say(WestBlockedMessage);
            }
            return CommandResult.Say(TerminalUnlockedMessage);
        }

        // « ouvre ascenseur ».
        if (c.Action == GameAction.Open && c.Target == ObjectId.Elevator)
        {
            return CommandResult.Say(NorthBlockedMessage);
        }

        // « lis afficheur » : même texte que Describe.
        if (c.Action == GameAction.Read && c.Target == ObjectId.Display)
        {
            return CommandResult.Say(HospitalLobbyObjects.DisplayDescription);
        }

        return null;
    }

    private static string[] WithUnlockMessage() => PatientRecord.Append(TerminalUnlockedMessage).ToArray();

    private static bool IsWestOpen(GameState s) =>
        s.StateOf(ObjectId.Terminal) == ObjectState.Activated;
}
