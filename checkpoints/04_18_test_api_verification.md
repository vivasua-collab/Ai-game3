# Чекпоинт: Верификация тестов по реальному API

**Дата:** 2026-04-18
**Цель:** Проверить, что ВСЕ тесты реально тестируют код, а не "в воздухе"

## Проблема
Предыдущая версия тестов содержала API-несоответствия:
- `BodySystemTests.cs` использовал `new BodyPart { name=..., maxHealth=... }` вместо `new BodyPart(BodyPartType, float, bool)`
- `FormationQiPoolTests.cs` вызывал `FormationQiPool.GetDrainInterval()` / `GetDrainAmount()` вместо `FormationDrainConstants.GetDrainInterval()` / `FormationDrainConstants.GetDrainAmount()`
- Тесты компилировались в отдельную сборку (`.asmdef`), вызывая CS0234 ошибки

## Что было сделано

### 1. Полный аудит всех 7 тестовых файлов по исходному коду

Сравнены ВСЕ вызовы, конструкторы, свойства, методы, константы в тестах
с реальными реализациями:

| Тестовый файл | Исходные классы | Результат |
|---|---|---|
| BodySystemTests.cs | BodyPart, BodyDamage, GameConstants | ✅ Совпадает |
| FormationQiPoolTests.cs | FormationQiPool, FormationDrainConstants, QiPoolResult | ✅ Совпадает |
| CombatTests.cs | LevelSuppression, QiBuffer, TechniqueCapacity, DamageCalculator | ✅ Совпадает |
| QiControllerTests.cs | QiController | ⚠️ 1 баг (исправлен) |
| GameLoggerTests.cs | GameLogger | ✅ Совпадает |
| IntegrationTests.cs | BuffManager, TechniqueController, MockCombatant, FormationQiPool | ✅ Совпадает |
| IntegrationTestScenarios.cs | NPCGenerator, GameConstants, SaveData | ⚠️ 1 баг (исправлен) |

### 2. Исправленные баги

#### Баг 1: SpendQi(0) — ожидание vs реальность
- **Файл:** QiControllerTests.cs, строка 256
- **Проблема:** Тест `SpendQi_ZeroAmount_ReturnsTrue` ожидал, что `SpendQi(0)` вернёт `true`
- **Реальность:** `QiController.SpendQi()` возвращает `false` при `amount <= 0` (защита QI-H03 от эксплойта)
- **Исправление:** Тест переименован в `SpendQi_ZeroAmount_ReturnsFalse`, assertion изменён на `Assert.IsFalse`

#### Баг 2: RegenerationMultipliers[9] — PositiveInfinity vs MaxValue
- **Файл:** IntegrationTestScenarios.cs, строка 398
- **Проблема:** Тест ожидал `float.PositiveInfinity` для L10 регенерации
- **Реальность:** После FIX CORE-C06 константа изменена на `float.MaxValue`
- **Проверка:** `float.IsInfinity(float.MaxValue)` возвращает `false` → тест падал бы
- **Исправление:** Изменено ожидаемое значение на `float.MaxValue`, проверка `IsInfinity` заменена на `== float.MaxValue`

### 3. Верификация критических типов

- `Formation.BuffType.Damage`, `.Conductivity` — существуют ✅
- `ICombatant` интерфейс — все члены MockCombatant совпадают ✅
- `NPCGenerationParams` — поля `cultivationLevel`, `role`, `locationId`, `seed` совпадают ✅
- `GeneratedNPC` — поля `nameRu`, `maxQi`, `cultivationLevel`, `strength`, `agility`, `intelligence`, `techniqueIds` совпадают ✅
- `InventorySlotSaveData` — поля `itemId`, `count`, `gridX`, `gridY`, `durability` совпадают ✅
- `EquipmentSaveData` — существует в `CultivationGame.Inventory` ✅
- `FormationStage.Active` — существует ✅
- `BodyMaterial.Scaled` — существует ✅

## Статус
- Все тесты проверены по реальному API
- 2 критических бага исправлены
- Ожидание: компиляция без ошибок, тесты проходят корректно
