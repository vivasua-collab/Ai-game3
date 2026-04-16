# 🚀 Запуск Unit тестов в Unity

**Создано:** 2026-04-09 06:35:00 UTC
**Обновлено:** 2026-04-17 19:30:00 UTC
**Проект:** Cultivation World Simulator

---

## 📋 Способы запуска тестов

### 1. Unity Test Runner (Рекомендуется)

#### Открытие Test Runner

```
Window → General → Test Runner
```

#### Запуск тестов

1. Открой **Test Runner** окно
2. Выбери вкладку **PlayMode** или **EditMode**
3. Нажми **Run All** для запуска всех тестов
4. Или выбери конкретный тест и нажми **Run Selected**

---

### 2. Через меню Tools

```
Tools → Run Tests → All Tests
Tools → Run Tests → Combat Tests
Tools → Run Tests → Integration Tests
```

---

### 3. Через контекстное меню компонента

1. Создай пустой GameObject в сцене
2. Add Component → **Balance Verification Component**
3. Правый клик на компоненте → **Run Quick Verification**

---

## 🧪 Типы тестов

### EditMode Tests

- Запускаются без входа в Play Mode
- Быстрые, не требуют загрузки сцены
- Подходят для Unit тестов логики

```
Assets/Scripts/Tests/
├── CombatTests.cs            — EditMode tests
├── BalanceVerification.cs    — EditMode verification
├── QiControllerTests.cs      — EditMode tests (NEW)
├── FormationQiPoolTests.cs   — EditMode tests (NEW)
├── BodySystemTests.cs        — EditMode tests (NEW)
├── GameLoggerTests.cs        — EditMode tests (NEW)
```

### PlayMode Tests

- Запускаются в Play Mode
- Могут тестировать корутины, физику, анимации
- Требуют больше времени

```
Assets/Scripts/Tests/
├── IntegrationTests.cs         — PlayMode tests
└── IntegrationTestScenarios.cs — PlayMode scenarios
```

---

## 📑 Полный перечень тестов

### 1. CombatTests (16 тестов)

| # | Метод | Описание |
|---|-------|----------|
| 1 | `LevelSuppression_SameLevel_ReturnsFullDamage` | Равные уровни — полный урон |
| 2 | `LevelSuppression_OneLevelDifference_AppliesCorrectPenalty` | Разница +1 уровень — корректный штраф |
| 3 | `LevelSuppression_TwoLevelDifference_AppliesCorrectPenalty` | Разница +2 уровня — корректный штраф |
| 4 | `LevelSuppression_ThreeLevelDifference_AppliesCorrectPenalty` | Разница +3 уровня — корректный штраф |
| 5 | `LevelSuppression_FivePlusLevelDifference_NoDamage` | Разница +5+ уровней — урон = 0 |
| 6 | `LevelSuppression_AttackerHigherLevel_FullDamage` | Атакующий старше — полный урон |
| 7 | `LevelSuppression_TechniqueLevelAffectsEffectiveLevel` | Уровень техники влияет на подавление |
| 8 | `QiBuffer_QiTechnique_RawQiAbsorbsCorrectly` | Qi-техника + RawQi — корректное поглощение |
| 9 | `QiBuffer_QiTechnique_ShieldAbsorbsFully` | Qi-техника + Shield — полное поглощение |
| 10 | `QiBuffer_PhysicalDamage_RawQiAbsorbsLess` | Физический урон + RawQi — меньше поглощение |
| 11 | `QiBuffer_PhysicalDamage_ShieldUsesHigherRatio` | Физический урон + Shield — повышенный ratio |
| 12 | `QiBuffer_InsufficientQi_PartialAbsorption` | Недостаточно Ци — частичное поглощение |
| 13 | `QiBuffer_NoDefense_NoAbsorption` | Нет защиты — нет поглощения |
| 14 | `QiBuffer_MinimumQiRequired_ActivatesDefense` | Минимальное Ци для активации защиты |
| 15 | `TechniqueCapacity_CalculatesCorrectDamage` | Корректный расчёт урона по грейдам |
| 16 | `TechniqueCapacity_Ultimate_HasMultiplier` | Ультимативная техника имеет множитель ×1.3 |

