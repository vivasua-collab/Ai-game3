# 📋 План внедрения системы формаций

**Дата:** 2026-04-03
**Статус:** 📋 План
**Источник:** docs/FORMATION_SYSTEM.md
**Реализовано:** FormationCoreData.cs

---

## 📊 Анализ текущего состояния

### ✅ Уже реализовано

| Компонент | Файл | Строк | Статус |
|-----------|------|-------|--------|
| FormationCoreData.cs | Scripts/Data/ScriptableObjects/ | ~290 | ✅ Готов |
| Enums | FormationCoreType, FormationCoreVariant, FormationType, FormationSize | — | ✅ Готов |
| QiController.cs | Scripts/Qi/ | ~390 | ✅ Готов (база) |
| ChargerController.cs | Scripts/Charger/ | ~560 | ✅ Готов (интеграция) |
| BuffData.cs | Scripts/Data/ScriptableObjects/ | — | ✅ Готов (баффы) |

### ❌ Требуется реализация

| Компонент | Описание | Приоритет |
|-----------|----------|-----------|
| FormationData.cs | ScriptableObject формации | 🔴 Высокий |
| FormationController.cs | Управление созданием/активацией | 🔴 Высокий |
| FormationCore.cs | Runtime активная формация | 🔴 Высокий |
| FormationQiPool.cs | Ёмкость и утечка Ци | 🟡 Средний |
| FormationEffects.cs | Применение эффектов | 🟡 Средний |
| FormationUI.cs | Интерфейс управления | 🟢 Низкий |
| FormationSaveData.cs | Сохранение состояния | 🟢 Низкий |

---

## 🏗️ Архитектура внедрения

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     АРХИТЕКТУРА СИСТЕМЫ ФОРМАЦИЙ                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                      SCRIPTABLE OBJECTS                             │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │  FormationCoreData.cs    ✅ Готов                                   │    │
│  │  FormationData.cs        ❌ Нужно создать                           │    │
│  │  BuffData.cs             ✅ Готов                                   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                      RUNTIME COMPONENTS                             │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │  FormationController.cs  → Управление формациями                    │    │
│  │  FormationCore.cs        → Активная формация в мире                 │    │
│  │  FormationQiPool.cs      → Ёмкость, утечка, наполнение              │    │
│  │  FormationEffects.cs     → Применение баффов/урона                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                      INTEGRATION                                    │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │  QiController.cs         ✅ Источник Ци                             │    │
│  │  ChargerController.cs    ✅ Камни Ци для подпитки                   │    │
│  │  BuffManager.cs          ❌ Требуется для баффов                    │    │
│  │  TimeController.cs       ✅ Тики для утечки                         │    │
│  │  CombatManager.cs        ✅ Интеграция урона                        │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Компоненты для создания

### 1. FormationData.cs (~400 строк)

**Назначение:** ScriptableObject с данными формации

```csharp
// Структура файла
public class FormationData : ScriptableObject
{
    // === Identity ===
    string formationId;
    string displayName;
    string description;
    Sprite icon;
    
    // === Classification ===
    FormationType type;           // Barrier, Trap, Amplification...
    FormationSize size;           // Small, Medium, Large...
    int level;                    // 1-9
    Element element;              // Стихия
    
    // === Costs ===
    int contourQi;                // Стоимость прорисовки = 80 × 2^(level-1)
    float castTime;               // Время создания
    float cooldown;               // Кулдаун
    
    // === Area ===
    float creationRadius;         // Радиус прорисовки
    float effectRadius;           // Радиус действия
    
    // === Duration ===
    bool isPermanent;             // Постоянная (только со ядром)
    float baseDuration;           // Базовая длительность
    
    // === Effects ===
    List<FormationEffect> allyEffects;
    List<FormationEffect> enemyEffects;
    
    // === Requirements ===
    int minCultivationLevel;
    int minFormationKnowledge;
    
    // === Core ===
    bool requiresCore;            // Требует физическое ядро?
    List<FormationCoreType> compatibleCores;
}

[System.Serializable]
public class FormationEffect
{
    public FormationEffectType effectType;  // Buff, Damage, Heal, Control...
    public BuffType buffType;               // Если бафф
    public float value;                     // Величина эффекта
    public bool isPercentage;               // Процент или абсолют
    public float tickInterval;              // Интервал тиков (0 = постоянный)
    public int tickValue;                   // Урон/исцеление за тик
}
```

