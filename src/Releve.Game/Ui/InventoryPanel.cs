using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;

namespace Releve.Game.Ui;

/// <summary>
/// Inventaire : liste verticale de libellés (§9.3), défilement Ctrl+Haut/Ctrl+Bas (sans Shift).
/// </summary>
public sealed class InventoryPanel
{
    private const string TitleText = "Inventaire :";
    private const float IconSize = 16f;

    private readonly Fonts _fonts;
    private int _offset;

    public InventoryPanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    public void Draw(IReadOnlyList<InventoryItem> items)
    {
        var bounds = Layout.InventoryBounds;
        Raylib.DrawRectangleRec(bounds, Palette.Panel);
        Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);

        var textX = bounds.X + Layout.Padding;
        _fonts.Draw(TitleText, new Vector2(textX, bounds.Y + Layout.Padding), Palette.DimText);

        var listY = bounds.Y + Layout.Padding + Fonts.LineHeight;
        var visible = Math.Max(0, (int)((bounds.Y + bounds.Height - Layout.Padding - listY) / Fonts.LineHeight));

        var first = Math.Clamp(_offset, 0, Math.Max(0, items.Count - 1));
        for (var i = first; i < items.Count && i < first + visible; i++)
        {
            var y = listY + (i - first) * Fonts.LineHeight;

            // Icône de remplacement : petit carré, aucune image requise.
            Raylib.DrawRectangle(
                (int)textX,
                (int)y + (Fonts.Size - (int)IconSize) / 2,
                (int)IconSize,
                (int)IconSize,
                Palette.Replacement);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(textX, y + (Fonts.Size - IconSize) / 2f, IconSize, IconSize),
                1, Palette.Border);

            _fonts.Draw(items[i].Name, new Vector2(textX + IconSize + Layout.Padding, y), Palette.Text);
        }
    }

    /// <summary>Défilement Ctrl+Haut / Ctrl+Bas (sans Shift) (§9.4).</summary>
    public void Update(int itemCount)
    {
        var visible = VisibleRows();
        var maximum = Math.Max(0, itemCount - visible);

        bool ctrlDown = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        bool shiftDown = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);

        // Ctrl+Haut / Ctrl+Bas (sans Shift)
        if (ctrlDown && !shiftDown)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _offset = Math.Max(0, _offset - 1);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _offset = Math.Min(maximum, _offset + 1);
            }
        }

        _offset = Math.Clamp(_offset, 0, maximum);
    }

    public void ResetScroll()
    {
        _offset = 0;
    }

    private int VisibleRows()
    {
        var bounds = Layout.InventoryBounds;
        var listY = bounds.Y + Layout.Padding + Fonts.LineHeight;
        return Math.Max(1, (int)((bounds.Y + bounds.Height - Layout.Padding - listY) / Fonts.LineHeight));
    }
}
