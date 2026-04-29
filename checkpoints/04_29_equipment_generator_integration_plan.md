# 🔧 Чекпоинт: План внедрения генераторов экипировки

**Дата:** 2026-04-29 07:11 UTC  
**Редактировано:** 2026-04-29 09:45 UTC — реализация: Этап 3 (LootGenerator + EquipmentSOFactory рефакторинг + GeneratorRegistry)
**Статус:** 🔄 В процессе (Этапы 1,2,3,4,UI ✅ | Этап 5 ❌)  
**Цель:** Подключить runtime-генераторы (WeaponGenerator, ArmorGenerator) к конвейеру создания ScriptableObject, чтобы экипировка генерировалась процедурно и попадала в инвентарь/экипировку персонажа.

**📎 Кодовая база:** [04_29_equipment_generator_integration_code.md](./04_29_equipment_generator_integration_code.md)

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

## 🔍 РЕЗУЛЬТАТЫ АУДИТА КОДА (2026-04-29 08:30 UTC)

### Аудит моделей данных

| Файл | Статус | Проблемы |
|------|--------|----------|
| `ItemData.cs` | ✅ | weight + volume + allowNesting присутствуют (строчная модель v2.0) |
| `EquipmentData.cs` | ⚠️ | Нет equippedSprite, moveSpeedPenalty, qiFlowPenalty |
| `Enums.cs` | ✅ | EquipmentSlot (15 значений), EquipmentGrade (5), WeaponHandType (2) — всё актуально |
| `EquipmentController.cs` | ✅ | v2.0, 1 слот=1 предмет, 1H/2H логика, OnEquipmentEquipped/Unequipped события |
| `EquipmentInstance` | ✅ | grade, durability, Slot, HandType, Condition |

### Аудит генераторов

| Файл | Статус | DTO-поля инвентаря |
|------|--------|---------------------|
| `WeaponGenerator.cs` | ✅ | weight, volume, stackable, maxStack, allowNesting, category (P1 выполнен) |
| `ArmorGenerator.cs` | ✅ | weight, volume, stackable, maxStack, allowNesting, category (P2 выполнен) |
| `ConsumableGenerator.cs` | ✅ | weight, volume, allowNesting, category (P3/P5/P7 выполнены) |
| `GeneratorRegistry.cs` | ✅ | Есть GenerateWeapon/GenerateArmor/GenerateConsumable методы |

### Аудит визуала

| Файл | Статус | Проблемы |
|------|--------|----------|
| `PlayerVisual.cs` | ⚠️ | Статический SpriteRenderer, нет контейнера для equipment overlay |
| `CharacterSpriteController.cs` | ⚠️ | Только scale-based flipX, нет интеграции с equipment |

### Аудит Editor-генераторов

| Файл | Статус | Примечание |
|------|--------|------------|
| `AssetGeneratorExtended.cs` | ✅ | JSON→SO мост работает, CalculateVolume/CalculateNestingFlag корректны |
| `Phase16InventoryData.cs` | ✅ | Тестовые предметы + backpack + storage ring |

### Аудит инфраструктуры

| Файл | Статус | Примечание |
|------|--------|------------|
| `MaterialSystem.cs` | ✅ | 5 тиров, кэши, MaterialProperties |
| `StorageRingData.cs` | ✅ | Наследует EquipmentData, 4 кольца |
| `ChargerData.cs` | ✅ | Зарядник Ци |
| `Combatant.cs` | ✅ | Ссылается на equipped items |

### Аудит цветовой системы

| Файл | Статус | Примечание |
|------|--------|------------|
| `InventorySlotUI.GetRarityColor()` | ✅ | 6 цветов ItemRarity (серый→красный) |
| `Phase16InventoryData.GetRarityBorderColor()` | ✅ | Дублирует Rarity-цвета для рамок |
| `EquipmentGrade` цветов | ❌ | НЕТ — только emoji в EQUIPMENT_SYSTEM.md |
| `MaterialTier` цветов | ❌ | НЕТ |

