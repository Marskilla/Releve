using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;

namespace Releve.Game.Ui;

/// <summary>
/// Vue du lieu (§9.3) : image mise en cache et mise a l'echelle en conservant les proportions,
/// ou rectangle de remplacement portant le titre si l'image est introuvable (§9.2).
/// Zones d'image actives (AltState) superposees selon l'etat du jeu (J5, J6.3).
/// </summary>
public sealed class RoomViewPanel
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> _altTextures = new(StringComparer.Ordinal);
    private readonly Fonts _fonts;

    public RoomViewPanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    public void Draw(RoomView view)
    {
        var bounds = Layout.ViewBounds;

        Raylib.DrawRectangle((int)bounds.X - Layout.Padding, (int)bounds.Y - Layout.Padding,
            (int)bounds.Width + 2 * Layout.Padding, (int)bounds.Height + 2 * Layout.Padding, Palette.Panel);

        if (TryGetTexture(view.ImageKey, out var texture))
        {
            DrawStretched(texture, bounds);

            // Dessiner les zones AltState actives (J6.3)
            if (view.ActivePatches.Count > 0)
            {
                DrawActivePatches(view.ImageKey, view.ActivePatches, bounds);
            }
        }
        else
        {
            DrawReplacement(view.Title, bounds);
        }

        Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);
    }

    private void DrawStretched(Texture2D texture, Rectangle bounds)
    {
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, bounds, Vector2.Zero, 0f, Color.White);
    }

    private void DrawReplacement(string title, Rectangle bounds)
    {
        Raylib.DrawRectangleRec(bounds, Palette.Replacement);

        var size = _fonts.MeasureWidth(title);
        var position = new Vector2(
            bounds.X + (bounds.Width - size) / 2f,
            bounds.Y + bounds.Height / 2f - Fonts.Size);

        _fonts.Draw(title, position, Palette.DimText);
    }

    /// <summary>Dessine les zones d'image active depuis l'image _AltState.png.</summary>
    private void DrawActivePatches(string baseImageKey, IReadOnlyList<ImageRect> patches, Rectangle destBounds)
    {
        var altKey = baseImageKey + "_AltState";
        if (!TryGetAltTexture(altKey, out var altTexture))
        {
            return; // pas d'image alternative, on ne dessine rien
        }

        // Calculer l'echelle : l'image source a des dimensions connues (1774x887)
        const int sourceWidth = 1774;
        const int sourceHeight = 887;

        float scaleX = destBounds.Width / (float)sourceWidth;
        float scaleY = destBounds.Height / (float)sourceHeight;

        foreach (var patch in patches)
        {
            // Rectangle source dans l'image _AltState
            var srcRect = new Rectangle(patch.X1, patch.Y1, patch.X2 - patch.X1, patch.Y2 - patch.Y1);

            // Rectangle destination echelonne
            var dstRect = new Rectangle(
                destBounds.X + patch.X1 * scaleX,
                destBounds.Y + patch.Y1 * scaleY,
                srcRect.Width * scaleX,
                srcRect.Height * scaleY);

            Raylib.DrawTexturePro(altTexture, srcRect, dstRect, Vector2.Zero, 0f, Color.White);
        }
    }

    private bool TryGetTexture(string imageKey, out Texture2D texture)
    {
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            texture = default;
            return false;
        }

        if (_textures.TryGetValue(imageKey, out texture))
        {
            return Raylib.IsTextureValid(texture);
        }

        var path = Assets.RoomImage(imageKey);
        if (!File.Exists(path))
        {
            texture = default;
            return false;
        }

        texture = Raylib.LoadTexture(path);
        if (!Raylib.IsTextureValid(texture))
        {
            return false;
        }

        _textures[imageKey] = texture;
        return true;
    }

    private bool TryGetAltTexture(string altKey, out Texture2D texture)
    {
        if (_altTextures.TryGetValue(altKey, out texture))
        {
            return Raylib.IsTextureValid(texture);
        }

        var path = Assets.RoomImage(altKey);
        if (!File.Exists(path))
        {
            texture = default;
            return false;
        }

        texture = Raylib.LoadTexture(path);
        if (!Raylib.IsTextureValid(texture))
        {
            return false;
        }

        _altTextures[altKey] = texture;
        return true;
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            if (Raylib.IsTextureValid(texture))
            {
                Raylib.UnloadTexture(texture);
            }
        }

        foreach (var texture in _altTextures.Values)
        {
            if (Raylib.IsTextureValid(texture))
            {
                Raylib.UnloadTexture(texture);
            }
        }

        _textures.Clear();
        _altTextures.Clear();
    }
}
