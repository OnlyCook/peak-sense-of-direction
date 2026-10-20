using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// The mod's shared "native-looking panel" chrome: a rounded, heavily
    /// outlined blue panel whose silhouette is roughened with fractal noise
    /// (a torn/crumpled-paper edge rather than a clean vector outline) and
    /// whose fill carries a baked fbm grain, matching PEAK's own boarding-pass/
    /// map-rotation panels.
    ///
    /// Ported from the maintainer's other PEAK mod (`peak-checkpoint-save`'s
    /// `SavePicker`/`HelpScreen`, MIT, same author) so this mod's config
    /// preview menu reads as part of the same menu system rather than a
    /// different-looking overlay. Same palette, same jag/grain constants -
    /// deliberately not re-tuned, since the whole point is that it matches.
    ///
    /// Everything here is baked once into cached <see cref="Sprite"/>s and then
    /// reused: the jagged edge "animates" by cycling three pre-seeded variants
    /// of the same shape (<see cref="JagFrameCount"/>) on a fixed interval, not
    /// by regenerating a texture per frame - swapping <c>Image.sprite</c>
    /// between three already-built textures is effectively free, which is the
    /// entire reason the noise is baked ahead of time instead of computed live.
    /// </summary>
    internal static class PanelChrome
    {
        internal static readonly Color DimColor = new Color(0f, 0f, 0f, 0.78f);
        internal static readonly Color PanelFillColor = new Color(0x34 / 255f, 0x54 / 255f, 0xD1 / 255f); // #3454D1
        internal static readonly Color PanelBorderColor = new Color(0x21 / 255f, 0x31 / 255f, 0x7E / 255f); // #21317E
        internal static readonly Color BadgeBorderColor = new Color(0x0A / 255f, 0x0D / 255f, 0x1A / 255f); // #0A0D1A
        internal static readonly Color TitleColor = new Color(0.98f, 0.99f, 1f);
        internal static readonly Color BodyColor = new Color(0.93f, 0.95f, 1f);
        internal static readonly Color FooterColor = new Color(0.85f, 0.9f, 1f);
        internal static readonly Color ChipFillColor = new Color(0.10f, 0.16f, 0.44f);
        internal static readonly Color ChipTextColor = new Color(1f, 0.95f, 0.72f);

        /// <summary>
        /// The selected tab. The fill is a *tint*, not a paint: it multiplies into
        /// the badge sprite, which already has a dark chip baked into it, so the
        /// bright amber below lands as a warm dark gold rather than as a highlight.
        /// The text therefore has to be light - dark-on-bright, the contrast the
        /// game's own menus use, is not available here, and dark text over that
        /// tinted chip is what made the selected tab the hardest one to read.
        /// </summary>
        internal static readonly Color SelectedFillColor = new Color(1f, 0.82f, 0.22f, 0.97f);

        /// <summary>Darkened off pure white (which read as glaring against the tab's own amber fill) but kept light enough to still contrast with it.</summary>
        internal static readonly Color SelectedTextColor = new Color(0.85f, 0.78f, 0.55f);

        /// <summary>The description box's un-hovered placeholder text - deliberately dimmer than every real description, which use <see cref="FooterColor"/>, so a glance at the box can tell "nothing hovered yet" from "here's what this setting does".</summary>
        internal static readonly Color PlaceholderTextColor = new Color(0.55f, 0.55f, 0.58f);

        /// <summary>The hovered description's "default: ..." sub-line - lighter than <see cref="PlaceholderTextColor"/> so it doesn't read as a second placeholder, but still clearly a step down from the description text above it.</summary>
        internal static readonly Color DefaultValueTextColor = new Color(0.68f, 0.70f, 0.75f);

        /// <summary>
        /// The color the settings list's rows dissolve into at a scrollable edge
        /// (see <see cref="ScrollFadeSprite"/>). It's the panel's own fill darkened
        /// by roughly what the list's plate - a 20%-black Image over that fill -
        /// darkens it, so a row sliding under the fade reads as sinking into the
        /// plate rather than under a grey smear laid on top of it. Alpha is the
        /// fade's peak strength at the very edge; the sprite ramps down from there.
        ///
        /// Opaque at that peak, deliberately: anything less leaves the plate's own
        /// rounded corner showing faintly *through* the fade, which is exactly the
        /// artifact a translucent version of this had.
        /// </summary>
        internal static readonly Color ScrollFadeColor = new Color(
            PanelFillColor.r * 0.8f, PanelFillColor.g * 0.8f, PanelFillColor.b * 0.8f, 1f);

        internal const float PanelCornerRadius = 26f;
        internal const float PanelBorderThickness = 11f;

        /// <summary>The border grows outward rather than eating into the fill, so content padding is unaffected by its thickness.</summary>
        internal const float PanelOuterMargin = PanelBorderThickness - 7f;

        // Jag scale is shared by every jagged element rather than tuned per
        // element, so they all read as the same torn material. Kept well under
        // the noise's sampling limit (~1 cycle/pixel): an earlier version of
        // this in the source mod pushed the frequency so far past it that the
        // result came back out as near-random static and washed out to nothing.
        private const float EdgeJagAmplitude = 5.0f;
        private const float EdgeJagFrequency = 1.2f;
        private const int EdgeJagOctaves = 2;
        private const float EdgeJagPersistence = 0.5f;
        private const float EdgeJagLacunarity = 2.44f;

        internal const int JagFrameCount = 3;
        internal const float JagFrameInterval = 0.5f;
        private static readonly float[] JagFrameSeedOffsets = { 0f, 173.2f, 401.7f };

        // Keyed by size: several differently-sized panels coexist (the menu
        // shell, the preview frame), and a single "last baked size" cache would
        // mean each one re-bakes the other's frames every time it's shown.
        // Never evicted - only a handful of distinct sizes occur in a session.
        private static readonly Dictionary<(int width, int height, int frame), Sprite> _panelSprites = new();
        private static readonly Dictionary<(int size, int radius), Sprite> _maskSprites = new();
        private static readonly Dictionary<(int width, int height, int radius), Sprite> _scrollFadeSprites = new();
        private static Sprite _badgeSprite;
        private static Texture2D _grainTexture;

        /// <summary>
        /// The whole panel shape (border baked into the pixels, so one Image
        /// draws both a border and a differently-colored fill) at an exact
        /// pixel size, for <c>Image.Type.Simple</c>.
        ///
        /// Baked at the panel's real size rather than 9-sliced on purpose:
        /// 9-slicing stretches the straight-edge strips along their long axis,
        /// which dilutes the jag on exactly those edges down to nothing, so
        /// only the corners would ever show it.
        /// </summary>
        internal static Sprite PanelSprite(int width, int height, int frame)
        {
            var key = (width, height, frame);
            if (_panelSprites.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = MakeFullPanelSprite(
                width, height, PanelCornerRadius, PanelBorderThickness,
                PanelFillColor, PanelBorderColor,
                EdgeJagAmplitude, EdgeJagFrequency, JagFrameSeedOffsets[frame]);
            _panelSprites[key] = sprite;
            return sprite;
        }

        private static Sprite MakeFullPanelSprite(int width, int height, float radius, float borderThickness,
            Color fill, Color border, float edgeJag, float jagFreq, float seedOffset)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            // SetPixels32 + a single Apply(), never SetPixel() in the loop:
            // per-call bounds/format checks dominate at these resolutions.
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;

                    // The outer silhouette and the inner border/fill boundary
                    // are sampled at different noise offsets so the two edges
                    // don't wobble in lockstep.
                    float jagOuter = Jag(fx * jagFreq + 11.3f + seedOffset, fy * jagFreq + 11.3f + seedOffset, edgeJag);
                    float jagInner = Jag(fx * jagFreq + 77.1f + seedOffset, fy * jagFreq + 41.9f + seedOffset, edgeJag);

                    float cx = Mathf.Clamp(fx, radius, width - radius);
                    float cy = Mathf.Clamp(fy, radius, height - radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));

                    float shapeAlpha = Mathf.Clamp01(radius - dist + jagOuter + 0.5f); // ~1px soft edge AA
                    float fillT = Mathf.Clamp01(radius - dist - borderThickness + jagInner + 0.5f);

                    Color c = Color.Lerp(border, fill, fillT);
                    c.a = shapeAlpha;
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float Jag(float x, float y, float amplitude)
        {
            if (amplitude <= 0f)
            {
                return 0f;
            }

            // fbm (multi-octave), not a single Perlin sample: one smooth octave
            // only gives slow, rounded "dents in metal" undulation - stacking a
            // finer octave on top is what breaks the edge into the small,
            // irregular notches a torn edge actually has.
            return (Fbm(x, y, EdgeJagOctaves, EdgeJagPersistence, EdgeJagLacunarity) - 0.5f) * amplitude;
        }

        /// <summary>
        /// Alpha-only rounded shape used as an invisible <see cref="Mask"/>
        /// host, so the grain overlay is clipped to the panel's fill area and
        /// never paints over the border ring.
        /// </summary>
        internal static Sprite MaskSprite(int size, float radius)
        {
            var key = (size, Mathf.RoundToInt(radius));
            if (_maskSprites.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = MakeRoundedSprite(size, radius, borderThickness: 0f, fill: Color.white, border: Color.white);
            _maskSprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// A vertical alpha ramp - opaque along the bottom edge, transparent at the
        /// top - laid over a scrollable list's edge to fade the rows out into the
        /// plate they sit on (the "there's more below/above" cue). The top edge's
        /// copy is the same sprite flipped, so only one has to be baked per size.
        ///
        /// Smoothstepped rather than linear: a straight ramp has a visible hard
        /// terminus where it reaches zero, which reads as a band drawn over the list
        /// instead of the list simply thinning out.
        ///
        /// Baked at the strip's exact size (like <see cref="PanelSprite"/>, and unlike
        /// every 9-sliced sprite here) because the *shape* matters, not just the ramp:
        /// its opaque edge carries the same rounded corners the plate underneath has,
        /// so the fade stops exactly where the plate does. A plain rectangle painted
        /// its full width right up to the edge - the first version of this - reached
        /// past the plate's corner arcs and left them showing behind it.
        /// </summary>
        internal static Sprite ScrollFadeSprite(int width, int height, float radius)
        {
            var key = (width, height, Mathf.RoundToInt(radius));
            if (_scrollFadeSprites.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = MakeScrollFadeSprite(width, height, radius);
            _scrollFadeSprites[key] = sprite;
            return sprite;
        }

        private static Sprite MakeScrollFadeSprite(int width, int height, float radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float fy = y + 0.5f;

                float t = 1f - fy / height;
                float ramp = t * t * (3f - 2f * t);

                for (int x = 0; x < width; x++)
                {
                    float fx = x + 0.5f;

                    // Only the bottom two corners are rounded: that's the opaque edge,
                    // the one that sits on the plate's own rounded end. The top edge
                    // is already invisible by the time it gets there, so a corner
                    // there would round off nothing.
                    float cx = Mathf.Clamp(fx, radius, width - radius);
                    float cy = Mathf.Max(fy, radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float corner = Mathf.Clamp01(radius - dist + 0.5f); // ~1px soft edge AA

                    pixels[y * width + x] = new Color(1f, 1f, 1f, ramp * corner);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Small rounded chip (key badges, tab buttons) - clean-edged, no jag, so it reads as a control rather than torn paper.</summary>
        internal static Sprite BadgeSprite() => _badgeSprite != null
            ? _badgeSprite
            : (_badgeSprite = MakeRoundedSprite(32, radius: 10f, borderThickness: 3f, fill: ChipFillColor, border: BadgeBorderColor));

        /// <summary>A 9-sliceable rounded rect with its border baked into the pixels (so border and fill can differ within one Image).</summary>
        internal static Sprite MakeRoundedSprite(int size, float radius, float borderThickness, Color fill, Color border)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));

                    float shapeAlpha = Mathf.Clamp01(radius - dist + 0.5f);
                    float fillT = borderThickness > 0f
                        ? Mathf.Clamp01(radius - dist - borderThickness + 0.5f)
                        : 1f;

                    Color c = Color.Lerp(border, fill, fillT);
                    c.a = shapeAlpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var slice = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, slice);
        }

        // Stretched (not tiled) across the panel, so this resolution directly
        // controls how fine the grain reads.
        private const int GrainTextureSize = 368;

        private const float GrainSeed = 1337f;
        private const float GrainEnvelopeFreq = 14.0f;
        private const int GrainOctaves = 6;
        private const float GrainPersistence = 0.76f;
        private const float GrainLacunarity = 2.98f;

        // Min > Max is intentional: SmoothStepEdge's denominator collapses to a
        // near-hard binary cutoff in that case, which is what gives the blobs
        // sharp edges instead of a soft gradient.
        private const float GrainSharpenMin = 0.61f;
        private const float GrainSharpenMax = 0.00f;
        private const float GrainLightMul = 1.03f;
        private const float GrainDarkMul = 1.00f;

        internal static Texture2D GrainTexture() => _grainTexture != null
            ? _grainTexture
            : (_grainTexture = GenerateGrainTexture(PanelFillColor, GrainTextureSize, GrainTextureSize));

        /// <summary>
        /// The fractal noise field itself *is* the cloud shape here, not a
        /// per-pixel jitter applied to a base color (which reads as TV static
        /// however it's tuned): a sharp SmoothStep pins most pixels flat to one
        /// of two very close tones, leaving only a narrow band in transition at
        /// each blob's edge - and stacking octaves is what makes that edge
        /// irregular rather than a smooth round arc.
        /// </summary>
        private static Texture2D GenerateGrainTexture(Color baseColor, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color dark = new Color(
                Mathf.Clamp01(baseColor.r * GrainDarkMul),
                Mathf.Clamp01(baseColor.g * GrainDarkMul),
                Mathf.Clamp01(baseColor.b * GrainDarkMul));
            Color light = new Color(
                Mathf.Clamp01(baseColor.r * GrainLightMul),
                Mathf.Clamp01(baseColor.g * GrainLightMul),
                Mathf.Clamp01(baseColor.b * GrainLightMul));

            // Two passes: the sharpen thresholds above are only meaningful
            // relative to the noise's *actual* output range, which stacking
            // octaves shifts away from a clean 0..1. Assuming 0..1 can land the
            // real range entirely outside the sharpen band, saturating
            // SmoothStepEdge to a constant and producing a perfectly flat,
            // texture-less fill.
            var envelopes = new float[width * height];
            float minEnvelope = float.MaxValue, maxEnvelope = float.MinValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float ox = x / (float)width * GrainEnvelopeFreq + GrainSeed * 0.001f;
                    float oy = y / (float)height * GrainEnvelopeFreq + GrainSeed * 0.001f;
                    float envelope = Fbm(ox, oy, GrainOctaves, GrainPersistence, GrainLacunarity);

                    envelopes[y * width + x] = envelope;
                    if (envelope < minEnvelope) minEnvelope = envelope;
                    if (envelope > maxEnvelope) maxEnvelope = envelope;
                }
            }

            float envelopeRange = Mathf.Max(0.0001f, maxEnvelope - minEnvelope);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                float normalized = (envelopes[i] - minEnvelope) / envelopeRange;
                pixels[i] = Color.Lerp(dark, light, SmoothStepEdge(GrainSharpenMin, GrainSharpenMax, normalized));
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Fractal Brownian motion: several octaves of Perlin at rising frequency and falling amplitude.</summary>
        private static float Fbm(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f, amplitude = 1f, frequency = 1f, max = 0f;
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total / max;
        }

        /// <summary>
        /// GLSL's <c>smoothstep(edge0, edge1, x)</c> - thresholds x against the
        /// two edges. Deliberately not <see cref="Mathf.SmoothStep"/>, which
        /// interpolates *between* two values using a smoothed t and never
        /// thresholds anything, so using it here would just blend between two
        /// nearly-identical constants and sharpen nothing.
        /// </summary>
        private static float SmoothStepEdge(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
