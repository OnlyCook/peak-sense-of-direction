using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SenseOfDirection.Ui
{
    // TEMP DIAGNOSTIC for the dx12 f8 -> memory to 99% -> crash reports
    internal class PreviewDiagnostics : MonoBehaviour
    {
        private const float SampleInterval = 0.5f;
        private const float SampleWindowSeconds = 30f;
        private const float SpikeFrameSeconds = 0.25f;
        private const long GrowthWarnBytes = 500L * 1024 * 1024;

        private float _activeTime;
        private float _nextSample;
        private long _baselineWorkingSet;
        private long _lastWarnedWorkingSet;
        private bool _baselineTaken;
        private float _worstFrame;
        private int _spikeLogs;

        internal static void Attach(GameObject host, Camera stageCamera, Camera handCamera)
        {
            var d = host.AddComponent<PreviewDiagnostics>();
            d.LogEnvironment(stageCamera, handCamera);
        }

        private static void Log(string message) => Plugin.Instance.Log.LogInfo("[F8-DIAG] " + message);

        private static void Warn(string message) => Plugin.Instance.Log.LogWarning("[F8-DIAG] " + message);

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessMemoryCounters
        {
            public uint cb;
            public uint PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCounters counters, uint size);

        private static bool _nativeMemoryBroken;

        // whole-machine ram load and this process's working set/commit, via Win32
        // returns the process working set (0 if unavailable), what the growth warning keys off
        private static long NativeMemory(out string text)
        {
            text = "sys=n/a";
            if (_nativeMemoryBroken)
            {
                return 0;
            }

            try
            {
                var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)) };
                var counters = new ProcessMemoryCounters { cb = (uint)Marshal.SizeOf(typeof(ProcessMemoryCounters)) };
                if (!GlobalMemoryStatusEx(ref status) || !GetProcessMemoryInfo(GetCurrentProcess(), out counters, counters.cb))
                {
                    _nativeMemoryBroken = true;
                    return 0;
                }

                text = "sysLoad=" + status.dwMemoryLoad + "% | sysFree=" + Mb((long)status.ullAvailPhys)
                    + " | commitFree=" + Mb((long)status.ullAvailPageFile)
                    + " | procWorkingSet=" + Mb((long)counters.WorkingSetSize)
                    + " | procCommit=" + Mb((long)counters.PagefileUsage);
                return (long)counters.WorkingSetSize;
            }
            catch (Exception)
            {
                _nativeMemoryBroken = true;
                return 0;
            }
        }

        private static string Mb(long bytes) => (bytes / (1024 * 1024)) + "MB";

        private void LogEnvironment(Camera stageCamera, Camera handCamera)
        {
            var sb = new StringBuilder("preview menu built. environment:");
            sb.Append("\n  api=").Append(SystemInfo.graphicsDeviceType)
              .Append(" | gpu=").Append(SystemInfo.graphicsDeviceName)
              .Append(" | driver=").Append(SystemInfo.graphicsDeviceVersion)
              .Append(" | vram=").Append(SystemInfo.graphicsMemorySize).Append("MB")
              .Append(" | ram=").Append(SystemInfo.systemMemorySize).Append("MB")
              .Append(" | shared-mem-gpu=").Append(SystemInfo.graphicsDeviceVendor);
            sb.Append("\n  screen=").Append(Screen.width).Append('x').Append(Screen.height)
              .Append(" | current-res=").Append(Screen.currentResolution)
              .Append(" | mode=").Append(Screen.fullScreenMode)
              .Append(" | dpi=").Append(Screen.dpi)
              .Append(" | vsync=").Append(QualitySettings.vSyncCount)
              .Append(" | targetFps=").Append(Application.targetFrameRate)
              .Append(" | quality=").Append(QualitySettings.names[QualitySettings.GetQualityLevel()])
              .Append(" | qualityMsaa=").Append(QualitySettings.antiAliasing);

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                sb.Append("\n  urp: renderScale=").Append(urp.renderScale)
                  .Append(" | msaa=").Append(urp.msaaSampleCount)
                  .Append(" | hdr=").Append(urp.supportsHDR)
                  .Append(" | hdrPrecision=").Append(urp.hdrColorBufferPrecision)
                  .Append(" | upscaling=").Append(urp.upscalingFilter)
                  .Append(" | shadowRes=").Append(urp.mainLightShadowmapResolution)
                  .Append(" | depthTex=").Append(urp.supportsCameraDepthTexture)
                  .Append(" | opaqueTex=").Append(urp.supportsCameraOpaqueTexture);
            }
            else
            {
                sb.Append("\n  pipeline=").Append(GraphicsSettings.currentRenderPipeline == null ? "built-in" : GraphicsSettings.currentRenderPipeline.GetType().Name);
            }

            AppendCamera(sb, "stage camera", stageCamera);
            AppendCamera(sb, "hand camera", handCamera);
            AppendCamera(sb, "game main camera", Camera.main);

            sb.Append("\n  cameras in scene=").Append(Camera.allCamerasCount);
            AppendMemory(sb.Append("\n  "), "baseline");
            Log(sb.ToString());
        }

        private static void AppendCamera(StringBuilder sb, string label, Camera camera)
        {
            sb.Append("\n  ").Append(label).Append(": ");
            if (camera == null)
            {
                sb.Append("<none>");
                return;
            }

            sb.Append("target=")
              .Append(camera.targetTexture != null ? camera.targetTexture.width + "x" + camera.targetTexture.height + " aa=" + camera.targetTexture.antiAliasing : "screen")
              .Append(" | allowMSAA=").Append(camera.allowMSAA)
              .Append(" | allowHDR=").Append(camera.allowHDR)
              .Append(" | pixelRect=").Append(camera.pixelWidth).Append('x').Append(camera.pixelHeight)
              .Append(" | depth=").Append(camera.depth)
              .Append(" | enabled=").Append(camera.enabled);
        }

        private static void AppendMemory(StringBuilder sb, string label)
        {
            NativeMemory(out string native);
            sb.Append(label).Append(" mem: ").Append(native)
              .Append(" | unityAllocated=").Append(Mb(Profiler.GetTotalAllocatedMemoryLong()))
              .Append(" | unityReserved=").Append(Mb(Profiler.GetTotalReservedMemoryLong()))
              .Append(" | gfxDriver=").Append(Mb(Profiler.GetAllocatedMemoryForGraphicsDriver()))
              .Append(" | monoHeap=").Append(Mb(System.GC.GetTotalMemory(false)));
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _activeTime += dt;

            long workingSet;
            if (!_baselineTaken)
            {
                workingSet = NativeMemory(out _);
                _baselineWorkingSet = workingSet;
                _lastWarnedWorkingSet = workingSet;
                _baselineTaken = true;
            }

            if (dt > _worstFrame)
            {
                _worstFrame = dt;
            }

            if (dt > SpikeFrameSeconds && _spikeLogs < 20)
            {
                _spikeLogs++;
                var sb = new StringBuilder();
                sb.Append("frame spike ").Append((int)(dt * 1000f)).Append("ms at t=").Append(_activeTime.ToString("F2")).Append("s (frame ").Append(Time.frameCount).Append(") ");
                AppendMemory(sb, "");
                Warn(sb.ToString());
            }

            bool sampling = _activeTime <= SampleWindowSeconds;
            if (_activeTime < _nextSample)
            {
                return;
            }
            _nextSample = _activeTime + (sampling ? SampleInterval : 2f);

            workingSet = NativeMemory(out _);

            if (workingSet - _lastWarnedWorkingSet > GrowthWarnBytes)
            {
                var sb = new StringBuilder();
                sb.Append("MEMORY GROWTH +").Append(Mb(workingSet - _baselineWorkingSet)).Append(" since preview built (t=").Append(_activeTime.ToString("F2")).Append("s) ");
                AppendMemory(sb, "");
                Warn(sb.ToString());
                _lastWarnedWorkingSet = workingSet;
            }

            if (sampling)
            {
                var sb = new StringBuilder();
                sb.Append("t=").Append(_activeTime.ToString("F1")).Append("s frame=").Append(Time.frameCount)
                  .Append(" worstFrame=").Append((int)(_worstFrame * 1000f)).Append("ms ");
                AppendMemory(sb, "");
                Log(sb.ToString());
                _worstFrame = 0f;
            }
        }
    }
}
