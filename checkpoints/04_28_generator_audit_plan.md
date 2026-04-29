# 🔍 Чекпоинт: План аудита генераторов на соответствие строчной модели инвентаря

**Дата:** 2026-04-28 06:20 UTC
**Статус:** ✅ аудит завершён (код + документация)
**Цель:** Проверить ВСЕ генераторы и документацию на соответствие строчной модели инвентаря: корректные ОБА ограничителя — weight (масса) И volume (объём).

---

## 📐 ЭТАЛОННАЯ МОДЕЛЬ — обязательные поля ItemData v2.0

Каждый предмет, создаваемый генератором, ДОЛЖЕН иметь:

| Поле | Тип | Описание | Ограничение |
|------|-----|----------|-------------|
| `weight` | float | Вес (кг) | > 0 |
| `volume` | float | Объём (литры) | > 0 |
| `stackable` | bool | Можно стакать | — |
| `maxStack` | int | Максимум в стаке | >= 1; если !stackable → 1 |
| `allowNesting` | NestingFlag | Куда помещать | None/Spirit/Ring/Any |
| `category` | ItemCategory | Категория | Не None/0 (если default=Weapon) |

### Формулы расчёта volume (из AssetGeneratorExtended.CalculateVolume)

| Категория | Формула volume | Примеры |
|-----------|---------------|---------|
| Consumable | 0.1 | Таблетка, эликсир |
| Material | max(0.5, weight×0.5) | Слиток 2кг → 1.0л |
| Technique | 0.1 | Свиток |
| Quest | 1.0 | Артефакт |
| Weapon | clamp(weight, 1, 4) | Меч 2.5кг → 2.5л |
| Armor | clamp(weight, 1, 4) | Шлем 2.5кг → 2.5л |
| Accessory | 0.2 | Кольцо |

### Формулы расчёта allowNesting (из AssetGeneratorExtended.CalculateNestingFlag)

| Категория | allowNesting |
|-----------|-------------|
| Quest | None |
| Technique | Spirit |
| Остальные | Any |

---

## 📊 КАРТА ГЕНЕРАТОРОВ

### Группа A: Runtime-генераторы (Generators/)

| # | Файл | Что генерирует | Класс результата | Обязательные поля ItemData заполнены? |
|---|------|---------------|-----------------|--------------------------------------|
| A1 | WeaponGenerator.cs | Оружие | GeneratedWeapon (DTO) | ❌ НЕТ volume, weight есть, stackable/maxStack НЕТ, allowNesting НЕТ |
| A2 | ArmorGenerator.cs | Броня | GeneratedArmor (DTO) | ❌ НЕТ volume, weight есть, stackable/maxStack НЕТ, allowNesting НЕТ |
| A3 | ConsumableGenerator.cs | Расходники | GeneratedConsumable (DTO) | ❌ sizeWidth/sizeHeight (легаси), НЕТ volume, weight есть, stackable/maxStack есть, allowNesting НЕТ |

### Группа B: Editor-генераторы (Editor/)

| # | Файл | Что генерирует | Класс результата | Обязательные поля ItemData заполнены? |
|---|------|---------------|-----------------|--------------------------------------|
| B1 | AssetGeneratorExtended.cs | WeaponData .asset | EquipmentData (SO) | ✅ weight, volume, allowNesting, stackable=false |
| B2 | AssetGeneratorExtended.cs | ArmorData .asset | EquipmentData (SO) | ✅ weight, volume, allowNesting, stackable=false |
| B3 | AssetGeneratorExtended.cs | ItemData .asset | ItemData (SO) | ✅ weight, volume, allowNesting, stackable |
| B4 | AssetGeneratorExtended.cs | MaterialData .asset | MaterialData (SO) | ✅ weight, volume, allowNesting |
| B5 | AssetGeneratorExtended.cs | StorageRingData .asset | StorageRingData (SO) | ✅ weight, volume, allowNesting=None |

### Группа C: SceneBuilder Phase-генераторы (Editor/SceneBuilder/)

