# 🔧 Чекпоинт: План внедрения генераторов экипировки

**Дата:** 2026-04-29 07:11 UTC
**Статус:** 📋 План готов к выполнению
**Цель:** Подключить runtime-генераторы (WeaponGenerator, ArmorGenerator) к конвейеру создания ScriptableObject, чтобы экипировка генерировалась процедурно и попадала в инвентарь/экипировку персонажа.

---

## 🔴 КРИТИЧЕСКИЙ РАЗРЫВ

### Текущее состояние

```
WeaponGenerator.Generate()     → GeneratedWeapon (DTO)    ──── ❌ НИКУДА
ArmorGenerator.Generate()      → GeneratedArmor  (DTO)    ──── ❌ НИКУДА
ConsumableGenerator.Generate() → GeneratedConsumable (DTO) ──── ❌ НИКУДА

AssetGeneratorExtended         ← equipment.json → EquipmentData (SO) ✅ РАБОТАЕТ
Phase16InventoryData           → CreateTestEquipment() → EquipmentData (SO) ✅ РАБОТАЕТ
```

**Генераторы создают DTO, но НИКТО не конвертирует DTO → EquipmentData SO.**

### Почему это критично

1. **WeaponGenerator/ArmorGenerator** — полностью функциональны (аудит P1-P7 подтверждён), но их выход никто не использует
2. **AssetGeneratorExtended** читает статичный `equipment.json` — генерация НЕ процедурная
3. **Phase16** создаёт 10 тестовых предметов вручную — этого мало для игры
4. Без моста DTO→SO невозможно:
   - Генерировать лут с монстров (процедурно)
   - Создавать предметы по параметрам (уровень, грейд, тир)
   - Масштабировать контент без ручного ввода JSON

---

## 📐 УТВЕРЖДЁННЫЕ РЕШЕНИЯ

### Д1: Единый EquipmentData SO (без подтипов)
- **Статус:** ✅ Утверждено кодом
- `WeaponData`/`ArmorData` НЕ существуют — всё через `EquipmentData`
- Различие: `category` (Weapon/Armor) + поля `damage`/`defense`
- Это значит: конвертер DTO→SO пишет в ОДИН тип (`EquipmentData`)

### Д2: Слоты экипировки — 7 видимых + 7 заглушек
- **Статус:** ✅ Утверждено кодом
- Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff — видимые
- Amulet, Ring×4, Charger, Hands, Back — скрытые-заглушки
- Двуручное: WeaponMain + блок WeaponOff

### Д3: Упрощённая модель — 1 слот = 1 предмет
- **Статус:** ✅ Решение 2026-04-29
- Слои (Матрёшка v1) упразднены
- Equipped-спрайт = 1 на слот (когда будет добавлен)

### Д4: Строчная модель инвентаря (weight + volume)
- **Статус:** ✅ P1-P7 выполнены и проверены аудитом
- `CanFitItem` проверяет ОБА ограничителя
- Формулы volume: оружие/броня = `clamp(weight, 1, 4)`, расходник = `0.1`

### Д5: Генерация «Матрёшка» — База × Материал × Грейд
- **Статус:** ✅ Реализовано в генераторах
- WeaponGenerator: 12 подтипов × 5 тиров × 5 грейдов = 300 комбинаций
- ArmorGenerator: 7 подтипов × 3 весовых класса × 5 тиров × 5 грейдов = 525 комбинаций

### Д6: equippedSprite — НЕТ в коде, К ДОБАВЛЕНИЮ
- **Статус:** ⏳ Спецификация готова (EQUIPPED_SPRITES_DRAFT.md)
- Поле `equippedSprite` нужно добавить в `EquipmentData`
- Equipped-спрайты = 45 штук (пока не сгенерированы)

---

## 📋 ПЛАН ВНЕДРЕНИЯ — 5 ЭТАПОВ

### Этап 1: Мост DTO → EquipmentData SO (КРИТИЧЕСКИЙ)

**Цель:** Создать конвертер `GeneratedWeapon`/`GeneratedArmor` → `EquipmentData` ScriptableObject

**Новый файл:** `UnityProject/Assets/Scripts/Editor/EquipmentSOFactory.cs`

```csharp
// Создано: 2026-04-29 07:11 UTC
// Фабрика создания EquipmentData SO из DTO генераторов

public static class EquipmentSOFactory
{
    /// Создаёт EquipmentData SO из GeneratedWeapon DTO
    public static EquipmentData CreateFromWeapon(GeneratedWeapon dto, string assetPath);
    
    /// Создаёт EquipmentData SO из GeneratedArmor DTO
    public static EquipmentData CreateFromArmor(GeneratedArmor dto, string assetPath);
    
    /// Обновляет существующий EquipmentData SO из DTO
    public static void ApplyWeaponToSO(EquipmentData so, GeneratedWeapon dto);
    public static void ApplyArmorToSO(EquipmentData so, GeneratedArmor dto);
}
```

