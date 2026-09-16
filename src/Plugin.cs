using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SlotLock
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    public class SlotLockPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ConfigMigration.Run(Config);
            ModConfig.Bind(Config);

            VerifyPatchTargets();

            _harmony = new Harmony(PluginInfo.Guid);
            _harmony.PatchAll(typeof(SlotLockPlugin).Assembly);

            Log.LogInfo(PluginInfo.Name + " " + PluginInfo.Version + " loaded.");
        }

        /// <summary>
        /// Several patch targets are private and therefore resolved by name, which a
        /// Valheim update can silently break. Check them up front so the log says which
        /// one moved, rather than the mod quietly doing nothing.
        /// </summary>
        private static void VerifyPatchTargets()
        {
            Expect(typeof(Inventory), "StackAll", new[] { typeof(Inventory), typeof(bool) });
            Expect(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });
            Expect(typeof(InventoryGrid), "OnLeftDown", new[] { typeof(UIInputHandler) });
            Expect(typeof(InventoryGrid), "UpdateGui", new[] { typeof(Player), typeof(ItemDrop.ItemData) });
        }

        private static void Expect(Type type, string method, Type[] parameters)
        {
            MethodInfo found = AccessTools.Method(type, method, parameters);
            if (found == null)
            {
                Log.LogError(type.Name + "." + method + " was not found. This build of " + PluginInfo.Name +
                             " is out of date for this version of Valheim; that feature will not work.");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
