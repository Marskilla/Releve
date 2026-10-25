using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>
/// Tests d'encodage : pas d'apostrophe typographique (U+2019), pas de caractères décomposés (§5.1).
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
            AssertNoTypographicApostrophe(room.Title, $"Room {roomId} Title");
            AssertNoDecomposedChars(room.Title, $"Room {roomId} Title");

            var desc = room.Description(state);
            AssertNoTypographicApostrophe(desc, $"Room {roomId} Description");
            AssertNoDecomposedChars(desc, $"Room {roomId} Description");

            // DetailedDescription (peut être null)
            var detailedMethod = room.GetType().GetMethod("DetailedDescription");
            if (detailedMethod != null)
            {
                var detailed = detailedMethod.Invoke(room, new object[] { state }) as string;
                if (!string.IsNullOrEmpty(detailed))
                {
                    AssertNoTypographicApostrophe(detailed, $"Room {roomId} DetailedDescription");
                    AssertNoDecomposedChars(detailed, $"Room {roomId} DetailedDescription");
                }
            }

            // Messages bloqués des sorties
            foreach (var exit in room.Exits)
            {
                if (!string.IsNullOrWhiteSpace(exit.BlockedMessage))
                {
                    AssertNoTypographicApostrophe(exit.BlockedMessage, $"Room {roomId} Exit {exit.Dir} BlockedMessage");
                    AssertNoDecomposedChars(exit.BlockedMessage, $"Room {roomId} Exit {exit.Dir} BlockedMessage");
                }
            }
        }

        // 2. Noms et Describe (état initial) de chaque objet
        foreach (var (objectId, def) in world.Objects)
        {
            AssertNoTypographicApostrophe(def.Name, $"Object {objectId} Name");
            AssertNoDecomposedChars(def.Name, $"Object {objectId} Name");

            var describe = def.Describe(state);
            AssertNoTypographicApostrophe(describe, $"Object {objectId} Describe");
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
                    AssertNoTypographicApostrophe(s, $"Messages.{field.Name}");
                    AssertNoDecomposedChars(s, $"Messages.{field.Name}");
                }
            }
        }

        // 4. Constantes de FlavourText
        foreach (var field in typeof(FlavourText).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string[]))
            {
                var arr = field.GetValue(null) as string[];
                if (arr != null)
                {
                    foreach (var s in arr)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            AssertNoTypographicApostrophe(s, $"FlavourText.{field.Name}");
                            AssertNoDecomposedChars(s, $"FlavourText.{field.Name}");
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void Source_Files_Have_No_Typographic_Apostrophe_In_Player_Texts()
    {
        var srcDir = FindSrcDirectory();
        Assert.True(Directory.Exists(srcDir), $"Dossier src introuvable : {srcDir}");

        var contentDir = Path.Combine(srcDir, "Releve.Engine", "Content");
        var coreDir = Path.Combine(srcDir, "Releve.Engine", "Core");
        var gameDir = Path.Combine(srcDir, "Releve.Game");

        var files = new List<string>();
        files.AddRange(Directory.GetFiles(contentDir, "*.cs", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(coreDir, "*.cs", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(gameDir, "*.cs", SearchOption.AllDirectories));

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
                    if (literal.Contains('\u2019'))
                    {
                        Assert.Fail($"Apostrophe typographique (U+2019) trouvée dans {file}:{i + 1} : {literal}");
                    }
                }
            }
        }
    }

    [Fact]
    public void No_Blouse_Word_In_Engine_Texts()
    {
        var world = GameWorld.Create();
        var state = world.CreateInitialState();

        foreach (var (roomId, room) in world.Rooms)
        {
            Assert.DoesNotContain("blouse", room.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("blouse", room.Description(state), StringComparison.OrdinalIgnoreCase);

            var detailedMethod = room.GetType().GetMethod("DetailedDescription");
            if (detailedMethod != null)
            {
                var detailed = detailedMethod.Invoke(room, new object[] { state }) as string;
                if (!string.IsNullOrEmpty(detailed))
                {
                    Assert.DoesNotContain("blouse", detailed, StringComparison.OrdinalIgnoreCase);
                }
            }

            foreach (var exit in room.Exits)
            {
                if (!string.IsNullOrWhiteSpace(exit.BlockedMessage))
                {
                    Assert.DoesNotContain("blouse", exit.BlockedMessage, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        foreach (var (objectId, def) in world.Objects)
        {
            Assert.DoesNotContain("blouse", def.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("blouse", def.Describe(state), StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in typeof(Messages).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string))
            {
                var s = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(s))
                {
                    Assert.DoesNotContain("blouse", s, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        foreach (var field in typeof(FlavourText).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string[]))
            {
                var arr = field.GetValue(null) as string[];
                if (arr != null)
                {
                    foreach (var s in arr)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            Assert.DoesNotContain("blouse", s, StringComparison.OrdinalIgnoreCase);
                        }
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

    private static void AssertNoTypographicApostrophe(string text, string context)
    {
        if (text.Contains('\u2019'))
        {
            Assert.Fail($"Apostrophe typographique (U+2019) dans {context} : {text}");
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