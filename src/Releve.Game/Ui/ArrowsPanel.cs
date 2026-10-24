using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Game.Ui;

/// <summary>
/// Croix des déplacements (§9.3, lot B3) : indicateurs d'etat, sans interaction souris.
/// Disposition en croise, centred horizontalement.
/// Absent : rien. Ouverte : fleche pleine, couleur vive. Bloquee : fleche creuse, gris sombre.
/// </summary>
public sealed class ArrowsPanel
{
    private const string TitleText = "Déplacements possibles :";
    private const float CellWidth = 100f;
    private const float RowHeight = 34f;
    private const float ArrowSize = 18f;

    private readonly Fonts _fonts;

    public ArrowsPanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    public void Draw(IReadOnlyDictionary<Direction, ExitState> exits)
    {
        var bounds = Layout.ArrowsBounds;
        Raylib.DrawRectangleRec(bounds, Palette.Panel);
        Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);

        var textX = bounds.X + Layout.Padding;
        _fonts.Draw(TitleText, new Vector2(textX, bounds.Y + Layout.Padding), Palette.DimText);

        var centreX = bounds.X + bounds.Width / 2f;
        var rowY1 = bounds.Y + Layout.Padding + Fonts.LineHeight;      // ligne 1 : Nord
        var rowY2 = rowY1 + RowHeight;                                   // ligne 2 : Ouest / Est
        var rowY3 = rowY2 + RowHeight;                                   // ligne 3 : Sud

        // Rang 1 : fleche Nord puis libellé, ensemble centre.
        DrawArrowRowCentered(exits[Direction.North], Direction.North, centreX, rowY1, centred: true);

        // Rang 2 : libellé Ouest + fleche (gauche), espace libre au centre, fleche Est + libellé (droite).
        DrawArrowRowSplit(exits[Direction.West], Direction.West, exits[Direction.East], Direction.East,
            textX, bounds.X + bounds.Width - Layout.Padding, rowY2);

        // Rang 3 : fleche Sud puis libellé, ensemble centre.
        DrawArrowRowCentered(exits[Direction.South], Direction.South, centreX, rowY3, centred: true);
    }

    /// <summary>Dessine une ligne centree : fleche puis libellé.</summary>
    private void DrawArrowRowCentered(ExitState state, Direction direction, float centreX, float y,
        bool centred)
    {
        if (state == ExitState.Absent)
        {
            return;
        }

        var arrowX = centred ? centreX - ArrowSize / 2f : 0f;
        DrawArrow(state, direction, arrowX, y);

        if (centred)
        {
            DrawLabel(direction, arrowX + ArrowSize + Layout.Padding, y, state);
        }
    }

    /// <summary>Dessine une ligne divisee : Ouest a gauche, Est a droite.</summary>
    private void DrawArrowRowSplit(ExitState westState, Direction westDir, ExitState eastState,
        Direction eastDir, float leftX, float rightX, float y)
    {
        if (westState != ExitState.Absent)
        {
            DrawLabel(westDir, leftX, y, westState);
            var labelWidth = _fonts.MeasureWidth(DirectionLabel(westDir));
            DrawArrow(westState, westDir, leftX + labelWidth + Layout.Padding, y);
        }

        if (eastState != ExitState.Absent)
        {
            var labelWidth = _fonts.MeasureWidth(DirectionLabel(eastDir));
            var arrowX = rightX - ArrowSize - labelWidth - Layout.Padding;
            DrawArrow(eastState, eastDir, arrowX, y);
            DrawLabel(eastDir, arrowX + ArrowSize + Layout.Padding, y, eastState);
        }
    }

    /// <summary>Dessine la fleche (pleine ou creuse) selon l'etat.</summary>
    private void DrawArrow(ExitState state, Direction direction, float x, float y)
    {
        if (state == ExitState.Absent)
        {
            return;
        }

        var colour = state == ExitState.Open ? Palette.ArrowOpen : Palette.ArrowBlocked;

        if (state == ExitState.Open)
        {
            DrawArrowFilled(direction, x, y, colour);
        }
        else
        {
            DrawArrowHollow(direction, x, y, colour);
        }
    }

    /// <summary>Fleche pleine : triangle seul (tete), ordre de sommets anti-horaire.</summary>
    private static void DrawArrowFilled(Direction direction, float x, float y, Color colour)
    {
        var half = ArrowSize / 2f;

        switch (direction)
        {
            case Direction.North:
                // Tete vers le haut : (centre-haut) -> (bas-gauche) -> (bas-droite)
                Raylib.DrawTriangle(
                    new Vector2(x + half, y),
                    new Vector2(x, y + ArrowSize),
                    new Vector2(x + ArrowSize, y + ArrowSize), colour);
                break;

            case Direction.South:
                // Tete vers le bas : (centre-bas) -> (haut-droite) -> (haut-gauche)
                Raylib.DrawTriangle(
                    new Vector2(x + half, y + ArrowSize),
                    new Vector2(x + ArrowSize, y),
                    new Vector2(x, y), colour);
                break;

            case Direction.East:
                // Tete vers la droite : (milieu-droit) -> (haut-gauche) -> (bas-gauche)
                Raylib.DrawTriangle(
                    new Vector2(x + ArrowSize, y + half),
                    new Vector2(x, y),
                    new Vector2(x, y + ArrowSize), colour);
                break;

            case Direction.West:
                // Tete vers la gauche : (milieu-gauche) -> (bas-droite) -> (haut-droite)
                Raylib.DrawTriangle(
                    new Vector2(x, y + half),
                    new Vector2(x + ArrowSize, y + ArrowSize),
                    new Vector2(x + ArrowSize, y), colour);
                break;
        }
    }

    /// <summary>Fleche creuse : contour du triangle seulement.</summary>
    private static void DrawArrowHollow(Direction direction, float x, float y, Color colour)
    {
        var half = ArrowSize / 2f;

        switch (direction)
        {
            case Direction.North:
                Raylib.DrawTriangleLines(
                    new Vector2(x + half, y),
                    new Vector2(x, y + ArrowSize),
                    new Vector2(x + ArrowSize, y + ArrowSize), colour);
                break;

            case Direction.South:
                Raylib.DrawTriangleLines(
                    new Vector2(x + half, y + ArrowSize),
                    new Vector2(x + ArrowSize, y),
                    new Vector2(x, y), colour);
                break;

            case Direction.East:
                Raylib.DrawTriangleLines(
                    new Vector2(x + ArrowSize, y + half),
                    new Vector2(x, y),
                    new Vector2(x, y + ArrowSize), colour);
                break;

            case Direction.West:
                Raylib.DrawTriangleLines(
                    new Vector2(x, y + half),
                    new Vector2(x + ArrowSize, y + ArrowSize),
                    new Vector2(x + ArrowSize, y), colour);
                break;
        }
    }

    private void DrawLabel(Direction direction, float x, float y, ExitState state)
    {
        var label = DirectionLabel(direction);
        var colour = state == ExitState.Open ? Palette.Text : Palette.DimText;
        _fonts.Draw(label, new Vector2(x, y), colour);
    }

    private static string DirectionLabel(Direction direction) => direction switch
    {
        Direction.North => "Nord",
        Direction.South => "Sud",
        Direction.East => "Est",
        Direction.West => "Ouest",
        _ => string.Empty,
    };
}
