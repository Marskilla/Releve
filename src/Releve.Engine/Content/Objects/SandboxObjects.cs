using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Content.Objects;

/// <summary>Objets du monde d'amorcage (§12.2). Contenu temporaire, a supprimer avec le contenu reel.</summary>
public static class SandboxObjects
{
    public static IReadOnlyDictionary<ObjectId, ObjectDef> Create() => new Dictionary<ObjectId, ObjectDef>
    {
        [ObjectId.SandboxItem] = new ObjectDef(
            Id: ObjectId.SandboxItem,
            Name: "caisse en métal",
            Aliases: new[] { "caisse", "caisse metal", "boite" },
            Portable: true,
            InitialLocation: Location.InRoom(RoomId.SandboxA),
            InitialState: ObjectState.None,
            Describe: _ => "Une caisse carrée en métal, fermée par deux loquets. Elle est vide."),

        [ObjectId.SandboxSwitch] = new ObjectDef(
            Id: ObjectId.SandboxSwitch,
            Name: "commutateur de la porte",
            Aliases: new[] { "commutateur", "commutateur porte", "interrupteur" },
            Portable: false,
            InitialLocation: Location.InRoom(RoomId.SandboxA),
            InitialState: ObjectState.Locked,
            Describe: DescribeSwitch),
    };

    private static string DescribeSwitch(GameState state) =>
        state.StateOf(ObjectId.SandboxSwitch) == ObjectState.Unlocked
            ? "Un commutateur encastré près de la porte est. Les deux ergots sont relevés et le crochet est ouvert."
            : "Un commutateur encastré près de la porte est. Les deux ergots sont ramenés vers le centre et retenus par un crochet.";
}
