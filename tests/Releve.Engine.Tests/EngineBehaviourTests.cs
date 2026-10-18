using Releve.Engine;
using Releve.Engine.Core;
using Releve.Engine.World;
using Releve.Engine.Tests.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Comportements par defaut du moteur (§7.3).</summary>
public class EngineBehaviourTests
{
    [Fact]
    public void Start_Returns_Room_Description()
    {
        var engine = TestSupport.FreshTestEngine();

        var messages = engine.Start();

        Assert.Single(messages);
        Assert.Equal("Une salle carrée. Une armoire est posée contre le mur nord, un coffre repose au pied d’un banc. À l’est, un couloir effondré.", messages[0]);
    }

    [Fact]
    public void Look_Without_Target_Repeats_Description()
    {
        var engine = TestSupport.FreshTestEngine();

        var result = engine.Execute("regarde");

        Assert.False(result.StateChanged);
        Assert.Contains(result.Messages, m => m.Contains("salle carrée", StringComparison.Ordinal));
    }

    [Fact]
    public void Take_And_Drop_Move_Between_Room_And_Inventory()
    {
        var engine = TestSupport.FreshTestEngine();

        var taken = engine.Execute("prends clef");
        Assert.Equal(["Pris."], taken.Messages);
        Assert.True(taken.StateChanged);
        Assert.True(engine.State.IsInInventory(ObjectId.TestKey));

        var inventory = engine.Execute("inventaire");
        Assert.Equal(["petite clé"], inventory.Messages);

        var dropped = engine.Execute("pose clef");
        Assert.Equal(["Posé."], dropped.Messages);
        Assert.False(engine.State.IsInInventory(ObjectId.TestKey));

        var empty = engine.Execute("inventaire");
        Assert.Equal(["Vous ne portez rien."], empty.Messages);
    }

    [Fact]
    public void Take_Fixed_Object_Is_Refused()
    {
        var engine = TestSupport.FreshTestEngine();

        var result = engine.Execute("prends armoire");

        Assert.Equal(["Vous ne pouvez pas prendre cela."], result.Messages);
    }

    [Fact]
    public void Drop_Object_Not_Carried_Is_Refused()
    {
        var engine = TestSupport.FreshTestEngine();

        var result = engine.Execute("pose clef");

        Assert.Equal(["Vous ne portez pas cela."], result.Messages);
    }

    [Fact]
    public void Open_Close_Toggle_Only_Objects_With_A_Closed_Or_Open_State()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(["Rien ne se passe."], engine.Execute("ouvre livre ancien").Messages);

        var open = engine.Execute("ouvre coffre");
        Assert.Equal(["Vous ouvrez coffre."], open.Messages);
        Assert.True(open.StateChanged);
        Assert.Equal(ObjectState.Open, engine.State.StateOf(ObjectId.TestChest));

        var close = engine.Execute("ferme coffre");
        Assert.Equal(["Vous fermez coffre."], close.Messages);
        Assert.Equal(ObjectState.Closed, engine.State.StateOf(ObjectId.TestChest));
    }

    [Fact]
    public void Use_And_Talk_Fall_Back_To_Nothing_Happens()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(["Précisez sur quoi l'utiliser."], engine.Execute("utilise clef").Messages);
        Assert.Equal(["Rien ne se passe."], engine.Execute("parle clef").Messages);
    }

    [Fact]
    public void Use_With_Instrument_Not_Carried_Is_Refused()
    {
        var engine = TestSupport.FreshTestEngine();

        var result = engine.Execute("utilise clef sur armoire");

        Assert.Equal(["Vous ne l’avez pas sur vous."], result.Messages);
        Assert.Equal(ObjectState.Locked, engine.State.StateOf(ObjectId.TestCabinet));
    }

    [Fact]
    public void Move_Without_Exit_Is_Refused()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(["Vous ne pouvez pas aller par là."], engine.Execute("sud").Messages);
        Assert.Equal(["Vous ne pouvez pas aller par là."], engine.Execute("ouest").Messages);
    }

    [Fact]
    public void Move_Towards_Blocked_Exit_Returns_Blocker_And_Stays()
    {
        var engine = TestSupport.FreshTestEngine();

        var blocked = engine.Execute("nord");
        Assert.Equal(["Une porte de pierre bloque le passage nord. Sa serrure est bien fermée."], blocked.Messages);
        Assert.False(blocked.StateChanged);
        Assert.Equal(RoomId.TestRoomStart, engine.State.CurrentRoom);
    }

    [Fact]
    public void Errors_Are_Neutral()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(["Cette commande n’est pas comprise."], engine.Execute("saute").Messages);
        Assert.Equal(["Précisez l’objet de la commande."], engine.Execute("prends").Messages);
        Assert.Equal(["Vous ne voyez pas cela ici."], engine.Execute("prends crayon").Messages);
        Assert.Empty(engine.Execute("   ").Messages);
    }

    [Fact]
    public void Hidden_Object_Becomes_Visible_After_Opening_The_Chest()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(["Vous ne voyez pas cela ici."], engine.Execute("regarde carnet").Messages);

        engine.Execute("ouvre coffre");

        var result = engine.Execute("regarde carnet");
        Assert.Equal(["Un carnet de poche, glissé au fond du coffre."], result.Messages);
    }
}

