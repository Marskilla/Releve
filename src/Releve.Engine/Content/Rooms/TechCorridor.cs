using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Couloir technique (lore §5.2, PoC §3.2).</summary>
public sealed class TechCorridor : RoomBase
{
    private const string RoomTitle = "Couloir technique";
    private const string RoomDescription =
        "Un couloir de service étroit, sans fenêtre. Quelques plafonniers sont faibles ou éteints ; une poussière légère recouvre le sol.";
    private const string NorthBlockedMessage =
        "La porte de l'escalier est fermee. Le lecteur de badge affiche un rouge fixe.";
    private const string EastBlockedMessage =
        "La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe.";
    private const string BadgeUsedOnStairsReader =
        "Le voyant passe au vert : la porte de l'escalier se deverrouille.";
    private const string StairsReaderAlreadyUnlocked =
        "Le lecteur de l'escalier est deja deverrouille.";
    private const string BadgeUsedOnTechReader =
        "Le lecteur refuse le badge. Son voyant clignote en rouge.";
    private const string OpenStairsDoorLocked =
        "La porte de l'escalier est fermee. Le lecteur de badge affiche un rouge fixe.";
    private const string OpenStairsDoorUnlocked = "La porte de l'escalier est deverrouillee.";
    private const string OpenTechDoor =
        "La porte du local technique est fermee. Le lecteur de badge affiche un rouge fixe.";

    // Description de la porte du local technique (reutilisee pour le dialogue Image)
    private const string TechDoorDescription =
        "Une porte pleine a hublot. A travers, on distingue les elements d'une chaufferie, "
        + "une rangee de lave-linge a l'arret depuis longtemps et des compteurs electriques.";

    // Coordonnées zones d'image (J5) - image 1774x887
    private const int StairsDoorX1 = 796, StairsDoorY1 = 205, StairsDoorX2 = 990, StairsDoorY2 = 566;
    private const int StairsReflectionX1 = 830, StairsReflectionY1 = 566, StairsReflectionX2 = 975, StairsReflectionY2 = 775;

    public override RoomId Id => RoomId.TechCorridor;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "TechCorridor";

    public override string Description(GameState s) => RoomDescription;

    /// <summary>Zones d'image actives selon l'etat (J5).</summary>
    public override IReadOnlyList<ImagePatch> ImagePatches => new[]
    {
        // Porte de l'escalier ouverte, panneau de sortie et lecteur associé
        new ImagePatch(StairsDoorX1, StairsDoorY1, StairsDoorX2, StairsDoorY2,
            s => s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked),
        // Reflet de la porte ouverte sur le sol
        new ImagePatch(StairsReflectionX1, StairsReflectionY1, StairsReflectionX2, StairsReflectionY2,
            s => s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked),
    };

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.West, RoomId.Bedroom, Words: new[] { "retourne", "retourner", "chambre" }),
        new Exit(Direction.North, RoomId.HospitalLobby, IsOpen: IsNorthOpen, BlockedMessage: NorthBlockedMessage,
            Words: new[] { "monte", "monter", "escalier" }),
        // Fausse piste : le local technique n'ouvre sur aucune zone de jeu.
        new Exit(Direction.East, null, BlockedMessage: EastBlockedMessage),
    };

    public override CommandResult? Handle(Command c, GameState s)
    {
        if (c.Action == GameAction.Use && c.Target == ObjectId.StairsReader)
        {
            if (c.Instrument != ObjectId.Badge)
            {
                return null; // autre instrument : comportement par defaut
            }

            if (s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked)
            {
                return CommandResult.Say(StairsReaderAlreadyUnlocked);
            }

            s.States[ObjectId.StairsReader] = ObjectState.Unlocked;
            return CommandResult.Changed(BadgeUsedOnStairsReader);
        }

        if (c.Action == GameAction.Use && c.Target == ObjectId.TechReader && c.Instrument == ObjectId.Badge)
        {
            return CommandResult.Say(BadgeUsedOnTechReader);
        }

        // « ouvre porte de l'escalier ».
        if (c.Action == GameAction.Open && c.Target == ObjectId.StairsDoor)
        {
            if (s.StateOf(ObjectId.StairsReader) == ObjectState.Locked)
            {
                return CommandResult.Say(OpenStairsDoorLocked);
            }
            return CommandResult.Say(OpenStairsDoorUnlocked);
        }

        // « ouvre porte du local technique ».
        if (c.Action == GameAction.Open && c.Target == ObjectId.TechDoor)
        {
            return CommandResult.Say(OpenTechDoor);
        }

        // J8-5: « regarde porte du local technique » / « regarde hublot » -> description + dialogue Image
        if (c.Action == GameAction.Look && (c.Target == ObjectId.TechDoor || c.Target == ObjectId.TechReader))
        {
            var dialog = new DialogRequest(
                DialogKind.Image,
                null,
                "technicalRoom",
                new[] { new DialogChoice(DialogIds.Close, "Fermer") },
                DialogIds.Close,
                DialogIds.Close);
            return CommandResult.Say(TechDoorDescription).WithDialog(dialog);
        }

        return null;
    }

    private static bool IsNorthOpen(GameState s) =>
        s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked;
}
