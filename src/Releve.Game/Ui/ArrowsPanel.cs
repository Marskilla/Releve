using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Game.Ui;

/// <summary>
/// Croix des déplacements (§9.3) : indicateurs d'état, sans interaction souris.
/// Absent : rien. Bloquée : flèche grise. Ouverte : flèche active.
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

        var rowY = bounds.Y + Layout.Padding + Fonts.LineHeight;

        // Nord au-dessus, ouest / sud / est sur la ligne du dessous.
        DrawArrowCell(exits[Direction.North], textX + CellWidth * 1.5f, rowY, Direction.North, centred: true);
        DrawArrowCell(exits[Direction.West], textX, rowY + RowHeight, Direction.West, centred: false);
        DrawArrowCell(exits[Direction.South], textX + CellWidth, rowY + RowHeight, Direction.South, centred: false);
        DrawArrowCell(exits[Direction.East], textX + CellWidth * 2f, rowY + RowHeight, Direction.East, centred: false);
    }

    /// <summary>Dessine la flèche et son libellé, si la sortie existe.</summary>
    private void DrawArrowCell(ExitState state, float x, float y, Direction direction, bool centred)
    {
        if (state == ExitState.Absent)
        {
            return; // sortie absente : rien n'est dessiné
        }

        var colour = state == ExitState.Open ? Palette.Prompt : Palette.DimText;
        var label = DirectionLabel(direction);
        var labelX = centred ? x + ArrowSize + Layout.Padding : x + ArrowSize + Layout.Padding * 0.5f;

        DrawArrowShape(x, y, direction, colour);
        _fonts.Draw(label, new Vector2(labelX, y), colour);
    }

    /// <summary>Triangle et fût : formes simples, sans image (§9.3).</summary>
    private static void DrawArrowShape(float x, float y, Direction direction, Color colour)
    {
        var half = ArrowSize / 2f;
        var shaft = ArrowSize / 4f;

        switch (direction)
        {
            case Direction.North:
                Raylib.DrawTriangle(
                    new Vector2(x + half, y),
                    new Vector2(x + ArrowSize, y + ArrowSize),
                    new Vector2(x, y + ArrowSize), colour);
                Raylib.DrawRectangle((int)(x + half - shaft / 2), (int)(y + ArrowSize), (int)shaft, (int)half, colour);
                break;

            case Direction.South:
                Raylib.DrawTriangle(
                    new Vector2(x, y),
                    new Vector2(x + ArrowSize, y),
                    new Vector2(x + half, y + ArrowSize), colour);
                Raylib.DrawRectangle((int)(x + half - shaft / 2), (int)y, (int)shaft, (int)half, colour);
                break;

            case Direction.East:
                Raylib.DrawTriangle(
                    new Vector2(x + ArrowSize, y + half),
                    new Vector2(x, y),
                    new Vector2(x, y + ArrowSize), colour);
                Raylib.DrawRectangle((int)x, (int)(y + half - shaft / 2), (int)half, (int)shaft, colour);
                break;

            case Direction.West:
                Raylib.DrawTriangle(
                    new Vector2(x, y + half),
                    new Vector2(x + ArrowSize, y),
                    new Vector2(x + ArrowSize, y + ArrowSize), colour);
                Raylib.DrawRectangle((int)(x + half), (int)(y + half - shaft / 2), (int)half, (int)shaft, colour);
                break;
        }
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
