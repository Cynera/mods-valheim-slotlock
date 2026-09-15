using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace SlotLock
{
    /// <summary>
    /// The set of grid positions the local player has locked, persisted per character
    /// under BepInEx/config/SlotLock/. Locks are a client-side personal preference, so
    /// they are deliberately not synced to the server or to other players.
    /// </summary>
    internal static class LockedSlots
    {
        private const long NoProfile = long.MinValue;

        private static readonly HashSet<Vector2i> Explicit = new HashSet<Vector2i>();
        private static long _loadedPlayerId = NoProfile;
        private static string _path;

        /// <summary>True when nothing at all is protected, so callers can bail out early.</summary>
        internal static bool Empty
        {
            get
            {
                EnsureLoaded();
                return Explicit.Count == 0 && !ModConfig.ProtectHotbarRow.Value;
            }
        }

        internal static bool IsLocked(Vector2i pos)
        {
            EnsureLoaded();
            if (ModConfig.ProtectHotbarRow.Value && pos.y == 0) return true;
            return Explicit.Contains(pos);
        }

        internal static void Toggle(Vector2i pos)
        {
            EnsureLoaded();
            if (_loadedPlayerId == NoProfile) return;

            bool nowLocked;
            if (Explicit.Contains(pos))
            {
                Explicit.Remove(pos);
                nowLocked = false;
            }
            else
            {
                Explicit.Add(pos);
                nowLocked = true;
            }

            Save();
            SlotLockPlugin.Log.LogInfo(
                "Slot (" + pos.x + "," + pos.y + ") " + (nowLocked ? "locked" : "unlocked") + ".");
        }

        // ---- persistence -------------------------------------------------

        private static string Directory =>
            Path.Combine(Paths.ConfigPath, "SlotLock");

        private static void EnsureLoaded()
        {
            long id = CurrentPlayerId(out string name);
            if (id == _loadedPlayerId) return;

            Explicit.Clear();
            _loadedPlayerId = id;
            _path = null;

            if (id == NoProfile) return;

            _path = Path.Combine(Directory, Sanitize(name) + "_" + id.ToString(CultureInfo.InvariantCulture) + ".txt");
            Load();
        }

        private static long CurrentPlayerId(out string name)
        {
            name = null;
            try
            {
                var game = Game.instance;
                if (game == null) return NoProfile;
                var profile = game.GetPlayerProfile();
                if (profile == null) return NoProfile;
                name = profile.GetName();
                return profile.GetPlayerID();
            }
            catch (Exception)
            {
                return NoProfile;
            }
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                foreach (string raw in File.ReadAllLines(_path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;

                    string[] parts = line.Split(',');
                    if (parts.Length != 2) continue;
                    if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) continue;
                    if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) continue;

                    Explicit.Add(new Vector2i(x, y));
                }
                SlotLockPlugin.Log.LogInfo("Loaded " + Explicit.Count + " locked slot(s) from " + Path.GetFileName(_path) + ".");
            }
            catch (Exception e)
            {
                SlotLockPlugin.Log.LogError("Could not read locked slots from " + _path + ": " + e.Message);
            }
        }

        private static void Save()
        {
            if (_path == null) return;
            try
            {
                System.IO.Directory.CreateDirectory(Directory);

                var sb = new StringBuilder();
                sb.AppendLine("# SlotLock - one \"x,y\" inventory grid position per line.");
                sb.AppendLine("# x is the column and y the row, both zero-based, row 0 being the hotbar.");
                foreach (Vector2i pos in Explicit)
                {
                    sb.Append(pos.x.ToString(CultureInfo.InvariantCulture))
                      .Append(',')
                      .AppendLine(pos.y.ToString(CultureInfo.InvariantCulture));
                }

                File.WriteAllText(_path, sb.ToString());
            }
            catch (Exception e)
            {
                SlotLockPlugin.Log.LogError("Could not save locked slots to " + _path + ": " + e.Message);
            }
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "character";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