### Ключевые находки

1. **EquipmentData не хватает полей** для полного маппинга из GeneratedArmor:
   - `moveSpeedPenalty` (GeneratedArmor.moveSpeedPenalty)
   - `qiFlowPenalty` (GeneratedArmor.qiFlowPenalty)  
   - `equippedSprite` (спецификация готова)
   
2. **WeaponSubtype → EquipmentSlot маппинг** нужен в фабрике:
   - Sword/Dagger/Axe/Mace/Wand/Hammer → WeaponMain
   - Greatsword/Spear/Bow/Staff → WeaponMain (handType=TwoHand)
   - Unarmed → WeaponMain (handType=OneHand)
   - Crossbow → WeaponMain (handType=TwoHand)

3. **ArmorSubtype → EquipmentSlot маппинг**:
   - Head → Head, Torso → Torso, Arms → Hands (нет Arms-слота!)
   - Hands → Hands, Legs → Legs, Feet → Feet, Full → Torso (как основная)

4. **Проблема Arms-слота**: ArmorSubtype.Arms защищает руки, но EquipmentSlot.Hands — это кисти, а не предплечья. Решение: Arms маппить на Hands (как ближайший видимый слот).

5. **Программная иконка**: Phase16 уже делает это — цветной квадрат с буквой. Можно переиспользовать + добавить GradeColors.

6. **Grade ≠ Rarity**: EquipmentGrade (качество) и ItemRarity (редкость) — разные оси, нужна отдельная цветовая система.

---

## 📐 УТВЕРЖДЁННЫЕ РЕШЕНИЯ

### Д1: Единый EquipmentData SO (без подтипов)
- **Статус:** ✅ Утверждено кодом
- `WeaponData`/`ArmorData` НЕ существуют — всё через `EquipmentData`
- Различие: `category` (Weapon/Armor) + поля `damage`/`defense`
- Это значит: конвертер DTO→SO пишет в ОДИН тип (`EquipmentData`)

### Д2: Слоты экипировки — 7 видимых + 8 заглушек
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

### Д6: equippedSprite — ДОБАВЛЕНО в EquipmentData
- **Статус:** ✅ Поле добавлено 2026-04-29
- Поле `equippedSprite` добавлено в EquipmentData.cs
- Спрайты = 45 штук (пока не сгенерированы, equippedSprite = null по умолчанию)

### Д7: штрафы брони — ДОБАВЛЕНЫ в EquipmentData
- **Статус:** ✅ Поля добавлены 2026-04-29
- Добавлены: `moveSpeedPenalty` (Range -50..0), `qiFlowPenalty` (Range -30..30)
- `dodgePenalty` → в существующее поле `dodgeBonus` (отрицательные значения)

### Д8: Arms → Hands маппинг
- **Статус:** ✅ Решение принято
- ArmorSubtype.Arms маппится на EquipmentSlot.Hands
- Наручи = ближайший видимый слот к предплечьям

### Д9: Цветовая палитра Grade (качество предмета)
- **Статус:** ✅ Утверждено 2026-04-29
- 5 цветов: Коричневый(`#8B4513`) → Серый(`#9CA3AF`) → Зелёный(`#22C55E`) → Синий(`#3B82F6`) → Золотой(`#F59E0B`)
- Отличается от Rarity (нет фиолетового/красного)
- Применение: фон иконки, текст грейда

| Grade | Emoji | Цвет | Hex |
|-------|-------|------|-----|
| Damaged | 🔴 | Коричнево-красный | `#8B4513` |
| Common | ⚪ | Серый | `#9CA3AF` |
| Refined | 🟢 | Зелёный | `#22C55E` |
| Perfect | 🔵 | Синий | `#3B82F6` |
| Transcendent | 🟡 | Золотой | `#F59E0B` |

