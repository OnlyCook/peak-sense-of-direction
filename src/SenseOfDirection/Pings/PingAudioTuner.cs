using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Forces a smoother, further-carrying falloff onto whichever pooled
    /// <c>SFX_Player</c> AudioSource is currently playing a ping clip. Just
    /// boosting <c>SFX_Instance.settings.range</c>
    /// (<see cref="PointPingerPatches"/>'s <c>PointPing.Awake</c> postfix)
    /// only pushes back the hard "don't even start playing" distance check
    /// in <c>SFX_Player.PlaySFX</c> - the AudioSource's own rolloff curve
    /// (baked onto the pooled source templates, not visible in the
    /// decompiled IL, RESEARCH.md Q6) is whatever vanilla ships.
    ///
    /// Two things are applied, both distance-driven every frame:
    /// - <see cref="AudioRolloffMode.Logarithmic"/> instead of Linear.
    ///   Linear rolloff is linear in *amplitude*, but human hearing perceives
    ///   loudness roughly logarithmically - the practical result (confirmed
    ///   via in-game testing) is a Linear-rolloff ping that barely seems to
    ///   quiet down for most of its range and then abruptly vanishes near
    ///   maxDistance. Logarithmic tracks perceived loudness much more
    ///   naturally, giving an actually-gradual fade instead of a cliff.
    /// - An <see cref="AudioLowPassFilter"/>, cutoff frequency lowered the
    ///   further away the ping is - distant real-world sounds are muffled
    ///   (high frequencies attenuate faster over distance/obstruction) as
    ///   well as quieter, so this reads as "far away" much more convincingly
    ///   than volume alone. Added/removed per pooled source on demand rather
    ///   than left in place, since these AudioSources are shared by every
    ///   sound in the game, not just pings - a source not currently playing
    ///   a ping clip must not stay muffled for whatever plays through it next.
    ///
    /// <c>SFX_Player.sources</c> and each <c>SFX_Source.source</c>/
    /// <c>.isPlaying</c> are public fields, so no reflection is needed here.
    /// </summary>
    public class PingAudioTuner : MonoBehaviour
    {
        private const float MinCutoffHz = 700f;
        private const float MaxCutoffHz = 22000f;

        private static PingAudioTuner _instance;

        public static PingAudioTuner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PingAudioTuner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PingAudioTuner>();
                }
                return _instance;
            }
        }

        private readonly HashSet<AudioSource> _activeThisFrame = new HashSet<AudioSource>();
        private readonly HashSet<AudioSource> _activeLastFrame = new HashSet<AudioSource>();

        private void Update()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            _activeThisFrame.Clear();

            if (cfg.EnablePingAudioBoost.Value && SFX_Player.instance != null && PointPingerPatches.PingClips.Count > 0)
            {
                Camera camera = Camera.main;
                List<SFX_Player.SFX_Source> sources = SFX_Player.instance.sources;
                for (int i = 0; i < sources.Count; i++)
                {
                    SFX_Player.SFX_Source pooled = sources[i];
                    if (!pooled.isPlaying || pooled.source == null || pooled.source.clip == null)
                    {
                        continue;
                    }
                    if (!PointPingerPatches.PingClips.Contains(pooled.source.clip))
                    {
                        continue;
                    }

                    AudioSource source = pooled.source;
                    _activeThisFrame.Add(source);

                    source.rolloffMode = AudioRolloffMode.Logarithmic;
                    source.minDistance = cfg.PingAudioMinDistanceMeters.Value;
                    source.maxDistance = cfg.PingAudioRangeMeters.Value;

                    float distance = camera != null ? Vector3.Distance(camera.transform.position, source.transform.position) : 0f;
                    float t = Mathf.InverseLerp(cfg.PingAudioMinDistanceMeters.Value, cfg.PingAudioRangeMeters.Value, distance);

                    AudioLowPassFilter lowPass = source.GetComponent<AudioLowPassFilter>();
                    if (lowPass == null)
                    {
                        lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
                    }
                    lowPass.enabled = true;
                    lowPass.cutoffFrequency = Mathf.Lerp(MaxCutoffHz, MinCutoffHz, t);
                }
            }

            // Any source we were muffling last frame but aren't touching this
            // frame (ping ended, or boost got disabled) must have its filter
            // disabled - it's a shared pooled AudioSource, so whatever plays
            // through it next (any other sound in the game) must not inherit
            // a stale muffle.
            foreach (AudioSource previouslyActive in _activeLastFrame)
            {
                if (previouslyActive == null || _activeThisFrame.Contains(previouslyActive))
                {
                    continue;
                }
                AudioLowPassFilter lowPass = previouslyActive.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    lowPass.enabled = false;
                }
            }

            _activeLastFrame.Clear();
            _activeLastFrame.UnionWith(_activeThisFrame);
        }
    }
}
