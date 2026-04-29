# 🔧 Чекпоинт: План переработки системы объектов

**Дата:** 2026-04-28 13:00 UTC
**Статус:** 📋 План готов к выполнению
**Основание:** Аудит `checkpoints/04_28_generator_audit_plan.md` (7 проблем кода + 2 проблемы документации)
**Цель:** Привести ВСЕ генераторы и DTO в соответствие с эталонной моделью ItemData v2.0 (строчная модель: weight + volume).

---

## 📐 ЭТАЛОННАЯ МОДЕЛЬ (напоминание)

Каждый предмет, создаваемый ЛЮБЫМ генератором, ДОЛЖЕН содержать 6 инвентарных полей:

| Поле | Тип | Оружие | Броня | Расходник |
|------|-----|--------|-------|-----------|
| `weight` | float | По подтипу + уровень + тир | По весовому классу + подтип | По типу (захардкожен) |
| `volume` | float | clamp(weight, 1, 4) | clamp(weight, 1, 4) | 0.1 (константа) |
| `stackable` | bool | false | false | true |
| `maxStack` | int | 1 | 1 | По типу (5-50) |
| `allowNesting` | NestingFlag | Any | Any | Any |
| `category` | ItemCategory | Weapon | Armor | Consumable |

---

## 🔴 КРИТИЧЕСКАЯ ПРОБЛЕМА: Отсутствует расчёт веса в генераторах

Аудит выявил не только отсутствие `volume` — в **WeaponGenerator** и **ArmorGenerator** **вообще нет поля `weight`** ни в DTO, ни в логике генерации. `GeneratedWeapon` не имеет веса. `GeneratedArmor` имеет `weightClass` (лёгкая/средняя/тяжёлая), но не численное значение веса.

**Это блокирует инвентарь:** без `weight` предмет нельзя поместить в рюкзак — `CanFitItem()` проверяет ОБА ограничителя.

---

## 📋 ПЛАН ПЕРЕРАБОТКИ — 4 ЭТАПА

### Этап 1: WeaponGenerator — добавить вес + инвентарные поля (P1)

**Файл:** `UnityProject/Assets/Scripts/Generators/WeaponGenerator.cs`

#### 1.1. Добавить справочник веса по подтипу

Оружие не имеет weight в DTO. Необходим справочник базового веса по подтипу, с масштабированием по уровню и тиру материала.

```csharp
// Базовый вес оружия по подтипу (кг)
// Источник: EQUIPMENT_SYSTEM.md §7 — весовой диапазон оружия
private static readonly Dictionary<WeaponSubtype, float> BaseWeightBySubtype = new()
{
    { WeaponSubtype.Unarmed,     0.3f },   // Кастеты
    { WeaponSubtype.Dagger,      0.5f },   // Кинжал
    { WeaponSubtype.Sword,       2.5f },   // Меч
    { WeaponSubtype.Greatsword,  6.0f },   // Двуручник
    { WeaponSubtype.Axe,         3.0f },   // Топор
    { WeaponSubtype.Spear,       2.8f },   // Копьё
    { WeaponSubtype.Bow,         1.2f },   // Лук
    { WeaponSubtype.Staff,       2.0f },   // Посох
    { WeaponSubtype.Hammer,      5.0f },   // Молот
    { WeaponSubtype.Mace,        3.5f },   // Булава
    { WeaponSubtype.Crossbow,    3.0f },   // Арбалет
    { WeaponSubtype.Wand,        0.2f }    // Жезл
};
```

**Формула итогового веса:**
```
weight = baseWeight × materialWeightMult × (1f + (itemLevel - 1) × 0.05f)
```
Где `materialWeightMult` — множитель веса материала:
- T1 (Iron): 1.0
- T2 (Steel): 1.0
- T3 (Spirit Iron): 0.8 (духовные материалы легче)
- T4 (Star Metal): 0.7
- T5 (Void Matter): 0.5 (пустотная материя сверхлёгкая)

```csharp
private static readonly float[] MaterialWeightMult = { 1.0f, 1.0f, 0.8f, 0.7f, 0.5f };
```

