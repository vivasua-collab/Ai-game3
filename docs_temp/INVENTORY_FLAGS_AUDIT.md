# Аудит: Сложность добавления флагов для Духовного хранилища и Колец хранения

**Создано:** 2026-04-18 18:31:36 UTC
**Версия:** 1.0
**Статус:** ✅ Аудит завершён
**Связанные документы:**
- `docs_temp/INVENTORY_UI_DRAFT.md` — Драфт v2.0 инвентаря
- `docs/EQUIPMENT_SYSTEM.md` — Система экипировки
- `docs/INVENTORY_SYSTEM.md` — Устаревший драфт инвентаря

---

## 1. Цель аудита

Оценить сложность добавления новых полей (`volume`, `allowNesting`) в систему предметов для поддержки:
- **Духовного хранилища** (межмировая складка, каталогизатор, Qi×weight)
- **Колец хранения** (объём-ограниченные, Qi×volume)

---

## 2. Текущее состояние моделей данных

### 2.1 ItemData.cs (ScriptableObject)

| Поле | Тип | Описание |
|------|-----|----------|
| itemId | string | Уникальный ID |
| nameRu / nameEn | string | Названия |
| description | string | Описание |
| category | ItemCategory | Категория |
| itemType | string | Детальный тип |
| rarity | ItemRarity | Редкость |
| icon | Sprite | Иконка |
| stackable | bool | Можно стакать |
| maxStack | int | Максимум в стеке |
| sizeWidth | int (1-2) | Ширина в сетке |
| sizeHeight | int (1-3) | Высота в сетке |
| weight | float | Вес (кг) |
| value | int | Стоимость |
| hasDurability | bool | Имеет прочность |
| maxDurability | int | Макс. прочность |
| effects | List<ItemEffect> | Эффекты |
| requiredCultivationLevel | int | Требование культивации |
| statRequirements | List<StatRequirement> | Требования статов |

**❌ НЕТ**: `volume`, `allowNesting`

### 2.2 EquipmentData.cs (наследник ItemData)

Дополнительные поля: slot, layers, damage, defense, coverage, damageReduction, dodgeBonus, materialId, materialTier, grade, itemLevel, statBonuses, specialEffects

**❌ НЕТ**: BackpackData, StorageRingData как подклассы

### 2.3 MaterialData.cs (наследник ItemData)

Дополнительные поля: tier, materialCategory, hardness, durability, conductivity, damageBonus, defenseBonus, qiConductivityBonus, source, dropChance, requiredLevel

### 2.4 EquipmentSlot enum (Enums.cs)

```
None, WeaponMain, WeaponOff, Armor, Clothing, Charger, RingLeft, RingRight, Accessory, Backpack
```

**⚠️ НЕ СООТВЕТСТВУЕТ драфту v2.0**:
- Нет: head, torso, belt, legs, feet
- Только 2 кольца вместо 4
- Armor/Clothing — раздельные (матрёшка), а не один слот на зону

---

## 3. Что нужно добавить — поуровневая сложность

### 3.1 🟢 ПРОСТО (1-2 часа, нулевой риск)

| Изменение | Файл | Риск | Пояснение |
|-----------|------|------|-----------|
| Добавить `volume` (float, default=1.0) в ItemData | ItemData.cs | Нулевой | Unity SO с новыми полями + default совместимы |
| Создать enum `NestingFlag` | Enums.cs | Нулевой | Чисто добавление нового enum |
| Добавить `allowNesting` (NestingFlag, default=Any) в ItemData | ItemData.cs | Нулевой | Новое поле с default |

**NestingFlag enum:**
```csharp
public enum NestingFlag
{
    None,       // Нельзя поместить ни в какое хранилище
    Spirit,     // Можно ТОЛЬКО в духовное хранилище
    Ring,       // Можно ТОЛЬКО в кольцо хранения
    Any         // Можно в любое хранилище
}
```

**Почему безопасно**: ScriptableObject при добавлении нового поля с default-значением не ломает существующие .asset файлы. Unity автоматически подставит default при десериализации.

**Default-значения для существующих предметов:**
- `volume = 1.0` — средний предмет (расходники/материалы уточним позже)
- `allowNesting = NestingFlag.Any` — по умолчанию можно куда угодно

**Что нужно уточнить**: Таблица volume по категориям из драфта §4:
- Расходники: 0.1
- Кольца/амулеты: 0.1-0.3
- Еда/травы: 0.5
- Материалы: 0.5-1
- Лёгкое оружие: 1
- Среднее оружие: 2
- Броня: 1-2
- Тяжёлое оружие: 3-4
- Тяжёлая броня: 3-4
- Рюкзак: 2-5
- Кольцо хранения (как предмет): 0.3

