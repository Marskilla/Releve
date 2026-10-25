using Releve.Engine.Content.Objects;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Accueil de l'hopital (lore §5.3, PoC §3.3).</summary>
public sealed class HospitalLobby : RoomBase
{
    private const string RoomTitle = "Accueil de l'hopital";
    private const string RoomDescription =
        "Le hall d'accueil est vaste, lumineux et parfaitement entretenu. Derriere le comptoir, un androide se tient immobile. Un afficheur lumineux est fixe au-dessus du comptoir.";
    private const string WestBlockedMessage =
        "La porte vitree de l'entree est fermee. L'androide s'adresse alors a vous : « S'il vous plait ! » Il semble vous inviter a venir le voir.";
    private const string NorthBlockedMessage =
        "Les portes de l'ascenseur restent closes. Le lecteur de badge affiche un rouge fixe.";
    private const string AndroidAnswerBeforeActivation =
        "L'androide, qui semblait etre en pause, prend vie et s'adresse a vous : « La politique de l'etablissement nous empeche de laisser sortir des patients sans emargement precedent. Passez votre badge sur la borne du comptoir pour emarger, merci. »";
    private const string AndroidAnswerAfterActivation =
        "L'androide s'anime : « Merci d'avoir fait appel a nos services, et j'espere, pour vous, ne pas vous revoir bientot. Ah ah ah ! »";
    private const string TerminalUnlockedMessage = "La porte vitree de l'entree se deverrouille.";
    private const string ElevatorReaderRefuses = "Le lecteur refuse le badge.";
    private const string ButtonDoesNothing = "Le bouton ne produit aucun effet.";
    // J8-6: messages pour le dialogue de la borne
    private const string BadgeOnTerminalMessage =
        "Vous passez le badge devant la borne. La fiche patient s'affiche.";
    private const string AcceptMessage =
        "Vous validez la sortie. La porte vitree de l'entree se deverrouille.";
    private const string RefuseMessage =
        "Vous ne validez pas la sortie.";
    // OpenGlassDoorLocked réutilise WestBlockedMessage (J1.2)
    // OpenGlassDoorUnlocked réutilise TerminalUnlockedMessage (J1.2)
    private const string OpenElevator = NorthBlockedMessage; // réutilise la constante (J1.2)

    // Coordonnées zones d'image (J5) - image 1774x887
    private const int GlassDoorX1 = 88, GlassDoorY1 = 150, GlassDoorX2 = 272, GlassDoorY2 = 668;

    // Fiche patient : texte du lore, repris a l'identique (PoC §3.3).
    private static readonly string[] PatientRecord =
    {
        "FICHE PATIENT",
        "Nom : Marchand - Prenom : Alex",        
        "Date d'entree : 14 mars 2113",
        "Motif d'admission : maladie inconnue evolutive necessitant un traitement adaptatif",
        "Conditions de sortie : maladie en remission complete",
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
        // Reponse de l'androïde selon l'etat de la borne.
        if (c.Action == GameAction.Talk && c.Target == ObjectId.Android)
        {
            var terminalActivated = s.StateOf(ObjectId.Terminal) == ObjectState.Activated;
            return CommandResult.Say(terminalActivated ? AndroidAnswerAfterActivation : AndroidAnswerBeforeActivation);
        }

        if (c.Action == GameAction.Use)
        {
            if (c.Target == ObjectId.Terminal && c.Instrument == ObjectId.Badge)
            {
                var terminalActivated = s.StateOf(ObjectId.Terminal) == ObjectState.Activated;
                
                // Construire la fiche patient
                var patientRecord = string.Join("\n", PatientRecord);
                
                DialogRequest dialog;
                if (terminalActivated)
                {
                    // Deja active : un seul choix Close
                    dialog = new DialogRequest(
                        DialogKind.Text,
                        null,
                        patientRecord,
                        new[] { new DialogChoice(DialogIds.Close, "Fermer") },
                        DialogIds.Close,
                        DialogIds.Close);
                }
                else
                {
                    // Pas encore active : deux choix PatientAccept / PatientRefuse
                    dialog = new DialogRequest(
                        DialogKind.Text,
                        null,
                        patientRecord,
                        new[]
                        {
                            new DialogChoice(DialogIds.PatientAccept, "J'accepte de sortir"),
                            new DialogChoice(DialogIds.PatientRefuse, "Je ne suis pas pret a sortir")
                        },
                        DialogIds.PatientRefuse,
                        DialogIds.PatientRefuse);
                }

                // J8-6: Say (pas Changed) + dialog. La fiche n'est plus dans la console.
                // L'etat de Terminal n'est PAS modifie ici.
                return CommandResult.Say(BadgeOnTerminalMessage).WithDialog(dialog);
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

        // « ouvre porte vitree » : meme message que la sortie bloquee si borne non activee.
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

        // « lis afficheur » / « regarde afficheur » / « examine horloge » : date du jour.
        if ((c.Action == GameAction.Read || c.Action == GameAction.Look) && c.Target == ObjectId.Display)
        {
            return CommandResult.Say(HospitalLobbyObjects.DisplayDescription);
        }

        return null;
    }

    private static bool IsWestOpen(GameState s) =>
        s.StateOf(ObjectId.Terminal) == ObjectState.Activated;
    
    /// <summary>J8-7: Reaction aux choix du dialogue de la borne.</summary>
    public override CommandResult? OnDialogChoice(string choiceId, GameState s)
    {
        return choiceId switch
        {
            DialogIds.PatientAccept => s.StateOf(ObjectId.Terminal) != ObjectState.Activated
                ? (s.States[ObjectId.Terminal] = ObjectState.Activated, CommandResult.Changed(AcceptMessage)).Item2
                : CommandResult.Say(), // deja active : aucun effet
            DialogIds.PatientRefuse => CommandResult.Say(RefuseMessage),
            DialogIds.Close => CommandResult.Say(),
            _ => null,
        };
    }
}