| # | Файл | Что генерирует | Класс результата | Обязательные поля ItemData заполнены? |
|---|------|---------------|-----------------|--------------------------------------|
| C1 | Phase16InventoryData.cs | BackpackData .asset | BackpackData (SO) | ✅ weight, volume, allowNesting=None, stackable=false |
| C2 | Phase16InventoryData.cs | StorageRingData .asset | StorageRingData (SO) | ✅ weight, volume, allowNesting=None, stackable=false |
| C3 | Phase16InventoryData.cs | Test Equipment .asset | EquipmentData (SO) | ⚠️ volume=2.0 для ВСЕХ (не зависит от типа/веса) |
| C4 | Phase16InventoryData.cs | UpdateExistingItemData | ItemData (SO) | ✅ volume + allowNesting по категории |

---

## 🐛 ВЫЯВЛЕННЫЕ ПРОБЛЕМЫ

### P1: WeaponGenerator.GeneratedWeapon — отсутствуют инвентарные поля
**Серьёзность:** 🔴 КРИТИЧЕСКАЯ
**Файл:** WeaponGenerator.cs
**Проблема:** DTO GeneratedWeapon не содержит volume, allowNesting, stackable, maxStack.
Генератор рассчитывает урон/прочность/бонусы, но не формирует данные для инвентаря.
**Влияние:** При конвертации GeneratedWeapon → EquipmentData .asset отсутствуют критичные поля.
Если кто-то вызовет WeaponGenerator.Generate() и создаст ассет — поля будут дефолтные.
**Решение:** Добавить в GeneratedWeapon:
- `float volume` — рассчитывать по формуле: clamp(weight, 1, 4)
- `NestingFlag allowNesting = NestingFlag.Any`
- `bool stackable = false`
- `int maxStack = 1`
**Контраргумент:** AssetGeneratorExtended.ApplyWeaponData() заполняет volume/allowNesting при
создании .asset — поэтому runtime-генератор МОЖЕТ не иметь этих полей, если единственный
путь в инвентарь — через AssetGeneratorExtended.
**Вердикт:** ⚠️ ДОБАВИТЬ поля — Defence in depth. Если генератор используется напрямую,
без AssetGeneratorExtended, инвентарь получит некорректные данные.

### P2: ArmorGenerator.GeneratedArmor — отсутствуют инвентарные поля
**Серьёзность:** 🔴 КРИТИЧЕСКАЯ
**Файл:** ArmorGenerator.cs
**Проблема:** Аналогично P1 — нет volume, allowNesting, stackable, maxStack.
**Решение:** Добавить в GeneratedArmor:
- `float volume` — рассчитывать по формуле: clamp(weight, 1, 4)
- `NestingFlag allowNesting = NestingFlag.Any`
- `bool stackable = false`
- `int maxStack = 1`

### P3: ConsumableGenerator.GeneratedConsumable — легаси sizeWidth/sizeHeight
**Серьёзность:** 🟡 СРЕДНЯЯ
**Файл:** ConsumableGenerator.cs (строки 92-93, 247-248, 614)
**Проблема:** DTO содержит `sizeWidth` и `sizeHeight` вместо `volume`.
Размер по типу `SizeByType` — сеточная модель (1×1 или 1×2), не строчная.
Отсутствует `allowNesting`.
**Влияние:** Не вызывает ошибку компиляции (поля в собственном классе), но:
1. Вывод `GenerateExamples()` показывает "Размер: 1x1" — легаси-информация
2. При конвертации GeneratedConsumable → ItemData .asset volume не берётся из генератора
3. Отсутствует allowNesting — Consumable должен быть NestingFlag.Any
**Решение:**
- Добавить `float volume` — рассчитывать по типу (0.1л для всех расходников)
- Добавить `NestingFlag allowNesting = NestingFlag.Any`
- Удалить `sizeWidth`, `sizeHeight` и `SizeByType`
- Обновить `GenerateExamples()` — показывать volume вместо размера

