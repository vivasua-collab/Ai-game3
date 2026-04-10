# Чекпоинт: Fix-08 — Save System + Inventory/Equipment/Crafting

**Дата:** 2026-04-10 13:37:00 UTC
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
| 4 | `Inventory/InventoryController.cs` | 743 | Проверить интеграцию с Save/Equipment |
| 5 | `Inventory/EquipmentController.cs` | 682 | CanEquip требования, интеграция с Combatant |
| 6 | `Inventory/CraftingController.cs` | 649 | Grade application, AddSkillExperience |
| 7 | `Inventory/InventorySlot.cs` | в InvCtrl | GetCondition durability=0→Broken |
| 8 | `Inventory/MaterialSystem.cs` | 546 | Проверить |

---

## Задачи

### CRITICAL
- [ ] SAV-C01: SaveManager.GetSlotFilePath — валидация slotId (регулярка `^[a-zA-Z0-9_-]+$` или Path.GetFullPath + StartsWith)
- [ ] INV-C01: InventorySlot.GetCondition — durability=0 → Condition.Broken (не Pristine)

### HIGH
- [ ] SAV-H01: SaveDataTypes — обернуть Dictionary в [Serializable] классы с массивами пар (KeyBindings, Objectives, customBonuses, skills)
- [ ] SAV-H02: SaveManager.TotalPlayTimeHours — использовать real play time (Time.unscaledDeltaTime), не game time
- [ ] SAV-H03: SaveManager.CollectSaveData — добавить сбор из:
  - FormationController → FormationSaveData (НОВЫЙ класс)
  - BuffManager → BuffSaveData (НОВЫЙ класс)
  - TileMapController → TileSaveData (НОВЫЙ класс)
  - ChargerController → ChargerSaveData (НОВЫЙ класс)
  - NPCController → NPCSaveData (дополнен в Fix-07)
  - QuestController → QuestSaveData
  - PlayerController → объединённый PlayerSaveData
- [ ] SAV-H04: SaveManager.RefreshSlotCache — читать файлы при cache miss, не оставлять пустые слоты
- [ ] SAV-H05: SaveManager — после FromJson проверять null/битые поля перед ApplySaveData
- [ ] INV-H01: CraftingController.Craft — применить рассчитанный grade к созданному предмету
- [ ] INV-H02: CraftingController.AddSkillExperience — заменить random 20% level-up на реальную XP систему
- [ ] INV-H03: EquipmentController.CanEquip — реализовать проверку:
  - Уровень культивации ≥ item.requiredCultivationLevel
  - Характеристики ≥ item.requiredStats (STR, AGI, etc.)
  - EquipmentSlot совпадает

### MEDIUM
- [ ] SAV-M01: SaveFileHandler — удалить мёртвый код или интегрировать (выбрать один подход к шифрованию)
- [ ] SAV-M03: SaveManager — выбрать один метод шифрования (XOR или AES, не оба)
- [ ] SAV-M05: GameManager + GameInitializer — разделить ответственность инициализации

---

## Новые SaveData классы (добавить в SaveDataTypes.cs)

```csharp
[Serializable]
public class FormationSaveData
{
    public string formationId;
    public int practitionerCount;
    public long qiPoolAmount;
    // ... по мере необходимости
}

[Serializable]
public class BuffSaveData
{
    public string buffId;
    public float remainingDuration;
    public int stacks;
}

[Serializable]  
public class TileSaveData
{
    public int width, height;
    public string serializedTiles; // JSON string
}

[Serializable]
public class ChargerSaveData
{
    public int slotCount;
    public float heatLevel;
    public long qiStored;
}
```

---

## Порядок выполнения

1. SaveDataTypes.cs — сериализуемые wrappers + новые SaveData классы
2. SaveManager.cs — path traversal + cache + validation + collect all systems
3. SaveFileHandler.cs — удалить мёртвый код или интегрировать
4. InventorySlot — GetCondition fix
5. CraftingController.cs — grade + skill experience
6. EquipmentController.cs — CanEquip requirements
7. InventoryController.cs + MaterialSystem.cs — проверка интеграции

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi long в SaveData), Fix-06 (BuffSaveData schema), Fix-07 (NPC/Quest SaveData)
- **Последующие:** Fix-12 (UI зависит от Inventory)

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*
