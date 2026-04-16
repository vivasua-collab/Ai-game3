# CHECKPOINT: 04_15 — Fix Runtime Errors from Unity Console Log
# Created: 2026-04-15
# Status: IN PROGRESS

## Errors Found in docs_temp/04_15.log

### 1. Tag "Resource" is not defined (CRITICAL — ~100+ occurrences)
- **File**: ResourceSpawner.cs:229
- **Root Cause**: `try { go.tag = "Resource"; } catch { ... }` — Unity does NOT throw C# exceptions for undefined tags; it logs an error but doesn't enter the catch block. The tag is never assigned.
- **Fix**: Remove tag assignment entirely. Use `FindObjectsOfType<ResourcePickup>()` for resource lookup (already documented in comment).

### 2. MissingComponentException: RectTransform on "Fill Area" (CRITICAL)
- **File**: HarvestFeedbackUI.cs:186
- **Root Cause**: `new GameObject("Fill Area")` creates a regular Transform, not RectTransform. When parented under a Slider, it doesn't auto-convert.
- **Fix**: Explicitly add `RectTransform` component before accessing it.

### 3. ServiceLocator double registration (WARNING)
- **File**: TimeController.cs:102, WorldController.cs:182
- **Root Cause**: Both register in Awake(), then GameInitializer also registers them.
- **Fix**: Remove Awake() registration; let GameInitializer handle it. OR add guard in Register().

### 4. Player initialized twice (WARNING)
- **File**: PlayerController.cs:188 (Start) + GameInitializer
- **Root Cause**: InitializePlayer() called from Start() AND GameInitializer
- **Fix**: Already has `if (isInitialized) return;` guard — OK, but the double log is confusing. Remove one.

### 5. Tile sprite gaps — white background visible (VISUAL)
- **File**: TileSpriteGenerator.cs (spriteBorder(1,1,1,1) not enough)
- **Root Cause**: spriteBorder only works for 9-slice sprites. Regular single sprites ignore border. Need different approach.
- **Fix**: Increase sprite texture size with overscan (extend edges by 2px on each side = 68×68 texture displayed at 64×64), OR set tilemap cellGap to (0,0,0) explicitly, OR disable anti-aliasing.

### 6. Player sprite not using AI-generated art (VISUAL)
- **File**: PlayerVisual.cs — creates procedural circle, ignores player_variant PNGs
- **Root Cause**: PlayerVisual.CreateCircleSprite() is hardcoded; never loads from Assets/Sprites/Characters/Player/
- **Fix**: Try loading from Resources first, fallback to circle. Need to create Resources/Sprites/ folder and move player sprites there, OR load via AssetDatabase at runtime (Editor only) or use Addressables.

### 7. Ore sprites not using AI-generated art (VISUAL)
- **File**: ResourceSpawner.cs:256 — CreateResourceSprite() generates procedural circles
- **Root Cause**: Never loads obj_ore_vein.png from Sprites/Tiles_AI/
- **Fix**: Try loading from Resources, fallback to procedural.

## Fix Priority
1. ResourceSpawner tag fix (CRITICAL — breaks resource identification)
2. HarvestFeedbackUI RectTransform fix (CRITICAL — crashes F-key UI)
3. ServiceLocator double registration (WARNING — log spam)
4. Tile sprite gaps (VISUAL — annoying but not breaking)
5. Player/Ore sprite loading (VISUAL — cosmetic improvement)