### P4: Phase16 CreateTestEquipment — volume=2.0 для всех предметов
**Серьёзность:** 🟡 СРЕДНЯЯ
**Файл:** Phase16InventoryData.cs, строка 410
**Проблема:** `data.volume = (damage > 0) ? 2f : 2f;` — обе ветви одинаковые,
volume=2.0 для всех 10 предметов. Не учитывается вес предмета.
**Ожидаемое:** volume должен рассчитываться по формуле CalculateVolume:
- Оружие: clamp(weight, 1, 4) — меч 2.5кг → 2.5л, кинжал 0.5кг → 1.0л
- Броня: clamp(weight, 1, 4) — шлем 2.5кг → 2.5л, роба 0.5кг → 1.0л
**Решение:** Заменить `data.volume = (damage > 0) ? 2f : 2f;` на:
```csharp
ItemCategory cat = (slot == EquipmentSlot.WeaponMain || slot == EquipmentSlot.WeaponOff)
    ? ItemCategory.Weapon : ItemCategory.Armor;
data.volume = Mathf.Clamp(weight, 1f, 4f);
data.allowNesting = NestingFlag.Any;
```

### P5: ConsumableGenerator — вес не масштабируется с уровнем
**Серьёзность:** 🟢 НИЗКАЯ
**Файл:** ConsumableGenerator.cs, строки 251-260
**Проблема:** Вес расходников захардкожен (pill=0.01, elixir=0.2 и т.д.) и не зависит от
itemLevel. Предмет 9-го уровня весит столько же, сколько 1-го.
**Влияние:** Незначительное — расходники лёгкие, разница в 0.01-0.3кг не критична.
**Решение:** Добавить множитель уровня: `weight *= 1f + (itemLevel - 1) * 0.1f;`
**Вердикт:** Можно отложить, не блокирует работу инвентаря.

### P6: AssetGeneratorExtended.ItemJson DTO — нет полей volume/allowNesting
**Серьёзность:** 🟢 НИЗКАЯ
**Файл:** AssetGeneratorExtended.cs, строки 1389-1410
**Проблема:** ItemJson DTO не содержит volume и allowNesting — они вычисляются
на лету в ApplyItemData() через CalculateVolume/CalculateNestingFlag.
Если JSON содержит явное значение volume — оно будет проигнорировано.
**Влияние:** Нет — текущие JSON-файлы не содержат volume, а вычисление корректно.
**Решение:** Добавить `float? volume` и `string allowNesting` в ItemJson,
использовать при наличии, иначе вычислять. Опционально.
**Вердикт:** Можно отложить.

### P7: ConsumableGenerator.SizeByType —字典 ссылается на сеточную модель
**Серьёзность:** 🟡 СРЕДНЯЯ
**Файл:** ConsumableGenerator.cs, строки 202-211
**Проблема:** `SizeByType` — словарь для сеточных размеров (1×1, 1×2).
В строчной модели не имеет смысла — Drink/Scroll имеют объём 0.1л,
а не "1×2 ячейки".
**Решение:** Заменить на `VolumeByType`:
```csharp
private static readonly Dictionary<ConsumableType, float> VolumeByType = new()
{
    { ConsumableType.Pill, 0.1f },
    { ConsumableType.Elixir, 0.1f },
    { ConsumableType.Food, 0.1f },
    { ConsumableType.Drink, 0.1f },
    { ConsumableType.Poison, 0.1f },
    { ConsumableType.Scroll, 0.1f },
    { ConsumableType.Talisman, 0.1f }
};
```
Все расходники = 0.1л (согласно CalculateVolume для Consumable).

---

## 📋 ПЛАН ПРОВЕРКИ (чек-лист)