**Edge Cases (CombatTests):**

| # | Метод | Описание |
|---|-------|----------|
| 17 | `DamageCalculator_FullPipeline_ProducesValidResult` | Полный pipeline расчёта урона |
| 18 | `DamageCalculator_LevelSuppression_ReducesDamage` | Подавление уровнем уменьшает урон |
| 19 | `QiBuffer_ZeroDamage_NoConsumption` | Нулевой урон — нет расхода Ци |
| 20 | `LevelSuppression_NegativeLevelDiff_FullDamage` | Отрицательная разница — полный урон |

---

### 2. QiControllerTests (20 тестов) — NEW

| # | Метод | Описание |
|---|-------|----------|
| 1 | `SpendQi_SufficientQi_ReturnsTrue` | Достаточно Ци — расход успешен |
| 2 | `SpendQi_InsufficientQi_ReturnsFalse` | Недостаточно Ци — расход отклонён |
| 3 | `AddQi_IncreasesCurrentQi` | AddQi увеличивает текущее Ци |
| 4 | `AddQi_ExceedsCapacity_CapsAtMax` | Превышение ёмкости — ограничение по MaxQi |
| 5 | `RestoreFull_SetsToMaxQi` | RestoreFull устанавливает Ци в максимум |
| 6 | `SetQi_SetsExactAmount` | SetQi устанавливает точное значение |
| 7 | `IsEmpty_WhenZeroQi_ReturnsTrue` | IsEmpty при нулевом Ци |
| 8 | `IsFull_AfterRestoreFull_ReturnsTrue` | IsFull после RestoreFull |
| 9 | `SetCultivationLevel_UpdatesLevelAndRecalculates` | Смена уровня — пересчёт параметров |
| 10 | `SetCultivationLevel_HigherLevel_HasMoreCapacity` | Высший уровень — больше ёмкость ядра |
| 11 | `CanBreakthrough_WhenQiFull_ReturnsTrue` | Прорыв возможен при полном Ци |
| 12 | `CanBreakthrough_WhenQiNotFull_ReturnsFalse` | Прорыв невозможен без полного Ци |
| 13 | `QiPercent_AtFull_ReturnsOne` | QiPercent = 1.0 при полном Ци |
| 14 | `QiPercent_AtZero_ReturnsZero` | QiPercent = 0.0 при нулевом Ци |
| 15 | `QiPercent_AtHalf_ReturnsHalf` | QiPercent ≈ 0.5 при половине Ци |
| 16 | `SpendQi_ZeroAmount_ReturnsTrue` | SpendQi(0) — без изменений |
| 17 | `AddQi_ZeroAmount_NoChange` | AddQi(0) — без изменений |
| 18 | `SetQi_NegativeValue_ClampsToZero` | Отрицательное значение — clamp к 0 |
| 19 | `EstimateCapacityAtLevel_HigherLevel_GreaterCapacity` | Ёмкость растёт с уровнем |
| 20 | `SetConductivityBonus_UpdatesConductivity` | ConductivityBonus обновляется |

---

### 3. FormationQiPoolTests (21 тест) — NEW

