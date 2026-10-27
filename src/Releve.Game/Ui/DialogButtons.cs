using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;

namespace Releve.Game.Ui;

/// <summary>
/// Composant partagé pour une rangée de boutons centrée.
/// Largeur de chaque bouton = largeur du libellé + 2 * Layout.Padding, espacement 20 px.
/// Gère l'indice sélectionné, le survol, le clic et le dessin.
/// </summary>
public sealed class DialogButtons
{
    private const float ButtonSpacing = 20f;
    private const float ButtonHeight = 36f;

    private readonly IReadOnlyList<DialogChoice> _choices;
    private readonly Fonts _fonts;
    private readonly float _centerY;

    private int _selectedIndex;
    private int _hoverIndex = -1;
    private bool _mousePressedOnButton = false;
    private bool _openedThisFrame = true;
    private Rectangle[] _buttonRects = Array.Empty<Rectangle>();

    public DialogButtons(IReadOnlyList<DialogChoice> choices, Fonts fonts, float centerY)
    {
        _choices = choices;
        _fonts = fonts;
        _centerY = centerY;
        _selectedIndex = 0; // Sera defini par SetDefaultIndex
    }

    public int SelectedIndex => _selectedIndex;
    public string? SelectedChoiceId => _selectedIndex >= 0 && _selectedIndex < _choices.Count ? _choices[_selectedIndex].Id : null;

    /// <summary>Definit l'index du bouton par defaut (appeler apres construction).</summary>
    public void SetDefaultIndex(int index)
    {
        _selectedIndex = Math.Clamp(index, 0, _choices.Count - 1);
    }

    /// <summary>Met a jour l'etat (souris, clavier). Renvoie l'index du bouton valide, ou -1.</summary>
    public int Update()
    {
        // Anti-clic fantome : ne pas traiter l'entree dans l'image d'ouverture
        if (_openedThisFrame)
        {
            _openedThisFrame = false;
            // Vider la file de caracteres
            while (Raylib.GetCharPressed() > 0) { }
            return -1;
        }

        var mousePos = Raylib.GetMousePosition();

        // Calculer les rectangles si pas deja fait
        if (_buttonRects.Length != _choices.Count)
        {
            ComputeButtonRects();
        }

        // Souris : survol et clic
        _hoverIndex = -1;
        for (int i = 0; i < _buttonRects.Length; i++)
        {
            if (Raylib.CheckCollisionPointRec(mousePos, _buttonRects[i]))
            {
                _hoverIndex = i;
                _selectedIndex = i; // Le survol selectionne
                break;
            }
        }

        bool mouseDown = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool mousePressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool mouseReleased = Raylib.IsMouseButtonReleased(MouseButton.Left);

        if (mousePressed && _hoverIndex >= 0)
        {
            _mousePressedOnButton = true;
        }

        if (mouseReleased && _mousePressedOnButton && _hoverIndex >= 0)
        {
            _mousePressedOnButton = false;
            return _hoverIndex; // Validation par clic
        }

        if (!mouseDown)
        {
            _mousePressedOnButton = false;
        }

        // Clavier : fleches gauche/droite et Tab / Maj+Tab
        if (Raylib.IsKeyPressed(KeyboardKey.Right) || (Raylib.IsKeyPressed(KeyboardKey.Tab) && !Raylib.IsKeyDown(KeyboardKey.LeftShift) && !Raylib.IsKeyDown(KeyboardKey.RightShift)))
        {
            _selectedIndex = (_selectedIndex + 1) % _choices.Count;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Left) || (Raylib.IsKeyPressed(KeyboardKey.Tab) && (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift))))
        {
            _selectedIndex = (_selectedIndex - 1 + _choices.Count) % _choices.Count;
        }

        // Entree = validation
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            return _selectedIndex;
        }

        return -1;
    }

    /// <summary>Dessine les boutons.</summary>
    public void Draw()
    {
        if (_buttonRects.Length != _choices.Count)
        {
            ComputeButtonRects();
        }

        for (int i = 0; i < _choices.Count; i++)
        {
            var rect = _buttonRects[i];
            bool isSelected = i == _selectedIndex;
            bool isHover = i == _hoverIndex;

            // Fond
            Color bgColor;
            if (isSelected)
            {
                bgColor = new Color(80, 80, 100, 230); // Fond plus clair pour selectionne
            }
            else if (isHover)
            {
                bgColor = new Color(60, 60, 80, 220);
            }
            else
            {
                bgColor = new Color(40, 40, 60, 200);
            }

            Raylib.DrawRectangleRec(rect, bgColor);

            // Bordure
            Color borderColor = isSelected ? Palette.Text : Palette.DimText;
            Raylib.DrawRectangleLinesEx(rect, 2f, borderColor);

            // Texte centre
            var label = _choices[i].Label;
            var textWidth = _fonts.MeasureWidth(label);
            var textX = rect.X + (rect.Width - textWidth) / 2f;
            var textY = rect.Y + (rect.Height - Fonts.Size) / 2f;
            _fonts.Draw(label, new Vector2(textX, textY), Palette.Text);
        }
    }

    private void ComputeButtonRects()
    {
        var buttonWidths = _choices.Select(c => _fonts.MeasureWidth(c.Label) + 2 * Layout.Padding).ToArray();
        float totalWidth = buttonWidths.Sum() + ButtonSpacing * (_choices.Count - 1);
        float startX = (Layout.ScreenWidth - totalWidth) / 2f;
        float y = _centerY - ButtonHeight / 2f;

        _buttonRects = new Rectangle[_choices.Count];
        float currentX = startX;
        for (int i = 0; i < _choices.Count; i++)
        {
            _buttonRects[i] = new Rectangle(currentX, y, buttonWidths[i], ButtonHeight);
            currentX += buttonWidths[i] + ButtonSpacing;
        }
    }

    /// <summary>Renvoie le rectangle du bouton « Fermer » (premier bouton si un seul).</summary>
    public Rectangle GetCloseRect()
    {
        if (_buttonRects.Length == 0)
        {
            ComputeButtonRects();
        }
        return _buttonRects[0];
    }
}