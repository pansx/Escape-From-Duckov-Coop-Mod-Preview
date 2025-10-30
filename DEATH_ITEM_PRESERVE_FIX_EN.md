# Client Death Item Preservation Temporary Fix

## Problem Description

In multiplayer mode, when a client player dies and re-enters the map, items in the grave disappear with the message `[loot] request denied: no_inv`. This is due to the grave system involving low-level code that is difficult to fix quickly.

## Temporary Solution

This fix addresses the problem through the following methods:

1. **Intercept Client Player Death Logic** - Prevent the original `OnDead` method from clearing items
2. **Preserve Items** - Client player inventory and equipment are not cleared upon death
3. **Maintain Death State** - Keep death visual effects and game state, but items are not lost

## Implementation Details

### Main Patch Classes

- `PlayerDeathItemPreservePatch` - Intercepts player death logic
- `PreventClientPlayerItemClearPatch` - Prevents inventory clearing

### How It Works

1. **Death Interception**: Intercept before the client player's `OnDead` method executes
2. **Visual Effects**: Execute death animations and visual effects, but skip item dropping
3. **State Management**: Set death state but preserve all items
4. **Spectator Mode**: Normally enter spectator mode (if enabled)

### Scope of Impact

- **Client Mode Only** - Host mode is unaffected
- **Local Player Only** - Does not affect AI or other players
- **Network Sync Maintained** - Death state is still properly synced to host

## Usage Instructions

1. Compile and install the mod
2. Connect to host in client mode
3. Items will be automatically preserved upon death
4. Console will display relevant log information

## Log Information

When the fix is activated, the following information will be displayed in the console:

```
[COOP] Client player death - item preservation mode enabled (temporary fix)
[COOP] Client player death processing complete - items preserved (temporary fix)
[COOP] Blocked inventory clearing on death (temporary fix)
```

## Multi-language Support

The fix supports the following languages:

- Chinese (zh-CN)
- English (en-US)
- Japanese (ja-JP)
- Korean (ko-KR)

## Important Notes

1. **Temporary Nature** - This is a temporary solution until the grave system is fundamentally fixed
2. **Client Limitation** - Only effective in client mode
3. **Compatibility** - Fully compatible with existing multiplayer features
4. **Performance Impact** - Minimal impact on game performance

## Future Plans

When the underlying grave system issue is fixed, this temporary patch can be removed or disabled.

## Technical Details

### File Locations

- Main patch: `EscapeFromDuckovCoopMod/Patch/Character/PlayerDeathItemPreservePatch.cs`
- Localization files: `Localization/*.json`

### Dependencies

- HarmonyLib (for method interception)
- Existing multiplayer mod infrastructure

### Testing Recommendations

1. Connect to host in client mode
2. Carry some items into the game
3. Intentionally die
4. Check if items are preserved
5. Verify spectator mode works normally