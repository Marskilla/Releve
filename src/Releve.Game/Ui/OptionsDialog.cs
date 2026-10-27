using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
using Releve.Engine.World;
using Releve.Game.Audio;

namespace Releve.Game.Ui;

/// <summary>
/// Dialogue modal « Options ».
/// Modale centrée (~480x260), opaque, titre « Options »,
/// deux cases à cocher « Musique » et « Plein écran », un bouton « Fermer ».
/// Cases dessinées en géométrie (carré de contour, repère plein quand coché).
/// Clavier : Haut/Bas et Tab/Maj+Tab = focus cyclique (3 contrôles).
/// Espace/Entrée = active le contrôle (bascule case ou ferme).
/// Échap = ferme.
/// Souris : survol = focus, clic gauche = bascule case ou ferme.
/// Mêmes règles de modalité et anti-clic fantôme.
/// </summary>
public sealed class OptionsDialog
{
    private const string TitleText = "Options";
    private const string MusicLabel = "Musique";
    private const string FullscreenLabel = "Plein ecran";
    private const string CloseLabel = "Fermer";

    private const float DialogWidth = 480f;
    private const float DialogHeight = 260f;
    private const float CheckboxSize = 24f;
    private const float CheckboxSpacing = 12f;
    private const float RowSpacing = 40f;
    private const float CloseButtonHeight = 36f;

    private readonly Fonts _fonts;
    private readonly SettingsStore _settingsStore;
    private readonly MusicPlayer _music;
    private readonly GameApp _gameApp;

    private Rectangle _bounds;
    private int _focusIndex; // 0 = Musique, 1 = Plein ecran, 2 = Fermer
    private bool _musicEnabled;
    private bool _fullscreenEnabled;
    private bool _openedThisFrame;
    private DialogButtons? _closeButtons;

    public OptionsDialog(Fonts fonts, SettingsStore settingsStore, MusicPlayer music, GameApp gameApp)
    {
        _fonts = fonts;
        _settingsStore = settingsStore;
        _music = music;
        _gameApp = gameApp;
    }

    /// <summary>Vrai tant que la fenetre est ouverte.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Ouvre le dialogue Options.</summary>
    public void Open()
    {
        // Centrer sur l'ecran logique 1280x720
        _bounds = new Rectangle(
            (Layout.ScreenWidth - DialogWidth) / 2f,
            (Layout.ScreenHeight - DialogHeight) / 2f,
            DialogWidth,
            DialogHeight);

        // Charger les reglages actuels
        var settings = _settingsStore.Load();
        _musicEnabled = settings.Music;
        _fullscreenEnabled = _gameApp.IsFullscreen; // Reflète l'etat reel a chaque frame

        _focusIndex = 0;
        _openedThisFrame = true;
        IsOpen = true;
    }

