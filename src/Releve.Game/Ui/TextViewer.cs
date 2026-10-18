using System.Numerics;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Visualiseur de texte modal et superpose (lot C3).
/// Opaque : remplit son rectangle d'une couleur pleine et masque ce qu'il recouvre.
/// Texte avec retour a la ligne automatique, défilement Page/Haut/Bas, fermeture par
/// Échap, Entrée ou bouton « Fermer ».
/// </summary>
public sealed class TextViewer
{
    private const string CloseLabel = "Fermer";
    private const float CloseButtonHeight = 30f;
    private const float CloseButtonWidth = 120f;
    private const float IndicatorSize = 10f;

    private readonly Fonts _fonts;
    private Rectangle _rectangle;
    private string? _title;
    private readonly List<string> _wrappedLines = new();
    private int _scroll;
    private bool _isOpen;
    private bool _closeHover;
    private bool _closePressed;

    public TextViewer(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Vrai tant que le visualiseur est ouvert.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Ouvre le visualiseur. Ne traite aucune entree dans l'image de creation.</summary>
    public void Open(Rectangle rectangle, string? title, string text)
    {
        _rectangle = rectangle;
        _title = title;
        _wrappedLines.Clear();
        WrapText(text, rectangle.Width - 2 * Layout.Padding);
        _scroll = 0;
        _isOpen = true;
    }

    /// <summary>Renvoie vrai lorsque le visualiseur est fermé (après avoir traité l'entree de fermeture).</summary>
    public bool Update()
    {
        if (!_isOpen)
        {
            return false;
        }

        // Échap ou Entrée ferme.
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter)
            || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            Close();
            return true;
        }

        // Défilement clavier.
        var page = Math.Max(1, VisibleLines() - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _scroll = Math.Max(0, _scroll - page);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _scroll = Math.Min(Math.Max(0, _wrappedLines.Count - 1), _scroll + page);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _scroll = Math.Max(0, _scroll - 1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _scroll = Math.Min(Math.Max(0, _wrappedLines.Count - 1), _scroll + 1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Home))
        {
            _scroll = 0;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.End))
        {
            _scroll = Math.Max(0, _wrappedLines.Count - 1);
        }

        // Bouton « Fermer » à la souris.
        UpdateCloseButton();

        return false;
    }

    /// <summary>Dessine la scène sous-jacente puis le visualiseur par-dessus.</summary>
    public void Draw()
    {
        if (!_isOpen)
        {
            return;
        }

        // Fond opaque du visualiseur.
        Raylib.DrawRectangleRec(_rectangle, Palette.Panel);
        Raylib.DrawRectangleLinesEx(_rectangle, 1, Palette.Border);

        var textX = _rectangle.X + Layout.Padding;
        var textY = _rectangle.Y + Layout.Padding;

        // Titre optionnel.
        if (_title is { Length: > 0 } t)
        {
            _fonts.Draw(t, new Vector2(textX, textY), Palette.Prompt);
            textY += Fonts.LineHeight + Layout.Padding;
        }

        // Lignes de texte visibles.
        var visible = VisibleLines();
        var last = Math.Min(_wrappedLines.Count - 1, _scroll + visible - 1);
        if (last < 0)
        {
            last = 0;
        }

        var y = textY;
        for (var i = _scroll; i <= last && i < _wrappedLines.Count; i++)
        {
            _fonts.Draw(_wrappedLines[i], new Vector2(textX, y), Palette.Text);
            y += Fonts.LineHeight;
        }

        // Bouton « Fermer ».
        DrawCloseButton();

        // Indicateurs de défilement (formes géométriques) - APRÈS le bouton pour ne pas être couverts.
        DrawScrollIndicators();
    }

    private void Close()
    {
        _isOpen = false;
        _wrappedLines.Clear();
        _title = null;
        _scroll = 0;
    }

    private void WrapText(string text, float maxWidth)
    {
        // Normaliser les fins de ligne Windows \r\n -> \n
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (var paragraph in normalized.Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                _wrappedLines.Add(string.Empty);
                continue;
            }

            var current = new System.Text.StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (_fonts.MeasureWidth(candidate) <= maxWidth || current.Length == 0)
                {
                    current.Clear();
                    current.Append(candidate);
                    continue;
                }

                _wrappedLines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }

            _wrappedLines.Add(current.ToString());
        }
    }

    private int VisibleLines()
    {
        var startY = _rectangle.Y + Layout.Padding;
        if (_title is { Length: > 0 })
        {
            startY += Fonts.LineHeight + Layout.Padding;
        }

        var available = _rectangle.Height - Layout.Padding - (startY - _rectangle.Y) - CloseButtonHeight - Layout.Padding;
        return Math.Max(1, (int)(available / Fonts.LineHeight));
    }

    private void DrawScrollIndicators()
    {
        var indicatorX = _rectangle.X + _rectangle.Width - Layout.Padding - IndicatorSize;
        var indicatorY = _rectangle.Y + Layout.Padding + IndicatorSize;

        // Haut : du texte au-dessus.
        if (_scroll > 0)
        {
            Raylib.DrawTriangle(
                new Vector2(indicatorX + IndicatorSize / 2f, indicatorY),
                new Vector2(indicatorX, indicatorY + IndicatorSize),
                new Vector2(indicatorX + IndicatorSize, indicatorY + IndicatorSize),
                Palette.Text);
        }

        // Bas : du texte en dessous.
        var lastVisible = _scroll + VisibleLines() - 1;
        if (lastVisible < _wrappedLines.Count - 1)
        {
            // Laisser de l'espace au-dessus du bouton "Fermer" (CloseButtonHeight + padding).
            var bottomY = _rectangle.Y + _rectangle.Height - Layout.Padding - CloseButtonHeight - Layout.Padding - IndicatorSize;
            // Comme ArrowsPanel South : (centre-bas) -> (haut-droite) -> (haut-gauche)
            Raylib.DrawTriangle(
                new Vector2(indicatorX + IndicatorSize / 2f, bottomY),
                new Vector2(indicatorX + IndicatorSize, bottomY - IndicatorSize),
                new Vector2(indicatorX, bottomY - IndicatorSize),
                Palette.Text);
        }
    }

    private void UpdateCloseButton()
    {
        var mouse = Raylib.GetMousePosition();
        var buttonRect = CloseButtonRect();
        _closeHover = Raylib.CheckCollisionPointRec(mouse, buttonRect);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _closeHover)
        {
            _closePressed = true;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            if (_closePressed && _closeHover)
            {
                Close();
            }

            _closePressed = false;
        }
    }

    private void DrawCloseButton()
    {
        var rect = CloseButtonRect();
        var background = _closePressed
            ? Palette.Replacement
            : (_closeHover ? new Color(60, 75, 100, 255) : Palette.Panel);
        var textColour = _closeHover ? Palette.Text : Palette.DimText;

        Raylib.DrawRectangleRec(rect, background);
        Raylib.DrawRectangleLinesEx(rect, 1, Palette.Border);

        var textWidth = _fonts.MeasureWidth(CloseLabel);
        var textX = rect.X + (rect.Width - textWidth) / 2f;
        var textY = rect.Y + (rect.Height - Fonts.Size) / 2f;
        _fonts.Draw(CloseLabel, new Vector2(textX, textY), textColour);
    }

    private Rectangle CloseButtonRect()
    {
        var x = _rectangle.X + _rectangle.Width - CloseButtonWidth - Layout.Padding;
        var y = _rectangle.Y + _rectangle.Height - CloseButtonHeight - Layout.Padding;
        return new Rectangle(x, y, CloseButtonWidth, CloseButtonHeight);
    }
}
