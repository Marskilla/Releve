using System.Numerics;
using System.Text;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Console d'interaction : historique, retour a la ligne automatique, ancrage en bas,
/// ligne de saisie editable, defilement vertical et boutons de defilement cliquables (§9.3, lot B2).
/// </summary>
public sealed class ConsolePanel
{
    private const string Prompt = "> ";

    private const int ScrollButtonWidth = 28;
    private const int ScrollLines = 3;

    private readonly Fonts _fonts;
    private readonly List<LineEntry> _lines = new();
    private readonly List<string> _sent = new();
    private string _current = string.Empty;
    private int _cursor;
    private int _scroll;
    private int _recallIndex = -1;
    private int _visibleLines = 1;
    private bool _scrollUpHover;
    private bool _scrollDownHover;
    private bool _scrollUpPressed;
    private bool _scrollDownPressed;
    private int _scrollHoldTimer;

    public ConsolePanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Une ligne d'historique avec son type (saisie du joueur ou reponse du jeu).</summary>
    private sealed record LineEntry(string Text, bool IsInput);

    public void Append(IEnumerable<string> messages, bool isInput = false)
    {
        foreach (var message in messages)
        {
            Append(message, isInput);
        }
    }

    public void Append(string message, bool isInput = false)
    {
        foreach (var line in Wrap(message ?? string.Empty))
        {
            _lines.Add(new LineEntry(line, isInput));
        }

        _scroll = 0;
    }

    public void Clear()
    {
        _lines.Clear();
        _sent.Clear();
        _current = string.Empty;
        _cursor = 0;
        _scroll = 0;
        _recallIndex = -1;
    }

    /// <summary>Lit les entrees ; renvoie la commande lorsque le joueur valide une ligne non vide.</summary>
    public bool TryReadCommand(out string command)
    {
        ScrollWithKeyboard();
        HandleMouseScroll();
        ReadCharacters();

        command = string.Empty;
        if (!Raylib.IsKeyPressed(KeyboardKey.Enter) && !Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            return false;
        }

        var line = _current;
        ResetLine();

        if (string.IsNullOrWhiteSpace(line))
        {
            return false; // saisie vide ignoree (§6.7)
        }

        _sent.Add(line);
        command = line;
        return true;
    }

    public void Draw()
    {
        var bounds = Layout.ConsoleBounds;
        var textX = bounds.X + Layout.Padding;
        var historyWidth = bounds.Width - 2 * Layout.Padding - ScrollButtonWidth;
        var maxWidth = historyWidth;

        Raylib.DrawRectangleRec(bounds, Palette.Panel);
        Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);

        var inputY = bounds.Y + bounds.Height - Layout.Padding - Fonts.LineHeight;
        _visibleLines = Math.Max(1, (int)((inputY - bounds.Y - Layout.Padding) / Fonts.LineHeight));

        // Historique ancre en bas : la derniere ligne visible est juste au-dessus de la saisie.
        var last = Math.Max(0, _lines.Count - 1 - _scroll);
        var first = Math.Max(0, last - _visibleLines + 1);
        var y = inputY - Layout.Padding - (last - first + 1) * Fonts.LineHeight;

        for (var i = first; i <= last; i++)
        {
            var entry = _lines[i];
            _fonts.Draw(entry.Text, new Vector2(textX, y), entry.IsInput ? Palette.ConsoleInput : Palette.ConsoleResponse);
            y += Fonts.LineHeight;
        }

