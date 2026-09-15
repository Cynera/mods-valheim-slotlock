using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SlotLock.Patches
{
    /// <summary>
    /// Every quick-stack route in the game funnels through Inventory.StackAll:
    /// the "Stack Items" button (InventoryGui.OnStackAll), the hover-a-chest hotkey
    /// (Container.StackAll -> RPC_StackResponse), and ValheimPlus' AutoStack sweep,
    /// which calls it once per nearby chest. Patching here covers all of them.
    ///
    /// The locked items are pulled out of the player's live inventory list for the
    /// duration of the call and put back in a finalizer, so the vanilla loop simply
    /// never sees them.
    ///
    /// Deliberately a prefix/finalizer and NOT a transpiler: ValheimPlus transpiles
    /// this same method to redirect its first ContainsItemByName call, and two
    /// transpilers rewriting the same IL would be asking for trouble.
    ///
    /// Priority.First matters. ValheimPlus' own prefix snapshots the player's item
    /// count and its postfix subtracts to produce the "stacked N items" message; if
    /// we withheld items after that snapshot, its count would be wrong.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll), new[] { typeof(Inventory), typeof(bool) })]
    internal static class Inventory_StackAll_Patch
    {
        // Inventory.Changed() is private, and vanilla StackAll calls it while our items
        // are still withheld -- which leaves the cached carry weight too low. Re-run it
        // after restoring so the weight and the UI agree with what's actually carried.
        private static readonly MethodInfo ChangedMethod =
            AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });

        private static void NotifyChanged(Inventory inventory)
        {
            if (ChangedMethod != null)
            {
                ChangedMethod.Invoke(inventory, new object[] { false, false });
                return;
            }

            // Signature moved in a game update: at least refresh the listeners.
            SlotLockPlugin.Log.LogWarning("Inventory.Changed(bool, bool) not found; falling back to m_onChanged.");
            inventory.m_onChanged?.Invoke();
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Inventory fromInventory, out List<ItemDrop.ItemData> __state)
        {
            __state = null;

            Player player = Player.m_localPlayer;
            if (fromInventory == null || player == null) return;

            // Only ever touch the local player's own inventory. Chest-to-chest or
            // other-player transfers are none of our business.
            if (!ReferenceEquals(fromInventory, player.GetInventory())) return;
            if (LockedSlots.Empty) return;

            List<ItemDrop.ItemData> live = fromInventory.GetAllItems();
            List<ItemDrop.ItemData> withheld = null;

            for (int i = live.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = live[i];
                if (item == null || !LockedSlots.IsLocked(item.m_gridPos)) continue;

                if (withheld == null) withheld = new List<ItemDrop.ItemData>();
                withheld.Add(item);
                live.RemoveAt(i);
            }

            __state = withheld;

            if (withheld != null && ModConfig.VerboseLogging.Value)
            {
                SlotLockPlugin.Log.LogInfo("Withheld " + withheld.Count + " item(s) in locked slots from a quick-stack.");
            }
        }

        [HarmonyFinalizer]
        private static void Finalizer(Inventory fromInventory, List<ItemDrop.ItemData> __state)
        {
            if (__state == null || fromInventory == null) return;

            List<ItemDrop.ItemData> live = fromInventory.GetAllItems();

            // Withheld items were collected back-to-front; re-add front-to-back so the
            // list ends up in roughly its original order.
            for (int i = __state.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = __state[i];
                if (item != null && !live.Contains(item)) live.Add(item);
            }

            NotifyChanged(fromInventory);
        }
    }
}
