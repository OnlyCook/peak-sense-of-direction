using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
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
                            if (open != null)
                            {
                                Crashed.Add(open);
                                Notes.Add("stage '" + open + "' killed the game (last checkpoint: " + (openMark ?? "none") + ")");
                            }
                            open = null;
                            openMark = null;
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

                    if (open != null)
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
            ToFile("==================== new launch, Sense of Direction " + PluginInfo.Version + " ====================");
            foreach (string note in Notes)
            {
                Log("previous launch: " + note + " - skipping it this time");
            }
        }

        private static string Memory() => PreviewDiagnostics.MemoryLine();

        internal static void Header()
        {
            Load();
            Hook();
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
