using System.Numerics;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Police du jeu (§9.5). Police TTF libre de droits couvrant le francais, chargee avec
/// une liste explicite de codepoints ; la police par defaut de raylib n'est utilisee qu'en repli.
/// </summary>
public sealed class Fonts : IDisposable
{
    public const int Size = 20;
    public const float Spacing = 1f;

    /// <summary>Taille de la police pour le titre du menu (lot C2).</summary>
    public const int TitleSize = 64;
    public const float TitleSpacing = 2f;

    /// <summary>Hauteur d'une ligne de console, interligne compris.</summary>
    public const float LineHeight = Size + 6;

    public Font Ui { get; }
    public Font Title { get; }

    private readonly bool _fromFile;
    private readonly bool _titleFromFile;

    public Fonts(string? path = null)
    {
        Ui = Load(path ?? Assets.Font, Size, Spacing, out _fromFile);
        Title = Load(Assets.Font, TitleSize, TitleSpacing, out _titleFromFile);
    }

    private static Font Load(string path, int size, float spacing, out bool fromFile)
    {
        if (File.Exists(path))
        {
            var codepoints = BuildCodepoints();
            var font = Raylib.LoadFontEx(path, size, codepoints, codepoints.Length);
            if (Raylib.IsFontValid(font))
            {
                fromFile = true;
                return font;
            }

            Raylib.UnloadFont(font);
        }

        fromFile = false;
        return Raylib.GetFontDefault();
    }

    public bool LoadedFromFile => _fromFile;
    public bool TitleLoadedFromFile => _titleFromFile;

    public float MeasureWidth(string text) => Raylib.MeasureTextEx(Ui, text, Size, Spacing).X;

    public float MeasureTitleWidth(string text) => Raylib.MeasureTextEx(Title, text, TitleSize, TitleSpacing).X;

    public void Draw(string text, Vector2 position, Color color) =>
        Raylib.DrawTextEx(Ui, text, position, Size, Spacing, color);

    public void DrawTitle(string text, Vector2 position, Color color) =>
        Raylib.DrawTextEx(Title, text, position, TitleSize, TitleSpacing, color);

    public void Dispose()
    {
        if (_fromFile)
        {
            Raylib.UnloadFont(Ui);
        }

        if (_titleFromFile)
        {
            Raylib.UnloadFont(Title);
        }
    }

    /// <summary>
    /// Codepoints necessaires au jeu : ASCII, accents francais, oe/OE, guillemets,
    /// apostrophe typographique, points de suspension, tirets cadratin et demi-cadratin.
    /// </summary>
    private static int[] BuildCodepoints()
    {
        var list = new List<int>(600);

        // ASCII imprimable.
        for (var c = 32; c <= 126; c++)
        {
            list.Add(c);
        }

        // Latin-1 Supplement et Latin Extended-A : accents francais, oe/OE, guillemets « ».
        for (var c = 0x00A0; c <= 0x017F; c++)
        {
            list.Add(c);
        }

        // Ponctuation generale.
        list.AddRange(new[]
        {
            0x2013, // tiret demi-cadratin
            0x2014, // tiret cadratin
            0x2018, // apostrophe culbutée
            0x2019, // apostrophe typographique
            0x201C, // guillemet double ouvrant
            0x201D, // guillemet double fermant
            0x2026, // points de suspension
        });

        return list.ToArray();
    }
}