**Маппинг полей:**

| GeneratedWeapon | → | EquipmentData |
|-----------------|---|---------------|
| id | → | itemId |
| nameRu | → | nameRu |
| category (=Weapon) | → | category |
| subtype → ParseEquipmentSlot | → | slot |
| — | → | handType (из подтипа: greatsword/bow/spear/staff = TwoHand) |
| baseDamage × grade × material | → | damage |
| 0 | → | defense |
| volume | → | volume |
| weight | → | weight |
| stackable | → | stackable |
| maxStack | → | maxStack |
| allowNesting | → | allowNesting |
| maxDurability | → | maxDurability (в ItemData) |
| grade | → | grade |
| itemLevel | → | itemLevel |
| bonuses | → | statBonuses |
| specialEffects | → | specialEffects |
| materialId | → | materialId |
| materialTier | → | materialTier |

| GeneratedArmor | → | EquipmentData |
|----------------|---|---------------|
| id | → | itemId |
| nameRu | → | nameRu |
| category (=Armor) | → | category |
| subtype → ParseEquipmentSlot | → | slot |
| OneHand | → | handType |
| 0 | → | damage |
| armor × grade × material | → | defense |
| coverage | → | coverage |
| damageReduction | → | damageReduction |
| dodgePenalty | → | dodgeBonus (отрицательный) |
| volume | → | volume |
| weight | → | weight |
| ... | → | (аналогично Weapon) |
| qiFlowPenalty | → | (новое поле или в statBonuses) |

**Критический вопрос:** `dodgePenalty` и `moveSpeedPenalty` из `GeneratedArmor` — куда класть?
- Вариант A: Добавить поля `moveSpeedPenalty` и `dodgePenalty` в `EquipmentData`
- Вариант B: Положить в `statBonuses` как структурированные бонусы
- **Рекомендация:** Вариант A — прямые поля, проще для CombatManager

**Подзадачи:**
- [ ] 1.1 Создать `EquipmentSOFactory.cs` с методами CreateFromWeapon/CreateFromArmor
- [ ] 1.2 Маппинг подтипов → слоты (WeaponSubtype→EquipmentSlot, ArmorSubtype→EquipmentSlot)
- [ ] 1.3 Маппинг подтипов → handType (Greatsword/Bow/Spear/Staff = TwoHand)
- [ ] 1.4 При необходимости добавить поля dodgePenalty/moveSpeedPenalty/qiFlowPenalty в EquipmentData
- [ ] 1.5 Создание иконки: программная генерация (цветной квадрат с буквой) из WeaponGenerator/ArmorGenerator данных
- [ ] 1.6 Тест: Generate() → CreateFromWeapon() → EquipmentData SO → AssetDatabase.CreateAsset()

### Этап 2: Editor-меню «Генерация экипировки» (ИНТЕГРАЦИЯ)

**Цель:** Добавить в Unity Editor меню для процедурной генерации экипировки

**Новый файл:** `UnityProject/Assets/Scripts/Editor/EquipmentGeneratorMenu.cs`

```csharp
// Создано: 2026-04-29 07:11 UTC
// Editor-меню генерации экипировки

public class EquipmentGeneratorMenu
{
    [MenuItem("Tools/Equipment/Generate Weapon Set")]
    public static void GenerateWeaponSet();
    
    [MenuItem("Tools/Equipment/Generate Armor Set")]
    public static void GenerateArmorSet();
    
    [MenuItem("Tools/Equipment/Generate Full Equipment Set")]
    public static void GenerateFullSet();
    
    [MenuItem("Tools/Equipment/Generate Random Loot")]
    public static void GenerateRandomLoot();
}
```

**Генерация Weapon Set (12 подтипов × 5 грейдов × 1 тир = 60 SO):**
```
Assets/Data/Equipment/Generated/Weapons/
├── T1_Iron/
│   ├── weapon_sword_iron_common.asset
│   ├── weapon_sword_iron_refined.asset
│   ├── weapon_dagger_iron_common.asset
│   └── ...
├── T3_SpiritIron/
│   ├── weapon_sword_spirit_common.asset
│   └── ...
└── T5_VoidMatter/
    └── ...
```

**Генерация Armor Set (7 подтипов × 3 весовых класса × 5 грейдов × 1 тир = 105 SO):**
```
Assets/Data/Equipment/Generated/Armor/
├── T1_Iron/
│   ├── armor_helmet_iron_heavy_common.asset
│   ├── armor_torso_cloth_light_common.asset
│   └── ...
└── ...
```

