using UnityEngine;

namespace SenseOfDirection.Compass
{
    /// <summary>
    /// Placeholder marker-icon sprites for the compass, generated procedurally
    /// at runtime (same technique Coomzy-Compass_UI's own CompassMark used for
    /// its circle/diamond/triangle textures - read as architectural reference
    /// only, this is an independent implementation; that mod ships no LICENSE
    /// file so its own pixel code isn't reusable, see RESEARCH.md's license
    /// notes on the other reference zips). No bundled art asset needed, and
    /// every shape is generated once and cached for the process lifetime.
    ///
    /// Real ripped assets are used instead wherever one exists (the campfire
    /// icon reuses <see cref="Labels.NativeAssets.CampfireIconSprite"/>, same
    /// as the off-screen indicator) - these are only for the things Sense of
    /// Direction has no native asset to pull for: the player "smiley" body/
    /// face, the generic ping ring, the item-ping diamond, and the small
    /// elevation arrow.
    /// </summary>
    public static class CompassIcons
    {
        private const int Size = 64;

        private static Sprite _filledCircle;
        private static Sprite _ringCircle;
        private static Sprite _diamond;
        private static Sprite _smileyFace;
        private static Sprite _triangle;

        public static Sprite FilledCircle => _filledCircle ??= BuildSprite(GenerateFilledCircle(Size, 0.46f));
        public static Sprite RingCircle => _ringCircle ??= BuildSprite(GenerateRingCircle(Size, 0.46f, 0.14f));
        public static Sprite Diamond => _diamond ??= BuildSprite(GenerateDiamond(Size, 0.46f));
        public static Sprite SmileyFace => _smileyFace ??= BuildSprite(GenerateSmileyFace(Size));
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

        private static Texture2D GenerateFilledCircle(int size, float radiusFraction)
        {
            var tex = NewTexture();
            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float radius = size * radiusFraction;
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float alpha = 1f - Mathf.Clamp01((dist - (radius - feather)) / feather);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateRingCircle(int size, float radiusFraction, float thicknessFraction)
        {
            var tex = NewTexture();
            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outerRadius = size * radiusFraction;
            float innerRadius = outerRadius * (1f - thicknessFraction * 2f);
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float outerAlpha = 1f - Mathf.Clamp01((dist - (outerRadius - feather)) / feather);
                    float innerAlpha = Mathf.Clamp01((dist - (innerRadius - feather)) / feather);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateDiamond(int size, float radiusFraction)
        {
            var tex = NewTexture();
            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float radius = size * radiusFraction;
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                    float alpha = 1f - Mathf.Clamp01((dist - (radius - feather)) / feather);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Two eye dots + a lower-half mouth arc, black on transparent - drawn on top of the (colored) filled-circle body, untinted.</summary>
        private static Texture2D GenerateSmileyFace(int size)
        {
            var tex = NewTexture();
            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            const float feather = 1.5f;

            float eyeRadius = size * 0.06f;
            float eyeOffsetX = size * 0.16f;
            float eyeOffsetY = size * 0.1f;
            Vector2 leftEye = new Vector2(cx - eyeOffsetX, cy + eyeOffsetY);
            Vector2 rightEye = new Vector2(cx + eyeOffsetX, cy + eyeOffsetY);

            float mouthRadius = size * 0.24f;
            float mouthThickness = size * 0.05f;
            Vector2 mouthCenter = new Vector2(cx, cy + size * 0.08f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 0f;

                    float distLeftEye = Vector2.Distance(new Vector2(x, y), leftEye);
                    alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01((distLeftEye - (eyeRadius - feather)) / feather));

                    float distRightEye = Vector2.Distance(new Vector2(x, y), rightEye);
                    alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01((distRightEye - (eyeRadius - feather)) / feather));

                    // Mouth: an annulus band, only the lower half (a smile arc), fading at both radial edges.
                    if (y < mouthCenter.y)
                    {
                        float distMouth = Vector2.Distance(new Vector2(x, y), mouthCenter);
                        float band = 1f - Mathf.Clamp01(Mathf.Abs(distMouth - mouthRadius) / mouthThickness);
                        alpha = Mathf.Max(alpha, band);
                    }

                    pixels[y * size + x] = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
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
