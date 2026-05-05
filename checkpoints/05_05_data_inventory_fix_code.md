# 📦 Кодовая база: Исправление данных и инвентаря

👉 Основной план: [05_05_data_inventory_fix.md](05_05_data_inventory_fix.md)
**Дата:** 2026-05-05 09:45:07 UTC
**Верификация:** 2026-05-05 14:06:23 MSK
**Статус:** complete ✅

---

## К-09: SpiritStorage requiredCultivationLevel

**Файл:** `Scripts/Inventory/SpiritStorageController.cs` строка 45

**Сейчас:**
```csharp
public int requiredCultivationLevel = 1; // AwakenedCore
```

**Должно быть:**
```csharp
public int requiredCultivationLevel = 3; // InternalFire (INVENTORY_SYSTEM.md)
```

---

## К-10: SpiritStorage — лимит 20 слотов

**Файл:** `Scripts/Inventory/SpiritStorageController.cs`

**Добавить:**
```csharp
private const int MAX_SPIRIT_SLOTS = 20;

public bool AddEntry(SpiritStorageEntry entry)
{
    if (entries.Count >= MAX_SPIRIT_SLOTS) return false;
    entries.Add(entry);
    return true;
}
```

---

## К-11: ChargerData — удалить бонусы к первичным статам

**Файл:** `Scripts/Charger/ChargerData.cs` строки 134–138

**Удалить:**
```csharp
[SerializeField] private int strengthBonus;
[SerializeField] private int agilityBonus;
[SerializeField] private int intelligenceBonus;
[SerializeField] private int vitalityBonus;
```

**Причина:** BUFF_MODIFIERS_SYSTEM.md: «БАФФЫ НЕ МОГУТ ВЛИЯТЬ НА ПЕРВИЧНЫЕ ХАРАКТЕРИСТИКИ!»

Если нужны бонусы — реализовать через вторичные статы (damage, defense, speed).

---

## К-12 + В-11 + С-13: NPCPresetData комплексное обновление

**Файл:** `Scripts/Data/ScriptableObjects/NPCPresetData.cs`

**Текущие проблемы:**
1. `baseDisposition = 0` — устаревшее (К-12)
2. `List<PersonalityTraitEntry>` — вместо PersonalityTrait [Flags] (В-11)
3. `Alignment` enum — D&D система вместо PersonalityTrait (С-13)

**Исправление:**
```csharp
// Удалить:
public int baseDisposition = 0;
public Alignment alignment = Alignment.TrueNeutral;

// Добавить:
public Attitude baseAttitude = Attitude.Neutral;
public PersonalityTrait personalityFlags = PersonalityTrait.None;

// PersonalityTraitEntry список — оставить для совместимости, но пометить:
[Obsolete("Используйте personalityFlags")]
public List<PersonalityTraitEntry> personalityTraits = new();
```

**Обновить NPCController.cs** строку 374:
```csharp
state.Attitude = preset.baseAttitude; // Вместо ValueToAttitude(preset.baseDisposition)
state.Personality = preset.personalityFlags; // Вместо конвертации из PersonalityTraitEntry
```

---

## В-10: FormationEffects ApplyShield()

**Файл:** `Scripts/Formation/FormationEffects.cs` строки 498–510

**Сейчас:** Только Debug.Log

**Исправление:**
```csharp
public static void ApplyShield(GameObject target, FormationEffect effect)
{
    if (effect.value <= 0) return;
    var qi = target.GetComponent<Qi.QiController>();
    if (qi != null)
    {
        qi.AddTemporaryShield(effect.value, effect.duration);
    }
}
```

Зависит от наличия QiController.AddTemporaryShield() — если нет, нужно добавить.

---

## В-12: ElementData — список противоположностей

**Файл:** `Scripts/Data/ScriptableObjects/ElementData.cs` строка 43

**Сейчас:**
```csharp
public Element oppositeElement;
```

**Должно быть:**
```csharp
public List<Element> oppositeElements = new List<Element>();
```

Void имеет ДВЕ противоположности: Lightning и Light (после добавления Light в К-04).

---

## В-14: BackpackData — ограничение для пояса

**Файл:** `Scripts/Data/ScriptableObjects/BackpackData.cs`

**Добавить:**
```csharp
public ItemType allowedBeltItemType = ItemType.Consumable;
```

И в InventoryController при добавлении в belt-слот — проверять тип.

---

## В-15: MaterialData — добавить flexibility и qiRetention

**Файл:** `Scripts/Data/ScriptableObjects/MaterialData.cs`

**Добавить:**
```csharp
[Header("Дополнительные свойства")]
[Range(0f, 1f)]
public float flexibility = 0.5f;    // Гибкость (EQUIPMENT_SYSTEM.md §3.2)

[Range(75f, 100f)]
public float qiRetention = 90f;     // Удержание Ци % (EQUIPMENT_SYSTEM.md §3.2)
```

---

## С-05: ConsumableSOFactory

Создать `Scripts/Generators/ConsumableSOFactory.cs` по аналогии с EquipmentSOFactory.
~150 строк. Конвертирует GeneratedConsumable DTO → ConsumableData SO.

---

## С-06: Переименовать LootGenerator

`Scripts/Combat/LootGenerator.cs` → `Scripts/Combat/DeathLootGenerator.cs`
Обновить все using-ссылки и имя класса.

---

> Задачи В-13, С-15, С-16 перенесены в 👉 [05_05_docs_update_code.md](05_05_docs_update_code.md)

---

## С-07: Объединить StatBonus

**Создать `Scripts/Data/StatBonus.cs`:**
```csharp
namespace CultivationGame.Data
{
    [Serializable]
    public class StatBonus
    {
        public string statName;
        public float value;
        public bool isPercentage;
    }
}
```

**Обновить:**
- `ItemData.cs` — удалить локальный StatBonus, использовать CultivationGame.Data.StatBonus
- `WeaponGenerator.cs` — удалить локальный StatBonus, использовать CultivationGame.Data.StatBonus
- Поле `bonus` в ItemData.StatBonus → `value` (унификация)

---

## С-11: IncreaseMastery baseGain

**Файл:** `Scripts/Combat/TechniqueController.cs`

**Сейчас:**
```csharp
IncreaseMastery(technique, 0.01f);  // Обычное использование
IncreaseMastery(technique, 0.02f);  // Накачка
```

**Исправление:**
```csharp
IncreaseMastery(technique, 1.0f);   // Обычное использование
IncreaseMastery(technique, 2.0f);   // Накачка
```

Тогда при mastery=0: max(0.1, 1.0 × 1.0) = 1.0 (осмысленное значение).
При mastery=90: max(0.1, 1.0 × 0.1) = 0.1 (корректное замедление).

---

## С-12: CombatTrigger fallback

**Файл:** `Scripts/Combat/CombatTrigger.cs` строки 123–126

**Сейчас:** `if (minAttitudeToEngage >= Attitude.Hostile) return true;` — всегда true при дефолте

**Исправление:** Добавить дополнительное условие для tag-based врагов:
```csharp
if (targetNpc.HasTag("Enemy") && minAttitudeToEngage >= Attitude.Hostile) return true;
```
