using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Game.Ui;

/// <summary>
/// Visualiseur d'image modal carré.
/// Image étirée sur tout le carré, bouton « Fermer » dessiné par-dessus.
/// Mêmes règles de modalité et anti-clic fantôme que TextViewer.
/// </summary>
public sealed class ImageViewer
{
    private const string CloseLabel = "Fermer";
    private const float CloseButtonHeight = 36f;
    private const float CloseButtonWidth = 140f;

    private readonly Fonts _fonts;
    private Rectangle _bounds;
    private Texture2D? _texture;
    private string? _imageKey;
    private bool _isOpen;
    private bool _openedThisFrame;
    private DialogButtons? _dialogButtons;

    public ImageViewer(Fonts fonts)
    {
        _fonts = fonts;
    }

    /// <summary>Vrai tant que le visualiseur est ouvert.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Ouvre le visualiseur d'image.</summary>
    public void Open(string imageKey)
    {
        _imageKey = imageKey;
        _bounds = Layout.ImageDialogBounds;
        _isOpen = true;
        _openedThisFrame = true;
        _texture = null;

        // Charger la texture
        var path = Assets.ViewImage(imageKey);
        if (File.Exists(path))
        {
            var tex = Raylib.LoadTexture(path);
            if (Raylib.IsTextureValid(tex))
            {
                _texture = tex;
            }
        }

        // Bouton « Fermer » unique — aligné en bas avec marge (DialogButtons centre sur _centerY)
        float marginBottom = Layout.Padding; // marge bas dans la modale
        // _centerY = bas_bouton - ButtonHeight/2 = (bounds.Bottom - marginBottom) - CloseButtonHeight/2
        float buttonsY = _bounds.Y + _bounds.Height - marginBottom - CloseButtonHeight / 2f;
        _dialogButtons = new DialogButtons(
            new[] { new DialogChoice(DialogIds.Close, CloseLabel) },
            _fonts,
            buttonsY);
        _dialogButtons.SetDefaultIndex(0);
    }

    /// <summary>Met a jour le visualiseur. Renvoie vrai a la fermeture.</summary>
    public bool Update()
    {
        if (!_isOpen)
        {
            return false;
        }

        // Anti-clic fantome
        if (_openedThisFrame)
        {
            _openedThisFrame = false;
            while (Raylib.GetCharPressed() > 0) { }
            return false;
        }

        // Gerer le bouton Fermer via DialogButtons
        if (_dialogButtons is not null)
        {
            int chosenIndex = _dialogButtons.Update();
            if (chosenIndex >= 0)
            {
                Close();
                return true;
            }

            // Echap = ferme
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                Close();
                return true;
            }
        }

        return false;
    }

    /// <summary>Dessine le visualiseur.</summary>
    public void Draw()
    {
        if (!_isOpen)
        {
            return;
        }

        // Fond opaque
        Raylib.DrawRectangleRec(_bounds, Palette.Panel);
        Raylib.DrawRectangleLinesEx(_bounds, 1, Palette.Border);

        // Image etiree sur tout le carre (marge interieure pour la bordure + bouton en bas)
        float marginBottom = Layout.Padding; // marge bas dans la modale
        // Laisser de l'espace pour le bouton (hauteur bouton + marge bas) + petite marge visuelle
        float imageBottomMargin = CloseButtonHeight + marginBottom + 4f;
        var innerBounds = new Rectangle(
            _bounds.X + 1,
            _bounds.Y + 1,
            _bounds.Width - 2,
            _bounds.Height - 2 - imageBottomMargin);

        if (_texture is { } tex && Raylib.IsTextureValid(tex))
        {
            Raylib.DrawTexturePro(tex,
                new Rectangle(0, 0, tex.Width, tex.Height),
                innerBounds,
                Vector2.Zero, 0f, Color.White);
        }
        else
        {
            // Image introuvable : texte au centre
            var msg = "Image introuvable.";
            var textWidth = _fonts.MeasureWidth(msg);
            var pos = new Vector2(
                _bounds.X + (_bounds.Width - textWidth) / 2f,
                _bounds.Y + (_bounds.Height - Fonts.Size) / 2f);
            _fonts.Draw(msg, pos, Palette.DimText);
        }

        // Dessiner le bouton Fermer par-dessus
        if (_dialogButtons is not null)
        {
            _dialogButtons.Draw();
        }
    }

    private void Close()
    {
        _isOpen = false;
        _imageKey = null;
        _dialogButtons = null;

        // Liberer la texture
        if (_texture is { } tex && Raylib.IsTextureValid(tex))
        {
            Raylib.UnloadTexture(tex);
        }
        _texture = null;
    }
}