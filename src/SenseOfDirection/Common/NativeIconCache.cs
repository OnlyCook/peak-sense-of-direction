using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Wraps the game's own item icons (<c>Item.UIData.GetIcon()</c>, a
    /// <see cref="Texture2D"/> - the same art vanilla's inventory slots show,
    /// via <c>RawImage</c>) in <see cref="Sprite"/>s, which is what this mod's
    /// UI (<c>Image</c>-based throughout) needs instead.
    ///
    /// Cached per source texture, because <see cref="Sprite.Create"/> allocates
    /// a new Sprite object every call and the ask here is per-frame ("what icon
    /// does this highlight show"), not once per ping. Every item of the same
    /// kind shares one icon texture (it lives on the prefab's UIData), so the
    /// cache stays tiny - one entry per item type actually pinged in a session.
    /// The textures themselves are the game's own assets and are never
    /// destroyed while a run is loaded, so entries never need invalidating;
    /// a null key can't happen (callers null-check first).
    /// </summary>
    public static class NativeIconCache
    {
        private static readonly Dictionary<Texture2D, Sprite> Sprites = new Dictionary<Texture2D, Sprite>();

        public static Sprite SpriteFor(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }
            if (!Sprites.TryGetValue(texture, out Sprite sprite) || sprite == null)
            {
                Texture2D source = Mipmapped(texture);
                sprite = Sprite.Create(
                    source, new Rect(0f, 0f, source.width, source.height),
                    new Vector2(0.5f, 0.5f), source.width);
                Sprites[texture] = sprite;
            }
            return sprite;
        }

        /// <summary>
        /// A mipmapped, trilinear-filtered copy of a game icon texture - the
        /// same anti-aliasing treatment <see cref="IconAssets"/> gives the mod's
        /// own bundled art, and needed here for exactly the same reason: these
        /// are large UI textures (the inventory slot draws them near their
        /// native size) shown by this mod far smaller (a ~30-44px crosshair /
        /// compass marker), and with no mip level to sample at that
        /// minification bilinear filtering aliases and shimmers instead of
        /// smoothing.
        ///
        /// A copy rather than just flipping the source's own filterMode/
        /// mipmapping, for two reasons: the game's textures are imported
        /// without a mip chain at all (there's nothing to switch trilinear
        /// filtering *to*, and mipmaps can't be added to an existing texture
        /// after upload), and mutating them would change how vanilla's own
        /// inventory UI draws the very same texture.
        ///
        /// The copy goes through a <see cref="RenderTexture"/> blit + ReadPixels
        /// rather than GetPixels/SetPixels, because a game texture is imported
        /// non-readable (no CPU-side copy kept) - GetPixels on one throws, while
        /// a GPU blit works regardless. Falls back to the original texture if
        /// anything about that path fails, so a broken copy can only ever cost
        /// the anti-aliasing, never the icon itself.
        /// </summary>
        private static Texture2D Mipmapped(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temp = null;
            try
            {
                temp = RenderTexture.GetTemporary(
                    texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(texture, temp);
                RenderTexture.active = temp;

                var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: true)
                {
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 4,
                };
                copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                copy.Apply(updateMipmaps: true);
                return copy;
            }
            catch (System.Exception e)
            {
                Plugin.Instance.Log.LogWarning(
                    $"NativeIconCache: couldn't build a mipmapped copy of '{texture.name}' ({e.Message}) - using it as-is.");
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temp != null)
                {
                    RenderTexture.ReleaseTemporary(temp);
                }
            }
        }

        /// <summary>
        /// The icon vanilla itself would show for this item (respecting the
        /// player's own bug-phobia/colorblind settings, since
        /// <c>ItemUIData.GetIcon()</c> swaps in <c>altIcon</c> for those) - or
        /// null for an item with no icon assigned at all, which callers treat
        /// as "fall back to the mod's own generic icon".
        /// </summary>
        public static Sprite ForItem(Item item)
        {
            return item == null || item.UIData == null ? null : SpriteFor(item.UIData.GetIcon());
        }
    }
}