#### 1.2. Добавить поля в GeneratedWeapon DTO

После секции `// Level` (строка 113) добавить:

```csharp
// Inventory (строчная модель v2.0)
public float weight;                         // Вес (кг)
public float volume;                         // Объём (литры) = clamp(weight, 1, 4)
public bool stackable = false;               // Оружие не стакается
public int maxStack = 1;
public NestingFlag allowNesting = NestingFlag.Any;
public ItemCategory category = ItemCategory.Weapon;
```

#### 1.3. Добавить расчёт в Generate()

После строки `weapon.materialCategory = parameters.materialCategory;` (строка 290) добавить:

```csharp
// Вес (строчная модель инвентаря)
float baseWeight = BaseWeightBySubtype.ContainsKey(weapon.subtype)
    ? BaseWeightBySubtype[weapon.subtype] : 2.0f;
float matWMult = MaterialWeightMult[weapon.materialTier - 1];
weapon.weight = baseWeight * matWMult * (1f + (weapon.itemLevel - 1) * 0.05f);
weapon.volume = Mathf.Clamp(weapon.weight, 1f, 4f);
weapon.stackable = false;
weapon.maxStack = 1;
weapon.allowNesting = NestingFlag.Any;
weapon.category = ItemCategory.Weapon;
```

#### 1.4. Обновить GenerateExamples()

В строку вывода (после строки `Прочность:`) добавить:
```csharp
sb.AppendLine($"  Вес: {weapon.weight:F2}кг, Объём: {weapon.volume:F1}л");
```

---

### Этап 2: ArmorGenerator — добавить вес + инвентарные поля (P2)

**Файл:** `UnityProject/Assets/Scripts/Generators/ArmorGenerator.cs`

#### 2.1. Добавить справочник веса по подтипу + весовой класс

Броня имеет `weightClass`, но не численный вес. Необходим справочник базового веса по подтипу И весовому классу:

```csharp
// Базовый вес брони по (подтип, весовой класс) в кг
// Источник: EQUIPMENT_SYSTEM.md §8 — вес брони зависит от типа и материала
private static readonly Dictionary<(ArmorSubtype, ArmorWeightClass), float> BaseWeightBySubtypeAndClass = new()
{
    // Light
    { (ArmorSubtype.Head,  ArmorWeightClass.Light), 0.3f },
    { (ArmorSubtype.Torso, ArmorWeightClass.Light), 0.5f },
    { (ArmorSubtype.Arms,  ArmorWeightClass.Light), 0.2f },
    { (ArmorSubtype.Hands, ArmorWeightClass.Light), 0.1f },
    { (ArmorSubtype.Legs,  ArmorWeightClass.Light), 0.3f },
    { (ArmorSubtype.Feet,  ArmorWeightClass.Light), 0.2f },
    { (ArmorSubtype.Full,  ArmorWeightClass.Light), 1.5f },
    // Medium
    { (ArmorSubtype.Head,  ArmorWeightClass.Medium), 2.5f },
    { (ArmorSubtype.Torso, ArmorWeightClass.Medium), 5.0f },
    { (ArmorSubtype.Arms,  ArmorWeightClass.Medium), 2.0f },
    { (ArmorSubtype.Hands, ArmorWeightClass.Medium), 0.8f },
    { (ArmorSubtype.Legs,  ArmorWeightClass.Medium), 3.0f },
    { (ArmorSubtype.Feet,  ArmorWeightClass.Medium), 1.5f },
    { (ArmorSubtype.Full,  ArmorWeightClass.Medium), 8.0f },
    // Heavy
    { (ArmorSubtype.Head,  ArmorWeightClass.Heavy), 4.0f },
    { (ArmorSubtype.Torso, ArmorWeightClass.Heavy), 8.0f },
    { (ArmorSubtype.Arms,  ArmorWeightClass.Heavy), 3.5f },
    { (ArmorSubtype.Hands, ArmorWeightClass.Heavy), 1.5f },
    { (ArmorSubtype.Legs,  ArmorWeightClass.Heavy), 5.0f },
    { (ArmorSubtype.Feet,  ArmorWeightClass.Heavy), 3.0f },
    { (ArmorSubtype.Full,  ArmorWeightClass.Heavy), 12.0f }
};
```