### 3.2 🟡 СРЕДНЕ (3-5 часов, требует осторожности)

| Изменение | Файл | Риск | Пояснение |
|-----------|------|------|-----------|
| Создать BackpackData : ItemData | BackpackData.cs (новый) | Низкий | Новый файл + [CreateAssetMenu] |
| Создать StorageRingData : ItemData | StorageRingData.cs (новый) | Низкий | Аналогично |
| Обновить EquipmentSlot enum | Enums.cs | **СРЕДНИЙ** | Сдвиг int-значений ломает сериализацию |
| Обновить AssetGeneratorExtended | AssetGeneratorExtended.cs | Средний | Добавить генерацию новых типов |

**Критический момент — EquipmentSlot enum**:

При добавлении/переименовании значений в enum, Unity сериализует по int-индексу. Если вставить значение в середину, все последующие сдвинутся.

**Безопасные варианты:**
1. ✅ Добавлять новые значения **ТОЛЬКО В КОНЕЦ** enum
2. ✅ Создать **параллельный DollSlot enum** для UI-куклы
3. ❌ НЕ переименовывать существующие значения
4. ❌ НЕ вставлять значения в середину

**Рекомендуемый вариант — DollSlot enum**:

```csharp
// Слоты куклы для UI (отображение)
public enum DollSlot
{
    Head,           // Голова
    Torso,          // Торс
    Belt,           // Пояс
    Legs,           // Ноги
    Feet,           // Ступни
    WeaponMain,     // Основная рука
    WeaponOff       // Вторичная рука
}
```

Маппинг DollSlot → EquipmentSlot делается отдельно, НЕ ломая сериализацию.

**BackpackData (новый ScriptableObject):**
```csharp
public class BackpackData : ItemData
{
    public int gridWidth = 3;
    public int gridHeight = 4;
    public float weightReduction = 0f;     // % снижения веса
    public float maxWeightBonus = 0f;      // бонус к максимальному весу
    public int beltSlots = 0;              // дополнительные слоты пояса
}
```

**StorageRingData (новый ScriptableObject):**
```csharp
public class StorageRingData : ItemData
{
    public float maxVolume = 5f;           // максимальный объём хранения
    public int qiCostBase = 5;             // базовая стоимость Qi
    public float qiCostPerUnit = 2f;       // стоимость Qi за единицу объёма
    public float accessTime = 1.5f;        // время доступа (сек)
}
```

**Урок из прошлого**: EquipmentData и MaterialData были вынесены из ItemData.cs в отдельные файлы (commit 2026-04-13), потому что Unity требует совпадение имени файла и класса для ScriptableObject с `[CreateAssetMenu]`. Каждый новый подкласс — **свой файл**.

### 3.3 🔴 СЛОЖНО (8-15 часов, новые системы)

| Изменение | Файл | Риск | Пояснение |
|-----------|------|------|-----------|
| SpiritStorageController | Новый файл | Низкий | Чисто новый код |
| StorageRingController | Новый файл | Низкий | Чисто новый код |
| Каталогизатор SpiritStorage | UI + backend | Высокий | Новый UI-компонент |
| Валидация вложений (allowNesting) | В Spirit/StorageRing | Средний | Логика проверок |
| Стоимость Qi для хранилищ | Интеграция с QiController | Средний | Зависимость от Qi |

**SpiritStorageController API (из драфта §7.3):**
- StoreItem(itemId, count) — переместить в духовное хранилище (Qi × weight)
- RetrieveItem(entryId, count) — извлечь (Qi × weight)
- GetContents() — каталог
- FilterByCategory(), FilterByRarity(), Search()
- GetStorageCost(weight), GetRetrievalCost(weight)

**StorageRingController API (из драфта §7.4):**
- StoreItem(itemId, count) — в кольцо (Qi × volume)
- RetrieveItem(entryId, count) — из кольца (Qi × volume)
- GetContents()
- GetCurrentVolume() / GetMaxVolume()
- GetStorageCost(volume)
- CanStore(itemId) — проверка allowNesting + объём

**Ключевая зависимость — QiController:**
Оба контроллера списывают Qi. Нужно проверить API QiController:

```csharp
// Текущий API QiController (из EquipmentController.cs)
var qiCtrl = ServiceLocator.GetOrFind<QiController>();
qiCtrl.CultivationLevel  // уровень культивации
```

Нужны дополнительные методы:
- `qiCtrl.SpendQi(long amount)` — списать Qi
- `qiCtrl.CurrentQi` — текущее количество Qi

---

## 4. Обратная совместимость Save Data

