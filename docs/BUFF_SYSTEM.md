# Система баффов и дебаффов (Buff System)

**Создано:** 2026-04-03
**Статус:** Теоретические изыскания
**Приоритет:** Высокий

---

## Обзор

Система баффов и дебаффов управляет временными эффектами, влияющими на характеристики персонажа. Баффы — положительные эффекты, дебаффы — отрицательные.

---

## Архитектура

### Компоненты

```
BuffSystem
├── BuffManager              # Управление эффектами
├── BuffData                 # Данные эффекта (ScriptableObject)
├── ActiveBuff               # Активный эффект на цели
├── BuffType                 # Типы эффектов
└── BuffUI                   # Отображение
```

---

## Типы эффектов

### Баффы (положительные)

| Тип | Описание | Примеры |
|-----|----------|---------|
| `StatBoost` | Увеличение характеристики | +20% атаки, +50% скорости |
| `Regeneration` | Регенерация | HP/Ци/Выносливость |
| `Immunity` | Иммунитет | К яду, оглушению |
| `Shield` | Щит | Поглощение урона |
| `Haste` | Ускорение | Скорость атаки, передвижения |
| `Invisibility` | Невидимость | Скрытность |
| `PowerUp` | Усиление | Урон способностей |

### Дебаффы (отрицательные)

| Тип | Описание | Примеры |
|-----|----------|---------|
| `StatReduction` | Снижение характеристики | -20% защиты, -30% скорости |
| `DamageOverTime` | Урон во времени | Отравление, горение |
| `Control` | Контроль | Оглушение, замедление, сон |
| `Weaken` | Ослабление | Снижение урона, лечения |
| `Curse` | Проклятие | Блокировка способностей |
| `Vulnerability` | Уязвимость | +50% урона от элемента |

---

## Структуры данных

### BuffType

```csharp
// Создано: 2026-04-03

/// <summary>
/// Тип баффа/дебаффа.
/// </summary>
public enum BuffType
{
    // Баффы
    AttackBoost,        // Увеличение атаки
    DefenseBoost,       // Увеличение защиты
    SpeedBoost,         // Увеличение скорости
    QiRegen,            // Регенерация Ци
    HealthRegen,        // Регенерация здоровья
    StaminaRegen,       // Регенерация выносливости
    CriticalChance,     // Шанс критического удара
    CriticalDamage,     // Урон критического удара
    DamageReduction,    // Снижение урона
    Evasion,            // Уклонение
    Shield,             // Щит

    // Дебаффы
    AttackReduction,    // Снижение атаки
    DefenseReduction,   // Снижение защиты
    SpeedReduction,     // Снижение скорости
    Poison,             // Отравление
    Burn,               // Горение
    Bleed,              // Кровотечение
    Freeze,             // Заморозка
    Stun,               // Оглушение
    Slow,               // Замедление
    Blind,              // Ослепление
    Silence,            // Безмолвие (блок техник)
    Curse,              // Проклятие
    Vulnerability       // Уязвимость к элементу
}

/// <summary>
/// Способ применения баффа.
/// </summary>
public enum BuffApplication
{
    Instant,            // Мгновенный эффект
    Duration,           // Длительный эффект
    Permanent,          // Постоянный (пока не снят)
    Stacking,           // Накапливающийся
    Refreshing          // Обновляет длительность
}

/// <summary>
/// Поведение при повторном применении.
/// </summary>
public enum BuffStacking
{
    Replace,            // Заменить
    Refresh,            // Обновить таймер
    Stack,              // Добавить стек
    Ignore              // Игнорировать
}
```

### BuffData

```csharp
/// <summary>
/// Данные баффа/дебаффа.
/// </summary>
[CreateAssetMenu(fileName = "BuffData", menuName = "Cultivation/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public BuffType type;
    public bool isDebuff;
    public Element element;         // Связанный элемент

    [Header("Effect")]
    public float value;             // Значение эффекта
    public bool isPercentage;       // true = %, false = абсолютное
    public StatType affectedStat;   // Затронутая характеристика

    [Header("Duration")]
    public BuffApplication application;
    public float duration;          // Длительность в секундах
    public int maxStacks = 1;       // Максимум стеков
    public BuffStacking stackingBehavior;

    [Header("Tick Effect (для DoT/HoT)")]
    public bool hasTickEffect;
    public float tickInterval = 1f;
    public int tickDamage;
    public int tickHealing;

    [Header("Visual")]
    public GameObject effectPrefab;
    public Color buffColor = Color.green;
    public Color debuffColor = Color.red;

    [Header("Sound")]
    public AudioClip applySound;
    public AudioClip removeSound;
}

/// <summary>
/// Затрагиваемые характеристики.
/// </summary>
public enum StatType
{
    Health,
    MaxHealth,
    Qi,
    MaxQi,
    Stamina,
    MaxStamina,
    Attack,
    Defense,
    Speed,
    CriticalChance,
    CriticalDamage,
    QiRegenRate,
    HealthRegenRate
}
```

