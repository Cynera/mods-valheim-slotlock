# Changelog

## 1.0.1
- Renamed the plugin GUID to `cynera.SlotLock`, so its settings now live in `BepInEx/config/cynera.SlotLock.cfg`.
- Existing settings are moved across automatically on first launch; the old `introvertedcats.slotlock.cfg` is removed once its contents have been carried over.
- Locked slots are unaffected - they are stored in `BepInEx/config/SlotLock/` and were never tied to the GUID.

## 1.0.0
- Initial release.
- Lock any inventory slot with a modifier + left-click; locked slots are never moved by Stack Items.
- Covers the Stack Items button, the hover-a-chest hotkey, and ValheimPlus AutoStack's nearby-chest sweep.
- Optional "protect the whole hotbar row" toggle.