**Подзадачи:**
- [ ] 2.1 Создать `EquipmentGeneratorMenu.cs` с 4 пунктами меню
- [ ] 2.2 Generate Weapon Set: цикл по подтипам × грейдам, вызов WeaponGenerator + EquipmentSOFactory
- [ ] 2.3 Generate Armor Set: цикл по подтипам × весовым классам × грейдам
- [ ] 2.4 Generate Full Set: объединение Weapon + Armor
- [ ] 2.5 Generate Random Loot: 1-3 случайных предмета по уровню/тиру
- [ ] 2.6 Структура папок: `Assets/Data/Equipment/Generated/{Weapons,Armor}/T{1-5}_{Material}/`

### Этап 3: Runtime-генерация лута (ИГРОВАЯ ЛОГИКА)

**Цель:** Генерировать экипировку в рантайме (лут с монстров, награды, торговцы)

**Новый файл:** `UnityProject/Assets/Scripts/Generators/LootGenerator.cs`

```csharp
// Создано: 2026-04-29 07:11 UTC
// Генератор лута для рантайма

public class LootGenerator : MonoBehaviour
{
    /// Генерирует массив предметов по параметрам encounters
    public static List<InventorySlot> GenerateLoot(LootTable lootTable, int playerLevel);
    
    /// Генерирует один случайный предмет экипировки
    public static EquipmentData GenerateRandomEquipment(int itemLevel, int materialTier, EquipmentGrade grade);
    
    /// Генерирует расходники
    public static List<InventorySlot> GenerateConsumableLoot(int playerLevel, int count);
}
```

**Проблема:** В рантайме нельзя создать ScriptableObject через `ScriptableObject.CreateInstance()` и назначить спрайт без Editor API.

**Решение:** ДВА подхода:

| Подход | Описание | Плюсы | Минусы |
|--------|----------|-------|--------|
| **A: Пул предгенерированных SO** | При старте сцены Phase генерирует все возможные SO и кладёт в реестр | Быстрый доступ в рантайме | Много SO в памяти (825 комбинаций) |
| **B: Runtime SO** | `ScriptableObject.CreateInstance<EquipmentData>()` в рантайме | Минимум памяти | Нет иконки (нужна программная генерация) |

**Рекомендация:** Подход B — Runtime SO. Иконка генерируется программно (как в Phase16.CreateTestEquipment — цветной квадрат с буквой). Для MVP этого достаточно. Спрайты-иконки заменяются позже.

**Подзадачи:**
- [ ] 3.1 Создать `LootGenerator.cs` с методами GenerateLoot/GenerateRandomEquipment
- [ ] 3.2 Метод `CreateRuntimeEquipmentSO()` — создание SO в рантайме из DTO
- [ ] 3.3 Программная генерация иконки (цветной квадрат + буква) для runtime SO
- [ ] 3.4 LootTable — ScriptableObject с таблицей лута (предметы + шансы)
- [ ] 3.5 Интеграция с CombatManager: после боя → GenerateLoot → добавить в инвентарь

### Этап 4: Добавить equippedSprite в EquipmentData (ВИЗУАЛ)

**Цель:** Подготовить поле для equipped-спрайтов (см. EQUIPPED_SPRITES_DRAFT.md)

**Изменение:** `EquipmentData.cs` — добавить 1 поле:

```csharp
[Header("Visuals")]
[Tooltip("Спрайт надетой экипировки (overlay на персонаже)")]
public Sprite equippedSprite;
```

**Изменение:** `EquipmentSOFactory.cs` — при создании SO из DTO:
- `equippedSprite = null` (пока нет спрайтов)
- В будущем: автоназначение по пути `Sprites/Equipment/Equipped/{Slot}/eq_{name}.png`

**Подзадачи:**
- [ ] 4.1 Добавить `equippedSprite` в `EquipmentData.cs`
- [ ] 4.2 Обновить `EquipmentSOFactory` — поле = null по умолчанию
- [ ] 4.3 (ОТДЕЛЬНО) AI-генерация 45 equipped-спрайтов по спецификации из EQUIPPED_SPRITES_DRAFT.md
- [ ] 4.4 (ОТДЕЛЬНО) Создать `EquipmentVisualController.cs` — наложение equipped-спрайтов на персонажа

### Этап 5: Обновить Phase16 — подключить генераторы (ЗАМЕНА ТЕСТОВЫХ ДАННЫХ)

**Цель:** Заменить 10 вручную созданных тестовых предметов на процедурно сгенерированные

**Изменение:** `Phase16InventoryData.cs` — метод `CreateTestEquipment()`:

**Было:** 10 захардкоженных предметов
**Стало:** Вызов `WeaponGenerator.Generate()` + `ArmorGenerator.Generate()` + `EquipmentSOFactory.CreateFromWeapon/Armor()`