### Д10: Цветовая палитра Tier (уровень материала)
- **Статус:** ✅ Утверждено 2026-04-29
- 5 цветов: Стальной → Зелёный → Голубой → Фиолетовый → Розовый
- Ортогональна Grade и Rarity
- Применение: индикатор материала, яркость иконки, glow T4-T5

| Tier | Материалы | Цвет | Hex |
|------|-----------|------|-----|
| T1 | Iron, Leather, Cloth, Wood, Bone | Стальной серый | `#8B9DAF` |
| T2 | Steel, Silk, Silver | Светло-зелёный | `#4ADE80` |
| T3 | Spirit Iron, Jade, Cold Iron | Голубой | `#60A5FA` |
| T4 | Star Metal, Dragon Bone, Elemental Core | Фиолетовый | `#A78BFA` |
| T5 | Void Matter, Chaos Matter, Primordial | Розовый | `#F472B6` |

### Д11: Комбинированная формула цвета иконки
- **Статус:** ✅ Утверждено 2026-04-29
- `iconBg = GradeColor × (0.7 + 0.3 × tier/5)` — чем выше тир, тем ярче
- Рамка = Rarity-цвет (существующая система)
- Tier-индикатор = 4×4px точка в правом нижнем углу

### Д12: Статический класс GradeColors — СОЗДАН
- **Статус:** ✅ Создан 2026-04-29
- Файл `Core/GradeColors.cs` — 110 строк
- Методы: GetGradeColor, GetTierColor, GetIconBgColor, GetGradeNameRu, GetTierNameRu, GetGradeHex, GetTierHex
- **Код:** → [04_29_equipment_generator_integration_code.md §1](./04_29_equipment_generator_integration_code.md)

---

## 📋 ПЛАН ВНЕДРЕНИЯ — 5 ЭТАПОВ

### Этап 1: Мост DTO → EquipmentData SO (КРИТИЧЕСКИЙ)

**Цель:** Создать конвертер `GeneratedWeapon`/`GeneratedArmor` → `EquipmentData` ScriptableObject

**Новый файл:** `EquipmentSOFactory.cs`  
**Код:** → [04_29_equipment_generator_integration_code.md §2](./04_29_equipment_generator_integration_code.md)

**Подзадачи:**
- [x] 1.1 Создать `EquipmentSOFactory.cs` с методами CreateFromWeapon/CreateFromArmor
- [x] 1.2 Маппинг WeaponSubtype → EquipmentSlot (12→WeaponMain)
- [x] 1.3 Маппинг ArmorSubtype → EquipmentSlot (7→соответствующие слоты, Arms→Hands)
- [x] 1.4 Маппинг WeaponSubtype → handType (5 двуручных)
- [x] 1.5 Добавить поля moveSpeedPenalty/qiFlowPenalty в EquipmentData (Д7)
- [x] 1.6 Добавить equippedSprite в EquipmentData (Д6)
- [x] 1.7 Программная иконка: поиск по имени + fallback (GradeColors + Tier-индикатор)
- [x] 1.8 Runtime-методы CreateRuntimeFromWeapon/CreateRuntimeFromArmor (без AssetDatabase)
- [ ] 1.9 Тест: WeaponGenerator.Generate() → EquipmentSOFactory.CreateFromWeapon() → EquipmentData SO

### Этап 2: Editor-меню «Генерация экипировки» (ИНТЕГРАЦИЯ)

**Цель:** Добавить в Unity Editor меню для процедурной генерации экипировки

**Новый файл:** `EquipmentGeneratorMenu.cs`  
**Код:** → [04_29_equipment_generator_integration_code.md §3](./04_29_equipment_generator_integration_code.md)

