using System.Numerics;
using System.Text;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Console d'interaction : historique, retour a la ligne automatique, ancrage en bas,
/// ligne de saisie editable et defilement vertical (§9.3).
/// </summary>
public sealed class ConsolePanel
{
    private const string Prompt = "> ";

    private readonly Fonts _fonts;
    private readonly List<string> _lines = new();
    private readonly List<string> _sent = new();
    private string _current = string.Empty;
    private int _cursor;
    private int _scroll;
    private int _recallIndex = -1;
    private int _visibleLines = 1;

    public ConsolePanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    public void Append(IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            Append(message);
        }
    }

    public void Append(string message)
    {
        foreach (var line in Wrap(message ?? string.Empty))
        {
            _lines.Add(line);
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
        var maxWidth = bounds.Width - 2 * Layout.Padding;

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
            _fonts.Draw(_lines[i], new Vector2(textX, y), Palette.Text);
            y += Fonts.LineHeight;
        }

        DrawInputLine(textX, inputY, maxWidth);
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

        if (Raylib.IsKeyPressed(KeyboardKey.Back))
        {
            if (_cursor > 0)
            {
                _current = _current.Remove(_cursor - 1, 1);
                _cursor--;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Delete))
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
        var maxWidth = Layout.ConsoleWidth - 2 * Layout.Padding;
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