```csharp
// Вместо ручного создания — генерация
var weaponParams = new WeaponGenerationParams {
    subtype = WeaponSubtype.Sword,
    itemLevel = 1,
    grade = EquipmentGrade.Common,
    materialTier = 1,
    materialCategory = MaterialCategory.Metal
};
GeneratedWeapon sword = WeaponGenerator.Generate(weaponParams);
EquipmentData swordSO = EquipmentSOFactory.CreateFromWeapon(sword, "Assets/Data/Equipment/Generated/...");
```

**Подзадачи:**
- [ ] 5.1 Переписать `CreateTestEquipment()` — использовать генераторы
- [ ] 5.2 Сгенерировать базовый набор: 5 оружия + 5 брони (T1, Common, Level 1)
- [ ] 5.3 Сгенерировать улучшенный набор: 3 оружия + 3 брони (T3, Refined, Level 3-5)
- [ ] 5.4 Сгенерировать 5 рюкзаков + 4 кольца (оставить как есть — они не через генераторы)

---

## 🔄 ЗАВИСИМОСТИ

```
Этап 1 (EquipmentSOFactory) ──── ФУНДАМЕНТ, всё зависит от него
         │
         ├── Этап 2 (Editor-меню) ─── зависит от 1
         ├── Этап 3 (LootGenerator) ── зависит от 1
         ├── Этап 4 (equippedSprite) ─ независим (можно параллельно с 1)
         └── Этап 5 (Phase16 замена) ── зависит от 1
```

**Рекомендуемый порядок:**
1. Этап 1 → Фабрика DTO→SO (критический, блокирует всё)
2. Этап 4 → equippedSprite в EquipmentData (независим, 1 строка)
3. Этап 5 → Переписать Phase16 (зависит от 1)
4. Этап 2 → Editor-меню (зависит от 1)
5. Этап 3 → Runtime LootGenerator (зависит от 1, самый сложный)

---

## 📊 МАТРИЦА ИЗМЕНЕНИЙ

| Файл | Что делаем | Строк | Этап |
|------|-----------|-------|------|
| `EquipmentSOFactory.cs` | НОВЫЙ: конвертер DTO→SO | ~150 | 1 |
| `EquipmentData.cs` | +equippedSprite, +(dodgePenalty?) | ~5 | 4 |
| `EquipmentGeneratorMenu.cs` | НОВЫЙ: Editor-меню генерации | ~120 | 2 |
| `LootGenerator.cs` | НОВЫЙ: Runtime генерация лута | ~100 | 3 |
| `Phase16InventoryData.cs` | Переписать CreateTestEquipment | ~30 | 5 |
| `EquipmentVisualController.cs` | НОВЫЙ: Overlay Layering (отдельно) | ~80 | 4.4 |

**Итого:** ~485 строк нового кода, 2 файла изменений.

---

## ⚠️ ОТКРЫТЫЕ ВОПРОСЫ (требуют решения)

### В1: dodgePenalty/moveSpeedPenalty/qiFlowPenalty — куда класть?
- Вариант A: Прямые поля в EquipmentData (3 новых поля)
- Вариант B: В statBonuses (структурированные бонусы)
- **Рекомендация:** A — прямые поля. CombatManager/DefenseProcessor уже используют coverage и damageReduction как прямые поля.

### В2: Сколько SO генерировать на старте?
- Вариант A: Полный набор (825 SO: 300 оружия + 525 брони) — много файлов
- Вариант B: По тиру (только T1 → 60+105 = 165 SO) — разумно для начала
- Вариант C: Только по запросу (Runtime SO) — минимум файлов
- **Рекомендация:** C — Runtime SO через LootGenerator. Editor-меню (Этап 2) для предгенерации при необходимости.

### В3: Иконки для генерируемых предметов?
- Вариант A: Программная генерация (цветной квадрат + буква, как Phase16)
- Вариант B: AI-сгенерированные иконки (24 уже существуют в Sprites/Equipment/Icons/)
- Вариант C: Иконка по имени (auto-find по `weapon_{subtype}_{material}.png`)
- **Рекомендация:** C — поиск по имени. 24 AI-иконки уже есть. Для отсутствующих — fallback на программную генерацию (A).

---

## ✅ КРИТЕРИИ ПРИЁМКИ

1. **EquipmentSOFactory** корректно конвертирует GeneratedWeapon → EquipmentData SO
2. **EquipmentSOFactory** корректно конвертирует GeneratedArmor → EquipmentData SO
3. Сгенерированные SO имеют ВСЕ инвентарные поля (weight, volume, stackable, maxStack, allowNesting)
4. Editor-меню «Generate Weapon Set» создаёт SO в Assets/Data/Equipment/Generated/
5. Runtime LootGenerator создаёт EquipmentData через ScriptableObject.CreateInstance
6. Phase16 использует генераторы вместо ручного CreateTestEquipment
7. equippedSprite добавлен в EquipmentData (null по умолчанию)
8. Компиляция без ошибок

---

*Создано: 2026-04-29 07:11 UTC*
