using Releve.Engine;
using Releve.Engine.Content;
using Releve.Engine.Tests.World;

namespace Releve.Engine.Tests;

/// <summary>Fabrication rapide d'un moteur pret a jouer.</summary>
internal static class TestSupport
{
    // Graine fixe pour determinisme des reponses variees (J4)
    private static readonly Random FixedRandom = new(42);

    public static GameEngine FreshTestEngine()
    {
        var engine = TestWorldFactory.CreateEngine();
        engine.Start();
        return engine;
    }

    public static GameEngine FreshTestEngineWithFixedRandom()
    {
        var world = TestWorldFactory.Create();
        var engine = new GameEngine(world, world.CreateInitialState(), FixedRandom);
        engine.Start();
        return engine;
    }
}