/// <summary>Etats d'affichage des directions, calcules a la volee (§5.5).</summary>
public class ExitStateTests
{
    [Fact]
    public void Test_Room_Starts_With_Blocked_And_Absent_Arrows()
    {
        var engine = TestSupport.FreshTestEngine();

        var view = engine.GetView();

        Assert.Equal(ExitState.Blocked, view.Exits[Direction.North]);
        Assert.Equal(ExitState.Blocked, view.Exits[Direction.East]);
        Assert.Equal(ExitState.Absent, view.Exits[Direction.South]);
        Assert.Equal(ExitState.Absent, view.Exits[Direction.West]);
    }

    [Fact]
    public void Test_Room_View_Reports_Title_Image_Key_And_Inventory()
    {
        var engine = TestSupport.FreshTestEngine();
        engine.Execute("prends clef");

        var view = engine.GetView();

        Assert.Equal(RoomId.TestRoomStart, view.Room);
        Assert.Equal("Salle de départ", view.Title);
        Assert.Equal("test_start", view.ImageKey);
        Assert.Equal(["petite clé"], view.Inventory.Select(i => i.Name));
    }

    [Fact]
    public void Test_Room_North_Opens_After_Unlocking_The_Cabinet()
    {
        var engine = TestSupport.FreshTestEngine();

        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.North]);

        engine.Execute("prends clef");
        engine.Execute("utilise clef sur armoire");

        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.North]);
    }

    [Fact]
    public void One_Way_Exit_Has_No_Arrow_On_The_Other_Side()
    {
        var engine = TestSupport.FreshTestEngine();
        engine.Execute("prends clef");
        engine.Execute("utilise clef sur armoire");
        engine.Execute("nord");

        Assert.Equal(RoomId.TestRoomVault, engine.State.CurrentRoom);

        var view = engine.GetView();
        Assert.Equal(ExitState.Open, view.Exits[Direction.South]);
        Assert.Equal(ExitState.Open, view.Exits[Direction.East]);

        engine.Execute("est");
        Assert.Equal(RoomId.TestRoomWall, engine.State.CurrentRoom);
        Assert.All(new[] { Direction.North, Direction.South, Direction.East, Direction.West },
            d => Assert.Equal(ExitState.Absent, engine.GetView().Exits[d]));
    }

    [Fact]
    public void Moving_Back_And_Forth_Changes_Room_And_Messages()
    {
        var engine = TestSupport.FreshTestEngine();
        engine.Execute("prends clef");
        engine.Execute("utilise clef sur armoire");

        var north = engine.Execute("nord");
        Assert.True(north.StateChanged);
        Assert.Equal("Une petite chambre forte aux parois de métal.", north.Messages[0]);

        var south = engine.Execute("sud");
        Assert.Equal("Une salle carrée. Une armoire est posée contre le mur nord, un coffre repose au pied d’un banc. À l’est, un couloir effondré.", south.Messages[0]);
    }
}
