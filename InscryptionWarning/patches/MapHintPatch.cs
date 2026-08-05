using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace InscryptionWarning.Patches
{
    // The Inscryption story event marks a creature with a secret 3-bit code
    // (SaveManager.GetInscryptionEventPath): the next three branch choices on the map must
    // match it (see FollowupConditionInscryptionEvent) to complete the event. This patch
    // highlights, on the map, which branch (left/right) matches the required bit at each of
    // those three distances.
    internal static class MapHintPatch
    {
        internal enum CueStyle
        {
            Off,
            Diegetic,
            Icon,
            Debug
        }

        private const string OverlayName = "InscryptionWarning_Hint";

        private static ConfigEntry<CueStyle> style = null!;

        // ponytail: only the most recently created Inscryption followup is tracked. A save
        // loaded mid-event (mod installed after the event already started) won't show a hint
        // until a new one is generated on a later run.
        private static int? startDistance;

        public static void Init(ConfigFile config)
        {
            style = config.Bind("Hint", "CueStyle", CueStyle.Diegetic,
                "How obvious the Inscryption path hint should be on the map: Off (disabled), " +
                "Diegetic (a barely-there tint on the correct node), Icon (a small visible badge), " +
                "Debug (an unmissable marker).");

            SelfCheck();
        }

        // Which branch (Left=bit 0, Right=bit 1) satisfies the code at the given map distance,
        // or null if that distance isn't one of the three the event checks.
        internal static MapScreen.BranchSelection? GetHintedBranch(int startDistance, int nodeDistance, int code)
        {
            var bitIndex = nodeDistance - (startDistance + 1);
            if (bitIndex < 0 || bitIndex > 2)
            {
                return null;
            }

            var requiredBit = (code >> (2 - bitIndex)) & 1;
            return requiredBit == 0 ? MapScreen.BranchSelection.LeftBranch : MapScreen.BranchSelection.RightBranch;
        }

        private static void SelfCheck()
        {
            // code 5 = 101: distances start+1..+3 must be Right, Left, Right.
            if (GetHintedBranch(10, 11, 5) != MapScreen.BranchSelection.RightBranch
                || GetHintedBranch(10, 12, 5) != MapScreen.BranchSelection.LeftBranch
                || GetHintedBranch(10, 13, 5) != MapScreen.BranchSelection.RightBranch
                || GetHintedBranch(10, 10, 5) != null
                || GetHintedBranch(10, 14, 5) != null)
            {
                throw new InvalidOperationException("MapHintPatch.GetHintedBranch self-check failed");
            }
        }

        [HarmonyPatch(typeof(FollowupConditionState), MethodType.Constructor, typeof(FollowupConditionData), typeof(int))]
        private static class TrackEventStart
        {
            private static void Postfix(FollowupConditionData data, int currentDistance)
            {
                if (data != null && data.ConditionName == nameof(FollowupConditionInscryptionEvent))
                {
                    startDistance = currentDistance;
                }
            }
        }

        [HarmonyPatch(typeof(MapNodeUI), nameof(MapNodeUI.RefreshState))]
        private static class HintNode
        {
            private static void Postfix(MapNodeUI __instance, SaveManager saveManager)
            {
                var overlay = GetOrCreateOverlay(__instance);
                if (!ShouldShowHint(__instance, saveManager))
                {
                    overlay.SetActive(false);
                    return;
                }

                ApplyStyle(overlay);
                overlay.SetActive(true);
            }
        }

        private static bool ShouldShowHint(MapNodeUI node, SaveManager saveManager)
        {
            if (style.Value == CueStyle.Off || startDistance == null || saveManager == null)
            {
                return false;
            }

            if (saveManager.GetInscryptionEventSuccessfullyCompleted())
            {
                return false;
            }

            var location = node.GetLocation();
            if (location == null)
            {
                return false;
            }

            var hinted = GetHintedBranch(startDistance.Value, location.Distance, saveManager.GetInscryptionEventPath());
            return hinted != null && node.GetBranch() == hinted;
        }

        // Parented directly to the node (not its icon child, which MapNodeUI.Set() destroys
        // and recreates), so it survives icon refreshes untouched.
        private static GameObject GetOrCreateOverlay(MapNodeUI node)
        {
            var existing = node.transform.Find(OverlayName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var overlay = new GameObject(OverlayName, typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(node.transform, worldPositionStays: false);
            overlay.GetComponent<Image>().raycastTarget = false;
            return overlay;
        }

        private static void ApplyStyle(GameObject overlay)
        {
            var rect = (RectTransform)overlay.transform;
            var image = overlay.GetComponent<Image>();

            switch (style.Value)
            {
                case CueStyle.Icon:
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(20f, 20f);
                    rect.anchoredPosition = Vector2.zero;
                    image.color = new Color(1f, 0.85f, 0.3f, 0.95f);
                    break;
                case CueStyle.Debug:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    image.color = new Color(1f, 0.1f, 0.9f, 0.8f);
                    break;
                default: // Diegetic
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    image.color = new Color(1f, 0.85f, 0.3f, 0.14f);
                    break;
            }
        }
    }
}
