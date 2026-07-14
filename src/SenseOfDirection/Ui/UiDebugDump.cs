using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Debug-only: walks an instantiated widget and logs what is actually in it -
    /// every transform's rect, anchors, components, and any layout driver or text.
    ///
    /// This exists because re-anchoring a game prefab whose internal hierarchy we
    /// have never looked at is guesswork, and guessing has already cost this menu
    /// two rounds of wrong fixes. Gated on <c>enable-debug-logging</c>, and logged
    /// through BepInEx's own logger - Unity's log writer does not always reach
    /// LogOutput.log, so a Debug.Log here could vanish silently.
    /// </summary>
    internal static class UiDebugDump
    {
        /// <summary>
        /// Dumps <paramref name="root"/> now and again after a layout pass has run:
        /// a ContentSizeFitter or LayoutGroup only re-derives sizes on the next
        /// pass, so a rect that looks right immediately after Instantiate can be
        /// something else entirely by the time it is drawn.
        /// </summary>
        internal static void DumpDeferred(GameObject root, string label)
        {
            if (root == null || !Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                return;
            }

            Dump(root, label + " (immediate)");

            var runner = root.AddComponent<DeferredDumpRunner>();
            runner.Label = label;
        }

        internal static void Dump(GameObject root, string label)
        {
            if (root == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== UI DUMP: " + label + " ===");
            Walk(root.transform, 0, sb);
            Plugin.Instance.Log.LogInfo(sb.ToString());
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            string indent = new string(' ', depth * 2);
            sb.Append(indent).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [INACTIVE]");

            if (t is RectTransform rt)
            {
                sb.Append(" | rect=").Append(rt.rect.size)
                  .Append(" aMin=").Append(rt.anchorMin)
                  .Append(" aMax=").Append(rt.anchorMax)
                  .Append(" pivot=").Append(rt.pivot)
                  .Append(" sizeDelta=").Append(rt.sizeDelta)
                  .Append(" anchoredPos=").Append(rt.anchoredPosition)
                  .Append(" scale=").Append(rt.localScale);
            }

            sb.AppendLine();

            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null || c is RectTransform)
                {
                    continue;
                }

                sb.Append(indent).Append("  - ").Append(c.GetType().Name);

                switch (c)
                {
                    case LayoutElement le:
                        sb.Append(" min=(").Append(le.minWidth).Append(',').Append(le.minHeight)
                          .Append(") pref=(").Append(le.preferredWidth).Append(',').Append(le.preferredHeight)
                          .Append(") flex=(").Append(le.flexibleWidth).Append(',').Append(le.flexibleHeight)
                          .Append(") ignore=").Append(le.ignoreLayout);
                        break;
                    case ContentSizeFitter csf:
                        sb.Append(" h=").Append(csf.horizontalFit).Append(" v=").Append(csf.verticalFit);
                        break;
                    case HorizontalOrVerticalLayoutGroup g:
                        sb.Append(" padding=").Append(g.padding)
                          .Append(" spacing=").Append(g.spacing)
                          .Append(" childForceExpand=(").Append(g.childForceExpandWidth).Append(',').Append(g.childForceExpandHeight)
                          .Append(") childControl=(").Append(g.childControlWidth).Append(',').Append(g.childControlHeight).Append(')');
                        break;
                    case TMP_Text text:
                        sb.Append(" text=\"").Append(text.text).Append("\" size=").Append(text.fontSize)
                          .Append(" autoSize=").Append(text.enableAutoSizing)
                          .Append(" color=").Append(text.color)
                          .Append(" enabled=").Append(text.enabled)
                          .Append(" font=").Append(text.font != null ? text.font.name : "null");
                        break;
                    case Image img:
                        sb.Append(" sprite=").Append(img.sprite != null ? img.sprite.name : "null")
                          .Append(" color=").Append(img.color)
                          .Append(" enabled=").Append(img.enabled);
                        break;
                    case TMP_Dropdown dd:
                        sb.Append(" value=").Append(dd.value).Append(" options=").Append(dd.options.Count)
                          .Append(" caption=").Append(dd.captionText != null ? dd.captionText.name : "null")
                          .Append(" template=").Append(dd.template != null ? dd.template.name : "null");
                        break;
                    case Behaviour b:
                        sb.Append(" enabled=").Append(b.enabled);
                        break;
                }

                sb.AppendLine();
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Walk(t.GetChild(i), depth + 1, sb);
            }
        }

        /// <summary>Dumps once more after a full layout pass, then removes itself.</summary>
        private class DeferredDumpRunner : MonoBehaviour
        {
            internal string Label;

            private IEnumerator Start()
            {
                yield return null;
                yield return new WaitForEndOfFrame();

                Dump(gameObject, Label + " (after layout)");
                Destroy(this);
            }
        }
    }
}
