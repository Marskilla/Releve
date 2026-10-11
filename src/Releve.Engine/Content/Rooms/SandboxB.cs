using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Rooms;

/// <summary>Seconde piece du monde d'amorcage (§12.2). Contenu temporaire.</summary>
public sealed class SandboxB : RoomBase
{
    private const string RoomTitle = "Sas B — zone d’amorçage";
    private const string RoomDescription =
        "Un sas étroit éclairé par une bande lumineuse au plafond. Une porte capitonnée donne à l’ouest. "
        + "Les autres parois sont nues, sans poignée ni clavier.";

    public override RoomId Id => RoomId.SandboxB;

    public override string Title => RoomTitle;

    public override string ImageKey(GameState s) => "sandbox_b";

    public override string Description(GameState s) => RoomDescription;

    public override IReadOnlyList<Exit> Exits { get; } = new[]
    {
        new Exit(Direction.West, RoomId.SandboxA),
    };

    /// <summary>Annonce unique : notee dans Evenements pour ne pas se rejouer (§7.4).</summary>
    public override IEnumerable<string> OnEnter(GameState s)
    {
        if (s.Events.Contains(GameEvent.SandboxAnnouncePlayed))
        {
            yield break;
        }

        s.Events.Add(GameEvent.SandboxAnnouncePlayed);
        yield return "Un déclic se fait entendre derrière la paroi, puis plus rien.";
    }
}
