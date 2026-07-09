using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Every distance/visibility calculation in this mod originally read
    /// <c>Character.Head</c>/<c>.Center</c> directly as "where is this
    /// character right now" - fine while alive, since those bodypart
    /// transforms track the character 1:1, but wrong in two related ways
    /// once death enters the picture (found via in-game testing of Phase 6):
    /// <list type="bullet">
    /// <item>A <b>dead</b> character's own <c>Head</c>/<c>Center</c>
    /// bodyparts become an unreliable reference - the ragdoll gets moved/
    /// despawned some time after death, so reading them can return a wildly
    /// distant position (observed as player labels/pings suddenly reading
    /// "11km away" once the referenced character died). Vanilla's own
    /// <c>Character.GetSpectatePosition()</c> already works around exactly
    /// this by freezing on <c>LastLivingPosition</c> once <c>data.dead</c>
    /// is true - <see cref="EffectivePosition"/> applies the same fix
    /// wherever this mod needs "this character's position" for a character
    /// that might be dead (e.g. a ghost-pinging player).</item>
    /// <item>The <b>local</b> viewer's own <c>Character.localCharacter.Head</c>
    /// stops tracking what's actually on screen the moment they die and
    /// start spectating/free-camming - the camera moves on to whoever
    /// they're spectating (or flies off further in free-cam), while their
    /// own body/head stays wherever they died. <see cref="LocalViewpoint"/>
    /// swaps to the real camera position in that case, which is also just
    /// the more correct reference in general for "how far is X from what
    /// I'm currently looking at".</item>
    /// </list>
    /// </summary>
    internal static class CharacterPositions
    {
        internal static Vector3 EffectivePosition(Character character)
        {
            return character.data.dead ? character.LastLivingPosition : character.Head;
        }

        internal static Vector3 LocalViewpoint()
        {
            Character local = Character.localCharacter;
            if (local != null && local.data.fullyPassedOut)
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    return camera.transform.position;
                }
            }
            return local != null ? local.Head : Vector3.zero;
        }
    }
}
