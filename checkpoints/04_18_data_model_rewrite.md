# Чекпоинт: Переделка моделей данных инвентаря

**Дата:** 2026-04-18 18:39:26 UTC
**Статус:** ⬜ pending
**Родительский чекпоинт:** `04_18_inventory_rewrite.md` (Этап 0)
**Обоснование:** `docs_temp/INVENTORY_FLAGS_AUDIT.md` v2.0

---

## Условие

Папка `Assets/` удаляется и пересоздаётся генератором (Phase09). Нет .asset файлов, нет save-данных. EquipmentSlot enum переписывается ПОЛНОСТЬЮ — нулевой риск совместимости.

---

## Задачи

### 0.1 Переписать EquipmentSlot enum ⬜

**Файл:** `Scripts/Core/Enums.cs`

**Было:**
```csharp
public enum EquipmentSlot
{
    None, WeaponMain, WeaponOff, Armor, Clothing, 
    Charger, RingLeft, RingRight, Accessory, Backpack
}
```

**Станет:**
```csharp
/// <summary>
/// Слот экипировки (переработан по INVENTORY_UI_DRAFT.md v2.0)
/// 
/// Видимые слоты куклы (7): Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff
/// Скрытые слоты (заглушки на будущее): Amulet, RingLeft1/2, RingRight1/2, Charger, Hands, Back
/// </summary>
public enum EquipmentSlot
{
    None,
    // === Видимые слоты куклы ===
    Head,           // Голова — шлем, шапка, корона
    Torso,          // Торс — нагрудник, рубашка, роба
    Belt,           // Пояс — ремень, пояс зелий, зарядник-пояс
    Legs,           // Ноги — поножи, штаны
    Feet,           // Ступни — сабатоны, сапоги
    WeaponMain,     // Основная рука — одноручное или щит
    WeaponOff,      // Вторичная рука — одноручное, щит или инструмент
    // === Скрытые слоты (заглушки) ===
    Amulet,         // Амулет — будет с ювелирной системой
    RingLeft1,      // Кольцо левое 1 — будет с системой хранения
    RingLeft2,      // Кольцо левое 2
    RingRight1,     // Кольцо правое 1
    RingRight2,     // Кольцо правое 2
    Charger,        // Зарядник Ци — будет с системой зарядников
    Hands,          // Перчатки — будет с расширением экипировки
    Back            // Плащ/спина — будет с расширением экипировки
}
```

**Удалены:** Armor, Clothing, Accessory, Backpack, RingLeft, RingRight
**Добавлены:** Head, Torso, Belt, Legs, Feet, Amulet, RingLeft1/2, RingRight1/2, Hands, Back

**Влияние на другие файлы:**

| Файл | Что менять |
|------|-----------|
| EquipmentData.cs | slot-поле подхватит новый enum автоматически |
| EquipmentController.cs | Будет переписан на Этапе 1 — не трогаем пока |
| AssetGeneratorExtended.cs | Обновить ParseEquipmentSlot() — см. 0.7 |
| NPCPresetData.cs | Обновить поля со старым EquipmentSlot |

---

### 0.2 Добавить NestingFlag enum ⬜

