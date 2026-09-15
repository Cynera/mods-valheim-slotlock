using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SlotLock.Patches
{
    /// <summary>Modifier + left-click on one of your own slots toggles its lock.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "OnLeftDown")]
    internal static class InventoryGrid_OnLeftDown_Patch
    {
        private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            if (clickHandler == null) return true;
            if (!ZInput.GetKey(ModConfig.ToggleModifier.Value, logWarning: false)) return true;

            Player player = Player.m_localPlayer;
            if (player == null) return true;
            if (!ReferenceEquals(__instance.GetInventory(), player.GetInventory())) return true;

            InventoryElement element = clickHandler.GetComponent<InventoryElement>();
            if (element == null) return true;

            LockedSlots.Toggle(element.Position);
            return false; // swallow the click so we don't also pick the item up
        }
    }

    /// <summary>Draws a frame over locked slots. Runs after the grid has laid itself out.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGrid_UpdateGui_Patch
    {
        private const string OverlayName = "SlotLock_Indicator";

        private static void Postfix(InventoryGrid __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;
            if (!ReferenceEquals(__instance.GetInventory(), player.GetInventory())) return;

            bool show = ModConfig.ShowOverlay.Value;

            foreach (InventoryElement element in __instance.GetComponentsInChildren<InventoryElement>(includeInactive: true))
            {
                Image overlay = GetOrCreateOverlay(element);
                if (overlay == null) continue;

                bool locked = show && LockedSlots.IsLocked(element.Position);
                if (overlay.gameObject.activeSelf != locked) overlay.gameObject.SetActive(locked);
                if (locked) overlay.color = ModConfig.OverlayColor.Value;
            }
        }

        /// <summary>
        /// Clones the slot's own "equipped" marker rather than shipping a sprite, which
        /// guarantees a valid sprite and the correct RectTransform anchoring for free.
        /// </summary>
        private static Image GetOrCreateOverlay(InventoryElement element)
        {
            Transform existing = element.transform.Find(OverlayName);
            if (existing != null) return existing.GetComponent<Image>();

            if (element.m_equiped == null) return null;

            GameObject go = Object.Instantiate(element.m_equiped.gameObject, element.transform);
            go.name = OverlayName;

            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                Object.Destroy(go);
                return null;
            }

            image.raycastTarget = false;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
            return image;
        }
    }
}