### Этап 1: Runtime DTO — добавить инвентарные поля
- [x] **P1:** WeaponGenerator.GeneratedWeapon +volume/+allowNesting/+stackable/+maxStack ✅ (аудит 2026-04-29)
- [x] **P2:** ArmorGenerator.GeneratedArmor +volume/+allowNesting/+stackable/+maxStack ✅ (аудит 2026-04-29)
- [x] **P3:** ConsumableGenerator.GeneratedConsumable -sizeWidth/-sizeHeight +volume/+allowNesting ✅ (аудит 2026-04-29)
- [x] **P7:** ConsumableGenerator.SizeByType → VolumeByType ✅ (аудит 2026-04-29)

### Этап 2: Editor генераторы — исправить формулы
- [x] **P4:** Phase16 CreateTestEquipment — volume по формуле вместо 2.0 ✅ (аудит 2026-04-29)
- [x] **P5:** ConsumableGenerator — вес × уровень ✅ (аудит 2026-04-29)

### Этап 3: Валидация при генерации (защита от некорректных данных)
- [ ] Добавить ValidateItemFields() в каждый генератор
- [ ] Проверка: weight > 0, volume > 0, maxStack >= 1
- [ ] Проверка: если stackable=false → maxStack=1
- [ ] Проверка: allowNesting соответствует категории

### Этап 4: Обновление примеров/вывода
- [x] WeaponGenerator.GenerateExamples() — +volume, +allowNesting ✅ (аудит 2026-04-29)
- [x] ArmorGenerator.GenerateExamples() — +volume, +allowNesting ✅ (аудит 2026-04-29)
- [x] ConsumableGenerator.GenerateExamples() — volume вместо sizeWidth×sizeHeight ✅ (аудит 2026-04-29)

---

## 🔄 ЗАВИСИМОСТИ

```
P1 (WeaponGenerator DTO) ← независимая
P2 (ArmorGenerator DTO) ← независимая
P3 (ConsumableGenerator DTO) ← зависит от P7 (SizeByType→VolumeByType)
P4 (Phase16 volume) ← независимая
P5 (ConsumableGenerator weight×level) ← независимая, опционально
P6 (ItemJson DTO) ← независимая, опционально
P7 (SizeByType→VolumeByType) ← независимая
```

P1, P2, P7 можно выполнять параллельно. P3 зависит от P7.

---

## 📐 СПРАВКА: Расчёт volume для GeneratedWeapon/GeneratedArmor

Эти DTO используются runtime-генераторами для создания предметов "на лету"
(например, лут с монстров). После генерации DTO конвертируется в ItemData/EquipmentData.

**Формула для оружия:**
```
volume = Mathf.Clamp(weight, 1f, 4f)
```
- Кинжал (0.5кг) → 1.0л (min clamp)
- Меч (2.5кг) → 2.5л
- Двуручник (6.0кг) → 4.0л (max clamp)

**Формула для брони:**
```
volume = Mathf.Clamp(weight, 1f, 4f)
```
- Перчатки (0.2кг) → 1.0л (min clamp)
- Шлем (2.5кг) → 2.5л
- Латы (8.0кг) → 4.0л (max clamp)

**Формула для расходников:**
```
volume = 0.1f  // Все расходники — 0.1 литра
```

---

*Создано: 2026-04-28 06:20 UTC*
*Редактировано: 2026-04-29 06:15 UTC — аудит кода подтверждает P1-P7, P4, P5, P6 выполнены; Этап 3 (ValidateItemFields) ещё не реализован*

---

## 📚 АУДИТ ДОКУМЕНТАЦИИ (2026-04-28 07:25 UTC)

### Принцип: ДВА ограничителя — weight И volume

Строчная модель инвентаря определяет **ДВА** ограничителя вместимости:
1. **weight (масса, кг)** — сколько весит содержимое
2. **volume (объём, литры)** — сколько места занимает содержимое

Это НЕ взаимоисключающие параметры. Предмет ДОЛЖЕН иметь ОБА поля > 0.
Инвентарь ограничен И по массе, И по объёму одновременно.