**Оценка времени:** 2-3 часа

---

### 2. FormationController.cs (~600 строк)

**Назначение:** Управление созданием, активацией и удалением формаций

```csharp
public class FormationController : MonoBehaviour
{
    // === State ===
    List<ActiveFormation> activeFormations;
    FormationData selectedFormation;
    bool isPlacingMode;
    
    // === References ===
    QiController practitionerQi;
    TimeController timeController;
    
    // === Events ===
    event Action<ActiveFormation> OnFormationCreated;
    event Action<ActiveFormation> OnFormationActivated;
    event Action<ActiveFormation> OnFormationDepleted;
    
    // === Main Methods ===
    
    // Создание без ядра (одноразовая)
    ActiveFormation CreateWithoutCore(FormationData data, Vector2 position);
    
    // Внедрение в ядро (многоразовая)
    bool ImbueCore(FormationCoreData core, FormationData formation);
    
    // Начать наполнение
    void StartFilling(ActiveFormation formation);
    
    // Участвовать в наполнении
    void ContributeToFilling(ActiveFormation formation, int qiAmount);
    
    // Активировать при 100% наполнении
    void Activate(ActiveFormation formation);
    
    // Деактивировать
    void Deactivate(ActiveFormation formation);
    
    // Обновление (тик утечки)
    void OnTimeTick(int currentTick);
}
```

**Оценка времени:** 4-5 часов

---

### 3. FormationCore.cs (~500 строк)

**Назначение:** Runtime представление активной формации

```csharp
public class FormationCore : MonoBehaviour
{
    // === Data ===
    FormationData data;
    FormationCoreData coreData;       // null если без ядра
    GameObject owner;
    
    // === State ===
    FormationStage stage;             // Drawing, Filling, Active, Depleted
    Vector2 center;
    float currentRadius;
    float remainingDuration;
    
    // === Qi Pool ===
    FormationQiPool qiPool;
    
    // === Filling ===
    List<FormationParticipant> participants;
    float totalFillRate;
    
    // === Effects ===
    Collider2D[] affectedBuffer;
    
    // === Lifecycle ===
    void Initialize(FormationData data, Vector2 position, GameObject creator);
    void Update();                     // Применение эффектов
    void ProcessTick();                // Утечка Ци
    void ApplyEffects();               // Баффы/урон целям
    void Deactivate();                 // Истощение
}

public enum FormationStage
{
    Drawing,       // Прорисовка контура
    Filling,       // Наполнение ёмкости
    Active,        // Работает
    Depleted       // Истощена
}
```

**Оценка времени:** 4-5 часов

---

### 4. FormationQiPool.cs (~300 строк)

**Назначение:** Управление ёмкостью, наполнением и утечкой Ци

```csharp
public class FormationQiPool
{
    // === Configuration ===
    long capacity;                     // Максимальная ёмкость
    int drainInterval;                 // Интервал утечки (тики)
    int drainAmount;                   // Ци за раз
    
    // === State ===
    long currentQi;
    float fillPercent;
    int lastDrainTick;
    
    // === Methods ===
    
    // Рассчитать ёмкость
    static long CalculateCapacity(int level, FormationSize size, bool isHeavy);
    
    // Добавить Ци при наполнении
    long AddQi(long amount);
    
    // Использовать Ци (для барьера)
    long ConsumeQi(long amount);
    
    // Утечка (вызывается каждый тик)
    int ProcessDrain(int currentTick);
    
    // Проверка активации
    bool IsReadyForActivation => currentQi >= capacity;
    
    // Время до истощения
    TimeSpan GetTimeUntilDepleted();
}

// Константы утечки
public static class FormationDrain
{
    public static readonly int[] INTERVAL_BY_LEVEL = { 60, 60, 40, 40, 20, 20, 10, 10, 5 };
    public static readonly int[] AMOUNT_BY_SIZE = { 1, 3, 10, 30, 100 };
}
```

