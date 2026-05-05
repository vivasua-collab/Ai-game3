# 🟡 Чекпоинт: Данные и инвентарь — план исправления

**Дата:** 2026-05-08 | **Статус:** ПЛАН | **Приоритет:** СРЕДНИЙ-ВЫСОКИЙ
**Связанный аудит:** AUDIT_COMBAT_EQUIPMENT_v3.md (К-09, К-10, К-11, К-12, В-07, В-08, В-10, В-11..В-17, С-05..С-07, С-08, С-09, С-11..С-17, Н-01..Н-14)

---

## 🔴 Критические (Data-домен)

### К-09: SpiritStorage — уровень разблокировки 1 вместо 3
- **Файл:** `Inventory/SpiritStorageController.cs` строка 45
- **Исправление:** `requiredCultivationLevel = 3; // InternalFire`

### К-10: SpiritStorage — нет лимита 20 слотов
- **Файл:** `Inventory/SpiritStorageController.cs`
- **Исправление:**
  ```csharp
  private const int MAX_SPIRIT_SLOTS = 20;
  
  public bool AddEntry(SpiritStorageEntry entry)
  {
      if (entries.Count >= MAX_SPIRIT_SLOTS) return false;
      entries.Add(entry);
      return true;
  }
  ```

### К-11: ChargerData бонусы к первичным статам (ЗАПРЕЩЕНО)
- **Файл:** `Charger/ChargerData.cs` строки 134–138
- **Проблема:** BUFF_MODIFIERS_SYSTEM.md: «БАФФЫ НЕ МОГУТ ВЛИЯТЬ НА ПЕРВИЧНЫЕ ХАРАКТЕРИСТИКИ!»
- **Сейчас:** `private int strengthBonus; agilityBonus; intelligenceBonus; vitalityBonus;`
- **Исправление:** Удалить все 4 поля. Если нужны бонусы — реализовать через вторичные статы (damage, defense, speed и т.д.)

### К-12: NPCPresetData устаревшее baseDisposition
- **Файл:** `Data/ScriptableObjects/NPCPresetData.cs` строка 96
- **Сейчас:** `public int baseDisposition = 0;` (устаревшее)
- **Исправление:**
  ```csharp
  [Obsolete("Используйте baseAttitude")]
  public int baseDisposition = 0;
  
  public Attitude baseAttitude = Attitude.Neutral; // NEW
  ```
- **Обновить NPCController.cs** строку 374: читать baseAttitude напрямую

---

## 🟠 Высокие (Data-домен)

### В-07: QiController проводимость от maxQiCapacity вместо coreCapacity
- **Файл:** `Qi/QiController.cs` строка 107
- **Сейчас:** `baseConductivity = maxQiCapacity / 360f;`
- **Должно быть:** `baseConductivity = coreCapacity / 360f;`
- **Исправление:** Заменить `maxQiCapacity` на `coreCapacity` (baseCapacity — сериализуемое поле)

### В-08: QiController Meditate() обходит тиковую систему
- **Файл:** `Qi/QiController.cs` строки 260–287
- **Исправление:** Заменить `AdvanceHours()` на цикл по тикам:
  ```csharp
  for (int i = 0; i < durationTicks; i++)
  {
      float qiGain = baseConductivity * qiDensity * meditationMult * tickInterval;
      AddQi((long)qiGain);
      timeController.ProcessOneTick(); // Вместо AdvanceHours
  }
  ```

### В-10: FormationEffects ApplyShield() — заглушка
- **Файл:** `Formation/FormationEffects.cs` строки 498–510
- **Исправление:** Реально создать Qi-щит через QiController:
  ```csharp
  var shield = qi.CreateTemporaryShield(effect.value, effect.duration);
  ```

### В-11: NPCPresetData PersonalityTraitEntry вместо [Flags]
- **Файл:** `Data/ScriptableObjects/NPCPresetData.cs` строка 84
- **Сейчас:** `List<PersonalityTraitEntry>` (string + int)
- **Должно быть:** `PersonalityTrait PersonalityFlags` (enum [Flags])
- **Исправление:**
  ```csharp
  public PersonalityTrait personalityFlags = PersonalityTrait.None; // [Flags] enum
  ```
- **Риск:** CS0104 collision (причина текущего workaround) — нужно проверить и устранить

### В-12: ElementData — одно oppositeElement вместо списка
- **Файл:** `Data/ScriptableObjects/ElementData.cs` строка 43
- **Исправление:**
  ```csharp
  public List<Element> oppositeElements = new List<Element>(); // Вместо одного
  ```

### В-13: CultivationLevelData qiDensity — редактируемое поле
- **Файл:** `Data/ScriptableObjects/CultivationLevelData.cs` строка 38
- **Исправление:** Добавить валидацию или вычислять динамически:
  ```csharp
  public int QiDensity => GameConstants.QiDensityByLevel[Mathf.Min(level, GameConstants.QiDensityByLevel.Length - 1)];
  ```