**Источник истины:**
- `checkpoints/04_27_inventory_line_model_plan.md` → «Два ограничителя: масса (weight) и объём (volume)»
- `checkpoints/04_27_inventory_line_model_implementation_plan.md` → «Два ограничителя: масса (weight) и объём (volume) — определяют вместимость»
- `InventoryController.cs` → CanFitItem проверяет ОБА: rawWeight + addedWeight > MaxWeight И rawVolume + addedVolume > MaxVolume
- `ItemData.cs` → поля weight И volume существуют рядом

### Результаты проверки по документам

| # | Документ | weight упоминается | volume упоминается | ОБА рядом | Проблема |
|---|----------|:------------------:|:------------------:|:---------:|----------|
| D1 | docs/INVENTORY_SYSTEM.md | ✅ строка 120 | ✅ строка 121 | ✅ Оба в таблице Item | Нет — корректно |
| D2 | docs/DATA_MODELS.md | ✅ строка 169 | ✅ строка 170 | ✅ Оба в таблице Item | Нет — корректно |
| D3 | docs/EQUIPMENT_SYSTEM.md | ✅ строка 190 | ❌ НЕ УПОМЯНУТ | ❌ | 🔴 volume отсутствует в таблице свойств материалов |
| D4 | docs_asset_setup/17_InventoryData.md | ✅ | ✅ | ✅ | Нет — корректно |
| D5 | checkpoints/04_27_inventory_line_model_plan.md | ✅ | ✅ | ✅ | Нет — корректно |
| D6 | checkpoints/04_27_inventory_line_model_implementation_plan.md | ✅ | ✅ | ✅ | Нет — корректно |

### 🔴 D3: EQUIPMENT_SYSTEM.md — отсутствует volume у материалов

**Файл:** `docs/EQUIPMENT_SYSTEM.md`, §3.2 «Свойства материалов» (строки 187-194)

**Текущее состояние:**
| Свойство | Описание | Диапазон |
|----------|----------|----------|
| baseDurability | Базовая прочность | 20-600 |
| weight | Вес (кг) | 0.1-10.0 |
| hardness | Твёрдость (1-10) | 1-10 |
| flexibility | Гибкость (0-1) | 0-1.0 |
| qiConductivity | Проводимость Ци | 0.3-5.0 |
| qiRetention | Сохранение Ци (%/час) | 75-100 |

**Проблема:** Нет поля `volume` (объём). Материалы — это предметы, которые ложатся
в инвентарь. Каждый материал ДОЛЖЕН иметь объём для CanFitItem().

**Формула volume для материалов:** `max(0.5, weight × 0.5)` (из CalculateVolume).

**Примеры:**
| Материал | weight | volume (расчётный) |
|----------|--------|-------------------|
| Iron Ingot (T1) | 1.0 кг | 0.5 л (min) |
| Steel Bar (T2) | 2.0 кг | 1.0 л |
| Spirit Iron (T3) | 3.0 кг | 1.5 л |
| Star Metal (T4) | 5.0 кг | 2.5 л |
| Void Matter (T5) | 8.0 кг | 4.0 л |

**Решение:** Добавить в §3.2 строку:
```
| volume | Объём (литры) | 0.5-4.0 |  // Формула: max(0.5, weight × 0.5)
```

### 🟡 D7: EQUIPMENT_SYSTEM.md — отсутствует volume у экипировки

**Проблема:** В разделе свойств экипировки (§8) нет упоминания volume.
Экипировка — это предметы, которые лежат в инвентаре до экипировки.
Без volume предмет невозможно поместить в рюкзак.

**Формулы:**
- Оружие: `clamp(weight, 1, 4)` л
- Броня: `clamp(weight, 1, 4)` л
- Аксессуар: `0.2` л

**Примеры:**
| Предмет | weight | volume |
|---------|--------|--------|
| Кинжал | 0.5 кг | 1.0 л (min) |
| Меч | 2.5 кг | 2.5 л |
| Двуручник | 6.0 кг | 4.0 л (max) |
| Тканевая роба | 0.5 кг | 1.0 л (min) |
| Латный доспех | 8.0 кг | 4.0 л (max) |
| Кольцо | 0.05 кг | 0.2 л |

