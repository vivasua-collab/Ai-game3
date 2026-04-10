# Чекпоинт: Fix-08 — Save System + Inventory/Equipment/Crafting

**Дата:** 2026-04-11 (completed)
**Фаза:** Phase 7 — Integration
**Статус:** ✅ complete
**Приоритет:** P0-HIGH

---

## Описание

Save: path traversal, Dictionary не сериализуем, неполное сохранение (нет Formation/Buff/Tile/Charger), cache miss, нет валидации после загрузки. Inventory: GetCondition инвертирован, Crafting не применяет grade, Equipment CanEquip всегда true.

---

## Файлы (6 файлов изменено)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Save/SaveManager.cs` | SAV-C01 path traversal, SAV-H02 real play time, SAV-H03 collect all systems, SAV-H04 cache miss, SAV-H05 validation, SAV-M01/M03 encryption comments |
| 2 | `Save/SaveDataTypes.cs` | SAV-H01 Dictionary wrappers (KeyBindingEntry, ObjectiveEntry, CustomBonusEntry, CraftingSkillEntry), SAV-H03 new SaveData classes (FormationSaveEntry, BuffSaveData, TileSaveData, ChargerSaveData) |
| 3 | `Inventory/InventoryController.cs` | INV-C01 GetCondition fix, INV-H01 grade field on InventorySlot |
| 4 | `Inventory/EquipmentController.cs` | INV-C01 GetCondition fix in EquipmentInstance, INV-H03 CanEquip requirements, SAV-H01 CustomBonusEntry helpers |
| 5 | `Inventory/CraftingController.cs` | INV-H01 grade application, INV-H02 real XP system, SAV-H01 CraftingSkillEntry serialization |

---

## Задачи

### CRITICAL
- [x] SAV-C01: SaveManager.GetSlotFilePath — regex валидация `^[a-zA-Z0-9_-]+$`
- [x] INV-C01: InventorySlot.GetCondition — durability=0 → Broken (в InventorySlot + EquipmentInstance)

### HIGH
- [x] SAV-H01: SaveDataTypes — Dictionary→сериализуемые wrappers (KeyBindingEntry, ObjectiveEntry, CustomBonusEntry, CraftingSkillEntry) с To/FromDictionary helpers
- [x] SAV-H02: SaveManager.TotalPlayTimeHours — realPlayTimeSeconds через Time.unscaledDeltaTime
- [x] SAV-H03: SaveManager.CollectSaveData — сбор из Formation, Buff, Tile, Charger, NPC, Quest, Player
- [x] SAV-H04: SaveManager.GetSlotInfo — чтение файла при cache miss
- [x] SAV-H05: SaveManager — ValidateSaveData перед ApplySaveData
- [x] INV-H01: CraftingController.Craft — grade applied to InventorySlot.grade
- [x] INV-H02: CraftingController.AddSkillExperience — реальная XP система (level*100 threshold)
- [x] INV-H03: EquipmentController.CanEquip — проверка requiredCultivationLevel + statRequirements

### MEDIUM
- [x] SAV-M01: SaveFileHandler — комментарий о дублировании XOR/AES (обратная совместимость)
- [x] SAV-M03: SaveManager — комментарий о миграции на SaveFileHandler.AES
- [ ] SAV-M05: GameManager + GameInitializer — вне scope Fix-08

---

*Чекпоинт обновлён: 2026-04-11 UTC*