| Формат | Влияние | Действие |
|--------|---------|----------|
| InventorySlotSaveData | ✅ Без изменений | volume/allowNesting берутся из ItemData, не из сейва |
| EquipmentSaveData | ✅ Без изменений | Новые слоты = пустые при загрузке старого сейва |
| CraftingSaveData | ✅ Без изменений | |
| **Новый** SpiritStorageSaveData | 🆕 Новый формат | Нужно создать |
| **Новый** StorageRingSaveData | 🆕 Новый формат | Нужно создать |

**Вывод**: Существующие save-форматы НЕ ломаются. Новые форматы — только для новых данных.

---

## 5. Проблема EquipmentSlot — детальный анализ

### 5.1 Текущий enum vs Драфт v2.0

| Слот драфта v2.0 | Текущий enum | Совпадение |
|-------------------|-------------|------------|
| head | ❌ | Нет — нужно добавить |
| torso | ❌ (есть Armor, Clothing) | Несоответствие модели |
| belt | ❌ | Нет — нужно добавить |
| legs | ❌ | Нет — нужно добавить |
| feet | ❌ | Нет — нужно добавить |
| weapon_main | ✅ WeaponMain | Да |
| weapon_off | ✅ WeaponOff | Да |
| ring_left_1 | ⚠️ RingLeft | Частично — нужен ring_left_2 |
| ring_right_1 | ⚠️ RingRight | Частично — нужен ring_right_2 |

### 5.2 Архитектурное решение

**Проблема**: Текущий EquipmentSlot основан на «матрёшке» (Armor + Clothing — раздельные слоты для одной зоны). Драфт v2.0 упрощает до 7 видимых слотов (один слот на зону, без разделения armor/clothing).

**Варианты:**

| Вариант | Описание | Плюсы | Минусы |
|---------|----------|-------|--------|
| A: Добавить в конец EquipmentSlot | Head, Torso, Belt, Legs, Feet, RingLeft2, RingRight2 | Просто | Старые слоты Armor/Clothing — мёртвый код |
| B: Новый DollSlot enum | Параллельный enum для UI-куклы | Не ломает EquipmentSlot | Нужен маппинг |
| C: Полная замена EquipmentSlot | Переписать enum по драфту | Чистая архитектура | **ЛОМАЕТ** все EquipmentData.assets |

**Рекомендация**: Вариант B. Создать `DollSlot` enum для UI-куклы, EquipmentSlot оставить для бэкенда экипировки. Маппинг DollSlot → List<EquipmentSlot> (например, DollSlot.Torso → [EquipmentSlot.Armor, EquipmentSlot.Clothing]).

**При полной переделке (текущий план)**: EquipmentSlot переписывается полностью, старые ассеты перегенерируются. Тогда вариант C становится допустимым.

---

## 6. Итоговая оценка сложности

| Этап | Сложность | Время | Зависимости |
|------|-----------|-------|-------------|
| Добавить volume + allowNesting в ItemData | 🟢 Простая | 1-2 ч | Нет |
| Создать BackpackData + StorageRingData | 🟡 Средняя | 3-5 ч | ItemData с volume |
| Решить вопрос EquipmentSlot/DollSlot | 🟡 Средняя | 2-3 ч | Архитектурное решение |
| SpiritStorageController | 🔴 Сложная | 5-8 ч | QiController |
| StorageRingController | 🔴 Сложная | 5-8 ч | QiController + volume |
| Интеграция allowNesting в UI | 🟡 Средняя | 3-4 ч | Оба контроллера |

**Итого**: 19-30 часов на полный цикл.

### Минимальный набор для «флаги работают»:
- volume + allowNesting в ItemData: **1-2 часа**
- NestingFlag enum в Enums.cs: **15 минут**
- BackpackData + StorageRingData ScriptableObject: **2-3 часа**

**Итого минимум**: 3-5 часов, чтобы данные были готовы для контроллеров.

---

## 7. Рекомендуемый порядок внедрения

1. ✅ Добавить `NestingFlag` enum в Enums.cs
2. ✅ Добавить `volume` + `allowNesting` в ItemData.cs (с defaults)
3. ✅ Создать BackpackData.cs (наследник ItemData)
4. ✅ Создать StorageRingData.cs (наследник ItemData)
5. ✅ Решить EquipmentSlot vs DollSlot (архитектурное решение)
6. 🔜 Написать SpiritStorageController
7. 🔜 Написать StorageRingController
8. 🔜 UI-интеграция: контекстное меню, валидация, стоимость Qi

---

*Документ создан: 2026-04-18 18:31:36 UTC*
*Статус: Аудит завершён*