**Решение:** Добавить в §8 «Свойства экипировки» таблицу volume по типам.

---

## 🔍 АУДИТ КОДА — проверка ОБЕИХ ограничителей (weight + volume)

### InventoryController.cs — ✅ ПРАВИЛЬНО

Оба ограничителя реализованы корректно:
- `CanFitItem()`: проверяет `rawWeight + addedWeight > MaxWeight` И `rawVolume + addedVolume > MaxVolume`
- `MaxFittingCount()`: считает минимум из `byWeight` и `byVolume`
- `IsOverVolume`: отдельный флаг для перегруза по объёму
- `OnWeightVolumeChanged`: событие с 4 параметрами (effWeight, maxWeight, curVolume, maxVolume)

### ItemData.cs — ✅ ПРАВИЛЬНО

Оба поля присутствуют:
- `public float weight = 0.1f;` — вес (кг)
- `public float volume = 1.0f;` — объём (литры)

### BackpackData.cs — ✅ ПРАВИЛЬНО

Оба ограничителя рюкзака:
- `public float maxWeight = 30f;` — максимальная масса
- `public float maxVolume = 50f;` — максимальный объём

### AssetGeneratorExtended.CalculateVolume — ✅ ПРАВИЛЬНО

Все категории рассчитывают volume на основе weight (или константой):
- Consumable: 0.1 (константа)
- Material: max(0.5, weight×0.5) — ЗАВИСИТ ОТ ВЕСА
- Weapon: clamp(weight, 1, 4) — ЗАВИСИТ ОТ ВЕСА
- Armor: clamp(weight, 1, 4) — ЗАВИСИТ ОТ ВЕСА
- Accessory: 0.2 (константа)

**Важно:** Для Weapon/Armor/Material volume ЗАВИСИТ ОТ weight — тяжёлый предмет
занимает больше места. Это логично и корректно.

---

## 📊 ИТОГОВАЯ СВОДКА АУДИТА

### Проблемы КОДА (7 шт.)

| # | Серьёзность | Проблема |
|---|-------------|----------|
| P1 | 🔴 | WeaponGenerator DTO — нет volume/weight (для инвентаря) |
| P2 | 🔴 | ArmorGenerator DTO — нет volume/weight (для инвентаря) |
| P3 | 🟡 | ConsumableGenerator DTO — sizeWidth/sizeHeight вместо volume |
| P4 | 🟡 | Phase16 CreateTestEquipment — volume=2.0 для всех |
| P5 | 🟢 | ConsumableGenerator — вес не масштабируется с уровнем |
| P6 | 🟢 | AssetGeneratorExtended ItemJson DTO — нет volume |
| P7 | 🟡 | ConsumableGenerator.SizeByType → VolumeByType |

### Проблемы ДОКУМЕНТАЦИИ (2 шт.)

| # | Серьёзность | Документ | Проблема |
|---|-------------|----------|----------|
| D3 | 🔴 | EQUIPMENT_SYSTEM.md §3.2 | Нет volume у свойств материалов |
| D7 | 🟡 | EQUIPMENT_SYSTEM.md §8 | Нет volume у свойств экипировки |

### Что ПРАВИЛЬНО (не требует изменений)

| Компонент | Статус |
|-----------|--------|
| InventoryController.cs — оба ограничителя | ✅ |
| ItemData.cs — оба поля | ✅ |
| BackpackData.cs — оба лимита | ✅ |
| AssetGeneratorExtended.CalculateVolume | ✅ |
| AssetGeneratorExtended.CalculateNestingFlag | ✅ |
| Phase16 BackpackData creation | ✅ |
| Phase16 UpdateExistingItemData | ✅ |
| docs/INVENTORY_SYSTEM.md | ✅ |
| docs/DATA_MODELS.md | ✅ |
| docs_asset_setup/17_InventoryData.md | ✅ |
