using System.Collections.Generic;
using SenseOfDirection.Indicators;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Draws each tracked player's bone rig as a through-walls skeleton overlay
    /// (the classic "ESP" look), backing <c>Player-Labels/show-skeleton</c>
    /// (off by default).
    ///
    /// Bones come straight off <c>character.refs.ragdoll.partDict</c> - PEAK's
    /// characters are ragdoll-driven, so every joint is a real
    /// <c>Bodypart</c> transform with a live world position, and no animator/
    /// bone-name guesswork is needed. <c>Character.GetBodypart</c> itself is
    /// <c>internal</c>, but the dictionary it reads is public, so this needs no
    /// reflection.
    ///
    /// Rendered as flat UI lines projected onto <see cref="IndicatorManager"/>'s
    /// existing overlay canvas rather than world-space <c>LineRenderer</c>s with
    /// a depth-defeating material: an overlay canvas draws over the world by
    /// construction, so "through walls" comes for free with no shader or
    /// render-pipeline assumptions. Line thickness is therefore constant in
    /// screen space rather than shrinking with distance, which is what an ESP
    /// skeleton wants anyway - a distant player stays a readable stick figure
    /// instead of fading to a hairline.
    ///
    /// Unlike every other widget in this mod, nothing here clamps to the screen
    /// edge or registers an <see cref="IndicatorAnchor"/>: a skeleton only makes
    /// sense drawn exactly where the body actually is, so off-screen and
    /// behind-camera joints are simply skipped.
    /// </summary>
    public sealed class PlayerSkeletonEsp
    {
        /// <summary>
        /// Bone chains, each an ordered walk along the rig. Consecutive
        /// <em>present</em> parts are connected, so a chain still renders
        /// sensibly if an intermediate joint is missing from a given rig
        /// (e.g. no shoulder: the neck connects straight to the upper arm)
        /// rather than silently dropping the whole limb.
        /// </summary>
        private static readonly BodypartType[][] Chains =
        {
            new[] { BodypartType.Hip, BodypartType.Mid, BodypartType.Torso, BodypartType.Neck, BodypartType.Head },
            new[] { BodypartType.Neck, BodypartType.Shoulder_L, BodypartType.Arm_L, BodypartType.Elbow_L, BodypartType.Hand_L },
            new[] { BodypartType.Neck, BodypartType.Shoulder_R, BodypartType.Arm_R, BodypartType.Elbow_R, BodypartType.Hand_R },
            new[] { BodypartType.Hip, BodypartType.Hip_L, BodypartType.Leg_L, BodypartType.Knee_L, BodypartType.Foot_L, BodypartType.Toe_L },
            new[] { BodypartType.Hip, BodypartType.Hip_R, BodypartType.Leg_R, BodypartType.Knee_R, BodypartType.Foot_R, BodypartType.Toe_R },
        };

        /// <summary>Every distinct joint mentioned in <see cref="Chains"/>, for the optional joint dots.</summary>
        private static readonly BodypartType[] Joints = BuildJointList();

        /// <summary>Joint dot diameter, as a multiple of the configured line thickness - keeps dots proportional to the bones they sit on without a second setting.</summary>
        private const float JointDotThicknessMultiplier = 2.4f;

        private static Sprite _dotSprite;

        private readonly RectTransform _root;
        private readonly RectTransform _lineContainer;
        private readonly RectTransform _dotContainer;

        /// <summary>Scratch buffer for the live-<c>Character</c> <see cref="Draw(Character, Camera, Vector2, Color, float, bool)"/> path's rig snapshot - a field, not a local, so a per-player per-frame draw allocates nothing.</summary>
        private readonly Dictionary<BodypartType, Vector3> _rigPositions = new Dictionary<BodypartType, Vector3>();

        private readonly List<Image> _lines = new List<Image>();
        private readonly List<Image> _dots = new List<Image>();

        /// <summary>How many pooled lines/dots this frame has used so far; everything past these is hidden by <see cref="EndFrame"/>.</summary>
        private int _linesUsed;
        private int _dotsUsed;

        public PlayerSkeletonEsp(RectTransform parent)
        {
            _root = NewChild(parent, "SoD.PlayerSkeletonEsp");
            // Dots are a later sibling than lines, so they draw on top of the
            // bones they cap rather than being half-swallowed by them.
            _lineContainer = NewChild(_root, "Lines");
            _dotContainer = NewChild(_root, "Dots");
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        /// <summary>Call once per frame before any <see cref="Draw"/> calls.</summary>
        public void BeginFrame()
        {
            _linesUsed = 0;
            _dotsUsed = 0;
        }

        /// <summary>
        /// Projects and draws one character's skeleton, reading its live rig.
        /// Safe to call for a character whose rig isn't built (or is being torn
        /// down) - it just draws nothing.
        /// </summary>
        public void Draw(Character character, Camera camera, Vector2 canvasSize, Color color, float thickness, bool showJoints)
        {
            // Deliberately not `character?.refs?...` - the null-conditional
            // operator tests real null and so sails straight past a destroyed
            // UnityEngine.Object, which is exactly the case worth catching here.
            if (character == null || camera == null || character.refs == null || character.refs.ragdoll == null)
            {
                return;
            }

            Dictionary<BodypartType, Bodypart> parts = character.refs.ragdoll.partDict;
            if (parts == null)
            {
                return;
            }

            // Snapshotted into a plain position lookup (reused across frames, so
            // no per-frame garbage) so the drawing below has one input shape,
            // whether the joints came from a real ragdoll or from the config
            // preview's hand-mapped stand-in.
            _rigPositions.Clear();
            foreach (KeyValuePair<BodypartType, Bodypart> part in parts)
            {
                if (part.Value != null)
                {
                    _rigPositions[part.Key] = part.Value.transform.position;
                }
            }

            Draw(_rigPositions, camera, canvasSize, color, thickness, showJoints);
        }

        /// <summary>
        /// Projects and draws a skeleton from bare joint world positions, for
        /// callers with no live <c>Character</c> to read: the config preview
        /// menu (<c>Ui.PreviewScene</c>), whose "player" is a figure in a static
        /// screenshot whose joints are mapped by hand. Missing joints are fine -
        /// see <see cref="Chains"/>.
        /// </summary>
        public void Draw(
            IReadOnlyDictionary<BodypartType, Vector3> joints, Camera camera, Vector2 canvasSize,
            Color color, float thickness, bool showJoints)
        {
            if (joints == null || camera == null)
            {
                return;
            }

            foreach (BodypartType[] chain in Chains)
            {
                // Index of the previous chain entry that both exists and
                // projected in front of the camera - the far end of the next
                // segment to draw.
                bool hasPrevious = false;
                Vector2 previous = Vector2.zero;

                foreach (BodypartType type in chain)
                {
                    if (!TryProject(joints, type, camera, canvasSize, out Vector2 point))
                    {
                        // A joint behind the camera would project to a mirrored,
                        // meaningless point, so it breaks the chain rather than
                        // connecting across the gap.
                        hasPrevious = false;
                        continue;
                    }

                    if (hasPrevious)
                    {
                        DrawLine(previous, point, color, thickness);
                    }

                    previous = point;
                    hasPrevious = true;
                }
            }

            if (!showJoints)
            {
                return;
            }

            foreach (BodypartType type in Joints)
            {
                if (TryProject(joints, type, camera, canvasSize, out Vector2 point))
                {
                    DrawDot(point, color, thickness * JointDotThicknessMultiplier);
                }
            }
        }

        /// <summary>Call once per frame after every <see cref="Draw"/> call, to park whatever this frame didn't need.</summary>
        public void EndFrame()
        {
            for (int i = _linesUsed; i < _lines.Count; i++)
            {
                _lines[i].gameObject.SetActive(false);
            }
            for (int i = _dotsUsed; i < _dots.Count; i++)
            {
                _dots[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Hides everything immediately - used when the feature is switched off mid-frame, so nothing is left frozen on screen.</summary>
        public void Clear()
        {
            BeginFrame();
            EndFrame();
        }

        private static bool TryProject(
            IReadOnlyDictionary<BodypartType, Vector3> joints, BodypartType type,
            Camera camera, Vector2 canvasSize, out Vector2 canvasPoint)
        {
            canvasPoint = Vector2.zero;
            if (!joints.TryGetValue(type, out Vector3 position))
            {
                return false;
            }

            Vector3 viewport = camera.WorldToViewportPoint(position);
            if (viewport.z <= 0f)
            {
                return false;
            }

            // Same viewport->canvas mapping ScreenSpaceTracker uses, minus its
            // edge clamping: a skeleton belongs on the body, not herded back
            // on-screen (see this class's own doc comment).
            canvasPoint = new Vector2(
                (viewport.x - 0.5f) * canvasSize.x,
                (viewport.y - 0.5f) * canvasSize.y);
            return true;
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Image image = Take(_lines, _lineContainer, ref _linesUsed, sprite: null);
            image.color = color;

            var rect = (RectTransform)image.transform;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void DrawDot(Vector2 at, Color color, float diameter)
        {
            Image image = Take(_dots, _dotContainer, ref _dotsUsed, DotSprite);
            image.color = color;

            var rect = (RectTransform)image.transform;
            rect.anchoredPosition = at;
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>Next free pooled image, growing the pool only when a frame genuinely needs more than any previous one did.</summary>
        private static Image Take(List<Image> pool, RectTransform parent, ref int used, Sprite sprite)
        {
            if (used == pool.Count)
            {
                var go = new GameObject("Segment", typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image created = go.AddComponent<Image>();
                created.raycastTarget = false;
                created.sprite = sprite;
                pool.Add(created);
            }

            Image image = pool[used];
            used++;
            image.gameObject.SetActive(true);
            return image;
        }

        private static RectTransform NewChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        /// <summary>A soft-edged white disc, generated once. Lines need no sprite at all (a null-sprite <see cref="Image"/> is already a solid quad), but a square joint dot reads badly against the round-ish limbs.</summary>
        private static Sprite DotSprite
        {
            get
            {
                if (_dotSprite == null)
                {
                    _dotSprite = BuildDotSprite(32);
                }
                return _dotSprite;
            }
        }

        private static Sprite BuildDotSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01((radius - distance) / feather);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static BodypartType[] BuildJointList()
        {
            var seen = new List<BodypartType>();
            foreach (BodypartType[] chain in Chains)
            {
                foreach (BodypartType type in chain)
                {
                    if (!seen.Contains(type))
                    {
                        seen.Add(type);
                    }
                }
            }
            return seen.ToArray();
        }
    }
}
