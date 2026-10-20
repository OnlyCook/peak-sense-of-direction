using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SenseOfDirection.Ui
{
    // makes a camera immune to the game's own render scale graphics option
    internal static class NativeResolutionCamera
    {
        private static readonly HashSet<Camera> _registered = new HashSet<Camera>();
        private static bool _subscribed;
        private static float _savedRenderScale;
        private static bool _overrodeThisRender;

        internal static void Register(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            _registered.Add(camera);
            StripUnneededWork(camera);

            // subscribe once lazily
            if (!_subscribed)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
                _subscribed = true;
            }
        }

        private static void StripUnneededWork(Camera camera)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.dithering = false;
            data.stopNaN = false;
        }

        internal static void Unregister(Camera camera)
        {
            _registered.Remove(camera);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            _overrodeThisRender = false;

            if (!_registered.Contains(camera))
            {
                return;
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.renderScale != 1f)
            {
                _savedRenderScale = urp.renderScale;
                urp.renderScale = 1f;
                _overrodeThisRender = true;
            }
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_overrodeThisRender || !_registered.Contains(camera))
            {
                return;
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = _savedRenderScale;
            }

            _overrodeThisRender = false;
        }
    }
}
