using UnityEngine;

namespace SenseOfDirection.Compass
{
    /// <summary>
    /// Placeholder marker-icon sprites for the compass, generated procedurally
    /// at runtime (same technique Coomzy-Compass_UI's own CompassMark used for
    /// its circle/diamond/triangle textures - read as architectural reference
    /// only, this is an independent implementation; that mod ships no LICENSE
    /// file so its own pixel code isn't reusable, see RESEARCH.md's license
    /// notes on the other reference zips).
    ///
    /// The player face/ping ring/item-ping diamond markers now use real
    /// bundled icon art instead (<see cref="Common.IconAssets"/>) rather than
    /// these procedural shapes - <see cref="CompassMarkerWidget"/> pulls those
    /// directly. What's left here is only what still has no dedicated art:
    /// the elevation-arrow triangle and the tape's own baseline fade.
    /// </summary>
    public static class CompassIcons
    {
        private const int Size = 64;

        private static Sprite _triangle;

        public static Sprite Triangle => _triangle ??= BuildSprite(GenerateTriangle(Size));

        private static Sprite _horizontalFadeLine;

        /// <summary>
        /// The one continuous baseline the tape's markers sit on (Coomzy-
        /// Compass_UI's own minimal look, read as reference only - see this
        /// file's own top-level doc comment): a plain horizontal bar that
        /// fades to transparent over its outer ~18% at each end, rather than
        /// a hard-edged rectangle, so it doesn't look like it's being cut off
        /// by the tape's edges.
        /// </summary>
        public static Sprite HorizontalFadeLine => _horizontalFadeLine ??= BuildSprite(GenerateHorizontalFadeLine(256, 8, 0.18f));

        private static Sprite BuildSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }

        private static Texture2D NewTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            return tex;
        }

        /// <summary>Upward-pointing triangle, feathered edges. Reused for the elevation arrow (rotated 180° for "below") and rotated for other directional needs.</summary>
        private static Texture2D GenerateTriangle(int size)
        {
            var tex = NewTexture();
            var pixels = new Color32[size * size];
            // Texture row 0 = bottom of the resulting sprite (Unity's pixel-data
            // convention), so the apex needs the *larger* y to point "up" at
            // the default (unrotated) orientation - matches Pings/PingWidget's
            // own "arrow art points up at rotation 0" convention.
            Vector2 apex = new Vector2(size * 0.5f, size * 0.88f);
            Vector2 baseLeft = new Vector2(size * 0.16f, size * 0.15f);
            Vector2 baseRight = new Vector2(size * 0.84f, size * 0.15f);
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = SignedEdgeDistance(p, apex, baseLeft);
                    float d2 = SignedEdgeDistance(p, baseLeft, baseRight);
                    float d3 = SignedEdgeDistance(p, baseRight, apex);

                    float inside = Mathf.Min(d1, Mathf.Min(d2, d3));
                    float alpha = Mathf.Clamp01(0.5f + inside / feather);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Positive = left of the a->b edge (inside, given CCW winding); used as a smooth signed distance for triangle-edge feathering.</summary>
        private static float SignedEdgeDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            Vector2 normal = new Vector2(-edge.y, edge.x).normalized;
            return Vector2.Dot(p - a, normal);
        }

        private static Texture2D GenerateHorizontalFadeLine(int width, int height, float fadeFraction)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[width * height];
            float fadeWidth = width * fadeFraction;

            for (int x = 0; x < width; x++)
            {
                float distFromEdge = Mathf.Min(x, width - 1 - x);
                float alpha = Mathf.Clamp01(distFromEdge / fadeWidth);
                var color = new Color(1f, 1f, 1f, alpha);
                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = color;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
