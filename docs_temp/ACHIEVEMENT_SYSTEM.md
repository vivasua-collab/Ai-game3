# Система достижений (Achievement System)

**Создано:** 2026-04-03
**Статус:** Теоретические изыскания
**Приоритет:** Средний

---

## Обзор

Система достижений отслеживает прогресс игрока и награждает за выполнение определённых условий. Достижения могут быть:
- Скрытые (до получения)
- Публичные (видны всегда)
- Секретные (особые условия)

---

## Архитектура

### Компоненты

```
AchievementSystem
├── AchievementManager          # Главный менеджер
├── AchievementData            # Данные достижения (ScriptableObject)
├── AchievementProgress        # Прогресс игрока
├── AchievementTracker         # Отслеживание условий
└── AchievementUI              # Отображение
```

### Структура данных

```csharp
// Создано: 2026-04-03

/// <summary>
/// Тип достижения.
/// </summary>
public enum AchievementType
{
    Cultivation,    // Культивация
    Combat,         // Бой
    Exploration,    // Исследование
    Social,         // Социальные
    Collection,     // Коллекционирование
    Hidden,         // Скрытые
    Story           // Сюжетные
}

/// <summary>
/// Условие получения достижения.
/// </summary>
public enum AchievementCondition
{
    ReachLevel,         // Достичь уровня
    DefeatEnemies,      // Победить врагов
    CollectItems,       // Собрать предметы
    CompleteQuest,      // Завершить квест
    LearnTechnique,     // Изучить технику
    VisitLocations,     // Посетить локации
    CraftItems,         // Создать предметы
    SurviveTime,        // Выжить время
    DealDamage,         // Нанести урон
    TakeDamage,         // Получить урон
    Heal,               // Исцелить
    Breakthrough        // Прорыв уровня
}

/// <summary>
/// Структура достижения.
/// </summary>
[System.Serializable]
public class Achievement
{
    public string id;
    public string nameRu;
    public string nameEn;
    public string description;
    public AchievementType type;
    public AchievementCondition condition;
    public int requiredValue;
    public int currentValue;
    public bool isUnlocked;
    public bool isHidden;
    public bool isSecret;
    public Sprite icon;
    public string rewardId;     // ID награды
    public int rewardValue;     // Значение награды
}
```

---

## Категории достижений

### Культивация (Cultivation)

| ID | Название | Условие | Награда |
|----|----------|---------|---------|
| `cult_first_breakthrough` | Первый шаг | Первый прорыв | +100 Ци |
| `cult_level_5` | Сердце Небес | Достичь 5 уровня | +500 Ци |
| `cult_level_10` | Вознесение | Достичь 10 уровня | +5000 Ци |
| `cult_perfect_core` | Идеальное ядро | Прорыв без повреждений | Титул |
| `cult_fast_breakthrough` | Скоростной прорыв | 3 прорыва за день | +200 Ци |

### Бой (Combat)

| ID | Название | Условие | Награда |
|----|----------|---------|---------|
| `combat_first_kill` | Первая кровь | Первое убийство | +50 Ци |
| `combat_100_kills` | Воин | 100 убийств | +200 Ци |
| `combat_1000_kills` | Мастер войны | 1000 убийств | +1000 Ци |
| `combat_no_damage` | Безупречная победа | Победа без урона | +100 Ци |
| `combat_kill_boss` | Убийца боссов | Убить босса | +300 Ци |
| `combat_element_master` | Мастер стихии | 50 убийств одной стихией | +200 Ци |

### Исследование (Exploration)

| ID | Название | Условие | Награда |
|----|----------|---------|---------|
| `exp_first_location` | Путник | Посетить первую локацию | +50 Ци |
| `exp_10_locations` | Исследователь | 10 локаций | +200 Ци |
| `exp_all_locations` | Картограф | Все локации | +500 Ци |
| `exp_find_secret` | Первооткрыватель | Найти секрет | +100 Ци |

### Коллекционирование (Collection)

| ID | Название | Условие | Награда |
|----|----------|---------|---------|
| `coll_first_item` | Коллекционер | Первый предмет | +25 Ци |
| `coll_100_items` | Собиратель | 100 предметов | +200 Ци |
| `coll_rare_item` | Охотник за редким | Редкий предмет | +100 Ци |
| `coll_all_techniques` | Мастер техник | Все техники | +1000 Ци |

---

## Реализация

### AchievementManager.cs

