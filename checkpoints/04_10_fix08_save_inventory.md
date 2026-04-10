# Чекпоинт: Fix-08 — Save System + Inventory/Equipment/Crafting

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

Save: path traversal, Dictionary не сериализуем, неполное сохранение (нет Formation/Buff/Tile/Charger), cache miss, нет валидации после загрузки. Inventory: GetCondition инвертирован, Crafting не применяет grade, Equipment CanEquip всегда true.

---

## Файлы (8 файлов, ~4200 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Save/SaveManager.cs` | ~570 | Path traversal, cache miss, validation after FromJson, collect all systems |
| 2 | `Save/SaveFileHandler.cs` | 443 | Мёртвый код — удалить или интегрировать |
| 3 | `Save/SaveDataTypes.cs` | 323 | Dictionary→сериализуемые wrappers, добавить недостающие SaveData классы |
| 4 | `Inventory/InventoryController.cs` | 743 | — (проверить интеграцию) |
| 5 | `Inventory/EquipmentController.cs` | 682 | CanEquip требования, интеграция с Combatant |
| 6 | `Inventory/CraftingController.cs` | 649 | Grade application, AddSkillExperience |
| 7 | `Inventory/InventorySlot.cs` | в InvCtrl | GetCondition durability=0→Broken |
| 8 | `Inventory/MaterialSystem.cs` | 546 | — (проверить) |

---

## Задачи

### CRITICAL
- [ ] SAV-C01: SaveManager.GetSlotFilePath — валидация slotId (регулярка или Path.GetFullPath + StartsWith)
- [ ] INV-C01: InventorySlot.GetCondition — durability=0 → Condition.Broken (не Pristine)

### HIGH
- [ ] SAV-H01: SaveDataTypes — обернуть Dictionary в [Serializable] классы с массивами пар
- [ ] SAV-H02: SaveManager.TotalPlayTimeHours — использовать real play time (Time.unscaledDeltaTime)
- [ ] SAV-H03: SaveManager.CollectSaveData — добавить сбор из FormationController, BuffManager, TileMapController, ChargerController. Создать недостающие SaveData классы в SaveDataTypes.cs
- [ ] SAV-H04: SaveManager.RefreshSlotCache — читать файлы при cache miss
- [ ] SAV-H05: SaveManager — после FromJson проверять null/битые поля перед ApplySaveData
- [ ] INV-H01: CraftingController.Craft — применить рассчитанный grade к созданному предмету
- [ ] INV-H02: CraftingController.AddSkillExperience — заменить random 20% на реальную XP систему
- [ ] INV-H03: EquipmentController.CanEquip — реализовать проверку уровня культивации и характеристик

### MEDIUM
- [ ] SAV-M01: SaveFileHandler — удалить мёртвый код или интегрировать
- [ ] SAV-M03: SaveManager — выбрать один метод шифрования (XOR или AES)
- [ ] SAV-M05: GameManager + GameInitializer — разделить ответственность инициализации

---

## Порядок выполнения

1. SaveDataTypes.cs — сериализуемые wrappers + новые SaveData классы
2. SaveManager.cs — path traversal + cache + validation + collect all systems
3. SaveFileHandler.cs — удалить или интегрировать
4. InventorySlot — GetCondition fix
5. CraftingController.cs — grade + skill experience
6. EquipmentController.cs — CanEquip requirements

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi long в SaveData), Fix-06 (BuffSaveData), Fix-07 (Quest/NPC SaveData)
- **Последующие:** нет прямых

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*
