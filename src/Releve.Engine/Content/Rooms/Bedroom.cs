using Releve.Engine.Content.Objects;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Chambre de réveil : pièce de départ du jeu (lore §5.1, PoC §3.1).</summary>
public sealed class Bedroom : RoomBase
{
    private const string RoomTitle = "Chambre de réveil";

    // Textes constants (J3.4)
    private const string IntroMessage =
        "Alors que vous ouvrez doucement les yeux, une voix de synthèse annonce : « Réveil terminé, constantes stables. » En vous examinant, vous constatez que vous portez une chemise d’hôpital, la tenue standard des malades alités.";

    private const string DescriptionText =
        "La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire.";

    private const string DetailedDescriptionWithBag =
        "La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire. Dans un coin se trouve une corbeille et son sac vide. Une porte contrôlée par un lecteur de badge constitue la seule sortie.";

    private const string DetailedDescriptionWithoutBag =
        "La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire. Dans un coin se trouve une corbeille. Une porte contrôlée par un lecteur de badge constitue la seule sortie.";

    private const string EastBlockedMessage =
        "La porte est solidement fermée et le lecteur de badge affiche un rouge fixe.";
    private const string BadgeUsedOnReader =
        "Vous passez le badge devant le lecteur. Le voyant passe au vert et la porte se déverrouille.";
    private const string ReaderAlreadyUnlocked = "Le lecteur est déjà déverrouillé.";

    // Coordonnées zones d'image (J5) - image 1774x887
    private const int WardrobeX1 = 705, WardrobeY1 = 140, WardrobeX2 = 1065, WardrobeY2 = 565;
    private const int BinX1 = 1215, BinY1 = 405, BinX2 = 1340, BinY2 = 540;
    private const int ReaderX1 = 1670, ReaderY1 = 232, ReaderX2 = 1730, ReaderY2 = 325;

    public override RoomId Id => RoomId.Bedroom;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "Bedroom";

    public override string Description(GameState s) => DescriptionText;

    /// <summary>Message d'introduction unique (utilise WakeAnnouncementDone).</summary>
    public override IEnumerable<string> IntroMessages(GameState s)
    {
        if (s.Events.Contains(GameEvent.WakeAnnouncementDone))
        {
            yield break;
        }

        s.Events.Add(GameEvent.WakeAnnouncementDone);
        yield return IntroMessage;
    }

    /// <summary>Description detaillee selon presence du sac poubelle.</summary>
    public override string? DetailedDescription(GameState s)
    {
        bool bagInRoom = s.Locations.TryGetValue(ObjectId.GarbageBag, out var loc)
            && loc.Kind == LocationKind.Room
            && loc.Room == RoomId.Bedroom;

        return bagInRoom ? DetailedDescriptionWithBag : DetailedDescriptionWithoutBag;
    }

    /// <summary>Zones d'image actives selon l'etat (J5).</summary>
    public override IReadOnlyList<ImagePatch> ImagePatches => new[]
    {
        // Armoire ouverte
        new ImagePatch(WardrobeX1, WardrobeY1, WardrobeX2, WardrobeY2,
            s => s.StateOf(ObjectId.BedroomWardrobe) == ObjectState.Open),
        // Corbeille (sac absent de la chambre)
        new ImagePatch(BinX1, BinY1, BinX2, BinY2,
            s => {
                var loc = s.Locations.GetValueOrDefault(ObjectId.GarbageBag);
                return loc.Kind != LocationKind.Room || loc.Room != RoomId.Bedroom;
            }),
        // Lecteur de badge déverrouillé
        new ImagePatch(ReaderX1, ReaderY1, ReaderX2, ReaderY2,
            s => s.StateOf(ObjectId.BedroomReader) == ObjectState.Unlocked),
    };

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.East, RoomId.TechCorridor, IsOpen: IsEastOpen, BlockedMessage: EastBlockedMessage,
            Words: new[] { "sors", "sortir" }),
    };

    /// <summary>OnEnter ne renvoie plus rien : l'annonce est dans IntroMessages.</summary>
    public override IEnumerable<string> OnEnter(GameState s) => Array.Empty<string>();

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

        // « lis machines » : même texte que Describe.
        if (c.Action == GameAction.Read && c.Target == ObjectId.BedroomMachines)
        {
            return CommandResult.Say(BedroomObjects.BedroomMachinesDescription);
        }

        return null;
    }

    private static bool IsEastOpen(GameState s) =>
        s.StateOf(ObjectId.BedroomReader) == ObjectState.Unlocked;
}
