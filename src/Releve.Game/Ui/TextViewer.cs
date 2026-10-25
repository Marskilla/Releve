using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Game.Ui;

/// <summary>
/// Visualiseur de texte modal et superpose (lot C3, J9).
/// Opaque : remplit son rectangle d'une couleur pleine et masque ce qu'il recouvre.
/// Texte avec retour a la ligne automatique, defilement Page/Haut/Bas, fermeture par
/// Echap, Entree ou bouton « Fermer » / boutons de dialogue.
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
    private bool _openedThisFrame;
    private bool _upIndicatorHover;
    private bool _downIndicatorHover;

    // J9-2: Boutons de dialogue
    private DialogButtons? _dialogButtons;
    private string? _cancelChoiceId;
    private string? _chosenId;

    public TextViewer(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Vrai tant que le visualiseur est ouvert.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Identifiant du choix valide (rempli a la validation).</summary>
    public string? ChosenId => _chosenId;

    /// <summary>J9-2: Ouvre le visualiseur avec boutons de dialogue.</summary>
    public void Open(Rectangle rectangle, string? title, string text,
        IReadOnlyList<DialogChoice> choices, string defaultChoiceId, string cancelChoiceId)
    {
        _rectangle = rectangle;
        _title = title;
        _wrappedLines.Clear();
        // Largeur reduite de 40 px pour la colonne des indicateurs de defilement
        WrapText(text, rectangle.Width - 2 * Layout.Padding - 40f);
        _scroll = 0;
        _isOpen = true;
        _openedThisFrame = true;
        _cancelChoiceId = cancelChoiceId;
        _chosenId = null;

        // Creer les boutons de dialogue (J9-1)
        // Les boutons sont places en bas du rectangle, centres
        float buttonsY = rectangle.Y + rectangle.Height - Layout.Padding - 50f;
        _dialogButtons = new DialogButtons(choices, _fonts, buttonsY);

        // Trouver l'index du choix par defaut
        int defaultIndex = 0;
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].Id == defaultChoiceId)
            {
                defaultIndex = i;
                break;
            }
        }
        _dialogButtons.SetDefaultIndex(defaultIndex);
    }

    /// <summary>Signature originale conservee (J9-2) : equivalentes a un seul bouton « Fermer » (Close).</summary>
    public void Open(Rectangle rectangle, string? title, string text)
    {
        Open(rectangle, title, text,
            new[] { new DialogChoice(DialogIds.Close, CloseLabel) },
            DialogIds.Close,
            DialogIds.Close);
    }

    /// <summary>Renvoie vrai lorsque le visualiseur est ferme (apres avoir traite l'entree de validation).</summary>
    public bool Update()
    {
        if (!_isOpen)
        {
            return false;
        }

        // Ignorer toute entree souris/clavier dans la frame d'ouverture (anti-clic fantome).
        if (_openedThisFrame)
        {
            _openedThisFrame = false;
            // Vider la file de caracteres
            while (Raylib.GetCharPressed() > 0) { }
            return false;
        }

        // J9-3: Si on a des boutons de dialogue, les gerer en priorite
        if (_dialogButtons is not null)
        {
            int chosenIndex = _dialogButtons.Update();
            if (chosenIndex >= 0)
            {
                _chosenId = _dialogButtons.SelectedChoiceId ?? _cancelChoiceId;
                Close();
                return true;
            }

            // Echap = valide cancelChoiceId
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                _chosenId = _cancelChoiceId;
                Close();
                return true;
            }
        }
        else
        {
            // Ancien comportement : Echap ou Entree ferme
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter)
                || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
            {
                Close();
                return true;
            }
        }

        // Defilement clavier (avec repetition pour Haut/Bas).
        var page = Math.Max(1, VisibleLines() - 1);
        var maxScroll = Math.Max(0, _wrappedLines.Count - VisibleLines());

        if (Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _scroll = Math.Max(0, _scroll - page);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _scroll = Math.Min(maxScroll, _scroll + page);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressedRepeat(KeyboardKey.Up))
        {
            _scroll = Math.Max(0, _scroll - 1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressedRepeat(KeyboardKey.Down))
        {
            _scroll = Math.Min(maxScroll, _scroll + 1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Home))
        {
            _scroll = 0;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.End))
        {
            _scroll = maxScroll;
        }

        // Indicateur haut (clic = -3 lignes).
        if (_scroll > 0)
        {
            var upRect = IndicatorUpRect();
            var mouse = Raylib.GetMousePosition();
            _upIndicatorHover = Raylib.CheckCollisionPointRec(mouse, upRect);
            if (_upIndicatorHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _scroll = Math.Max(0, _scroll - 3);
            }
        }
        else
        {
            _upIndicatorHover = false;
        }

        // Indicateur bas (clic = +3 lignes).
        if (_scroll < maxScroll)
        {
            var downRect = IndicatorDownRect();
            var mouse = Raylib.GetMousePosition();
            _downIndicatorHover = Raylib.CheckCollisionPointRec(mouse, downRect);
            if (_downIndicatorHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _scroll = Math.Min(maxScroll, _scroll + 3);
            }
        }
        else
        {
            _downIndicatorHover = false;
        }

        // Bouton « Fermer » a la souris (seulement si pas de boutons de dialogue).
        if (_dialogButtons is null && UpdateCloseButton())
        {
            return true;
        }

        return false;
    }

    /// <summary>Dessine la scene sous-jacente puis le visualiseur par-dessus.</summary>
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

        // J9-2: Dessiner les boutons de dialogue (remplace le bouton « Fermer »)
        if (_dialogButtons is not null)
        {
            _dialogButtons.Draw();
        }
        else
        {
            // Bouton « Fermer » classique.
            DrawCloseButton();
        }

        // Indicateurs de defilement (formes geometriques) - APRES les boutons pour ne pas etre couverts.
        DrawScrollIndicators();
    }

    private void Close()
    {
        _isOpen = false;
        _wrappedLines.Clear();
        _title = null;
        _scroll = 0;
        _dialogButtons = null;
        _cancelChoiceId = null;
    }

    private void WrapText(string text, float maxWidth)
    {
        _wrappedLines.AddRange(TextWrapper.Wrap(text, maxWidth, _fonts.MeasureWidth));
    }

    private int VisibleLines()
    {
        var startY = _rectangle.Y + Layout.Padding;
        if (_title is { Length: > 0 })
        {
            startY += Fonts.LineHeight + Layout.Padding;
        }

        // Reserver l'espace pour les boutons de dialogue ou le bouton Fermer
        float buttonsHeight = _dialogButtons is not null ? 60f : CloseButtonHeight + Layout.Padding;
        var available = _rectangle.Height - Layout.Padding - (startY - _rectangle.Y) - buttonsHeight;
        return Math.Max(1, (int)(available / Fonts.LineHeight));
    }

    private void DrawScrollIndicators()
    {
        var indicatorX = _rectangle.X + _rectangle.Width - Layout.Padding - IndicatorSize;
        var indicatorY = _rectangle.Y + Layout.Padding + IndicatorSize;

        // Haut : du texte au-dessus.
        if (_scroll > 0)
        {
            var color = _upIndicatorHover ? Palette.Prompt : Palette.Text;
            Raylib.DrawTriangle(
                new Vector2(indicatorX + IndicatorSize / 2f, indicatorY),
                new Vector2(indicatorX, indicatorY + IndicatorSize),
                new Vector2(indicatorX + IndicatorSize, indicatorY + IndicatorSize),
                color);
        }

        // Bas : du texte en dessous.
        var lastVisible = _scroll + VisibleLines() - 1;
        if (lastVisible < _wrappedLines.Count - 1)
        {
            // Laisser de l'espace au-dessus des boutons.
            float buttonsHeight = _dialogButtons is not null ? 60f : CloseButtonHeight + Layout.Padding;
            var bottomY = _rectangle.Y + _rectangle.Height - Layout.Padding - buttonsHeight - IndicatorSize;
            var color = _downIndicatorHover ? Palette.Prompt : Palette.Text;
            Raylib.DrawTriangle(
                new Vector2(indicatorX + IndicatorSize / 2f, bottomY),
                new Vector2(indicatorX + IndicatorSize, bottomY - IndicatorSize),
                new Vector2(indicatorX, bottomY - IndicatorSize),
                color);
        }
    }

    private bool UpdateCloseButton()
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
                _closePressed = false;
                return true;
            }

            _closePressed = false;
        }

        return false;
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

    private Rectangle IndicatorUpRect()
    {
        var indicatorX = _rectangle.X + _rectangle.Width - Layout.Padding - IndicatorSize;
        var indicatorY = _rectangle.Y + Layout.Padding + IndicatorSize;
        const float clickSize = 30f;
        // Centre sur la pointe du triangle (indicatorY)
        return new Rectangle(
            indicatorX + IndicatorSize / 2f - clickSize / 2f,
            indicatorY - clickSize / 2f,
            clickSize, clickSize);
    }

    private Rectangle IndicatorDownRect()
    {
        var indicatorX = _rectangle.X + _rectangle.Width - Layout.Padding - IndicatorSize;
        float buttonsHeight = _dialogButtons is not null ? 60f : CloseButtonHeight + Layout.Padding;
        var bottomY = _rectangle.Y + _rectangle.Height - Layout.Padding - buttonsHeight - IndicatorSize;
        const float clickSize = 30f;
        // Centre sur la pointe du triangle (bottomY)
        return new Rectangle(
            indicatorX + IndicatorSize / 2f - clickSize / 2f,
            bottomY - clickSize / 2f,
            clickSize, clickSize);
    }
}