**Подзадачи:**
- [x] 2.1 Создать `EquipmentGeneratorMenu.cs` с 7 пунктами меню
- [x] 2.2 Generate Weapon Set: цикл по подтипам × грейдам × тирам (12×3=36 за T1)
- [x] 2.3 Generate Armor Set: цикл по подтипам × весовым классам × грейдам (7×3×3=63 за T1)
- [x] 2.4 Generate Full Set: объединение Weapon + Armor (99 SO за T1)
- [x] 2.5 Generate Random Loot: 3 случайных предмета уровня 1 (оружие/броня 50/50)
- [x] 2.6 Clear Generated: AssetDatabase.DeleteAsset(OUTPUT_BASE)
- [x] 2.7 Структура папок: `Assets/Data/Equipment/Generated/{Weapons,Armor,Loot}/T{1-5}/`

### Этап 3: Runtime-генерация лута (ИГРОВАЯ ЛОГИКА)

**Цель:** Генерировать экипировку в рантайме (лут с монстров, награды, торговцы)

**Новый файл:** `LootGenerator.cs`  
**Код:** → [04_29_equipment_generator_integration_code.md §4](./04_29_equipment_generator_integration_code.md)

**Решение по runtime SO:** Подход B — `ScriptableObject.CreateInstance<EquipmentData>()` в рантайме.
Иконка = программная (GradeColors). Для MVP достаточно.

