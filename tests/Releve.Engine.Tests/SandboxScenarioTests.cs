using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Scenario du monde d'amorcage (§12.2) : les trois etats de fleche, la prise, l'inventaire, la sauvegarde.</summary>
public class SandboxScenarioTests
{
    [Fact]
    public void Full_Loop_Take_Unlock_Move_Back_And_Event_Once()
    {
        var world = BootstrapWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());

        // 1. depart
        var start = engine.Start();
        Assert.Single(start);
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.East]);

        // 2. sortie est bloquee : le message dit ce qui bloque, pas comment debloquer
        var blocked = engine.Execute("est");
        Assert.Equal(["La porte est verrouillée : le voyant du loquet est rouge."], blocked.Messages);
        Assert.Equal(RoomId.SandboxA, engine.State.CurrentRoom);

        // 3. sortie nord sans destination : egalement bloquee
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.North]);

        // 4. l'instrument doit etre dans l'inventaire
        var notCarried = engine.Execute("utilise caisse sur commutateur");
        Assert.Equal(["Vous ne l’avez pas sur vous."], notCarried.Messages);

        // 5. prise puis utilisation
        Assert.Equal(["Pris."], engine.Execute("prends caisse").Messages);
        var unlocked = engine.Execute("utilise la caisse sur le commutateur");
        Assert.Equal(["La caisse fait levier sur le crochet : les ergots se relèvent et le voyant du loquet passe au vert."], unlocked.Messages);
        Assert.Equal(ObjectState.Unlocked, engine.State.StateOf(ObjectId.SandboxSwitch));

        // 6. la fleche est devient active
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.East]);

        // 7. deplacement : description + annonce unique
        var entered = engine.Execute("est");
        Assert.Equal(2, entered.Messages.Count);
        Assert.Contains("sas étroit", entered.Messages[0], StringComparison.Ordinal);
        Assert.Contains("déclic", entered.Messages[1], StringComparison.Ordinal);
        Assert.Equal(RoomId.SandboxB, engine.State.CurrentRoom);
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.West]);
        Assert.Equal(ExitState.Absent, engine.GetView().Exits[Direction.North]);

        // 8. retour : l'annonce ne se rejoue pas
        var back = engine.Execute("ouest");
        Assert.Single(back.Messages);
        Assert.Contains("salle nue", back.Messages[0], StringComparison.Ordinal);

        // 9. la fleche est reflete toujours l'etat du commutateur
        var view = engine.GetView();
        Assert.Equal(RoomId.SandboxA, view.Room);
        Assert.Equal(ExitState.Open, view.Exits[Direction.East]);

        // 10. second passage : l'annonce unique ne se rejoue pas
        var again = engine.Execute("est");
        Assert.Single(again.Messages);
    }

    [Fact]
    public void Quit_And_Continue_Restores_The_Party_Identically()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(directory, "save.dat");
        var store = new EncryptedFileSaveStore(filePath);

        try
        {
            var world = BootstrapWorld.Create();
            var engine = new GameEngine(world, world.CreateInitialState());
            engine.Start();
            engine.Execute("prends caisse");
            engine.Execute("utilise caisse sur commutateur");
            engine.Execute("est");

            store.Save(engine.State);

            var loaded = store.Load();
            Assert.NotNull(loaded);

            var continuedEngine = new GameEngine(world, loaded!);
            var messages = continuedEngine.Start();

            Assert.Equal(RoomId.SandboxB, continuedEngine.State.CurrentRoom);
            Assert.True(continuedEngine.State.IsInInventory(ObjectId.SandboxItem));
            Assert.Equal(ExitState.Open, continuedEngine.GetView().Exits[Direction.West]);
            Assert.DoesNotContain(messages, m => m.Contains("déclic", StringComparison.Ordinal));
        }
        finally
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void Continue_Is_Not_Available_Without_A_Valid_Save()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var store = new EncryptedFileSaveStore(Path.Combine(directory, "save.dat"));

        Assert.False(store.Exists());
        Assert.Null(store.Load());
    }
}
