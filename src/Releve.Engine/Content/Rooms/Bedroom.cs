using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Chambre de reveil : piece de depart du jeu (lore §5.1, PoC §3.1).</summary>
public sealed class Bedroom : RoomBase
{
    private const string RoomTitle = "Chambre de réveil";
    private const string RoomDescription =
        "Vous portez une blouse médicale. La pièce est propre, éclairée, sans fenêtre.";
    private const string WakeAnnouncement =
        "Une voix de synthèse annonce : « Réveil terminé, constantes stables. »";
    private const string EastBlockedMessage =
        "La porte est solidement fermée et le lecteur de badge affiche un rouge fixe.";
    private const string BadgeUsedOnReader =
        "Vous passez le badge devant le lecteur. Le voyant passe au vert et la porte se déverrouille.";
    private const string ReaderAlreadyUnlocked = "Le lecteur est déjà déverrouillé.";

    public override RoomId Id => RoomId.Bedroom;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "Bedroom";

    public override string Description(GameState s) => RoomDescription;

    /// <summary>Annonce unique, notee dans Evenements pour ne pas se rejouer (§7.4).</summary>
    public override IEnumerable<string> OnEnter(GameState s)
    {
        if (s.Events.Contains(GameEvent.WakeAnnouncementDone))
        {
            yield break;
        }

        s.Events.Add(GameEvent.WakeAnnouncementDone);
        yield return WakeAnnouncement;
    }

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.East, RoomId.TechCorridor, IsOpen: IsEastOpen, BlockedMessage: EastBlockedMessage),
    };

    public override CommandResult? Handle(Command c, GameState s)
    {
        if (c.Action == GameAction.Use && c.Target == ObjectId.BedroomReader)
        {
            if (c.Instrument != ObjectId.Badge)
            {
                return null; // autre instrument : comportement par defaut
            }

            if (s.StateOf(ObjectId.BedroomReader) == ObjectState.Unlocked)
            {
                return CommandResult.Say(ReaderAlreadyUnlocked);
            }

            s.States[ObjectId.BedroomReader] = ObjectState.Unlocked;
            return CommandResult.Changed(BadgeUsedOnReader);
        }

        // « ouvre porte » : meme message que la sortie bloquee tant que le lecteur est verrouille.
        if (c.Action == GameAction.Open && c.Target == ObjectId.BedroomDoor
            && s.StateOf(ObjectId.BedroomReader) == ObjectState.Locked)
        {
            return CommandResult.Say(EastBlockedMessage);
        }

        return null;
    }

    private static bool IsEastOpen(GameState s) =>
        s.StateOf(ObjectId.BedroomReader) == ObjectState.Unlocked;
}