#### 2.2. Добавить поля в GeneratedArmor DTO

После секции `// Level` (строка 98) добавить:

```csharp
// Inventory (строчная модель v2.0)
public float weight;                         // Вес (кг)
public float volume;                         // Объём (литры) = clamp(weight, 1, 4)
public bool stackable = false;               // Броня не стакается
public int maxStack = 1;
public NestingFlag allowNesting = NestingFlag.Any;
public ItemCategory category = ItemCategory.Armor;
```

#### 2.3. Добавить расчёт в Generate()

После строки `armor.materialCategory = parameters.materialCategory;` (строка 259) добавить:

```csharp
// Вес (строчная модель инвентаря)
var weightKey = (armor.subtype, armor.weightClass);
float baseWeight = BaseWeightBySubtypeAndClass.ContainsKey(weightKey)
    ? BaseWeightBySubtypeAndClass[weightKey] : 3.0f;
float matWMult = MaterialWeightMult[armor.materialTier - 1];
armor.weight = baseWeight * matWMult * (1f + (armor.itemLevel - 1) * 0.05f);
armor.volume = Mathf.Clamp(armor.weight, 1f, 4f);
armor.stackable = false;
armor.maxStack = 1;
armor.allowNesting = NestingFlag.Any;
armor.category = ItemCategory.Armor;
```

Где `MaterialWeightMult` — тот же справочник, что и для оружия (T1=1.0 ... T5=0.5). Добавить в ArmorGenerator:

```csharp
private static readonly float[] MaterialWeightMult = { 1.0f, 1.0f, 0.8f, 0.7f, 0.5f };
```

#### 2.4. Обновить GenerateExamples()

Добавить строку:
```csharp
sb.AppendLine($"  Вес: {armor.weight:F2}кг, Объём: {armor.volume:F1}л");
```

---

### Этап 3: ConsumableGenerator — удалить легаси + добавить volume (P3 + P7)

**Файл:** `UnityProject/Assets/Scripts/Generators/ConsumableGenerator.cs`

#### 3.1. Заменить SizeByType на VolumeByType (P7)

**Удалить** (строки 202-211):
```csharp
private static readonly Dictionary<ConsumableType, (int w, int h)> SizeByType = new Dictionary<ConsumableType, (int, int)>
{
    { ConsumableType.Pill, (1, 1) },
    ...
};
```

**Добавить:**
```csharp
// Объём расходников по типу (строчная модель v2.0)
// Источник: AssetGeneratorExtended.CalculateVolume → Consumable = 0.1
private static readonly Dictionary<ConsumableType, float> VolumeByType = new()
{
    { ConsumableType.Pill,     0.1f },
    { ConsumableType.Elixir,   0.1f },
    { ConsumableType.Food,     0.1f },
    { ConsumableType.Drink,    0.1f },
    { ConsumableType.Poison,   0.1f },
    { ConsumableType.Scroll,   0.1f },
    { ConsumableType.Talisman, 0.1f }
};
```

#### 3.2. Заменить поля в GeneratedConsumable DTO (P3)

**Удалить** (строки 91-93):
```csharp
// Size
public int sizeWidth;
public int sizeHeight;
```

**Добавить:**
```csharp
// Inventory (строчная модель v2.0)
public float volume;                         // Объём (литры) = 0.1 для всех расходников
public NestingFlag allowNesting = NestingFlag.Any;
public ItemCategory category = ItemCategory.Consumable;
```

#### 3.3. Заменить расчёт в Generate()

**Удалить** (строки 245-248):
```csharp
// Size
var (w, h) = SizeByType.ContainsKey(consumable.type) ? SizeByType[consumable.type] : (1, 1);
consumable.sizeWidth = w;
consumable.sizeHeight = h;
```

