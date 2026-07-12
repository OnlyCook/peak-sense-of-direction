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
    ///
    /// Critical bug this class used to have (found via in-game report - ping
    /// audio boost was making unrelated creature/item sounds like a Beetle's
    /// growl or a dropped Energy Drink audible from 50m+ away): per the
    /// decompiled <c>SFX_Player.IPlaySFX</c>, every single sound played
    /// through a pooled source has its <c>maxDistance</c> reset from
    /// <c>SFX_Instance.settings.range</c> on every play, but its
    /// <c>rolloffMode</c>/<c>minDistance</c> are never touched by vanilla at
    /// all. Once this tuner set a pooled source to
    /// <see cref="AudioRolloffMode.Logarithmic"/> with a boosted
    /// <c>minDistance</c> for a ping, those two values used to stick forever
    /// on that shared source - the very next unrelated sound played through
    /// the same pooled slot (any creature/item SFX, chosen at random by
    /// <c>SFX_Player.GetAvailibleSource</c>) silently inherited the ping's
    /// falloff shape. Fixed by snapshotting each source's original
    /// <c>rolloffMode</c>/<c>minDistance</c> the first time it's touched and
    /// restoring both (not just disabling the low-pass filter) the moment it
    /// stops playing a ping clip.
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

        private readonly struct OriginalSourceState
        {
            public readonly AudioRolloffMode RolloffMode;
            public readonly float MinDistance;

            public OriginalSourceState(AudioRolloffMode rolloffMode, float minDistance)
            {
                RolloffMode = rolloffMode;
                MinDistance = minDistance;
            }
        }

        private readonly Dictionary<AudioSource, OriginalSourceState> _originalState = new Dictionary<AudioSource, OriginalSourceState>();
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

                    if (!_originalState.ContainsKey(source))
                    {
                        _originalState[source] = new OriginalSourceState(source.rolloffMode, source.minDistance);
                    }

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
            // disabled AND its rolloffMode/minDistance restored - it's a
            // shared pooled AudioSource, so whatever plays through it next
            // (any other sound in the game) must not inherit a stale muffle
            // or falloff shape.
            foreach (AudioSource previouslyActive in _activeLastFrame)
            {
                if (previouslyActive == null)
                {
                    continue;
                }
                if (_activeThisFrame.Contains(previouslyActive))
                {
                    continue;
                }
                AudioLowPassFilter lowPass = previouslyActive.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    lowPass.enabled = false;
                }
                if (_originalState.TryGetValue(previouslyActive, out OriginalSourceState original))
                {
                    previouslyActive.rolloffMode = original.RolloffMode;
                    previouslyActive.minDistance = original.MinDistance;
                }
            }

            _activeLastFrame.Clear();
            _activeLastFrame.UnionWith(_activeThisFrame);
        }
    }
}