### ActiveBuff

```csharp
/// <summary>
/// Активный бафф на цели.
/// </summary>
public class ActiveBuff
{
    public BuffData data;
    public float remainingDuration;
    public int currentStacks;
    public float tickTimer;
    public GameObject source;
    public bool isActive;

    public float Progress => data.duration > 0 ? 1f - (remainingDuration / data.duration) : 0f;
    public float TotalValue => data.value * currentStacks;
}
```

---

## Реализация

### BuffManager.cs

```csharp
// Создано: 2026-04-03
// Теоретическая реализация

using UnityEngine;
using System.Collections.Generic;

public class BuffManager : MonoBehaviour
{
    #region Configuration

    [Header("Settings")]
    [SerializeField] private int maxBuffs = 10;
    [SerializeField] private int maxDebuffs = 10;

    #endregion

    #region State

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private Dictionary<string, ActiveBuff> buffDict = new Dictionary<string, ActiveBuff>();

    // Кэшированные модификаторы
    private Dictionary<StatType, float> statModifiers = new Dictionary<StatType, float>();

    #endregion

    #region Events

    public event System.Action<ActiveBuff> OnBuffApplied;
    public event System.Action<ActiveBuff> OnBuffRemoved;
    public event System.Action<ActiveBuff, int> OnBuffStacked;
    public event System.Action<StatType, float> OnStatModified;

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        UpdateBuffs(Time.deltaTime);
    }

    #endregion

    #region Apply Buff

    /// <summary>
    /// Применяет бафф к цели.
    /// </summary>
    public bool ApplyBuff(BuffData buffData, GameObject source = null)
    {
        if (buffData == null) return false;

        // Проверяем лимит
        var buffList = buffData.isDebuff ? GetDebuffs() : GetBuffs();
        int maxCount = buffData.isDebuff ? maxDebuffs : maxBuffs;

        if (buffList.Count >= maxCount && !buffDict.ContainsKey(buffData.id))
        {
            Debug.LogWarning($"[BuffManager] Buff limit reached for {(buffData.isDebuff ? "debuffs" : "buffs")}");
            return false;
        }

        // Проверяем, есть ли уже такой бафф
        if (buffDict.TryGetValue(buffData.id, out var existingBuff))
        {
            return HandleExistingBuff(existingBuff, buffData, source);
        }

        // Создаём новый бафф
        var newBuff = new ActiveBuff
        {
            data = buffData,
            remainingDuration = buffData.duration,
            currentStacks = 1,
            tickTimer = 0f,
            source = source,
            isActive = true
        };

        activeBuffs.Add(newBuff);
        buffDict[buffData.id] = newBuff;

        // Применяем эффект к характеристикам
        ApplyStatModifier(buffData);

        OnBuffApplied?.Invoke(newBuff);

        // Звук применения
        if (buffData.applySound != null)
        {
            AudioSource.PlayClipAtPoint(buffData.applySound, transform.position);
        }

        return true;
    }

    /// <summary>
    /// Обрабатывает повторное применение баффа.
    /// </summary>
    private bool HandleExistingBuff(ActiveBuff existing, BuffData data, GameObject source)
    {
        switch (data.stackingBehavior)
        {
            case BuffStacking.Replace:
                RemoveBuff(data.id);
                return ApplyBuff(data, source);

            case BuffStacking.Refresh:
                existing.remainingDuration = data.duration;
                return true;

            case BuffStacking.Stack:
                if (existing.currentStacks < data.maxStacks)
                {
                    existing.currentStacks++;
                    existing.remainingDuration = data.duration;
                    ApplyStatModifier(data); // Добавляем ещё один модификатор
                    OnBuffStacked?.Invoke(existing, existing.currentStacks);
                    return true;
                }
                return false;

            case BuffStacking.Ignore:
            default:
                return false;
        }
    }

    #endregion

    #region Remove Buff

    /// <summary>
    /// Удаляет бафф по ID.
    /// </summary>
    public bool RemoveBuff(string buffId)
    {
        if (!buffDict.TryGetValue(buffId, out var buff))
            return false;

        // Убираем модификаторы
        RemoveStatModifier(buff.data, buff.currentStacks);

        // Удаляем из списков
        activeBuffs.Remove(buff);
        buffDict.Remove(buffId);

        OnBuffRemoved?.Invoke(buff);

        // Звук удаления
        if (buff.data.removeSound != null)
        {
            AudioSource.PlayClipAtPoint(buff.data.removeSound, transform.position);
        }

        return true;
    }

    /// <summary>
    /// Удаляет все баффы типа.
    /// </summary>
    public void RemoveAllBuffsOfType(BuffType type)
    {
        var toRemove = activeBuffs.FindAll(b => b.data.type == type);
        foreach (var buff in toRemove)
        {
            RemoveBuff(buff.data.id);
        }
    }

    /// <summary>
    /// Удаляет все баффы.
    /// </summary>
    public void RemoveAllBuffs()
    {
        var ids = new List<string>(buffDict.Keys);
        foreach (var id in ids)
        {
            RemoveBuff(id);
        }
    }

    /// <summary>
    /// Удаляет все дебаффы.
    /// </summary>
    public void RemoveAllDebuffs()
    {
        var toRemove = activeBuffs.FindAll(b => b.data.isDebuff);
        foreach (var buff in toRemove)
        {
            RemoveBuff(buff.data.id);
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Обновляет все баффы.
    /// </summary>
    private void UpdateBuffs(float deltaTime)
    {
        var expiredBuffs = new List<ActiveBuff>();

        foreach (var buff in activeBuffs)
        {
            // Обновляем длительность
            if (buff.data.application != BuffApplication.Permanent)
            {
                buff.remainingDuration -= deltaTime;

                if (buff.remainingDuration <= 0)
                {
                    expiredBuffs.Add(buff);
                    continue;
                }
            }

            // Обновляем тиковые эффекты
            if (buff.data.hasTickEffect)
            {
                buff.tickTimer += deltaTime;

                if (buff.tickTimer >= buff.data.tickInterval)
                {
                    ProcessTickEffect(buff);
                    buff.tickTimer = 0f;
                }
            }
        }

        // Удаляем истёкшие баффы
        foreach (var buff in expiredBuffs)
        {
            RemoveBuff(buff.data.id);
        }
    }

    /// <summary>
    /// Обрабатывает тиковый эффект.
    /// </summary>
    private void ProcessTickEffect(ActiveBuff buff)
    {
        // Урон
        if (buff.data.tickDamage > 0)
        {
            // var health = GetComponent<IHealth>();
            // health?.TakeDamage(buff.data.tickDamage);
        }

        // Исцеление
        if (buff.data.tickHealing > 0)
        {
            // var health = GetComponent<IHealth>();
            // health?.Heal(buff.data.tickHealing);
        }
    }

    #endregion

    #region Stat Modifiers

    /// <summary>
    /// Применяет модификатор характеристики.
    /// </summary>
    private void ApplyStatModifier(BuffData data)
    {
        if (!statModifiers.ContainsKey(data.affectedStat))
        {
            statModifiers[data.affectedStat] = 0f;
        }

        statModifiers[data.affectedStat] += data.value;
        OnStatModified?.Invoke(data.affectedStat, statModifiers[data.affectedStat]);
    }

    /// <summary>
    /// Убирает модификатор характеристики.
    /// </summary>
    private void RemoveStatModifier(BuffData data, int stacks = 1)
    {
        if (statModifiers.ContainsKey(data.affectedStat))
        {
            statModifiers[data.affectedStat] -= data.value * stacks;
            OnStatModified?.Invoke(data.affectedStat, statModifiers[data.affectedStat]);
        }
    }

    /// <summary>
    /// Получает модификатор характеристики.
    /// </summary>
    public float GetStatModifier(StatType stat)
    {
        return statModifiers.TryGetValue(stat, out var value) ? value : 0f;
    }

    /// <summary>
    /// Вычисляет итоговое значение с модификаторами.
    /// </summary>
    public float CalculateStat(StatType stat, float baseValue)
    {
        float modifier = GetStatModifier(stat);
        return baseValue + modifier;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Проверяет наличие баффа.
    /// </summary>
    public bool HasBuff(string buffId)
    {
        return buffDict.ContainsKey(buffId);
    }

    /// <summary>
    /// Проверяет наличие баффа типа.
    /// </summary>
    public bool HasBuffOfType(BuffType type)
    {
        return activeBuffs.Exists(b => b.data.type == type);
    }

    /// <summary>
    /// Получает активный бафф.
    /// </summary>
    public ActiveBuff GetBuff(string buffId)
    {
        return buffDict.TryGetValue(buffId, out var buff) ? buff : null;
    }

    /// <summary>
    /// Получает все баффы.
    /// </summary>
    public List<ActiveBuff> GetBuffs()
    {
        return activeBuffs.FindAll(b => !b.data.isDebuff);
    }

    /// <summary>
    /// Получает все дебаффы.
    /// </summary>
    public List<ActiveBuff> GetDebuffs()
    {
        return activeBuffs.FindAll(b => b.data.isDebuff);
    }

    /// <summary>
    /// Получает количество стеков баффа.
    /// </summary>
    public int GetStackCount(string buffId)
    {
        var buff = GetBuff(buffId);
        return buff?.currentStacks ?? 0;
    }

    #endregion
}
```