**Добавить:**
```csharp
// Объём (строчная модель инвентаря)
consumable.volume = VolumeByType.ContainsKey(consumable.type) ? VolumeByType[consumable.type] : 0.1f;
consumable.allowNesting = NestingFlag.Any;
consumable.category = ItemCategory.Consumable;
```

#### 3.4. Обновить GenerateExamples()

**Заменить** (строка 614):
```csharp
sb.AppendLine($"  Размер: {consumable.sizeWidth}x{consumable.sizeHeight}, Стек: {consumable.maxStack}");
```
**На:**
```csharp
sb.AppendLine($"  Вес: {consumable.weight:F3}кг, Объём: {consumable.volume:F1}л, Стек: {consumable.maxStack}");
```

---

### Этап 4: Phase16 + AssetGeneratorExtended + документация (P4 + P5 + P6 + D3 + D7)

#### 4.1. Phase16 CreateTestEquipment — volume по формуле (P4)

**Файл:** `Phase16InventoryData.cs`, строка 410

**Заменить:**
```csharp
data.volume = (damage > 0) ? 2f : 2f;
```
**На:**
```csharp
// Объём по формуле строчной модели (Источник: AssetGeneratorExtended.CalculateVolume)
data.volume = Mathf.Clamp(weight, 1f, 4f);
```

`data.allowNesting = NestingFlag.Any;` уже есть на строке 411 — не трогаем.

#### 4.2. ConsumableGenerator — масштабирование веса по уровню (P5)

**Файл:** `ConsumableGenerator.cs`, после строки 260 (после switch weight)

**Добавить:**
```csharp
// Масштабирование веса по уровню (P5)
consumable.weight *= 1f + (consumable.itemLevel - 1) * 0.1f;
```

Примеры:
| Тип | Базовый вес | Ур.1 | Ур.5 | Ур.9 |
|-----|-------------|------|------|------|
| Pill | 0.01 | 0.01 | 0.014 | 0.018 |
| Elixir | 0.20 | 0.20 | 0.28 | 0.36 |
| Food | 0.30 | 0.30 | 0.42 | 0.54 |

Разница незначительна для инвентаря, но логически корректна — старшие препараты требуют больше ингредиентов.

#### 4.3. AssetGeneratorExtended.ItemJson DTO — добавить volume/allowNesting (P6)

**Файл:** `AssetGeneratorExtended.cs`

В класс `ItemJson` добавить опциональные поля:
```csharp
public float? volume;           // null = вычислить по CalculateVolume
public string allowNesting;     // null = вычислить по CalculateNestingFlag
```

В `ApplyItemData()` изменить логику:
```csharp
// Текущее:
asset.volume = CalculateVolume(ParseItemCategory(data.category), data.weight);
asset.allowNesting = CalculateNestingFlag(ParseItemCategory(data.category));

// Новое:
asset.volume = data.volume.HasValue ? data.volume.Value
    : CalculateVolume(ParseItemCategory(data.category), data.weight);
asset.allowNesting = !string.IsNullOrEmpty(data.allowNesting)
    ? ParseNestingFlag(data.allowNesting)
    : CalculateNestingFlag(ParseItemCategory(data.category));
```

Добавить парсер:
```csharp
private static NestingFlag ParseNestingFlag(string value)
{
    return value?.ToLower() switch
    {
        "none" => NestingFlag.None,
        "spirit" => NestingFlag.Spirit,
        "ring" => NestingFlag.Ring,
        "any" => NestingFlag.Any,
        _ => NestingFlag.Any
    };
}
```

#### 4.4. EQUIPMENT_SYSTEM.md §3.2 — добавить volume материалов (D3)

Добавить строку в таблицу свойств материалов:
```
| volume | Объём (литры) | 0.5-4.0 |  // Формула: max(0.5, weight × 0.5) |
```

#### 4.5. EQUIPMENT_SYSTEM.md §8 — добавить volume экипировки (D7)

