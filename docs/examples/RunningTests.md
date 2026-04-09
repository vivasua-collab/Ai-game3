# 🚀 Запуск Unit тестов в Unity

**Создано:** 2026-04-09 06:35:00 UTC
**Проект:** Cultivation World Simulator

---

## 📋 Способы запуска тестов

### 1. Unity Test Runner (Рекомендуется)

#### Открытие Test Runner

```
Window → General → Test Runner
```

#### Запуск тестов

1. Открой **Test Runner**窗口
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
├── CombatTests.cs          — EditMode tests
└── BalanceVerification.cs  — EditMode verification
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
[IntegrationTests] Test_QiController_TechniqueController_Integration - PASS

=== Tests Complete: 25 passed, 0 failed ===
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
2. **手动 проверка** — просмотр списка тестов

### Список протестированных модулей

| Модуль | Статус |
|--------|--------|
| Combat/DamageCalculator | ✅ Unit + Integration |
| Combat/LevelSuppression | ✅ Unit |
| Combat/QiBuffer | ✅ Unit |
| Combat/TechniqueCapacity | ✅ Unit |
| Qi/QiController | ✅ Integration |
| Formation/FormationQiPool | ✅ Integration |
| Save/SaveManager | ✅ Integration |

---

## ⚡ Быстрая шпаргалка

| Действие | Путь |
|----------|------|
| Открыть Test Runner | `Window → General → Test Runner` |
| Запустить все тесты | `Run All` в Test Runner |
| Запустить один тест | Выбрать → `Run Selected` |
| Верификация баланса | `Tools → Verify Balance` |
| Сохранить отчёт | `Tools → Save Balance Report` |

---

*Документ создан: 2026-04-09 06:35:00 UTC*