**Файл:** `Scripts/Core/Enums.cs` (секция #region Equipment)

```csharp
/// <summary>
/// Флаг вложения — куда можно поместить предмет
/// Источник: INVENTORY_UI_DRAFT.md §3.6.3
/// </summary>
public enum NestingFlag
{
    None,       // Нельзя поместить ни в какое хранилище (живые существа, квестовые)
    Spirit,     // Можно ТОЛЬКО в духовное хранилище
    Ring,       // Можно ТОЛЬКО в кольцо хранения
    Any         // Можно в любое хранилище (по умолчанию)
}
```

---

### 0.3 Добавить volume + allowNesting в ItemData ⬜

**Файл:** `Scripts/Data/ScriptableObjects/ItemData.cs`

Добавить в конец класса, перед закрывающей скобкой:

```csharp
[Header("Storage")]
[Tooltip("Объём предмета (для колец хранения)")]
public float volume = 1.0f;

[Tooltip("Куда можно поместить предмет (флаг вложения)")]
public NestingFlag allowNesting = NestingFlag.Any;
```

Также исправить:
- `sizeHeight` Range(1,3) → Range(1,2) — в дизайне макс 2

**Default-значения для генератора (по категориям):**

| ItemCategory | volume | allowNesting |
|-------------|--------|-------------|
| Consumable | 0.1 | Any |
| Material | 0.5-1.0 | Any |
| Weapon | 1-4 | Any |
| Armor | 1-4 | Any |
| Accessory | 0.1-0.3 | Any |
| Quest | 1.0 | None |
| Technique | 0.1 | Spirit |
| Misc | 1.0 | Any |

---

### 0.4 Создать BackpackData.cs ⬜

**Файл:** `Scripts/Data/ScriptableObjects/BackpackData.cs` (НОВЫЙ)

```csharp
// Создано: YYYY-MM-DD HH:MM:SS UTC

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные рюкзака. Определяет размер сетки инвентаря и бонусы веса.
    /// Рюкзак НЕ экипируется на куклу — отдельная система персонажа.
    /// </summary>
    [CreateAssetMenu(fileName = "Backpack", menuName = "Cultivation/Backpack")]
    public class BackpackData : ItemData
    {
        [Header("Backpack Grid")]
        [Tooltip("Ширина сетки (слотов)")]
        [Range(3, 10)]
        public int gridWidth = 3;

        [Tooltip("Высота сетки (слотов)")]
        [Range(3, 8)]
        public int gridHeight = 4;

        [Header("Weight Bonuses")]
        [Tooltip("Снижение веса содержимого (%)")]
        [Range(0f, 50f)]
        public float weightReduction = 0f;

        [Tooltip("Бонус к максимальному весу (кг)")]
        public float maxWeightBonus = 0f;

        [Header("Belt")]
        [Tooltip("Дополнительные слоты пояса (0-4)")]
        [Range(0, 4)]
        public int beltSlots = 0;
    }
}
```

**Пресеты для генератора:**

| Название | gridWidth | gridHeight | weightReduction | maxWeightBonus | beltSlots |
|----------|-----------|------------|-----------------|----------------|-----------|
| Тканевая сумка | 3 | 4 | 0% | +0 | 0 |
| Кожаный ранец | 4 | 5 | 10% | +10 | 1 |
| Железный контейнер | 5 | 5 | 15% | +20 | 2 |
| Духовный мешок | 6 | 6 | 25% | +30 | 2 |
| Межпространственный сундук | 8 | 7 | 40% | +50 | 4 |

---

### 0.5 Создать StorageRingData.cs ⬜

**Файл:** `Scripts/Data/ScriptableObjects/StorageRingData.cs` (НОВЫЙ)

```csharp
// Создано: YYYY-MM-DD HH:MM:SS UTC

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные кольца хранения. Объём-ограниченное хранилище,
    /// экипируется на слот кольца на кукле.
    /// </summary>
    [CreateAssetMenu(fileName = "StorageRing", menuName = "Cultivation/Storage Ring")]
    public class StorageRingData : ItemData
    {
        [Header("Storage Ring")]
        [Tooltip("Максимальный объём хранения")]
        public float maxVolume = 5f;

        [Tooltip("Базовая стоимость Qi для доступа")]
        public int qiCostBase = 5;

        [Tooltip("Стоимость Qi за единицу объёма")]
        public float qiCostPerUnit = 2f;

        [Tooltip("Время доступа (сек)")]
        public float accessTime = 1.5f;
    }
}
```

**Пресеты для генератора:**

| Название | maxVolume | qiCostBase | qiCostPerUnit | accessTime |
|----------|-----------|------------|---------------|------------|
| Кольцо-щель | 5 | 5 | 3 | 1.5 |
| Кольцо-карман | 15 | 5 | 2 | 1.5 |
| Кольцо-кладовая | 30 | 5 | 1 | 1.5 |
| Кольцо-пространство | 60 | 5 | 0.5 | 1.5 |

**Важно:** StorageRingData наследует ItemData, где `allowNesting = NestingFlag.Any` по умолчанию. Но для колец хранения нужно `allowNesting = NestingFlag.None` (кольцо нельзя вложить ни в какое хранилище — пространственная нестабильность). Генератор должен это учитывать.

---

### 0.6 Добавить WeaponHandType enum + handType в EquipmentData ⬜

**Файл 1:** `Scripts/Core/Enums.cs` (секция #region Equipment)

```csharp
/// <summary>
/// Тип хвата оружия — определяет, сколько слотов рук занимает
/// </summary>
public enum WeaponHandType
{
    OneHand,        // Одноручное — занимает 1 слот
    TwoHand         // Двуручное — занимает оба слота (WeaponMain + WeaponOff)
}
```

**Файл 2:** `Scripts/Data/ScriptableObjects/EquipmentData.cs`

Добавить после `slot`:
```csharp
[Header("Weapon")]
[Tooltip("Тип хвата (одноручное/двуручное)")]
public WeaponHandType handType = WeaponHandType.OneHand;
```

**Правило:** При equip предмета с `handType == TwoHand`:
- Если WeaponOff занят → снять в инвентарь
- Занять WeaponMain + заблокировать WeaponOff
- При unequip двуручного → освободить оба

---

### 0.7 Обновить AssetGeneratorExtended.cs ⬜

**Файл:** `Scripts/Generators/AssetGeneratorExtended.cs`

**Изменения:**
1. Обновить `ParseEquipmentSlot()` — маппинг JSON → новый EquipmentSlot enum
2. Добавить генерацию BackpackData из JSON
3. Добавить генерацию StorageRingData из JSON
4. При генерации оружия — устанавливать handType по подтипу
5. Установить volume по категории предмета
6. Установить allowNesting по категории

**Маппинг ParseEquipmentSlot (старый → новый):**

| JSON значение | Старый EquipmentSlot | Новый EquipmentSlot |
|---------------|---------------------|---------------------|
| "head" | Armor | Head |
| "torso" | Clothing | Torso |
| "legs" | Armor | Legs |
| "feet" | Armor | Feet |
| "weapon_main" | WeaponMain | WeaponMain |
| "weapon_off" | WeaponOff | WeaponOff |
| "belt" | — | Belt |
| "ring_left" | RingLeft | RingLeft1 |
| "ring_right" | RingRight | RingRight1 |

---

### 0.8 Обновить файлы со ссылками на EquipmentSlot ⬜

**Файлы для проверки/обновления:**

| Файл | Что проверять |
|------|-------------|
| NPCPresetData.cs | Поля со старым EquipmentSlot |
| PlayerController.cs | API вызовы Inventory — проверить совместимость |
| ResourcePickup.cs | API вызовы Inventory — проверить совместимость |
| QuestController.cs | API вызовы Inventory — проверить совместимость |
| InventoryUI.cs | Удалить (будет заменён на Этапе 3) |
| CharacterPanelUI.cs | Удалить (будет заменён на Этапе 3) |
| Phase06Player.cs | Компоненты на Player — без изменений |

---

### 0.9 Проверить компиляцию ⬜

После всех изменений — убедиться, что проект компилируется без ошибок.

**Предупреждения (допустимы):**
- CS0618 Obsolete warnings — старые ссылки на EquipmentSlot.Armor/Clothing/Accessory
- CS0067 unused event warnings

**Ошибки (НЕдопустимы):**
- CS0246 missing type — не обновлён файл
- CS0117 не содержит определения — старое значение enum

---

## Порядок выполнения

```
0.1 EquipmentSlot enum ──┐
0.2 NestingFlag enum     │  (параллельно, оба в Enums.cs)
0.6 WeaponHandType enum ─┘
         │
         ▼
0.3 ItemData: volume + allowNesting
0.6 EquipmentData: handType
         │
         ▼
0.4 BackpackData.cs (новый)
0.5 StorageRingData.cs (новый)
         │
         ▼
0.7 AssetGeneratorExtended.cs
0.8 Обновить зависимые файлы
         │
         ▼
0.9 Проверить компиляцию → git commit + push
```

---

## Итого изменений по файлам

| Файл | Действие | Новые поля/типы |
|------|----------|----------------|
| `Enums.cs` | ОБНОВИТЬ | EquipmentSlot (переписать), NestingFlag (новый), WeaponHandType (новый) |
| `ItemData.cs` | ОБНОВИТЬ | +volume, +allowNesting, fix sizeHeight Range |
| `EquipmentData.cs` | ОБНОВИТЬ | +handType |
| `BackpackData.cs` | НОВЫЙ | gridWidth, gridHeight, weightReduction, maxWeightBonus, beltSlots |
| `StorageRingData.cs` | НОВЫЙ | maxVolume, qiCostBase, qiCostPerUnit, accessTime |
| `AssetGeneratorExtended.cs` | ОБНОВИТЬ | ParseEquipmentSlot + генерация новых SO |
| `NPCPresetData.cs` | ОБНОВИТЬ | EquipmentSlot поля |
| `InventoryUI.cs` | УДАЛИТЬ | Будет заменён на Этапе 3 |
| `CharacterPanelUI.cs` | УДАЛИТЬ | Будет заменён на Этапе 3 |

---

*Чекпоинт создан: 2026-04-18 18:39:26 UTC*
