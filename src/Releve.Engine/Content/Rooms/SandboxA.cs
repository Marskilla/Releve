using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Piece de depart du monde d'amorcage (§12.2). Contenu temporaire.</summary>
public sealed class SandboxA : RoomBase
{
    private const string RoomTitle = "Salle A — zone d’amorçage";
    private const string RoomDescription =
        "Une salle nue aux murs de béton. Deux bancs métalliques longent le mur ouest et une caisse en métal repose sur l’un d’eux. "
        + "Une porte capitonnée occupe le mur est, flanquée d’un commutateur. Le mur nord est plein.";
    private const string EastBlockedMessage =
        "La porte est verrouillée : le voyant du loquet est rouge.";
    private const string NorthBlockedMessage =
        "Le mur nord est plein, sans porte ni ouverture.";
    private const string ItemUsedOnSwitch =
        "La caisse fait levier sur le crochet : les ergots se relèvent et le voyant du loquet passe au vert.";
    private const string ItemUsedOnSwitchAgain = "Les ergots sont déjà relevés.";
    private const string ItemUsedOnNothing = "Rien ne bouge sous la pression.";

    public override RoomId Id => RoomId.SandboxA;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "sandbox_a";

    public override string Description(GameState s) => RoomDescription;

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.East, RoomId.SandboxB, IsOpen: IsEastOpen, BlockedMessage: EastBlockedMessage),
        new Exit(Direction.North, null, BlockedMessage: NorthBlockedMessage),
    };

    public override CommandResult? Handle(Command c, GameState s)
    {
        if (c.Action != GameAction.Use || c.Target != ObjectId.SandboxSwitch)
        {
            return null; // comportements par defaut
        }

        if (c.Instrument != ObjectId.SandboxItem)
        {
            return CommandResult.Say(ItemUsedOnNothing);
        }

        if (s.StateOf(ObjectId.SandboxSwitch) == ObjectState.Unlocked)
        {
            return CommandResult.Say(ItemUsedOnSwitchAgain);
        }

        s.States[ObjectId.SandboxSwitch] = ObjectState.Unlocked;
        s.Events.Add(GameEvent.SandboxSwitchUnlocked);
        return CommandResult.Changed(ItemUsedOnSwitch);
    }

    private static bool IsEastOpen(GameState s) =>
        s.StateOf(ObjectId.SandboxSwitch) == ObjectState.Unlocked;
}
