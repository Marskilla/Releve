using System.Numerics;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Menu minimal de lancement (§9.7) : « Continuer » (grisé si aucune sauvegarde valide)
/// et « Nouvelle partie ». Navigation au clavier (haut/bas, Entrée).
/// </summary>
public sealed class MenuScreen
{
    private const string TitleText = "Relève";

    private readonly Fonts _fonts;

    private int _selected;

    public MenuScreen(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Place la selection sur la reprise si une sauvegarde existe, sinon sur une nouvelle partie.</summary>
    public void Reset(bool canContinue)
    {
        _selected = canContinue ? 0 : 1;
    }

    /// <summary>-1 : nouvelle partie, 0 : continuer.</summary>
    public int Selected => _selected;

    /// <summary>Renvoie -1 pour une nouvelle partie, 0 pour reprendre, null si rien n'est choisi.</summary>
    public int? Update(bool canContinue)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selected = Math.Abs(_selected - 1);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selected = Math.Abs(_selected - 1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            if (_selected == 0 && !canContinue)
            {
                return null; // entrée inexistante : grisée, rien à faire
            }

            return _selected == 0 ? 0 : -1;
        }

        return null;
    }

    public void Draw(bool canContinue)
    {
        Raylib.ClearBackground(Palette.Background);

        var width = (float)Raylib.GetScreenWidth();
        var centre = width / 2f;

        var titleWidth = _fonts.MeasureWidth(TitleText);
        _fonts.Draw(TitleText, new Vector2(centre - titleWidth / 2f, 180), Palette.Text);

        DrawEntry(0, "Continuer", canContinue ? Palette.Text : Palette.DimText, centre);
        DrawEntry(1, "Nouvelle partie", Palette.Text, centre);
    }

    private void DrawEntry(int index, string label, Color colour, float centre)
    {
        var y = 300f + index * 60f;
        var textWidth = _fonts.MeasureWidth(label);

        if (index == _selected)
        {
            Raylib.DrawRectangle(
                (int)(centre - textWidth / 2f) - Layout.Padding,
                (int)(y - Layout.Padding / 2),
                (int)textWidth + Layout.Padding * 2,
                Fonts.Size + Layout.Padding,
                Palette.Replacement);
        }

        _fonts.Draw(label, new Vector2(centre - textWidth / 2f, y), colour);
    }
}
