using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>
/// Tests d'encodage : pas d'apostrophe droite, pas de caractères décomposés (§5.1).
/// </summary>
public class EncodingTests
{
    private static readonly Regex StringLiteralRegex = new(@"(?<!//\s*)""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);
    private static readonly Regex CommentLineRegex = new(@"^\s*//", RegexOptions.Compiled);

    [Fact]
    public void Game_World_Texts_Have_Correct_Encoding()
    {
        var world = GameWorld.Create();
        var state = world.CreateInitialState();

        // 1. Titres et descriptions des pièces (état initial)
        foreach (var (roomId, room) in world.Rooms)
        {
            AssertNoStraightApostrophe(room.Title, $"Room {roomId} Title");
            AssertNoDecomposedChars(room.Title, $"Room {roomId} Title");

            var desc = room.Description(state);
            AssertNoStraightApostrophe(desc, $"Room {roomId} Description");
            AssertNoDecomposedChars(desc, $"Room {roomId} Description");

            // DetailedDescription (peut être null)
            var detailedMethod = room.GetType().GetMethod("DetailedDescription");
            if (detailedMethod != null)
            {
                var detailed = detailedMethod.Invoke(room, new object[] { state }) as string;
                if (!string.IsNullOrEmpty(detailed))
                {
                    AssertNoStraightApostrophe(detailed, $"Room {roomId} DetailedDescription");
                    AssertNoDecomposedChars(detailed, $"Room {roomId} DetailedDescription");
                }
            }

            // Messages bloqués des sorties
            foreach (var exit in room.Exits)
            {
                if (!string.IsNullOrWhiteSpace(exit.BlockedMessage))
                {
                    AssertNoStraightApostrophe(exit.BlockedMessage, $"Room {roomId} Exit {exit.Dir} BlockedMessage");
                    AssertNoDecomposedChars(exit.BlockedMessage, $"Room {roomId} Exit {exit.Dir} BlockedMessage");
                }
            }
        }

        // 2. Noms et Describe (état initial) de chaque objet
        foreach (var (objectId, def) in world.Objects)
        {
            AssertNoStraightApostrophe(def.Name, $"Object {objectId} Name");
            AssertNoDecomposedChars(def.Name, $"Object {objectId} Name");

            var describe = def.Describe(state);
            AssertNoStraightApostrophe(describe, $"Object {objectId} Describe");
            AssertNoDecomposedChars(describe, $"Object {objectId} Describe");
        }

        // 3. Constantes de Messages
        foreach (var field in typeof(Messages).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string))
            {
                var s = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(s))
                {
                    AssertNoStraightApostrophe(s, $"Messages.{field.Name}");
                    AssertNoDecomposedChars(s, $"Messages.{field.Name}");
                }
            }
        }
    }

    [Fact]
    public void Source_Files_Have_No_Straight_Apostrophe_In_Player_Texts()
    {
        var srcDir = FindSrcDirectory();
        Assert.True(Directory.Exists(srcDir), $"Dossier src introuvable : {srcDir}");

        var contentDir = Path.Combine(srcDir, "Releve.Engine", "Content");
        var coreDir = Path.Combine(srcDir, "Releve.Engine", "Core");

        var files = new List<string>();
        files.AddRange(Directory.GetFiles(contentDir, "*.cs", SearchOption.AllDirectories));
        files.Add(Path.Combine(coreDir, "Messages.cs"));
        // FlavourText.cs n'existe pas encore (créé en J4)

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (CommentLineRegex.IsMatch(line))
                {
                    continue;
                }

                var matches = StringLiteralRegex.Matches(line);
                foreach (Match m in matches)
                {
                    var literal = m.Value;
                    if (literal.Contains("'"))
                    {
                        Assert.Fail($"Apostrophe droite trouvée dans {file}:{i + 1} : {literal}");
                    }
                }
            }
        }
    }

    private static string FindSrcDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }
        return dir == null ? string.Empty : Path.Combine(dir.FullName, "src");
    }

    private static void AssertNoStraightApostrophe(string text, string context)
    {
        if (text.Contains("'"))
        {
            Assert.Fail($"Apostrophe droite (U+0027) dans {context} : {text}");
        }
    }

    private static void AssertNoDecomposedChars(string text, string context)
    {
        foreach (var c in text)
        {
            if (c >= '\u0300' && c <= '\u036f')
            {
                Assert.Fail($"Caractère décomposé (U+{((int)c):X4}) dans {context} : {text}");
            }
        }
    }
}