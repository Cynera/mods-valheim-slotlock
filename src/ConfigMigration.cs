using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace SlotLock
{
    /// <summary>
    /// SlotLock 1.0.1 changed its plugin GUID from "introvertedcats.slotlock" to
    /// "cynera.SlotLock", and BepInEx names a config file after the GUID. Without this,
    /// everyone who had tuned their settings would silently get the defaults back.
    ///
    /// Locked slots themselves are unaffected: they live in config/SlotLock/, keyed by the
    /// mod name rather than the GUID.
    /// </summary>
    internal static class ConfigMigration
    {
        private const string OldGuid = "introvertedcats.slotlock";

        /// <summary>Must run before anything is bound, so the values are on disk by the
        /// time the config file is read.</summary>
        internal static void Run(ConfigFile config)
        {
            string current = Path.Combine(Paths.ConfigPath, PluginInfo.Guid + ".cfg");
            string previous = Path.Combine(Paths.ConfigPath, OldGuid + ".cfg");

            if (File.Exists(current) || !File.Exists(previous)) return;

            try
            {
                // A move rather than a copy: leaving the old file behind would be litter
                // under a name that no longer means anything.
                File.Move(previous, current);
                config.Reload();
                SlotLockPlugin.Log.LogInfo(
                    "Migrated settings from " + OldGuid + ".cfg to " + PluginInfo.Guid + ".cfg.");
            }
            catch (Exception e)
            {
                SlotLockPlugin.Log.LogWarning(
                    "Could not migrate settings from " + OldGuid + ".cfg (" + e.Message +
                    "). Starting from defaults; your old settings are still in that file.");
            }
        }
    }
}