---

## Примеры баффов

### Положительные (Баффы)

```json
{
  "buffs": [
    {
      "id": "attack_boost_20",
      "type": "AttackBoost",
      "displayName": "Ярость",
      "description": "+20% к урону атак",
      "value": 20,
      "isPercentage": true,
      "duration": 30,
      "color": "#FFD700"
    },
    {
      "id": "defense_boost_30",
      "type": "DefenseBoost",
      "displayName": "Железная кожа",
      "description": "+30% к защите",
      "value": 30,
      "isPercentage": true,
      "duration": 60,
      "color": "#808080"
    },
    {
      "id": "qi_regen_50",
      "type": "QiRegen",
      "displayName": "Поток Ци",
      "description": "+50% регенерация Ци",
      "value": 50,
      "isPercentage": true,
      "duration": 120,
      "color": "#00BFFF"
    },
    {
      "id": "shield_100",
      "type": "Shield",
      "displayName": "Ци-щит",
      "description": "Поглощает 100 урона",
      "value": 100,
      "isPercentage": false,
      "duration": 60,
      "color": "#FFFFFF"
    }
  ]
}
```

### Отрицательные (Дебаффы)

```json
{
  "debuffs": [
    {
      "id": "poison",
      "type": "Poison",
      "displayName": "Отравление",
      "description": "Наносит 5 урона каждые 2 секунды",
      "duration": 10,
      "hasTickEffect": true,
      "tickInterval": 2,
      "tickDamage": 5,
      "color": "#800080",
      "stackingBehavior": "Refresh"
    },
    {
      "id": "burn",
      "type": "Burn",
      "displayName": "Горение",
      "description": "Наносит 8 урона каждую секунду",
      "element": "Fire",
      "duration": 5,
      "hasTickEffect": true,
      "tickInterval": 1,
      "tickDamage": 8,
      "color": "#FF4500"
    },
    {
      "id": "slow_30",
      "type": "Slow",
      "displayName": "Замедление",
      "description": "-30% скорость передвижения",
      "value": -30,
      "isPercentage": true,
      "duration": 5,
      "color": "#87CEEB"
    },
    {
      "id": "stun",
      "type": "Stun",
      "displayName": "Оглушение",
      "description": "Невозможно действовать",
      "duration": 2,
      "color": "#FFFF00"
    }
  ]
}
```