Добавить таблицу после §8.2:
```
### 8.3 Объём экипировки

Объём экипировки рассчитывается по формуле и определяет место в рюкзаке.

| Категория | Формула | Пример |
|-----------|---------|--------|
| Оружие | clamp(weight, 1, 4) | Меч 2.5кг → 2.5л |
| Броня | clamp(weight, 1, 4) | Шлем 2.5кг → 2.5л |
| Аксессуар | 0.2 (константа) | Кольцо → 0.2л |
```

---

## 🔄 ПОРЯДОК ВЫПОЛНЕНИЯ И ЗАВИСИМОСТИ

```
Этап 1 (WeaponGenerator)  ─┐
Этап 2 (ArmorGenerator)   ─┤── параллельные, независимые
Этап 3 (ConsumableGenerator)─┘
         │
         ▼
Этап 4 (Phase16 + AssetGen + Docs) — зависит от 1-3 (формулы volume должны совпадать)
```

**Рекомендуемый порядок:**
1. Этапы 1, 2, 3 — параллельно (можно в любом порядке)
2. Этап 4 — после завершения 1-3

---

## 📊 МАТРИЦА ИЗМЕНЕНИЙ

| Файл | Что делаем | Строк затронуто | Проблемы |
|------|-----------|-----------------|----------|
| WeaponGenerator.cs | +DTO поля, +BaseWeightBySubtype, +MaterialWeightMult, +расчёт в Generate(), +Examples | ~40 | P1 |
| ArmorGenerator.cs | +DTO поля, +BaseWeightBySubtypeAndClass, +MaterialWeightMult, +расчёт в Generate(), +Examples | ~50 | P2 |
| ConsumableGenerator.cs | -sizeWidth/sizeHeight, +volume/allowNesting/category, -SizeByType, +VolumeByType, +weight×level, +Examples | ~25 | P3, P5, P7 |
| Phase16InventoryData.cs | 1 строка: volume по формуле | 1 | P4 |
| AssetGeneratorExtended.cs | +2 поля в ItemJson, +ParseNestingFlag, +условная логика в ApplyItemData | ~20 | P6 |
| EQUIPMENT_SYSTEM.md | +1 строка в §3.2, +таблица в §8 | ~15 | D3, D7 |

**Итого:** ~150 строк изменений, 6 файлов.

---

## ✅ КРИТЕРИИ ПРИЁМКИ

После выполнения плана:

1. ✅ **GeneratedWeapon** содержит: weight, volume, stackable, maxStack, allowNesting, category — АУДИТ 2026-04-29
2. ✅ **GeneratedArmor** содержит: weight, volume, stackable, maxStack, allowNesting, category — АУДИТ 2026-04-29
3. ✅ **GeneratedConsumable** содержит: weight, volume, stackable, maxStack, allowNesting, category; НЕ содержит sizeWidth, sizeHeight — АУДИТ 2026-04-29
4. ✅ Все volume рассчитываются по единым формулам из CalculateVolume — АУДИТ 2026-04-29
5. ✅ Phase16 CreateTestEquipment: volume = Mathf.Clamp(weight, 1f, 4f) вместо 2.0 — АУДИТ 2026-04-29
6. ✅ AssetGeneratorExtended.ItemJson поддерживает явное указание volume/allowNesting из JSON — АУДИТ 2026-04-29
7. ✅ EQUIPMENT_SYSTEM.md содержит volume в §3.2 и §8.4 — АУДИТ 2026-04-29
8. ⏳ Компиляция — проверяется при следующем запуске Unity

---

## 📚 ССЫЛКИ

- Аудит: `checkpoints/04_28_generator_audit_plan.md`
- Эталонная модель: `checkpoints/04_27_inventory_line_model_plan.md`
- ItemData v2.0: `UnityProject/Assets/Scripts/Data/ScriptableObjects/ItemData.cs`
- CalculateVolume: `UnityProject/Assets/Scripts/Editor/AssetGeneratorExtended.cs` строки 814-827
- InventoryController: `UnityProject/Assets/Scripts/Inventory/InventoryController.cs`

---

*Создано: 2026-04-28 13:00 UTC*
*Редактировано: 2026-04-29 06:15 UTC — аудит кода подтвердил выполнение всех 7 критериев приёмки (кроме компиляции)*