| # | Метод | Описание |
|---|-------|----------|
| 1 | `Constructor_SetsCorrectCapacity` | Конструктор устанавливает ёмкость |
| 2 | `Constructor_StartsEmpty` | Пул начинается пустым |
| 3 | `AddQi_PositiveAmount_IncreasesCurrentQi` | AddQi увеличивает Ци |
| 4 | `AddQi_ExceedsCapacity_FillsToCapacity` | Превышение — заполнение до ёмкости |
| 5 | `AddQi_FillsToCapacity_SetsWasFilled` | Заполнение до конца — wasFilled = true |
| 6 | `ConsumeQi_SufficientQi_ReturnsSuccess` | Достаточно Ци — расход успешен |
| 7 | `ConsumeQi_InsufficientQi_ReturnsFailure` | Недостаточно Ци — истощение пула |
| 8 | `FillPercent_Empty_ReturnsZero` | Пустой пул = 0% |
| 9 | `FillPercent_Half_ReturnsHalf` | Половина = 50% |
| 10 | `FillPercent_Full_ReturnsOne` | Полный = 100% |
| 11 | `GetDrainInterval_Level1_Returns60` | Интервал стока L1 = 60 тиков |
| 12 | `GetDrainInterval_Level5_Returns20` | Интервал стока L5 = 20 тиков |
| 13 | `GetDrainAmount_Size0_Returns1` | Размер 0 = 1 единица стока |
| 14 | `GetDrainAmount_Size4_Returns100` | Размер 4 = 100 единиц стока |
| 15 | `ProcessDrain_BeforeInterval_NoDrain` | До истечения интервала — нет стока |
| 16 | `ProcessDrain_AfterInterval_DrainsCorrectly` | После интервала — корректный сток |
| 17 | `Reset_ClearsQi` | Reset очищает пул |
| 18 | `Fill_FillsToCapacity` | Fill заполняет до ёмкости |
| 19 | `AddQi_ZeroAmount_NoChange` | AddQi(0) — без изменений |
| 20 | `ConsumeQi_ZeroAmount_NoChange` | ConsumeQi(0) — без изменений |
| 21 | `ProcessDrain_EmptyPool_NoNegativeQi` | Сток из пустого пула — нет отрицательного Ци |

---

### 4. BodySystemTests (5 тестов) — NEW

| # | Метод | Описание |
|---|-------|----------|
| 1 | `IsAlive_AllPartsHealthy_ReturnsTrue` | Все части здоровы — IsAlive = true |
| 2 | `IsAlive_AllPartsDestroyed_ReturnsFalse` | Все части уничтожены — IsAlive = false |
| 3 | `GetOverallHealthPercent_FullHealth_ReturnsOne` | Полное здоровье = 1.0 |
| 4 | `GetOverallHealthPercent_NoHealth_ReturnsZero` | Нет здоровья = 0.0 |
| 5 | `CalculateDamagePenalty_NoDamage_ReturnsZero` | Без повреждений штраф = 0 |

---

### 5. GameLoggerTests (15 тестов) — NEW

| # | Метод | Описание |
|---|-------|----------|
| **Категории — вкл/выкл** | | |
| 1 | `SetCategoryEnabled_DisableCategory_CategoryIsDisabled` | Выключение категории |
| 2 | `SetCategoryEnabled_EnableCategory_CategoryIsEnabled` | Включение категории |
| 3 | `DisableAllCategories_OnlySystemEnabled` | DisableAll — только System остаётся |
| 4 | `EnableAllCategories_AllEnabled` | EnableAll — все включены |
| **Уровни логов** | | |
| 5 | `SetMinLogLevel_Warning_WarningAndErrorPass` | Уровень Warning — проходят Warning и Error |
| 6 | `SetMinLogLevel_Error_OnlyErrorPass` | Уровень Error — проходит только Error |
| **Основные методы** | | |
| 7 | `Log_DoesNotThrow` | Log не вызывает исключений |
| 8 | `Log_WithFormat_DoesNotThrow` | Log с форматированием не вызывает исключений |
| 9 | `LogDebug_DoesNotThrow` | LogDebug не вызывает исключений |
| 10 | `LogWarning_DoesNotThrow` | LogWarning не вызывает исключений |
| 11 | `LogError_DoesNotThrow` | LogError не вызывает исключений |
| **Фильтрация** | | |
| 12 | `Log_DisabledCategory_DoesNotThrow` | Лог в выключенную категорию — без исключений |
| **Состояние** | | |
| 13 | `LogConfigState_DoesNotThrow` | LogConfigState не падает |
| **Завершение** | | |
| 14 | `Shutdown_DoesNotThrow` | Shutdown не падает |
| 15 | `Shutdown_CanReinitialize` | Реинициализация после Shutdown |

---

### 6. IntegrationTests (9 тестов)

