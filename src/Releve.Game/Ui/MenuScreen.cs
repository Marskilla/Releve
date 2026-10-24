using System.Numerics;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Menu de lancement a trois entrees (lot C2) : « Continuer », « Nouvelle partie », « Aide ».
/// Retourne un choix typé. « Continuer » est grisé sans sauvegarde valide.
/// </summary>
public sealed class MenuScreen
{
    private const string TitleText = "Relève";

    private readonly Fonts _fonts;

    private int _selected;
    private Texture2D? _background;

    public MenuScreen(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Définit l'image de fond du menu (null = fond uni).</summary>
    public void SetBackground(Texture2D? background)
    {
        _background = background;
    }

    /// <summary>Place la selection sur la reprise si une sauvegarde existe, sinon sur une nouvelle partie.</summary>
    public void Reset(bool canContinue)
    {
        _selected = canContinue ? 0 : 1;
    }

    /// <summary>Choix retourne par le menu.</summary>
    public enum MenuChoice { Continue, NewGame, Help }

    /// <summary>Renvoie le choix lorsque le joueur valide, null sinon.</summary>
    public MenuChoice? Update(bool canContinue)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selected = (_selected - 1 + 3) % 3;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selected = (_selected + 1) % 3;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            if (_selected == 0 && !canContinue)
            {
                return null; // entree inexistante : grisee, rien à faire
            }

            return _selected switch
            {
                0 => MenuChoice.Continue,
                1 => MenuChoice.NewGame,
                _ => MenuChoice.Help,
            };
        }

        return null;
    }

    public void Draw(bool canContinue)
    {
        var width = (float)Raylib.GetScreenWidth();
        var height = (float)Raylib.GetScreenHeight();
        var centre = width / 2f;

        // Fond : image etiree si disponible, sinon couleur unie.
        if (_background is { } bg && Raylib.IsTextureValid(bg))
        {
            Raylib.DrawTexturePro(bg,
                new Rectangle(0, 0, bg.Width, bg.Height),
                new Rectangle(0, 0, width, height),
                Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Raylib.ClearBackground(Palette.Background);
        }

        // Titre en grand, centré en haut (police secondaire, lot C2).
        var titleWidth = _fonts.MeasureTitleWidth(TitleText);
        var titleX = centre - titleWidth / 2f;
        var titleY = 100f;
        DrawTextWithShadow(TitleText, new Vector2(titleX, titleY), Palette.Text);

        var entryY = 300f;
        var entrySpacing = 60f;

        DrawEntry(0, "Continuer", canContinue ? Palette.Text : Palette.DimText, centre, entryY);
        DrawEntry(1, "Nouvelle partie", Palette.Text, centre, entryY + entrySpacing);
        DrawEntry(2, "Aide", Palette.Text, centre, entryY + entrySpacing * 2);

        // Version en bas à droite (même police que les entrées de menu : 20 px, couleur DimText, ombre légère).
        var versionText = GameInfo.Version;
        var versionWidth = _fonts.MeasureWidth(versionText);
        var versionX = width - Layout.Padding - versionWidth;
        var versionY = height - Layout.Padding - Fonts.Size;
        DrawVersionText(versionText, new Vector2(versionX, versionY), Palette.DimText);
    }

    private void DrawEntry(int index, string label, Color colour, float centre, float y)
    {
        var textWidth = _fonts.MeasureWidth(label);
        var x = centre - textWidth / 2f;

        if (index == _selected)
        {
            Raylib.DrawRectangle(
                (int)(x - Layout.Padding),
                (int)(y - Layout.Padding / 2),
                (int)textWidth + Layout.Padding * 2,
                Fonts.Size + Layout.Padding,
                Palette.Replacement);
        }

        _fonts.Draw(label, new Vector2(x, y), colour);
    }

    /// <summary>Dessine le texte avec une ombre portée pour la lisibilité sur l'image de fond.</summary>
    private void DrawTextWithShadow(string text, Vector2 position, Color colour)
    {
        var shadowOffset = new Vector2(2, 2);
        _fonts.DrawTitle(text, position + shadowOffset, new Color(0, 0, 0, 180));
        _fonts.DrawTitle(text, position, colour);
    }

    /// <summary>Dessine la version avec la police standard (20 px) et une ombre discrète.</summary>
    private void DrawVersionText(string text, Vector2 position, Color colour)
    {
        var shadowOffset = new Vector2(1, 1);
        _fonts.Draw(text, position + shadowOffset, new Color(0, 0, 0, 120));
        _fonts.Draw(text, position, colour);
    }
}