---

## UI представление

### Панель баффов

```
┌─────────────────────────────────┐
│  Баффы:                         │
│  [⚔️] [🛡️] [💧] [✨]            │
│   28s   55s  112s  60s          │
├─────────────────────────────────┤
│  Дебаффы:                       │
│  [☠️]x3 [🔥]                    │
│   5s     3s                     │
└─────────────────────────────────┘
```

### Tooltip баффа

```
┌───────────────────────────┐
│ ⚔️ Ярость                 │
├───────────────────────────┤
│ +20% к урону атак         │
│                           │
│ Осталось: 28 секунд       │
│ Стеки: x2                 │
└───────────────────────────┘
```

---

## Интеграция

### С техниками

```csharp
// В TechniqueController
private void ApplyTechniqueEffects(TechniqueData technique, GameObject target)
{
    var buffManager = target.GetComponent<BuffManager>();

    foreach (var buffId in technique.buffsToApply)
    {
        var buffData = BuffDatabase.GetBuff(buffId);
        buffManager?.ApplyBuff(buffData, gameObject);
    }
}
```

### С предметами

```csharp
// В ConsumableItem
public override void Use(GameObject user)
{
    var buffManager = user.GetComponent<BuffManager>();

    foreach (var buff in buffs)
    {
        buffManager?.ApplyBuff(buff);
    }
}
```

---

*Документ создан: 2026-04-03*
*Статус: Теоретические изыскания*