| # | Метод | Описание |
|---|-------|----------|
| 1 | `Test_BuffManager_CombatManager_Integration` | Баффы влияют на урон |
| 2 | `Test_QiController_TechniqueController_Integration` | Ци расходуется на техники |
| 3 | `Test_QiController_TechniqueController_InsufficientQi` | Недостаточно Ци для техники |
| 4 | `Test_BuffManager_ConductivityPayback` | Откат проводимости |
| 5 | `Test_BuffManager_ConductivityCannotBeModifiedThroughStats` | Проводимость только через спец. метод |
| 6 | `Test_Formation_QiPool_Integration` | Формация расходует Ци через пул |
| 7 | `Test_Practitioner_TransferQi_ToFormation` | Передача Ци в формацию |
| 8 | `Test_SaveLoad_Integration` | Сохранение/загрузка состояния |
| 9 | `Test_QiPool_SaveLoad_RoundTrip` | Целостность QiPool при save/load |

**Edge Cases (IntegrationTests):**

| # | Метод | Описание |
|---|-------|----------|
| 10 | `Test_EdgeCase_ZeroQi_TechniqueUse` | Нулевое Ци при использовании техники |
| 11 | `Test_EdgeCase_QiOverflow` | Защита от overflow в QiController |
| 12 | `Test_EdgeCase_ZeroConductivity_Formation` | Нулевая проводимость в формации |

---

### 7. IntegrationTestScenarios (4 сценария)

| # | Сценарий | Описание |
|---|----------|----------|
| 1 | `Scenario_NPCGeneration` | Генерация NPC через генератор |
| 2 | `Scenario_CombatFlow` | Полный цикл боя Player vs NPC |
| 3 | `Scenario_CultivationBreakthrough` | Прорыв уровня культивации |
| 4 | `Scenario_SaveLoad` | Сохранение и загрузка |

---

### 8. BalanceVerification (верификация)

| # | Метод | Описание |
|---|-------|----------|
| 1 | `VerifyCoreCapacityFormula` | Формула ёмкости ядра |
| 2 | `VerifyLevelSuppressionTable` | Таблица подавления уровнем |
| 3 | `VerifyQiBufferEfficiency` | Эффективность Qi Buffer |
| 4 | `VerifyMeditationTime` | Время медитации |
| 5 | `VerifyGradeDistribution` | Распределение грейдов |
| 6 | `GenerateDamageCurve` | Кривая урона по уровням |
| 7 | `QuickVerify` | Быстрая проверка всех формул |

---

## 📊 Сводка

| Файл | Тип | Кол-во тестов |
|------|-----|---------------|
| CombatTests.cs | EditMode Unit | 20 |
| QiControllerTests.cs | EditMode Unit | 20 |
| FormationQiPoolTests.cs | EditMode Unit | 21 |
| BodySystemTests.cs | EditMode Unit | 5 |
| GameLoggerTests.cs | EditMode Unit | 15 |
| IntegrationTests.cs | PlayMode Integration | 12 |
| IntegrationTestScenarios.cs | PlayMode Scenarios | 4 |
| BalanceVerification.cs | Verification | 7 |
| **ИТОГО** | | **104** |

---

## 📊 Просмотр результатов

### В Test Runner

| Цвет | Статус |
|------|--------|
| 🟢 Зелёный | Тест пройден |
| 🔴 Красный | Тест провален |
| ⚪ Серый | Тест пропущен |
| 🟡 Жёлтый | Тест в процессе |

### В Console

После запуска тестов выводится детальная информация:

```
[CombatTests] LevelSuppression_SameLevel_ReturnsFullDamage - PASS
[CombatTests] QiBuffer_InsufficientQi_PartialAbsorption - PASS
[QiControllerTests] SpendQi_SufficientQi_ReturnsTrue - PASS
[FormationQiPoolTests] AddQi_PositiveAmount_IncreasesCurrentQi - PASS
[BodySystemTests] IsAlive_AllPartsHealthy_ReturnsTrue - PASS
[GameLoggerTests] SetCategoryEnabled_DisableCategory_CategoryIsDisabled - PASS
[IntegrationTests] Test_QiController_TechniqueController_Integration - PASS

=== Tests Complete: 104 passed, 0 failed ===
```

