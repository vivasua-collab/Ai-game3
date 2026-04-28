# 🔍 Чекпоинт: План аудита генераторов на соответствие строчной модели инвентаря

**Дата:** 2026-04-28 06:20 UTC
**Статус:** 📋 план (аудит не начат)
**Цель:** Проверить ВСЕ генераторы предметов на соответствие требованиям строчной модели инвентаря: корректные volume, weight, stackable/maxStack, allowNesting.

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
- [ ] **P1:** WeaponGenerator.GeneratedWeapon +volume/+allowNesting/+stackable/+maxStack
- [ ] **P2:** ArmorGenerator.GeneratedArmor +volume/+allowNesting/+stackable/+maxStack
- [ ] **P3:** ConsumableGenerator.GeneratedConsumable -sizeWidth/-sizeHeight +volume/+allowNesting
- [ ] **P7:** ConsumableGenerator.SizeByType → VolumeByType

### Этап 2: Editor генераторы — исправить формулы
- [ ] **P4:** Phase16 CreateTestEquipment — volume по формуле вместо 2.0
- [ ] **P5:** ConsumableGenerator — вес × уровень (опционально)

### Этап 3: Валидация при генерации (защита от некорректных данных)
- [ ] Добавить ValidateItemFields() в каждый генератор
- [ ] Проверка: weight > 0, volume > 0, maxStack >= 1
- [ ] Проверка: если stackable=false → maxStack=1
- [ ] Проверка: allowNesting соответствует категории

### Этап 4: Обновление примеров/вывода
- [ ] WeaponGenerator.GenerateExamples() — +volume, +allowNesting
- [ ] ArmorGenerator.GenerateExamples() — +volume, +allowNesting
- [ ] ConsumableGenerator.GenerateExamples() — volume вместо sizeWidth×sizeHeight

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