    /// <summary>Met a jour le dialogue. Renvoie vrai a la fermeture.</summary>
    public bool Update()
    {
        if (!IsOpen)
        {
            return false;
        }

        if (_openedThisFrame)
        {
            _openedThisFrame = false;
            while (Raylib.GetCharPressed() > 0) { }
            return false;
        }

        // La case « Plein écran » reflète _fullscreen à chaque image
        _fullscreenEnabled = _gameApp.IsFullscreen;

        // Clavier : navigation focus
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || (Raylib.IsKeyPressed(KeyboardKey.Tab) && (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift))))
        {
            _focusIndex = (_focusIndex - 1 + 3) % 3;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            _focusIndex = (_focusIndex + 1) % 3;
        }

        // Espace ou Entree = action sur le focus
        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            if (_focusIndex == 0)
            {
                ToggleMusic();
            }
            else if (_focusIndex == 1)
            {
                ToggleFullscreen();
            }
            else
            {
                Close();
                return true;
            }
        }

        // Echap = ferme
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            Close();
            return true;
        }

        // Souris : survol et clic
        var mousePos = Raylib.GetMousePosition();
        var checkboxRects = GetCheckboxRects();
        var closeRect = GetCloseRect();

        // Survol case Musique
        if (Raylib.CheckCollisionPointRec(mousePos, checkboxRects[0]))
        {
            _focusIndex = 0;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                ToggleMusic();
            }
        }
        // Survol case Plein ecran
        else if (Raylib.CheckCollisionPointRec(mousePos, checkboxRects[1]))
        {
            _focusIndex = 1;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                ToggleFullscreen();
            }
        }
        // Survol bouton Fermer
        else if (Raylib.CheckCollisionPointRec(mousePos, closeRect))
        {
            _focusIndex = 2;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Close();
                return true;
            }
        }

        return false;
    }

    private void ToggleMusic()
    {
        _musicEnabled = !_musicEnabled;
        _music.SetEnabled(_musicEnabled);
        SaveSettings();
    }

    private void ToggleFullscreen()
    {
        _gameApp.SetFullscreen(!_fullscreenEnabled);
        // _fullscreenEnabled sera mis a jour a la frame suivante via _gameApp.IsFullscreen
    }

    private void SaveSettings()
    {
        var settings = _settingsStore.Load();
        settings = settings with { Music = _musicEnabled };
        _settingsStore.Save(settings);
    }

    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>Dessine le dialogue Options.</summary>
    public void Draw()
    {
        if (!IsOpen)
        {
            return;
        }

        // Creer le bouton Fermer pour le dessin
        if (_closeButtons == null)
        {
            var closeY = _bounds.Y + _bounds.Height - Layout.Padding - 50f;
            _closeButtons = new DialogButtons(
                new[] { new DialogChoice(DialogIds.Close, CloseLabel) },
                _fonts,
                closeY);
            _closeButtons.SetDefaultIndex(0);
        }

        // Fond opaque
        Raylib.DrawRectangleRec(_bounds, Palette.Panel);
        Raylib.DrawRectangleLinesEx(_bounds, 2f, Palette.Border);

        // Titre
        var titleWidth = _fonts.MeasureTitleWidth(TitleText);
        var titleX = _bounds.X + (_bounds.Width - titleWidth) / 2f;
        var titleY = _bounds.Y + Layout.Padding;
        _fonts.DrawTitle(TitleText, new Vector2(titleX, titleY), Palette.Text);

        // Lignes : Musique, Plein ecran
        var startY = titleY + Fonts.TitleSize + Fonts.TitleSpacing + Layout.Padding * 2;
        var leftX = _bounds.X + Layout.Padding * 3;

        DrawCheckboxRow(0, MusicLabel, _musicEnabled, leftX, startY, _focusIndex == 0);
        DrawCheckboxRow(1, FullscreenLabel, _fullscreenEnabled, leftX, startY + RowSpacing, _focusIndex == 1);

        // Dessiner le bouton Fermer
        if (_closeButtons is not null)
        {
            // Forcer la selection visuelle si focus sur Fermer
            // On ne peut pas changer l'index selectionne apres coup, donc on dessine manuellement
            var closeRect = _closeButtons.GetCloseRect();
            DrawCloseButton(closeRect, _focusIndex == 2);
        }
    }

    private void DrawCheckboxRow(int index, string label, bool checkedState, float x, float y, bool hasFocus)
    {
        var checkRect = new Rectangle(x, y + (Fonts.Size - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
        var labelX = x + CheckboxSize + CheckboxSpacing;
        var labelY = y;

        // Carre de contour
        Color borderColor = hasFocus ? Palette.Text : Palette.DimText;
        Raylib.DrawRectangleLinesEx(checkRect, 2f, borderColor);

        // Repere plein si coche
        if (checkedState)
        {
            var innerPadding = 4f;
            Raylib.DrawRectangle(
                (int)(checkRect.X + innerPadding),
                (int)(checkRect.Y + innerPadding),
                (int)(CheckboxSize - 2 * innerPadding),
                (int)(CheckboxSize - 2 * innerPadding),
                Palette.Text);
        }

        // Label
        Color labelColor = hasFocus ? Palette.Text : Palette.DimText;
        _fonts.Draw(label, new Vector2(labelX, labelY), labelColor);
    }

    private Rectangle[] GetCheckboxRects()
    {
        var titleY = _bounds.Y + Layout.Padding;
        var startY = titleY + Fonts.TitleSize + Fonts.TitleSpacing + Layout.Padding * 2;
        var leftX = _bounds.X + Layout.Padding * 3;

        return new[]
        {
            new Rectangle(leftX, startY + (Fonts.Size - CheckboxSize) / 2f, CheckboxSize, CheckboxSize),
            new Rectangle(leftX, startY + RowSpacing + (Fonts.Size - CheckboxSize) / 2f, CheckboxSize, CheckboxSize)
        };
    }

    private Rectangle GetCloseRect()
    {
        if (_closeButtons == null)
        {
            var closeY = _bounds.Y + _bounds.Height - Layout.Padding - 50f;
            _closeButtons = new DialogButtons(
                new[] { new DialogChoice(DialogIds.Close, CloseLabel) },
                _fonts,
                closeY);
            _closeButtons.SetDefaultIndex(0);
        }
        return _closeButtons.GetCloseRect();
    }

    private void DrawCloseButton(Rectangle rect, bool hasFocus)
    {
        var background = hasFocus
            ? new Color(80, 80, 100, 230)
            : new Color(40, 40, 60, 200);
        var textColour = hasFocus ? Palette.Text : Palette.DimText;
        var borderColor = hasFocus ? Palette.Text : Palette.DimText;

        Raylib.DrawRectangleRec(rect, background);
        Raylib.DrawRectangleLinesEx(rect, 2f, borderColor);

        var textWidth = _fonts.MeasureWidth(CloseLabel);
        var textX = rect.X + (rect.Width - textWidth) / 2f;
        var textY = rect.Y + (rect.Height - Fonts.Size) / 2f;
        _fonts.Draw(CloseLabel, new Vector2(textX, textY), textColour);
    }
}