---

## 🔧 Верификация баланса

### Quick Verify

Быстрая проверка всех ключевых значений:

```csharp
// Через код
BalanceVerification.QuickVerify();

// Через компонент
// GameObject → Add Component → Balance Verification Component
// Правый клик → Run Quick Verification
```

### Полный отчёт

```csharp
// Генерация Markdown отчёта
string report = BalanceVerification.GenerateFullReport();
Debug.Log(report);

// Сохранение в файл
BalanceVerification.SaveReportToFile();
// → Assets/balance_report.md
```

### Проверка конкретных формул

```csharp
// Ёмкость ядра
BalanceVerification.VerifyCoreCapacityFormula();

// Подавление уровнем
BalanceVerification.VerifyLevelSuppressionTable();

// Эффективность Qi Buffer
BalanceVerification.VerifyQiBufferEfficiency();

// Время медитации
BalanceVerification.VerifyMeditationTime();
```

---

## 🐛 Отладка проваленных тестов

### 1. Чтение ошибки

```
Assert.AreEqual(expected, actual)
Expected: 100
But was:  50
```

### 2. Добавление Debug.Log

```csharp
[Test]
public void Test_Debugging()
{
    int result = SomeMethod();
    Debug.Log($"[DEBUG] Input: {input}");
    Debug.Log($"[DEBUG] Expected: {expected}");
    Debug.Log($"[DEBUG] Actual: {result}");

    Assert.AreEqual(expected, result);
}
```

### 3. Пошаговая отладка

1. Поставь breakpoint в тесте
2. Запусти тест через **Debug** в Test Runner
3. Unity остановится на breakpoint

---

## 📈 Покрытие кода

### Проверка покрытия

Unity не имеет встроенного инструмента покрытия, но можно использовать:

1. **ReportGenerator** — для анализа результатов
2. **Ручная проверка** — просмотр списка тестов

### Список протестированных модулей

| Модуль | Статус | Файл тестов |
|--------|--------|-------------|
| Combat/DamageCalculator | ✅ Unit + Integration | CombatTests.cs, IntegrationTests.cs |
| Combat/LevelSuppression | ✅ Unit | CombatTests.cs |
| Combat/QiBuffer | ✅ Unit | CombatTests.cs |
| Combat/TechniqueCapacity | ✅ Unit | CombatTests.cs |
| Qi/QiController | ✅ Unit + Integration | QiControllerTests.cs, IntegrationTests.cs |
| Formation/FormationQiPool | ✅ Unit + Integration | FormationQiPoolTests.cs, IntegrationTests.cs |
| Body/BodyDamage | ✅ Unit | BodySystemTests.cs |
| Body/BodyController | ✅ Unit | BodySystemTests.cs |
| Core/GameLogger | ✅ Unit | GameLoggerTests.cs |
| Buff/BuffManager | ✅ Integration | IntegrationTests.cs |
| Qi/TechniqueController | ✅ Integration | IntegrationTests.cs |
| Save/SaveManager | ✅ Integration | IntegrationTests.cs |
| NPC/NPCGenerator | ✅ Scenario | IntegrationTestScenarios.cs |

---

## ⚡ Быстрая шпаргалка

| Действие | Путь |
|----------|------|
| Открыть Test Runner | `Window → General → Test Runner` |
| Запустить все тесты | `Run All` в Test Runner |
| Запустить один тест | Выбрать → `Run Selected` |
| Верификация баланса | `Tools → Verify Balance` |
| Сохранить отчёт | `Tools → Save Balance Report` |
| Запустить боевые тесты | `Tools → Run Tests → Combat Tests` |
| Запустить интеграционные | `Tools → Run Tests → Integration Tests` |

---

*Документ создан: 2026-04-09 06:35:00 UTC*
*Документ обновлён: 2026-04-17 19:30:00 UTC — добавлены QiControllerTests (20), FormationQiPoolTests (21), BodySystemTests (5), GameLoggerTests (15)*