        DrawInputLine(textX, inputY, maxWidth);
        DrawScrollButtons(bounds, inputY, historyWidth);
    }

    private void DrawInputLine(float x, float y, float maxWidth)
    {
        var promptWidth = _fonts.MeasureWidth(Prompt);
        var caretX = x + promptWidth + _fonts.MeasureWidth(_current[.._cursor]);

        // Si le curseur sort de la zone visible, la ligne est decalee vers la gauche.
        var offset = caretX > x + maxWidth ? x + maxWidth - caretX : 0f;

        _fonts.Draw(Prompt, new Vector2(x + offset, y), Palette.Prompt);
        _fonts.Draw(_current, new Vector2(x + offset + promptWidth, y), Palette.Text);

        // Curseur clignotant.
        if ((int)(Raylib.GetTime() * 2.0) % 2 == 0)
        {
            Raylib.DrawRectangle((int)(caretX + offset), (int)y + 2, 2, Fonts.Size, Palette.Text);
        }
    }

    private void DrawScrollButtons(Rectangle bounds, float inputY, float historyWidth)
    {
        var buttonX = bounds.X + bounds.Width - ScrollButtonWidth;
        var buttonYTop = bounds.Y + Layout.Padding;
        var buttonYBottom = inputY - Layout.Padding - Fonts.LineHeight;

        var canScrollUp = _scroll > 0;
        var canScrollDown = _scroll < Math.Max(0, _lines.Count - 1);

        // Bouton haut : triangle pointe vers le haut.
        DrawScrollButton(buttonX, buttonYTop, ScrollButtonWidth, ScrollButtonWidth,
            direction: -1, canScroll: canScrollUp, ref _scrollUpHover, ref _scrollUpPressed);

        // Bouton bas : triangle pointe vers le bas.
        DrawScrollButton(buttonX, buttonYBottom, ScrollButtonWidth, ScrollButtonWidth,
            direction: 1, canScroll: canScrollDown, ref _scrollDownHover, ref _scrollDownPressed);
    }

    private void DrawScrollButton(float x, float y, float width, float height, int direction,
        bool canScroll, ref bool hover, ref bool pressed)
    {
        var mouse = Raylib.GetMousePosition();
        hover = canScroll && Raylib.CheckCollisionPointRec(mouse, new Rectangle(x, y, width, height));

        var background = canScroll
            ? (pressed ? Palette.Panel : (hover ? new Color(60, 75, 100, 255) : Palette.Panel))
            : new Color(40, 50, 68, 255);

        var arrowColour = canScroll
            ? (hover ? Palette.Text : Palette.DimText)
            : new Color(80, 95, 125, 255);

        Raylib.DrawRectangleRec(new Rectangle(x, y, width, height), background);
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, width, height), 1, Palette.Border);

        // Triangle de defilement (forme geometrique, pas de glyphe).
        var half = width / 2f;
        var centerY = y + height / 2f;
        var arrowSize = height * 0.5f;
        var offsetY = direction * arrowSize * 0.3f;

        if (direction < 0)
        {
            // Pointe vers le haut.
            Raylib.DrawTriangle(
                new Vector2(x + half, y + height * 0.3f + offsetY),
                new Vector2(x + width * 0.2f, y + height * 0.7f + offsetY),
                new Vector2(x + width * 0.8f, y + height * 0.7f + offsetY), arrowColour);
        }
        else
        {
            // Pointe vers le bas.
            Raylib.DrawTriangle(
                new Vector2(x + half, y + height * 0.7f + offsetY),
                new Vector2(x + width * 0.2f, y + height * 0.3f + offsetY),
                new Vector2(x + width * 0.8f, y + height * 0.3f + offsetY), arrowColour);
        }
    }

    private void HandleMouseScroll()
    {
        var mouse = Raylib.GetMousePosition();
        var bounds = Layout.ConsoleBounds;
        var inputY = bounds.Y + bounds.Height - Layout.Padding - Fonts.LineHeight;
        var historyWidth = bounds.Width - 2 * Layout.Padding - ScrollButtonWidth;

        var buttonX = bounds.X + bounds.Width - ScrollButtonWidth;
        var buttonYTop = bounds.Y + Layout.Padding;
        var buttonYBottom = inputY - Layout.Padding - Fonts.LineHeight;

        var rectTop = new Rectangle(buttonX, buttonYTop, ScrollButtonWidth, ScrollButtonWidth);
        var rectBottom = new Rectangle(buttonX, buttonYBottom, ScrollButtonWidth, ScrollButtonWidth);

        var canScrollUp = _scroll > 0;
        var canScrollDown = _scroll < Math.Max(0, _lines.Count - 1);

        var overTop = canScrollUp && Raylib.CheckCollisionPointRec(mouse, rectTop);
        var overBottom = canScrollDown && Raylib.CheckCollisionPointRec(mouse, rectBottom);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (overTop)
            {
                _scroll = Math.Max(0, _scroll - ScrollLines);
                _scrollUpPressed = true;
            }
            else if (overBottom)
            {
                _scroll = Math.Min(Math.Max(0, _lines.Count - 1), _scroll + ScrollLines);
                _scrollDownPressed = true;
            }
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            _scrollUpPressed = false;
            _scrollDownPressed = false;
        }

        // Repetition quand maintenu.
        if (_scrollUpPressed && canScrollUp)
        {
            _scrollHoldTimer++;
            if (_scrollHoldTimer > 10) // ~10 images avant repetition, puis toutes les 4 images
            {
                _scroll = Math.Max(0, _scroll - ScrollLines);
                if (_scrollHoldTimer % 4 == 0)
                {
                    _scrollHoldTimer = 10;
                }
            }
        }
        else if (_scrollDownPressed && canScrollDown)
        {
            _scrollHoldTimer++;
            if (_scrollHoldTimer > 10)
            {
                _scroll = Math.Min(Math.Max(0, _lines.Count - 1), _scroll + ScrollLines);
                if (_scrollHoldTimer % 4 == 0)
                {
                    _scrollHoldTimer = 10;
                }
            }
        }
        else
        {
            _scrollHoldTimer = 0;
        }
    }

    private void ScrollWithKeyboard()
    {
        var page = Math.Max(1, _visibleLines - 1);

        if (Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _scroll = Math.Min(_scroll + page, Math.Max(0, _lines.Count - 1));
        }

        if (Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _scroll = Math.Max(0, _scroll - page);
        }
    }

    private void ReadCharacters()
    {
        // Caracteres saisis, lus en Unicode (accents et symboles inclus).
        var pressed = Raylib.GetCharPressed();
        while (pressed > 0)
        {
            Insert(char.ConvertFromUtf32(pressed));
            pressed = Raylib.GetCharPressed();
        }

        var left = Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressedRepeat(KeyboardKey.Left);
        var right = Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressedRepeat(KeyboardKey.Right);

        if (Raylib.IsKeyPressed(KeyboardKey.Home) || Raylib.IsKeyPressedRepeat(KeyboardKey.Home))
        {
            _cursor = 0;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.End) || Raylib.IsKeyPressedRepeat(KeyboardKey.End))
        {
            _cursor = _current.Length;
        }

        if (left)
        {
            _cursor = Math.Max(0, _cursor - 1);
        }

        if (right)
        {
            _cursor = Math.Min(_current.Length, _cursor + 1);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace))
        {
            if (_cursor > 0)
            {
                _current = _current.Remove(_cursor - 1, 1);
                _cursor--;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressedRepeat(KeyboardKey.Delete))
        {
            if (_cursor < _current.Length)
            {
                _current = _current.Remove(_cursor, 1);
            }
        }

        RecallHistory();
    }

    private void RecallHistory()
    {
        if (!Raylib.IsKeyPressed(KeyboardKey.Up) && !Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            if (_sent.Count == 0)
            {
                return;
            }

            if (_recallIndex == -1)
            {
                _recallIndex = _sent.Count - 1;
            }
            else
            {
                _recallIndex = Math.Max(0, _recallIndex - 1);
            }

            _current = _sent[_recallIndex];
            _cursor = _current.Length;
        }
        else
        {
            if (_recallIndex == -1)
            {
                return;
            }

            _recallIndex++;
            if (_recallIndex >= _sent.Count)
            {
                _recallIndex = -1;
                ResetLine();
                return;
            }

            _current = _sent[_recallIndex];
            _cursor = _current.Length;
        }
    }

    private void Insert(string text)
    {
        _current = _current.Insert(_cursor, text);
        _cursor += text.Length;
    }

    private void ResetLine()
    {
        _current = string.Empty;
        _cursor = 0;
    }

    private List<string> Wrap(string text)
    {
        var maxWidth = Layout.ConsoleWidth - 2 * Layout.Padding - ScrollButtonWidth;
        var lines = new List<string>();

        foreach (var paragraph in text.Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var current = new StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (_fonts.MeasureWidth(candidate) <= maxWidth || current.Length == 0)
                {
                    current.Clear();
                    current.Append(candidate);
                    continue;
                }

                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }

            lines.Add(current.ToString());
        }

        return lines;
    }
}
