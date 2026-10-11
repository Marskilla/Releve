using Releve.Engine;
using Releve.Engine.Content;
using Releve.Engine.Tests.World;

namespace Releve.Engine.Tests;

/// <summary>Fabrication rapide d'un moteur pret a jouer.</summary>
internal static class TestSupport
{
    public static GameEngine FreshSandboxEngine()
    {
        var world = BootstrapWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();
        return engine;
    }

    public static GameEngine FreshTestEngine()
    {
        var engine = TestWorldFactory.CreateEngine();
        engine.Start();
        return engine;
    }
}
