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
        "La porte de l’escalier est fermée. Le lecteur de badge affiche un rouge fixe.";
    private const string EastBlockedMessage =
        "La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe.";
    private const string BadgeUsedOnStairsReader =
        "Le voyant passe au vert : la porte de l’escalier se déverrouille.";
    private const string BadgeUsedOnTechReader =
        "Le lecteur refuse le badge. Son voyant clignote en rouge.";
    private const string OpenStairsDoorLocked =
        "La porte de l'escalier est fermée. Le lecteur de badge affiche un rouge fixe.";
    private const string OpenStairsDoorUnlocked = "La porte de l'escalier est déverrouillée.";
    private const string OpenTechDoor =
        "La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe.";

    public override RoomId Id => RoomId.TechCorridor;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "TechCorridor";

    public override string Description(GameState s) => RoomDescription;

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
            if (c.Instrument != ObjectId.Badge
                || s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked)
            {
                return null; // autre instrument ou lecteur deja deverrouille : comportement par defaut
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

        return null;
    }

    private static bool IsNorthOpen(GameState s) =>
        s.StateOf(ObjectId.StairsReader) == ObjectState.Unlocked;
}