**Подзадачи:**
- [x] 3.1 Создать `LootGenerator.cs` с методами GenerateRandomEquipment/GenerateLoot/GenerateMixedLoot
- [x] 3.2 Рефакторинг EquipmentSOFactory — вынос runtime-методов из #if UNITY_EDITOR (CreateRuntime*, Apply*, CreateProceduralIcon теперь доступны в runtime)
- [x] 3.3 Программная иконка для runtime SO (ResolveWeaponIcon/ResolveArmorIcon с #if для AssetDatabase.LoadAssetAtPath)
- [x] 3.4 Интеграция с GeneratorRegistry — Equipment Loot region + bounded LRU кэш + GenerateRandomEquipmentSO/GenerateEquipmentLoot
- [ ] 3.5 (ОТДЕЛЬНО) LootTable ScriptableObject — таблица лута (предметы + шансы)
- [ ] 3.6 (ОТДЕЛЬНО) Интеграция с CombatManager: после боя → GenerateLoot → добавить в инвентарь

### Этап 4: Добавить equippedSprite и штрафы в EquipmentData (ВИЗУАЛ + БАЛАНС)

**Цель:** Расширить EquipmentData для полноценного маппинга из DTO

**Изменение:** `EquipmentData.cs` — добавить поля  
**Код:** → [04_29_equipment_generator_integration_code.md §5](./04_29_equipment_generator_integration_code.md)

**⚠️ РЕШЕНИЕ:** НЕ добавлять `dodgePenalty` отдельно — использовать существующее `dodgeBonus` (оно уже принимает отрицательные значения). Добавить только `moveSpeedPenalty`, `qiFlowPenalty` и `equippedSprite`.

**Подзадачи:**
- [x] 4.1 Добавить `moveSpeedPenalty` в EquipmentData.cs (float, Range(-50, 0))
- [x] 4.2 Добавить `qiFlowPenalty` в EquipmentData.cs (float, Range(-30, 30))
- [x] 4.3 Добавить `equippedSprite` в EquipmentData.cs (Sprite)
- [x] 4.4 Обновить EquipmentSOFactory — заполнение новых полей из DTO
- [ ] 4.5 (ОТДЕЛЬНО) AI-генерация 45 equipped-спрайтов по спецификации из EQUIPPED_SPRITES_DRAFT.md
- [ ] 4.6 (ОТДЕЛЬНО) Создать `EquipmentVisualController.cs` — наложение equipped-спрайтов на персонажа

### Этап 5: Обновить Phase16 — подключить генераторы (ЗАМЕНА ТЕСТОВЫХ ДАННЫХ)

**Цель:** Заменить 10 вручную созданных тестовых предметов на процедурно сгенерированные

**Изменение:** `Phase16InventoryData.cs` — метод `CreateTestEquipment()`  
**Код:** → [04_29_equipment_generator_integration_code.md §8](./04_29_equipment_generator_integration_code.md)

**Было:** 10 захардкоженных предметов  
**Стало:** Вызов `WeaponGenerator.Generate()` + `ArmorGenerator.Generate()` + `EquipmentSOFactory.CreateFromWeapon/Armor()`

**Подзадачи:**
- [ ] 5.1 Переписать `CreateTestEquipment()` — использовать генераторы (ЧАСТИЧНО: GradeColors интегрированы)
- [ ] 5.2 Сгенерировать базовый набор: 5 оружия + 5 брони (T1, Common, Level 1)
- [ ] 5.3 Сгенерировать улучшенный набор: 3 оружия + 3 брони (T3, Refined, Level 3-5)
- [x] 5.4 Сгенерировать 5 рюкзаков + 4 кольца (оставить как есть — они не через генераторы)

### UI: Интеграция GradeColors в интерфейс

**Изменения:** `InventorySlotUI.cs`, `TooltipPanel.cs`  
**Код:** → [04_29_equipment_generator_integration_code.md §6-7](./04_29_equipment_generator_integration_code.md)

- [x] UI.1 InventorySlotUI: подсветка фона по Grade (15% прозрачности)
- [x] UI.2 TooltipPanel: цвет текста грейда = GradeColors.GetGradeColor()
- [x] UI.3 TooltipPanel: цвет текста тира = GradeColors.GetTierColor()

---

## 🔄 ЗАВИСИМОСТИ

```
Подготовка ─────────────────────────────────────────────────────┐
  ├── 4.1-4.3: EquipmentData поля                              │
  └── GradeColors.cs (Д12) ─────────────────── НОВАЯ           │
         │                                                      │
Этап 1 (EquipmentSOFactory) ──── ФУНДАМЕНТ                    │
  ├── использует GradeColors.GetIconBgColor()                   │
  └── зависит от EquipmentData полей                            │
         │                                                      │
         ├── Этап 2 (Editor-меню) ─── зависит от 1             │
         ├── Этап 3 (LootGenerator) ── зависит от 1            │
         ├── Этап 5 (Phase16 замена) ── зависит от 1           │
         ├── UI: InventorySlotUI+TooltipPanel ── от GradeColors │
         └── Этап 4.5-4.6 (спрайты+визуал) ── отдельно        │
```

**Обновлённый порядок:**
1. **Подготовка** → EquipmentData поля + GradeColors.cs
2. **Этап 1** → EquipmentSOFactory (с GradeColors)
3. **Этап 5** → Phase16 (видимый результат)
4. **UI** → InventorySlotUI + TooltipPanel (подсветка Grade/Tier)
5. **Этап 2** → Editor-меню
6. **Этап 3** → Runtime LootGenerator
7. **Этап 4.5-4.6** → equipped-спрайты + EquipmentVisualController

---

## 📊 МАТРИЦА ИЗМЕНЕНИЙ

| Файл | Что делаем | Строк | Этап |
|------|-----------|-------|------|
| `EquipmentData.cs` | +moveSpeedPenalty, +qiFlowPenalty, +equippedSprite | ~12 | Подготовка |
| `GradeColors.cs` | НОВЫЙ: цветовая палитра Grade+Tier | ~90 | Подготовка |
| `EquipmentSOFactory.cs` | НОВЫЙ: конвертер DTO→SO (Editor + Runtime), GradeColors | ~220 | 1 |
| `EquipmentGeneratorMenu.cs` | НОВЫЙ: Editor-меню генерации | ~150 | 2 |
| `LootGenerator.cs` | НОВЫЙ: Runtime генерация лута | ~60 | 3 |
| `Phase16InventoryData.cs` | Переписать CreateTestEquipment, GradeColors | ~60 | 5 |
| `InventorySlotUI.cs` | Добавить GradeColors подсветку фона | ~8 | UI |
| `TooltipPanel.cs` | Добавить Grade+Tier цвет текста | ~10 | UI |
| `EquipmentVisualController.cs` | НОВЫЙ: Overlay Layering (отдельно) | ~80 | 4.6 |

**Итого:** ~690 строк нового кода, 4 файла изменений.

---

## ⚠️ ОТКРЫТЫЕ ВОПРОСЫ — РЕШЁННЫЕ

### В1: dodgePenalty/moveSpeedPenalty/qiFlowPenalty — куда класть?
- ✅ **РЕШЕНО:** dodgePenalty → в существующее поле `dodgeBonus` (отрицательное значение)
- ✅ **РЕШЕНО:** moveSpeedPenalty → новое прямое поле в EquipmentData
- ✅ **РЕШЕНО:** qiFlowPenalty → новое прямое поле в EquipmentData

### В2: Сколько SO генерировать на старте?
- ✅ **РЕШЕНО:** Подход C — Runtime SO через LootGenerator. Editor-меню для предгенерации.

### В3: Иконки для генерируемых предметов?
- ✅ **РЕШЕНО:** Поиск по имени (weapon_{subtype}.png) → fallback на программную генерацию с GradeColors

### В4: Arms-слот в ArmorSubtype → EquipmentSlot
- ✅ **РЕШЕНО:** Arms маппится на Hands (ближайший видимый слот)

### В5: Цвет грейда = цвет редкости?
- ✅ **РЕШЕНО:** НЕТ, разные палитры (Д9 vs ItemRarity). Grade = коричневый/серый/зелёный/синий/золотой. Rarity = серый/зелёный/синий/фиолет/золотой/красный.

### В6: Нужен ли отдельный индикатор Tier на иконке?
- ✅ **РЕШЕНО:** Да — 4×4px точка в правом нижнем углу иконки, цвет = Tier. Реализуется в CreateProceduralIcon.

---

## ✅ КРИТЕРИИ ПРИЁМКИ

1. **EquipmentData** имеет новые поля: moveSpeedPenalty, qiFlowPenalty, equippedSprite
2. **EquipmentSOFactory** корректно конвертирует GeneratedWeapon → EquipmentData SO
3. **EquipmentSOFactory** корректно конвертирует GeneratedArmor → EquipmentData SO
4. Сгенерированные SO имеют ВСЕ инвентарные поля (weight, volume, stackable, maxStack, allowNesting)
5. Маппинг WeaponSubtype→EquipmentSlot корректный (5 двуручных = TwoHand)
6. Маппинг ArmorSubtype→EquipmentSlot корректный (Arms→Hands)
7. Editor-меню «Generate Weapon Set» создаёт SO в Assets/Data/Equipment/Generated/
8. Runtime LootGenerator создаёт EquipmentData через ScriptableObject.CreateInstance
9. Phase16 использует генераторы вместо ручного CreateTestEquipment
10. equippedSprite = null по умолчанию (спрайты — отдельная задача)
11. Компиляция без ошибок
12. **GradeColors.cs** предоставляет GetGradeColor/GetTierColor/GetIconBgColor
13. Программная иконка использует GradeColors.GetIconBgColor(grade, tier) для фона
14. Тир-индикатор (4×4px точка) отображается на программной иконке
15. TooltipPanel показывает грейд цветом GradeColors.GetGradeColor()
16. TooltipPanel показывает тир цветом GradeColors.GetTierColor()
17. InventorySlotUI подсвечивает фон слота цветом грейда (15% прозрачности)

---

*Создано: 2026-04-29 07:11 UTC*  
*Редактировано: 2026-04-29 09:05 UTC — реализация: Подготовка + Этап 1 + Этап 4 + UI. Коммит 5cb04fc*
