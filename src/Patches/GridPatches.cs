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

    /// <summary>Draws a border around locked slots. Runs after the grid has laid itself out.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGrid_UpdateGui_Patch
    {
        private const string OverlayName = "SlotLock_Indicator";

        private static bool _loggedAttach;

        private static void Postfix(InventoryGrid __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;
            if (!ReferenceEquals(__instance.GetInventory(), player.GetInventory())) return;

            bool show = ModConfig.ShowOverlay.Value;
            Color color = ModConfig.OverlayColor.Value;
            int attached = 0;

            foreach (InventoryElement element in __instance.GetComponentsInChildren<InventoryElement>(includeInactive: true))
            {
                GameObject overlay = GetOrCreateOverlay(element);
                if (overlay == null) continue;
                attached++;

                bool locked = show && LockedSlots.IsLocked(element.Position);
                if (overlay.activeSelf != locked) overlay.SetActive(locked);
                if (!locked) continue;

                foreach (Image edge in overlay.GetComponentsInChildren<Image>(includeInactive: true))
                {
                    edge.color = color;
                }
            }

            if (!_loggedAttach && attached > 0)
            {
                _loggedAttach = true;
                SlotLockPlugin.Log.LogInfo("Lock border attached to " + attached + " inventory slot(s).");
            }
        }

        /// <summary>
        /// Builds the border out of four stretched Images rather than reusing one of the
        /// slot's own markers. An Image with no sprite draws a solid rectangle, so this
        /// needs no art, can't inherit a disabled component, and leaves the item icon
        /// fully visible instead of tinting over it.
        /// </summary>
        private static GameObject GetOrCreateOverlay(InventoryElement element)
        {
            Transform existing = element.transform.Find(OverlayName);
            if (existing != null) return existing.gameObject;

            var root = new GameObject(OverlayName, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(element.transform, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            float t = ModConfig.OverlayThickness.Value;
            AddEdge(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t)); // top
            AddEdge(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t)); // bottom
            AddEdge(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f)); // left
            AddEdge(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f)); // right

            root.transform.SetAsLastSibling();
            root.SetActive(false);
            return root;
        }

        private static void AddEdge(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)edge.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var image = edge.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = true;
        }
    }
}
