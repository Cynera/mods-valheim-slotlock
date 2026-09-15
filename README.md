# SlotLock

Lock individual inventory slots so **Stack Items** never puts them in a chest.

You keep your pickaxe, hammer, food and portal wood exactly where you put them, and
still get to mash the stack button on every chest in the base.

## Usage

**Hold `Left Alt` and left-click a slot** to lock or unlock it. Locked slots get a gold
frame. That's the whole mod.

Locked slots are ignored by:

- the **Stack Items** button in the chest window
- the **hover-a-chest and press the stack key** shortcut
- **ValheimPlus AutoStack**, including its sweep across every nearby chest

Everything else still works normally — you can drag items out of a locked slot by hand,
equip them, drop them, and Take All is unaffected.

## Compatibility

Client-side only. **It does not need to be installed on the server**, and it does not
need to match between players — lock whichever slots you like, your friends lock theirs.

Tested alongside ValheimPlus 0.10.1.1 with `[AutoStack] enabled = true`.

SlotLock hooks `Inventory.StackAll` with a prefix and a finalizer, and deliberately does
**not** use a transpiler there, because ValheimPlus transpiles that same method. Its
prefix runs at `Priority.First` so ValheimPlus' "stacked N items" count stays correct.

If you use **QuickStackStore**, you probably don't need this — its favouriting feature
covers the same ground and more. SlotLock exists to be a small, single-purpose
alternative that stays out of ValheimPlus' way.

## Configuration

`BepInEx/config/introvertedcats.slotlock.cfg`

| Setting | Default | What it does |
| --- | --- | --- |
| `ToggleModifier` | `LeftAlt` | Hold this and left-click a slot to lock it |
| `ProtectHotbarRow` | `false` | Treat the entire hotbar row as locked |
| `ShowOverlay` | `true` | Draw the frame on locked slots |
| `OverlayColor` | gold | Colour of that frame |
| `VerboseLogging` | `false` | Log each withheld item |

Which slots you locked is saved per character, in
`BepInEx/config/SlotLock/<character>_<id>.txt`. Deleting that file clears your locks.

## Known limitations

- The lock frame shows in the inventory screen, not on the on-screen hotbar HUD.
- Locks are per character, not per world.
