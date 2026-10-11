using Releve.Engine;
using Releve.Engine.Content;
using Releve.Engine.Tests.World;

namespace Releve.Engine.Tests;

/// <summary>Fabrication rapide d'un moteur pret a jouer.</summary>
internal static class TestSupport
{
    public static GameEngine FreshTestEngine()
    {
        var engine = TestWorldFactory.CreateEngine();
        engine.Start();
        return engine;
    }
}