### В-14: BackpackData — нет ограничения «только расходники»
- **Файл:** `Data/ScriptableObjects/BackpackData.cs`
- **Исправление:** Добавить поле и логику:
  ```csharp
  public ItemType allowedBeltItemType = ItemType.Consumable;
  ```

### В-15: MaterialData — отсутствуют flexibility и qiRetention
- **Файл:** `Data/ScriptableObjects/MaterialData.cs`
- **Исправление:** Добавить поля:
  ```csharp
  [Range(0f, 1f)] public float flexibility = 0.5f;
  [Range(75f, 100f)] public float qiRetention = 90f;
  ```

---

## 🟡 Средние (Data-домен)

### С-05: ConsumableSOFactory не существует
- **Решение:** Создать `Generators/ConsumableSOFactory.cs` по аналогии с EquipmentSOFactory
- **Объём:** ~150 строк

### С-06: Дубликат LootGenerator
- **Решение:** Переименовать `Combat/LootGenerator.cs` → `Combat/DeathLootGenerator.cs`
- **Объём:** ~5 мин

### С-07: Дубликат StatBonus
- **Решение:** Объединить в один класс `Data/StatBonus.cs`:
  ```csharp
  [Serializable] public class StatBonus { public string statName; public float value; public bool isPercentage; }
  ```
- **Обновить:** WeaponGenerator.cs и ItemData.cs — использовать единый класс

### С-08: BodyPartState — 6 состояний (включая Destroyed)
- **Файл:** `Core/Enums.cs` строки 303–311
- **Решение:** Если BODY_SYSTEM.md описывает 5 — убрать Destroyed. Если 6 — обновить документацию.
- **Сейчас Destroyed unreachable в UpdateState()** — нужно либо реализовать, либо удалить.

### С-09: Quadruped Torso HP = 150 вместо 100
- **Файл:** `Body/BodyDamage.cs` строка 157
- **Решение:** Уточнить в документации. Если Torso=100 верно, заменить 150f на 100f.

### С-11: IncreaseMastery baseGain слишком мал
- **Файл:** `Combat/TechniqueController.cs`
- **Решение:** Увеличить baseGain:
  ```csharp
  IncreaseMastery(technique, 1.0f);   // Обычное использование (было 0.01f)
  IncreaseMastery(technique, 2.0f);   // Накачка (было 0.02f)
  ```

### С-12: CombatTrigger ShouldEngage() — fallback обходит attitude
- **Файл:** `Combat/CombatTrigger.cs` строки 113–119
- **Решение:** Исправить fallback:
  ```csharp
  // БЫЛО: if (minAttitudeToEngage >= Attitude.Hostile) return true; — всегда true
  // СТАЛО: if (targetNpc.HasTag("AlwaysFight") && minAttitudeToEngage >= Attitude.Hostile) return true;
  ```

### С-13: NPCPresetData Alignment (D&D) вместо PersonalityTrait
- **Решение:** Заменить Alignment на PersonalityTrait [Flags] (связано с В-11)

### С-15: CultivationLevelData coreCapacityMultiplier — редактируемое
- **Решение:** Добавить формульную валидацию:
  ```csharp
  private void OnValidate() { coreCapacityMultiplier = Mathf.Pow(1.1f, totalSubLevels); }
  ```

### С-16: MortalStageData шансы пробуждения = 0 по умолчанию
- **Решение:** Установить дефолты по MORTAL_DEVELOPMENT.md:
  ```csharp
  public float baseAwakeningChance = 0.0001f;   // 0.01%
  public float highDensityBonus = 0.001f;        // 0.1%
  public float criticalBonus = 0.01f;            // 1%
  ```

### С-17: FactionData.FactionType не совпадает
- **Решение:** Обновить enum или документацию — согласовать типологию

---

## Порядок исправления

### Фаза 1: Критические данные (~60 мин)
1. К-09 + К-10 — SpiritStorage исправления
2. К-11 — Удалить первичные бонусы из ChargerData
3. К-12 — Добавить baseAttitude в NPCPresetData

### Фаза 2: Qi и формации (~60 мин)
4. В-07 — Исправить проводимость QiController
5. В-08 — Переписать Meditate() на тики
6. В-10 — Реализовать ApplyShield()

### Фаза 3: Data SO исправления (~90 мин)
7. В-11 + С-13 — PersonalityTrait [Flags] в NPCPresetData
8. В-12 — ElementData: список противоположностей
9. В-13 + С-15 — qiDensity и coreCapacity формулы
10. В-14 + В-15 — BackpackData + MaterialData поля

### Фаза 4: Рефакторинг (~60 мин)
11. С-05 — ConsumableSOFactory
12. С-06 — Переименовать DeathLootGenerator
13. С-07 — Единый StatBonus

**Итого:** ~270 мин (~4.5 часа)
