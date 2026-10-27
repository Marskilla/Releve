using System.Numerics;
using Raylib_cs;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Game.Ui;

/// <summary>
/// Vue du lieu : image mise en cache et mise à l'échelle,
/// ou rectangle de remplacement portant le titre si l'image est introuvable.
/// Zones d'image actives (AltState) superposées selon l'état du jeu (patches d'état, transitions).
/// Transitions de changement de pièce (défilement 0,4s).
/// </summary>
public sealed class RoomViewPanel
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> _altTextures = new(StringComparer.Ordinal);
    private readonly Fonts _fonts;

    // Transition de pièce (défilement 0,4s)
    private const float TransitionDuration = 0.4f;
    private RoomView? _fromView;
    private RoomView? _toView;
    private Direction _transitionDir;
    private float _transitionTimer;
    private bool _ignoreFirstTransitionUpdate;

    public RoomViewPanel(Fonts fonts)
    {
        _fonts = fonts;
    }

public void Draw(RoomView view)
        {
            if (IsTransitioning)
            {
                DrawTransition();
            }
            else
            {
                DrawView(view, 0f, 0f, true);
            }
        }

    /// <summary>Dessine une vue décalée de dx, dy.</summary>
    private void DrawView(RoomView view, float dx, float dy, bool drawBackground = true)
    {
        var bounds = Layout.ViewBounds;
        var shiftedBounds = new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width, bounds.Height);

        if (drawBackground)
        {
            Raylib.DrawRectangle((int)bounds.X - Layout.Padding, (int)bounds.Y - Layout.Padding,
                (int)bounds.Width + 2 * Layout.Padding, (int)bounds.Height + 2 * Layout.Padding, Palette.Panel);
        }

        if (TryGetTexture(view.ImageKey, out var texture))
        {
            DrawStretched(texture, shiftedBounds);

            // Dessiner les zones AltState actives (patches d'état)
            if (view.ActivePatches.Count > 0)
            {
                DrawActivePatches(texture, view.ActivePatches, shiftedBounds);
            }
        }
        else
        {
            DrawReplacement(view.Title, shiftedBounds);
        }

        if (drawBackground)
        {
            Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);
        }
    }

    /// <summary>Dessin pendant une transition.</summary>
    private void DrawTransition()
    {
        if (_fromView is null || _toView is null)
        {
            return;
        }

        float p = MathF.Min(_transitionTimer / TransitionDuration, 1f);
        // Progression adoucie (smoothstep)
        float e = p * p * (3f - 2f * p);

        var bounds = Layout.ViewBounds;
        float W = bounds.Width;
        float H = bounds.Height;

        // Calcul des décalages selon la direction
        (float fromDx, float fromDy, float toDx, float toDy) = _transitionDir switch
        {
            Direction.East  => (-e * W, 0f,  W - e * W, 0f),
            Direction.West  => ( e * W, 0f, -W + e * W, 0f),
            Direction.North => (0f,  e * H, 0f, -H + e * H),
            Direction.South => (0f, -e * H, 0f,  H - e * H),
            _ => (0f, 0f, 0f, 0f)
        };

        // Fond du panneau (une seule fois)
        Raylib.DrawRectangle((int)bounds.X - Layout.Padding, (int)bounds.Y - Layout.Padding,
            (int)bounds.Width + 2 * Layout.Padding, (int)bounds.Height + 2 * Layout.Padding, Palette.Panel);

        // BeginScissorMode sur ViewBounds
        Raylib.BeginScissorMode((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);

        // Dessiner la vue sortante puis l'entrante SANS fond (deja dessine)
        DrawView(_fromView, fromDx, fromDy, false);
        DrawView(_toView, toDx, toDy, false);

        Raylib.EndScissorMode();

        // Le cadre est dessine apres le scissor (par-dessus)
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

    /// <summary>Dessine les zones d'image active avec arrondi séparé de chaque bord.</summary>
    private void DrawActivePatches(Texture2D baseTexture, IReadOnlyList<ImageRect> patches, Rectangle destBounds)
    {
        var altKey = GetAltKeyForTexture(baseTexture);
        if (string.IsNullOrEmpty(altKey) || !TryGetAltTexture(altKey, out var altTexture))
        {
            return; // pas d'image alternative, on ne dessine rien
        }

        // Dimensions de référence lues dans la texture de base chargée
        float sourceWidth = baseTexture.Width;
        float sourceHeight = baseTexture.Height;

        float scaleX = destBounds.Width / sourceWidth;
        float scaleY = destBounds.Height / sourceHeight;

        foreach (var patch in patches)
        {
            // Rectangle source dans l'image _AltState
            var srcRect = new Rectangle(patch.X1, patch.Y1, patch.X2 - patch.X1, patch.Y2 - patch.Y1);

            // Arrondi séparé de chaque bord
            float left = destBounds.X + MathF.Round(patch.X1 * scaleX);
            float right = destBounds.X + MathF.Round(patch.X2 * scaleX);
            float top = destBounds.Y + MathF.Round(patch.Y1 * scaleY);
            float bottom = destBounds.Y + MathF.Round(patch.Y2 * scaleY);

            var dstRect = new Rectangle(left, top, right - left, bottom - top);

            Raylib.DrawTexturePro(altTexture, srcRect, dstRect, Vector2.Zero, 0f, Color.White);
        }
    }

    private string? GetAltKeyForTexture(Texture2D texture)
    {
        foreach (var kvp in _textures)
        {
            if (kvp.Value.Id == texture.Id)
            {
                return kvp.Key + "_AltState";
            }
        }
        return null;
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

    /// <summary>Démarre une transition de pièce.</summary>
    /// <param name="from">Vue d'origine.</param>
    /// <param name="to">Vue de destination.</param>
    /// <param name="dir">Direction du deplacement (Est, Ouest, Nord, Sud).</param>
    public void BeginTransition(RoomView from, RoomView to, Direction dir)
    {
        _fromView = from;
        _toView = to;
        _transitionDir = dir;
        _transitionTimer = 0f;
        _ignoreFirstTransitionUpdate = true;

        // Precharger les textures des deux vues (base et AltState)
        PreloadViewTextures(from);
        PreloadViewTextures(to);
    }

    private void PreloadViewTextures(RoomView view)
    {
        TryGetTexture(view.ImageKey, out _);
        if (view.ActivePatches.Count > 0)
        {
            var altKey = view.ImageKey + "_AltState";
            TryGetAltTexture(altKey, out _);
        }
    }

    /// <summary>Met à jour la transition. Renvoie vrai si la transition est terminée.</summary>
    public void Update(float dt)
    {
        if (!IsTransitioning)
        {
            return;
        }

        if (_ignoreFirstTransitionUpdate)
        {
            _ignoreFirstTransitionUpdate = false;
            return;
        }

        _transitionTimer += dt;
        if (_transitionTimer >= TransitionDuration)
        {
            _transitionTimer = TransitionDuration;
            CompleteTransition();
        }
    }

    /// <summary>Vrai si une transition est en cours.</summary>
    public bool IsTransitioning => _fromView != null && _toView != null && _transitionTimer < TransitionDuration;

    /// <summary>Termine instantanément la transition en cours.</summary>
    public void CompleteTransition()
    {
        _fromView = null;
        _toView = null;
        _transitionTimer = 0f;
        _ignoreFirstTransitionUpdate = false;
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