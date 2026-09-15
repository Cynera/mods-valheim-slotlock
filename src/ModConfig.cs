using BepInEx.Configuration;
using UnityEngine;

namespace SlotLock
{
    /// <summary>All user-facing settings. Locked slots themselves live in LockedSlots,
    /// because they are per-character rather than per-profile.</summary>
    internal static class ModConfig
    {
        internal static ConfigEntry<KeyCode> ToggleModifier;
        internal static ConfigEntry<bool> ShowOverlay;
        internal static ConfigEntry<Color> OverlayColor;
        internal static ConfigEntry<bool> ProtectHotbarRow;
        internal static ConfigEntry<float> OverlayThickness;
        internal static ConfigEntry<bool> VerboseLogging;

        internal static void Bind(ConfigFile cfg)
        {
            ToggleModifier = cfg.Bind(
                "1 - Controls", "ToggleModifier", KeyCode.LeftAlt,
                "Hold this key and left-click an inventory slot to lock or unlock it.");

            ProtectHotbarRow = cfg.Bind(
                "2 - Behaviour", "ProtectHotbarRow", false,
                "Treat the whole hotbar (the top row of your inventory) as locked, on top of any slots you lock by hand.");

            ShowOverlay = cfg.Bind(
                "3 - Appearance", "ShowOverlay", true,
                "Draw a coloured frame over locked slots.");

            OverlayColor = cfg.Bind(
                "3 - Appearance", "OverlayColor", new Color(1f, 0.78f, 0.23f, 0.85f),
                "Colour of that frame.");

            OverlayThickness = cfg.Bind(
                "3 - Appearance", "OverlayThickness", 3f,
                new ConfigDescription("Thickness of that frame, in pixels.",
                    new AcceptableValueRange<float>(1f, 10f)));

            VerboseLogging = cfg.Bind(
                "4 - Debug", "VerboseLogging", false,
                "Log every item withheld from a quick-stack. Noisy; for troubleshooting only.");
        }
    }
}