```csharp
// Создано: 2026-04-03
// Теоретическая реализация

using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private List<Achievement> achievements;
    private Dictionary<string, Achievement> achievementDict;

    // События
    public event System.Action<Achievement> OnAchievementUnlocked;
    public event System.Action<Achievement, int> OnProgressUpdated;

    private void Awake()
    {
        Instance = this;
        achievementDict = new Dictionary<string, Achievement>();

        foreach (var achievement in achievements)
        {
            achievementDict[achievement.id] = achievement;
        }
    }

    /// <summary>
    /// Обновляет прогресс достижения.
    /// </summary>
    public void UpdateProgress(AchievementCondition condition, int value)
    {
        foreach (var achievement in achievements)
        {
            if (achievement.condition == condition && !achievement.isUnlocked)
            {
                achievement.currentValue += value;

                if (achievement.currentValue >= achievement.requiredValue)
                {
                    UnlockAchievement(achievement);
                }
                else
                {
                    OnProgressUpdated?.Invoke(achievement, achievement.currentValue);
                }
            }
        }
    }

    /// <summary>
    /// Устанавливает прогресс достижения.
    /// </summary>
    public void SetProgress(AchievementCondition condition, int value)
    {
        foreach (var achievement in achievements)
        {
            if (achievement.condition == condition && !achievement.isUnlocked)
            {
                achievement.currentValue = value;

                if (achievement.currentValue >= achievement.requiredValue)
                {
                    UnlockAchievement(achievement);
                }
            }
        }
    }

    /// <summary>
    /// Разблокирует достижение.
    /// </summary>
    private void UnlockAchievement(Achievement achievement)
    {
        achievement.isUnlocked = true;
        OnAchievementUnlocked?.Invoke(achievement);
        GrantReward(achievement);
    }

    /// <summary>
    /// Выдаёт награду за достижение.
    /// </summary>
    private void GrantReward(Achievement achievement)
    {
        // TODO: Интеграция с системой наград
        Debug.Log($"[Achievement] Unlocked: {achievement.nameRu}");
    }

    /// <summary>
    /// Получает достижение по ID.
    /// </summary>
    public Achievement GetAchievement(string id)
    {
        return achievementDict.TryGetValue(id, out var achievement) ? achievement : null;
    }

    /// <summary>
    /// Получает все достижения типа.
    /// </summary>
    public List<Achievement> GetAchievementsByType(AchievementType type)
    {
        return achievements.FindAll(a => a.type == type);
    }

    /// <summary>
    /// Получает прогресс (0-1).
    /// </summary>
    public float GetProgress(string id)
    {
        var achievement = GetAchievement(id);
        if (achievement == null) return 0f;

        return (float)achievement.currentValue / achievement.requiredValue;
    }
}
```

---

## Интеграция с GameEvents

```csharp
// В GameEvents.cs добавить:

// Achievements
public static event System.Action<string> OnAchievementUnlocked;
public static event System.Action<string, int, int> OnAchievementProgress; // id, current, required

// Методы вызова
public static void TriggerAchievementUnlocked(string achievementId)
{
    OnAchievementUnlocked?.Invoke(achievementId);
}

public static void TriggerAchievementProgress(string id, int current, int required)
{
    OnAchievementProgress?.Invoke(id, current, required);
}
```

---

## UI представление

### Окно достижений

```
┌─────────────────────────────────────┐
│           ДОСТИЖЕНИЯ                │
├─────────────────────────────────────┤
│ [Культивация] [Бой] [Исслед.] [Все] │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ 🏆 Первый шаг                   │ │
│ │    Совершить первый прорыв      │ │
│ │    ████████░░ 8/10             │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ ⭐ Воин ✓                       │ │
│ │    Победить 100 врагов          │ │
│ │    ██████████ 100/100 ✓        │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ ??? Скрытое достижение          │ │
│ │    ???                          │ │
│ │    ????????                     │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

---

## Уведомления

При получении достижения показывать всплывающее уведомление:

```
┌─────────────────────────────┐
│ 🏆 Достижение получено!     │
│                             │
│ "Первый шаг"                │
│ Совершить первый прорыв     │
│                             │
│ Награда: +100 Ци            │
└─────────────────────────────┘
```

---

## Хранение данных

### SaveData

```csharp
[System.Serializable]
public class AchievementSaveData
{
    public List<string> unlockedAchievements = new List<string>();
    public Dictionary<string, int> progressData = new Dictionary<string, int>();
}
```

### Сохранение

```csharp
// В SaveManager
public void SaveAchievements()
{
    var data = new AchievementSaveData();

    foreach (var achievement in AchievementManager.Instance.GetAllAchievements())
    {
        if (achievement.isUnlocked)
        {
            data.unlockedAchievements.Add(achievement.id);
        }
        else if (achievement.currentValue > 0)
        {
            data.progressData[achievement.id] = achievement.currentValue;
        }
    }

    SaveToFile("achievements", data);
}
```

---

## Точки интеграции

| Событие | Где вызывать | Метод |
|---------|--------------|-------|
| Прорыв уровня | CultivationController | `UpdateProgress(AchievementCondition.Breakthrough, 1)` |
| Убийство врага | CombatManager | `UpdateProgress(AchievementCondition.DefeatEnemies, 1)` |
| Сбор предмета | InventoryController | `UpdateProgress(AchievementCondition.CollectItems, 1)` |
| Посещение локации | LocationController | `UpdateProgress(AchievementCondition.VisitLocations, 1)` |
| Изучение техники | TechniqueController | `UpdateProgress(AchievementCondition.LearnTechnique, 1)` |

---

## Статистика

Отслеживаемые метрики для достижений:

```csharp
public class PlayerStatistics
{
    // Культивация
    public int totalBreakthroughs;
    public int currentLevel;
    public int perfectBreakthroughs;

    // Бой
    public int totalKills;
    public int bossKills;
    public int damageDealt;
    public int damageTaken;
    public int healingDone;

    // Исследование
    public int locationsVisited;
    public int secretsFound;

    // Коллекции
    public int itemsCollected;
    public int techniquesLearned;
    public int rareItemsFound;

    // Время
    public float totalPlayTime;
    public int daysSurvived;
}
```

---

## Рекомендации по реализации

1. **Этап 1:** Создать базовую структуру (Achievement, AchievementManager)
2. **Этап 2:** Интегрировать с GameEvents
3. **Этап 3:** Добавить UI (окно достижений + уведомления)
4. **Этап 4:** Добавить сохранение прогресса
5. **Этап 5:** Наполнить контентом (30-50 достижений)

---

*Документ создан: 2026-04-03*
*Статус: Теоретические изыскания*
