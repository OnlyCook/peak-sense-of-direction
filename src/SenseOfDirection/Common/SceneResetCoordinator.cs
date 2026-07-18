using SenseOfDirection.Labels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// ISSUES.md: player labels (and their compass markers) could be left
    /// stuck on screen forever - starting a run while some were visible, then
    /// leaving back to the lobby or all the way out to the main menu, never
    /// cleared them. Root cause: <see cref="PlayerLabelController"/> only ever
    /// drops a tracked <c>Character</c> via <c>Character.OnDestroy</c>
    /// (<see cref="Labels.PlayerLabelPatches"/>) - if a character carries over
    /// a scene change without that firing (observed leaving a run back to the
    /// lobby, and again leaving all the way to the main menu), its label is
    /// frozen at whatever alpha it last had, visible or not, with nothing left
    /// to ever unregister it.
    ///
    /// Rather than chase every path that can leave a character stale, this
    /// listens for <em>any</em> scene load - main menu, lobby, a run, all of
    /// them - and unconditionally clears every tracked label, since nothing
    /// from before a scene load is still valid afterwards regardless of why
    /// its own cleanup path didn't run. Every other controller that owns a
    /// persistent (DontDestroyOnLoad) anchor already re-derives its own
    /// tracked target every frame and tears itself down on its own once that
    /// target disappears (<c>CampfireIndicator.CampfireIndicatorController</c>,
    /// <c>PirateCompass.PirateCompassLuggageIndicatorController</c>) - only
    /// the player-label roster needed an explicit sweep like this one.
    ///
    /// <see cref="PlayerLabelController.ResetAll"/> fades every currently
    /// tracked label out first rather than destroying them outright this same
    /// frame, so one still visible right as a scene loads (e.g. stepping into
    /// the main menu) eases away instead of popping off - the same vanilla-
    /// style fade every label already uses to appear/disappear day to day.
    /// </summary>
    public class SceneResetCoordinator : MonoBehaviour
    {
        private static SceneResetCoordinator _instance;

        public static SceneResetCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.SceneResetCoordinator");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SceneResetCoordinator>();
                    SceneManager.sceneLoaded += _instance.OnSceneLoaded;
                }
                return _instance;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayerLabelController.Instance.ResetAll();
        }
    }
}
