# Task 5-8: Inventory Line Model Migration — Work Record

**Agent**: Code Agent
**Date**: 2026-04-27
**Task ID**: 5-8 (combined)

## Summary

Converted 10 files from grid-based to line-based (list) inventory system, and deleted 1 legacy file.

## Files Edited

1. **EquipmentController.cs** — Removed `EquipmentSlotsUI` class (dead code, lines 767-819), added BUG-8 fix comment for ServiceLocator.GetOrFind caching in CanEquip
2. **InventoryScreen.cs** — Replaced `backpackPanel.RebuildGrid()` → `backpackPanel.RefreshList()` (2 occurrences: Refresh() and SortInventory())
3. **TooltipPanel.cs** — Added edit timestamp comment. No sizeWidth/sizeHeight display was present (already clean)
4. **SpiritStorageController.cs** — Replaced `HasFreeSpace(sizeWidth, sizeHeight)` → `CanFitItem(itemData, count)` in both CanRetrieveWithQi and RetrieveToInventory
5. **StorageRingController.cs** — Same as SpiritStorageController: replaced `HasFreeSpace(sizeWidth, sizeHeight)` → `CanFitItem(itemData, count)` in both CanRetrieveWithQi and RetrieveToInventory
6. **CraftingController.cs** — Replaced `FreeCells < sizeWidth * sizeHeight` → `CanFitItem(recipe.resultItem, recipe.resultAmount * count)` in ValidateRecipe
7. **StorageRingPanel.cs** — BUG-12 fix: replaced `AddComponent<TMP_Text>()` → `AddComponent<TextMeshProUGUI>()` (TMP_Text is abstract)
8. **Phase16InventoryData.cs** — Updated BackpackData creation: `gridWidth/gridHeight/maxWeightBonus` → `maxWeight/maxVolume/ownWeight` with values from spec table; removed `sizeWidth/sizeHeight` from ItemData/EquipmentData/StorageRingData; removed `sizeW/sizeH` params from CreateTestEquipment
9. **Phase17InventoryUI.cs** — Replaced grid setup (GridBackground, GridContainer, GridLayoutGroup) with VerticalLayoutGroup + ScrollRect; rewired BackpackPanel references for new fields (slotRowPrefab, listContainer, volumeText, volumeBar, countText); rewrote CreateSlotUIPrefab from 50×50 grid cell to horizontal row (icon + name + count + weight + volume)
10. **Phase18InventoryComponents.cs** — Added edit timestamp; no gridWidth/gridHeight references found (already clean)

## File Deleted

11. **InventoryUI.cs** — Legacy v1 inventory UI, fully replaced by v2/v3 system

## Key API Changes Applied

| Old API | New API |
|---------|---------|
| `RebuildGrid()` | `RefreshList()` |
| `HasFreeSpace(w, h)` | `CanFitItem(itemData, count)` |
| `FreeCells` | `CanFitItem()` |
| `sizeWidth / sizeHeight` | (removed) |
| `gridWidth / gridHeight` | `maxWeight / maxVolume / ownWeight` |
| `maxWeightBonus` | `maxWeight` (direct) |
| `slotUIPrefab` | `slotRowPrefab` |
| `gridContainer / gridBackground` | `listContainer` |
| `slotsText` | `countText` |
| `AddComponent<TMP_Text>()` | `AddComponent<TextMeshProUGUI>()` |
