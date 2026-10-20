using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SenseOfDirection.Ui
{
    // TEMP TEST HARNESS for the nvidia/dx12 f8 crash. builds the menu one stage at a time, logging memory around each,
    // and remembers (in a small file) which stage was running when the game died so the next launch skips it and carries on
    internal static class F8Bisect
    {
        private const string Tag = "[F8-TEST] ";

        private static readonly HashSet<string> Crashed = new HashSet<string>();
        private static readonly HashSet<string> SkipLogged = new HashSet<string>();
        private static bool _loaded;
        private static bool _hooked;
        private static bool _tracing;
        private static readonly List<string> Notes = new List<string>();

        // crash records only count for the exact build that wrote them, so an old file from an earlier test build is harmless
        private static string BuildId => typeof(F8Bisect).Module.ModuleVersionId.ToString();

        private static string FilePath => Path.Combine(Paths.ConfigPath, "SenseOfDirection.f8-test.log");

        // append-only, one file for everything: the full log of every launch plus the "@state" events the next launch replays to learn what crashed
        internal static void ToFile(string line)
        {
            try
            {
                File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + "\n");
            }
            catch (Exception)
            {
            }
        }

        internal static void Log(string message)
        {
            Plugin.Instance.Log.LogInfo(Tag + message);
            ToFile(Tag + message);
        }

        internal static void Raw(string line)
        {
            Plugin.Instance.Log.LogInfo(line);
            ToFile(line);
        }

        private static void State(string what) => ToFile("@state " + what);

        private static void Load()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;

            try
            {
                string open = null;
                string openMark = null;
                string fileBuild = null;
                if (File.Exists(FilePath))
                {
                    foreach (string raw in File.ReadAllLines(FilePath))
                    {
                        int at = raw.IndexOf("@state ", StringComparison.Ordinal);
                        if (at < 0)
                        {
                            continue;
                        }

                        string ev = raw.Substring(at + 7);
                        if (ev == "launch")
                        {
                            if (open != null && fileBuild == BuildId)
                            {
                                Crashed.Add(open);
                                Notes.Add("stage '" + open + "' killed the game (last checkpoint: " + (openMark ?? "none") + ")");
                            }
                            open = null;
                            openMark = null;
                        }
                        else if (ev.StartsWith("build "))
                        {
                            fileBuild = ev.Substring(6);
                        }
                        else if (ev.StartsWith("begin "))
                        {
                            open = ev.Substring(6);
                            openMark = null;
                        }
                        else if (ev.StartsWith("end "))
                        {
                            open = null;
                        }
                        else if (ev.StartsWith("mark "))
                        {
                            openMark = ev.Substring(5);
                        }
                    }

                    if (open != null && fileBuild == BuildId)
                    {
                        Crashed.Add(open);
                        Notes.Add("stage '" + open + "' killed the game (last checkpoint: " + (openMark ?? "none") + ")");
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Instance.Log.LogWarning(Tag + "could not read the test file: " + e.Message);
            }

            State("launch");
            State("build " + BuildId);
            ToFile("==================== new launch, Sense of Direction " + PluginInfo.Version + " ====================");
            foreach (string note in Notes)
            {
                Log("previous launch: " + note + " - skipping it this time");
            }
        }

        private static string _lastFonts;

        private static void AppendFont(StringBuilder sb, TMP_FontAsset font, string role)
        {
            sb.Append("\n    ").Append(role).Append(": ");
            if (font == null)
            {
                sb.Append("<none>");
                return;
            }

            sb.Append(font.name).Append(" | mode=").Append(font.atlasPopulationMode)
              .Append(" | multiAtlas=").Append(font.isMultiAtlasTexturesEnabled)
              .Append(" | atlas ").Append(font.atlasWidth).Append('x').Append(font.atlasHeight)
              .Append(" pages=").Append(font.atlasTextures == null ? 0 : font.atlasTextures.Length)
              .Append(" | glyphs=").Append(font.characterTable == null ? 0 : font.characterTable.Count);
        }

        // the font atlas state of the menu's font and its fallbacks - logged only when it changed since the last stage
        private static void LogFontsIfChanged(string after)
        {
            try
            {
                var sb = new StringBuilder();
                TMP_FontAsset main = NativeAssets.Font;
                AppendFont(sb, main, "menu font");
                if (main != null && main.fallbackFontAssetTable != null)
                {
                    foreach (TMP_FontAsset fallback in main.fallbackFontAssetTable)
                    {
                        AppendFont(sb, fallback, "  fallback");
                    }
                }
                if (TMP_Settings.fallbackFontAssets != null)
                {
                    foreach (TMP_FontAsset fallback in TMP_Settings.fallbackFontAssets)
                    {
                        AppendFont(sb, fallback, "  tmp-global fallback");
                    }
                }

                string now = sb.ToString();
                if (now != _lastFonts)
                {
                    _lastFonts = now;
                    Log("  font atlases after '" + after + "':" + now);
                }
            }
            catch (Exception e)
            {
                Log("  font stats unavailable: " + e.Message);
            }
        }

        private static string Memory() => PreviewDiagnostics.MemoryLine();

        internal static void Header()
        {
            Load();
            Hook();
            LogFontsIfChanged("F8 pressed");
            Log("=== F8 pressed - staged menu test start. stages skipped because they crashed before: " + (Crashed.Count == 0 ? "none" : string.Join(", ", new List<string>(Crashed).ToArray())));
        }

        // a checkpoint inside a stage - also written to the state file so a crash tells us the last place it got to
        internal static void Mark(string what)
        {
            Load();
            State("mark " + what);
            Log("  mark: " + what + " | " + Memory());
        }

        internal static bool Enabled(string stage)
        {
            Load();
            if (!Crashed.Contains(stage))
            {
                return true;
            }
            if (SkipLogged.Add(stage))
            {
                Log("SKIPPING '" + stage + "' (crashed on an earlier launch)");
            }
            return false;
        }

        // runs one stage: begin marker, the work, a few rendered frames with per-frame memory, end marker
        internal static IEnumerator Stage(string name, Action work, int settleFrames = 2)
        {
            if (!Enabled(name))
            {
                yield break;
            }

            Load();
            _tracing = true;
            State("begin " + name);
            Log("BEGIN '" + name + "' | " + Memory());

            try
            {
                work();
            }
            catch (Exception e)
            {
                Plugin.Instance.Log.LogError(Tag + "stage '" + name + "' threw (game still alive): " + e);
            }

            Log("  worked '" + name + "' | " + Memory());

            for (int i = 1; i <= settleFrames; i++)
            {
                State("mark rendering frame " + i);
                yield return null;
                Log("  '" + name + "' frame +" + i + " dt=" + (int)(Time.unscaledDeltaTime * 1000f) + "ms | " + Memory());
            }

            Log("END '" + name + "' | " + Memory());
            LogFontsIfChanged(name);
            _tracing = false;
            State("end " + name);
        }

        internal static void Finished()
        {
            Log("=== staged F8 menu test finished without crashing. skipped stages: " + (Crashed.Count == 0 ? "none" : string.Join(", ", new List<string>(Crashed).ToArray())));
        }

        private static void Hook()
        {
            if (_hooked)
            {
                return;
            }
            _hooked = true;

            RenderPipelineManager.beginContextRendering += (ctx, cams) =>
            {
                if (_tracing)
                {
                    Log("    render: context begin (" + cams.Count + " cameras) | " + Memory());
                }
            };
            RenderPipelineManager.endContextRendering += (ctx, cams) =>
            {
                if (_tracing)
                {
                    Log("    render: context end | " + Memory());
                }
            };
            RenderPipelineManager.beginCameraRendering += (ctx, cam) =>
            {
                if (_tracing)
                {
                    Log("    render: camera begin '" + cam.name + "' " + cam.pixelWidth + "x" + cam.pixelHeight + " | " + Memory());
                }
            };
            RenderPipelineManager.endCameraRendering += (ctx, cam) =>
            {
                if (_tracing)
                {
                    Log("    render: camera end '" + cam.name + "' | " + Memory());
                }
            };
            Canvas.willRenderCanvases += () =>
            {
                if (_tracing)
                {
                    Log("    render: canvases about to render | " + Memory());
                }
            };
        }
    }
}