**Оценка времени:** 2-3 часа

---

### 5. FormationEffects.cs (~400 строк)

**Назначение:** Применение эффектов формации к целям

```csharp
public static class FormationEffects
{
    // Найти цели в радиусе
    static Collider2D[] FindTargets(Vector2 center, float radius, LayerMask layerMask);
    
    // Определить союзник/враг
    static bool IsAlly(GameObject target, GameObject owner);
    
    // Применить эффекты
    static void ApplyEffects(GameObject target, List<FormationEffect> effects, GameObject source);
    
    // Применить бафф
    static void ApplyBuff(GameObject target, BuffType buffType, float value, bool isPercentage);
    
    // Нанести урон
    static void ApplyDamage(GameObject target, int damage, Element element);
    
    // Исцелить
    static void ApplyHeal(GameObject target, int healAmount);
    
    // Контроль (заморозка, замедление)
    static void ApplyControl(GameObject target, ControlType controlType, float duration);
}
```

**Оценка времени:** 2-3 часа

---

## 🔄 Варианты реализации

### Вариант А: Минимальный (MVP)

**Цель:** Базовая работающая система

**Объём:** ~1500 строк, 12-15 часов

| Этап | Компонент | Часы |
|------|-----------|------|
| 1 | FormationData.cs | 3 |
| 2 | FormationQiPool.cs | 2 |
| 3 | FormationCore.cs (базовый) | 4 |
| 4 | FormationController.cs (базовый) | 4 |
| 5 | Интеграция с QiController | 2 |

**Функционал:**
- ✅ Создание формации без ядра
- ✅ Наполнение одним практиком
- ✅ Активация при 100%
- ✅ Базовые эффекты (баффы)
- ✅ Естественная утечка
- ❌ Физические ядра (отложено)
- ❌ Множественное наполнение (отложено)

---

### Вариант Б: Стандартный

**Цель:** Полноценная система с ядрами

**Объём:** ~2500 строк, 20-25 часов

| Этап | Компонент | Часы |
|------|-----------|------|
| 1 | FormationData.cs | 3 |
| 2 | FormationQiPool.cs | 3 |
| 3 | FormationCore.cs | 5 |
| 4 | FormationController.cs | 5 |
| 5 | FormationEffects.cs | 3 |
| 6 | Интеграция с Charger | 3 |
| 7 | UI базовый | 3 |

**Функционал:**
- ✅ Всё из Варианта А
- ✅ Физические ядра (диски, алтари)
- ✅ Внедрение формации в ядро
- ✅ Множественное наполнение
- ✅ Подпитка от камней Ци
- ✅ Все типы формаций
- ✅ Контур сбора Ци (L8+)

---

### Вариант В: Расширенный

**Цель:** Полная система с UI и сохранением

**Объём:** ~3500 строк, 30-35 часов

| Этап | Компонент | Часы |
|------|-----------|------|
| 1-7 | Всё из Варианта Б | 25 |
| 8 | FormationUI.cs | 5 |
| 9 | FormationSaveData.cs | 2 |
| 10 | Тестирование | 3 |

**Функционал:**
- ✅ Всё из Варианта Б
- ✅ Полноценный UI
- ✅ Сохранение/загрузка
- ✅ Визуальные эффекты
- ✅ Превью при размещении

---

## 📋 Детальный план (Вариант Б)

### Этап 1: FormationData.cs (3 часа)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationData.cs                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] Определить FormationEffectType enum                                    │
│  [ ] Создать FormationEffect class                                          │
│  [ ] Создать FormationData : ScriptableObject                               │
│  [ ] Добавить CreateAssetMenu attribute                                     │
│  [ ] Методы расчёта: GetContourQi(), GetCapacity(), GetDrainParams()        │
│  [ ] Протестировать в Unity Editor                                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 2: FormationQiPool.cs (3 часа)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationQiPool.cs                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] Статические методы расчёта ёмкости                                     │
│  [ ] Статические таблицы утечки (INTERVAL_BY_LEVEL, AMOUNT_BY_SIZE)         │
│  [ ] Класс FormationQiPool                                                  │
│  [ ] Методы: AddQi(), ConsumeQi(), ProcessDrain()                           │
│  [ ] События: OnQiChanged, OnFilled, OnDepleted                             │
│  [ ] Unit тесты                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 3: FormationCore.cs (5 часов)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationCore.cs                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] FormationStage enum                                                    │
│  [ ] FormationParticipant struct                                            │
│  [ ] FormationCore : MonoBehaviour                                          │
│  [ ] Initialize() — создание формации                                       │
│  [ ] StartFilling() — начать наполнение                                     │
│  [ ] AddParticipant() — добавить участника                                  │
│  [ ] Activate() — активация при 100%                                        │
│  [ ] Update() — применение эффектов                                         │
│  [ ] ProcessDrain() — обработка утечки                                      │
│  [ ] Deactivate() — истощение                                               │
│  [ ] Интеграция с TimeController для тиков                                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 4: FormationController.cs (5 часов)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationController.cs                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] ActiveFormation class (runtime состояние)                              │
│  [ ] FormationController : MonoBehaviour                                    │
│  [ ] Register KnownFormations (изученные формации)                          │
│  [ ] CreateWithoutCore() — одноразовая формация                             │
│  [ ] ImbueCore() — внедрение в ядро                                         │
│  [ ] MountAltar() — монтаж алтаря                                           │
│  [ ] StartFilling() — начать наполнение                                     │
│  [ ] ContributeQi() — внести Ци                                             │
│  [ ] OnTimeTick() — обработка тиков для утечки                              │
│  [ ] События: OnFormationCreated, OnActivated, OnDepleted                   │
│  [ ] Интеграция с QiController                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 5: FormationEffects.cs (3 часа)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationEffects.cs                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] ControlType enum (Freeze, Slow, Root, Stun)                            │
│  [ ] FindTargets() — поиск целей в радиусе                                  │
│  [ ] IsAlly() — проверка союзника                                           │
│  [ ] ApplyEffects() — применить список эффектов                             │
│  [ ] ApplyBuff() — применить бафф                                           │
│  [ ] ApplyDamage() — нанести урон (интеграция CombatManager)                │
│  [ ] ApplyHeal() — исцелить                                                 │
│  [ ] ApplyControl() — наложить контроль                                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 6: Интеграция с Charger (3 часа)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Интеграция с ChargerController                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Добавить метод ChargeFormation() в ChargerController                   │
│  [ ] Подключить FormationQiPool к слотам камней                             │
│  [ ] Автоматическая подпитка от камней Ци                                   │
│  [ ] Ограничение проводимостью ядра                                         │
│  [ ] UI индикация подпитки                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Этап 7: Базовый UI (3 часа)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  FormationUI.cs                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ ] Создать файл                                                           │
│  [ ] FormationUIState struct                                                │
│  [ ] Показ списка изученных формаций                                        │
│  [ ] Превью при размещении                                                  │
│  [ ] Индикатор наполнения                                                   │
│  [ ] Информация об активной формации                                        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔗 Интеграция с существующими системами

### QiController

```csharp
// Добавить в QiController.cs

/// <summary>
/// Передать Ци в формацию.
/// </summary>
public int TransferToFormation(FormationCore formation, int maxAmount)
{
    int amount = Mathf.Min((int)currentQi, maxAmount);
    
    if (amount > 0)
    {
        // Скорость передачи = проводимость × плотность
        float transferRate = conductivity * qiDensity;
        
        // Формация принимает с учётом своей проводимости
        int accepted = formation.AcceptQi(amount, transferRate);
        
        if (accepted > 0)
        {
            SpendQi(accepted);
        }
        
        return accepted;
    }
    
    return 0;
}
```

### ChargerController

```csharp
// Добавить в ChargerController.cs

/// <summary>
/// Подпитать формацию от камней Ци.
/// </summary>
public int ChargeFormation(FormationCore formation)
{
    if (!IsOperational || !HasStones) return 0;
    
    // Ограничено проводимостью зарядника
    int maxTransfer = Mathf.RoundToInt(buffer.Conductivity * Time.deltaTime);
    
    // Используем Ци из буфера
    ChargerBufferResult result = buffer.UseQiForTechnique(maxTransfer, 0);
    
    if (result.QiFromBuffer > 0)
    {
        formation.AcceptQi(result.QiFromBuffer, buffer.Conductivity);
    }
    
    return result.QiFromBuffer;
}
```

### TimeController

```csharp
// Уже есть событие OnTick
// FormationController подписывается на него

private void OnEnable()
{
    if (TimeController.Instance != null)
    {
        TimeController.Instance.OnTick += HandleTimeTick;
    }
}

private void HandleTimeTick(int tick)
{
    // Обрабатываем утечку для всех активных формаций
    foreach (var formation in activeFormations)
    {
        formation.ProcessDrain(tick);
    }
}
```

---

## 📊 Метрики успеха

### MVP (Вариант А)

| Метрика | Цель |
|---------|------|
| Компиляция | ✅ Без ошибок |
| Создание формации | ✅ Работает |
| Наполнение | ✅ Работает |
| Активация | ✅ При 100% |
| Утечка | ✅ По формуле |

### Стандарт (Вариант Б)

| Метрика | Цель |
|---------|------|
| Ядра (диски) | ✅ 4 типа |
| Ядра (алтари) | ✅ 4 типа |
| Множественное наполнение | ✅ До 50 участников |
| Подпитка от камней | ✅ Работает |
| Все типы формаций | ✅ 8 типов |

---

## 📁 Структура файлов после внедрения

```
Scripts/
├── Formation/
│   ├── FormationData.cs           # ScriptableObject
│   ├── FormationController.cs     # Главный контроллер
│   ├── FormationCore.cs           # Активная формация
│   ├── FormationQiPool.cs         # Ёмкость и утечка
│   ├── FormationEffects.cs        # Применение эффектов
│   ├── FormationUI.cs             # Интерфейс
│   └── FormationSaveData.cs       # Сохранение
│
├── Data/ScriptableObjects/
│   ├── FormationCoreData.cs       # ✅ Уже есть
│   └── FormationData.cs           # Новый
│
└── (интеграция)
    ├── Qi/QiController.cs         # + TransferToFormation()
    ├── Charger/ChargerController.cs # + ChargeFormation()
    └── World/TimeController.cs     # + OnTick (уже есть)
```

---

## ⚠️ Риски и решения

| Риск | Решение |
|------|---------|
| Производительность при множестве формаций | Использовать пул объектов, ограничить максимум |
| Десинхронизация утечки | Привязка к TimeController.OnTick |
| Читы с бесконечными формациями | Ограничить время жизни через утечку |
| Сложность UI | Начать с минимального, добавлять по запросу |

---

## 📅 Рекомендуемый порядок

1. **Этап 1-2:** FormationData + FormationQiPool (6 часов) —理论基础
2. **Этап 3:** FormationCore (5 часов) — ядро системы
3. **Этап 4:** FormationController (5 часов) — управление
4. **Этап 5-6:** Эффекты + Интеграция (6 часов) — функциональность
5. **Этап 7:** UI (3 часа) — визуализация

**Итого:** 25 часов (Вариант Б)

---

*План создан: 2026-04-03*
*Источник: docs/FORMATION_SYSTEM.md*
*Реализовано: FormationCoreData.cs*
