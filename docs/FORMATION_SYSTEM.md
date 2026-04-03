# Система формаций (Formation System)

**Создано:** 2026-04-03
**Статус:** Теоретические изыскания
**Приоритет:** Высокий

---

## Обзор

Система формаций позволяет создавать магические массивы на земле, которые дают бонусы союзникам и/или дебаффы врагам в зоне действия. Формации — ключевой элемент cultivation/xianxia игр.

---

## Архитектура

### Компоненты

```
FormationSystem
├── FormationController       # Управление формациями
├── FormationData            # Данные формации (ScriptableObject)
├── FormationCore            # Ядро формации (активная)
├── FormationSlot            # Слот для усиления формации
└── FormationUI              # Интерфейс управления
```

---

## Типы формаций

### По назначению

| Тип | Описание | Примеры |
|-----|----------|---------|
| `Offensive` | Усиление атаки | Меча Дао, Тигровая формация |
| `Defensive` | Защита и сопротивление | Черепашья защита, Стена Ци |
| `Support` | Поддержка и регенерация | Источник жизни, Поток Ци |
| `Control` | Контроль врагов | Ледяная ловушка, Теневые оковы |
| `Special` | Особые эффекты | Телепорт, Призыв |

### По форме

```
○ Круг      — равномерное действие по площади
□ Квадрат   — направленное действие
◇ Ромб      — концентрированный центр
☆ Звезда    — усиленные лучи
◎ Кольцо    — действие по периметру
```

---

## Структуры данных

### FormationType

```csharp
// Создано: 2026-04-03

/// <summary>
/// Тип формации по назначению.
/// </summary>
public enum FormationType
{
    Offensive,      // Наступательная
    Defensive,      // Оборонительная
    Support,        // Поддержка
    Control,        // Контроль
    Special         // Особая
}

/// <summary>
/// Форма формации.
/// </summary>
public enum FormationShape
{
    Circle,         // Круг
    Square,         // Квадрат
    Diamond,        // Ромб
    Star,           // Звезда
    Ring            // Кольцо
}

/// <summary>
/// Ранг формации.
/// </summary>
public enum FormationRank
{
    Basic = 1,      // Базовая
    Intermediate,   // Средняя
    Advanced,       // Продвинутая
    Master,         // Мастерская
    Legendary       // Легендарная
}
```

### FormationData

```csharp
/// <summary>
/// Данные формации.
/// </summary>
[CreateAssetMenu(fileName = "FormationData", menuName = "Cultivation/Formation Data")]
public class FormationData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public FormationType type;
    public FormationShape shape;
    public FormationRank rank;
    public Element primaryElement;

    [Header("Requirements")]
    public int minCultivationLevel = 1;
    public int qiCost = 100;
    public float castTime = 2f;
    public float cooldown = 30f;

    [Header("Area")]
    public float baseRadius = 3f;
    public float maxRadius = 5f;
    public float duration = 60f;

    [Header("Effects - Allies")]
    public List<FormationEffect> allyEffects = new List<FormationEffect>();

    [Header("Effects - Enemies")]
    public List<FormationEffect> enemyEffects = new List<FormationEffect>();

    [Header("Slots")]
    public int maxSlots = 0;           // Слоты для усиления
    public List<string> allowedItems;  // Разрешённые предметы

    [Header("Visual")]
    public GameObject formationPrefab;
    public Color primaryColor = Color.cyan;
    public Color secondaryColor = Color.white;
}

/// <summary>
/// Эффект формации.
/// </summary>
[System.Serializable]
public class FormationEffect
{
    public BuffType buffType;
    public float value;
    public bool isPercentage;
    public float tickInterval;      // 0 = постоянный эффект
    public int tickValue;           // Урон/исцеление за тик
}
```

### FormationCore

```csharp
/// <summary>
/// Активная формация.
/// </summary>
public class FormationCore : MonoBehaviour
{
    public FormationData data;
    public GameObject owner;
    public Vector2 center;
    public float currentRadius;
    public float remainingDuration;
    public List<FormationSlot> slots = new List<FormationSlot>();
    public bool isActive;

    // Состояние
    private float tickTimer;
    private Collider2D[] affectedBuffer = new Collider2D[50];

    #region Lifecycle

    public void Initialize(FormationData formationData, Vector2 position, GameObject creator)
    {
        data = formationData;
        center = position;
        owner = creator;
        currentRadius = formationData.baseRadius;
        remainingDuration = formationData.duration;
        isActive = true;

        transform.position = position;

        // Создаём слоты
        for (int i = 0; i < data.maxSlots; i++)
        {
            var slot = CreateSlot(i);
            slots.Add(slot);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        UpdateDuration();
        UpdateEffects();
    }

    #endregion

    #region Effects

    private void UpdateDuration()
    {
        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0)
        {
            Deactivate();
        }
    }

    private void UpdateEffects()
    {
        // Применяем эффекты с интервалом
        tickTimer += Time.deltaTime;

        if (tickTimer >= 1f) // Базовый интервал 1 сек
        {
            ApplyEffects();
            tickTimer = 0f;
        }
    }

    private void ApplyEffects()
    {
        // Находим цели в радиусе
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            center,
            currentRadius,
            affectedBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            var target = affectedBuffer[i];
            ProcessTarget(target);
        }
    }

    private void ProcessTarget(Collider2D target)
    {
        bool isOwner = target.gameObject == owner;
        bool isAlly = IsAlly(target.gameObject);

        // Применяем эффекты
        var effects = isAlly ? data.allyEffects : data.enemyEffects;

        foreach (var effect in effects)
        {
            ApplyEffect(target.gameObject, effect);
        }
    }

    private void ApplyEffect(GameObject target, FormationEffect effect)
    {
        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null) return;

        // Создаём временный бафф
        // buffManager.ApplyBuff(CreateBuffFromEffect(effect));
    }

    private bool IsAlly(GameObject target)
    {
        // TODO: Проверка через систему фракций
        return false;
    }

    #endregion

    #region Slots

    private FormationSlot CreateSlot(int index)
    {
        // Создаём слот для усиления
        var slotGO = new GameObject($"Slot_{index}");
        slotGO.transform.SetParent(transform);

        // Позиция по кругу
        float angle = (360f / data.maxSlots) * index * Mathf.Deg2Rad;
        float slotRadius = currentRadius * 0.7f;

        slotGO.transform.localPosition = new Vector3(
            Mathf.Cos(angle) * slotRadius,
            Mathf.Sin(angle) * slotRadius,
            0f
        );

        return slotGO.AddComponent<FormationSlot>();
    }

    /// <summary>
    /// Усиливает формацию через слот.
    /// </summary>
    public bool EnhanceSlot(int slotIndex, GameObject item)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        if (slots[slotIndex].IsOccupied) return false;

        slots[slotIndex].PlaceItem(item);

        // Увеличиваем радиус/эффект
        currentRadius = Mathf.Min(currentRadius * 1.2f, data.maxRadius);

        return true;
    }

    #endregion

    #region Control

    public void Deactivate()
    {
        isActive = false;

        // Визуальный эффект закрытия
        // ...

        Destroy(gameObject, 0.5f);
    }

    public void ExtendDuration(float additionalTime)
    {
        remainingDuration += additionalTime;
    }

    #endregion
}
```

---

## Примеры формаций

### Наступательные

| Название | Эффект | Радиус | Длительность |
|----------|--------|--------|--------------|
| Меч Дао | +30% урон союзникам | 4м | 60с |
| Тигровая ярость | +20% крит. шанс | 3м | 45с |
| Огненный круг | 10 урона/сек врагам | 5м | 30с |
| Громовой шторм | Замедление -20%, 5 урона/сек | 6м | 40с |

### Оборонительные

| Название | Эффект | Радиус | Длительность |
|----------|--------|--------|--------------|
| Черепашья защита | +40% защита союзникам | 4м | 90с |
| Стена Ци | Блок 50 урона | 3м | 60с |
| Земляной барьер | +30% сопротивления | 5м | 120с |
| Небесный щит | Иммунитет к контролю | 3м | 30с |

### Поддержки

| Название | Эффект | Радиус | Длительность |
|----------|--------|--------|--------------|
| Источник жизни | 10 HP/сек союзникам | 4м | 60с |
| Поток Ци | +50% реген Ци | 5м | 90с |
| Дыхание небес | +20% ко всем статам | 3м | 45с |
| Эфирный туман | Невидимость | 2м | 20с |

### Контроля

| Название | Эффект | Радиус | Длительность |
|----------|--------|--------|--------------|
| Ледяная ловушка | Заморозка при входе | 3м | 30с |
| Теневые оковы | -50% скорость врагам | 4м | 45с |
| Ядовитый газ | 15 урона/сек, -30% атаки | 5м | 60с |
| Бездна | Вытягивание к центру | 6м | 20с |

---

## Механика слотов

### Усиление формации

Формации могут иметь слоты для усиления:

```
┌─────────────────────────────────┐
│         ⭐ Формация             │
│       ╱    │    ╲               │
│     🔮    [Слот]   💎           │
│    ╱       │       ╲            │
│  Слот    Центр     Слот         │
│                                 │
│  Предметы усиления:             │
│  💎 Spirit Stone → +20% радиус  │
│  🔮 Jade Pearl → +30% эффект    │
│  ⭐ Star Metal → +50% время     │
└─────────────────────────────────┘
```

### Система слотов

```csharp
/// <summary>
/// Слот формации для усиления.
/// </summary>
public class FormationSlot : MonoBehaviour
{
    public bool IsOccupied => placedItem != null;

    private GameObject placedItem;
    private FormationCore core;

    public void PlaceItem(GameObject item)
    {
        if (IsOccupied) return;

        placedItem = item;
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;

        ApplyEnhancement();
    }

    private void ApplyEnhancement()
    {
        if (placedItem == null || core == null) return;

        // Определяем тип усиления по предмету
        var enhancer = placedItem.GetComponent<IFormationEnhancer>();
        if (enhancer != null)
        {
            enhancer.Enhance(core);
        }
    }

    public GameObject RemoveItem()
    {
        if (!IsOccupied) return null;

        var item = placedItem;
        placedItem = null;
        return item;
    }
}
```

---

## Создание формаций

### Каст

```csharp
/// <summary>
/// Контроллер создания формаций.
/// </summary>
public class FormationController : MonoBehaviour
{
    [SerializeField] private List<FormationData> knownFormations;
    [SerializeField] private float placementRange = 10f;

    private FormationData selectedFormation;
    private bool isPlacing;
    private GameObject placementPreview;

    #region Selection

    public void SelectFormation(string formationId)
    {
        selectedFormation = knownFormations.Find(f => f.id == formationId);

        if (selectedFormation != null)
        {
            StartPlacement();
        }
    }

    #endregion

    #region Placement

    private void StartPlacement()
    {
        if (selectedFormation == null) return;

        // Проверяем требования
        if (!CanAfford(selectedFormation))
        {
            Debug.Log("Not enough Qi!");
            return;
        }

        isPlacing = true;

        // Создаём превью
        CreatePlacementPreview();
    }

    private void CreatePlacementPreview()
    {
        // Показываем превью формации
        placementPreview = Instantiate(selectedFormation.formationPrefab);
        placementPreview.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
    }

    private void Update()
    {
        if (!isPlacing) return;

        UpdatePlacementPreview();

        if (Input.GetMouseButtonDown(0))
        {
            ConfirmPlacement();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    private void UpdatePlacementPreview()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPos = transform.position;

        // Ограничиваем дистанцию
        Vector2 direction = (mousePos - playerPos).normalized;
        float distance = Vector2.Distance(playerPos, mousePos);

        if (distance > placementRange)
        {
            mousePos = playerPos + direction * placementRange;
        }

        placementPreview.transform.position = mousePos;
    }

    private void ConfirmPlacement()
    {
        Vector2 position = placementPreview.transform.position;

        // Тратим Ци
        SpendQi(selectedFormation.qiCost);

        // Создаём формацию
        CreateFormation(selectedFormation, position);

        // Очищаем превью
        EndPlacement();
    }

    private void CancelPlacement()
    {
        EndPlacement();
    }

    private void EndPlacement()
    {
        isPlacing = false;
        selectedFormation = null;

        if (placementPreview != null)
        {
            Destroy(placementPreview);
        }
    }

    #endregion

    #region Creation

    private void CreateFormation(FormationData data, Vector2 position)
    {
        var coreGO = Instantiate(data.formationPrefab, position, Quaternion.identity);
        var core = coreGO.AddComponent<FormationCore>();
        core.Initialize(data, position, gameObject);
    }

    #endregion

    #region Costs

    private bool CanAfford(FormationData data)
    {
        // var qi = GetComponent<QiController>();
        // return qi.CurrentQi >= data.qiCost;
        return true;
    }

    private void SpendQi(int amount)
    {
        // var qi = GetComponent<QiController>();
        // qi.SpendQi(amount);
    }

    #endregion
}
```

---

## UI формаций

### Выбор формации

```
┌─────────────────────────────────────┐
│         ФОРМАЦИИ                    │
├─────────────────────────────────────┤
│ [⚔️ Атака] [🛡️ Защита] [💚 Поддержка] │
├─────────────────────────────────────┤
│ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐     │
│ │ ⚔️  │ │ 🐅  │ │ 🔥  │ │ ⚡  │     │
│ │Меч  │ │Тигр │ │Огонь│ │Гром │     │
│ │Дао  │ │     │ │     │ │     │     │
│ │100 ци│ │150 ци│ │200 ци│ │250 ци│     │
│ └─────┘ └─────┘ └─────┘ └─────┘     │
└─────────────────────────────────────┘
```

### Панель активной формации

```
┌─────────────────────────────────┐
│ Меч Дао                    45с  │
│ ████████████████░░░░░░░░░       │
│                                 │
│ Радиус: 4.8м (усилено)          │
│ +30% урон союзникам             │
│                                 │
│ Слоты: [💎] [ empty ] [ empty ] │
└─────────────────────────────────┘
```

---

## Интеграция с JSON

### formations.json

```json
{
  "formations": [
    {
      "id": "dao_sword",
      "displayName": "Меч Дао",
      "description": "Усиливает урон союзников в зоне действия",
      "type": "Offensive",
      "shape": "Circle",
      "rank": 2,
      "element": "Neutral",
      "minCultivationLevel": 3,
      "qiCost": 100,
      "castTime": 2,
      "cooldown": 30,
      "baseRadius": 4,
      "maxRadius": 6,
      "duration": 60,
      "allyEffects": [
        {
          "buffType": "AttackBoost",
          "value": 30,
          "isPercentage": true
        }
      ],
      "enemyEffects": [],
      "maxSlots": 3
    },
    {
      "id": "poison_mist",
      "displayName": "Ядовитый туман",
      "description": "Наносит урон врагам и снижает их атаку",
      "type": "Control",
      "shape": "Circle",
      "rank": 3,
      "element": "Poison",
      "minCultivationLevel": 4,
      "qiCost": 150,
      "castTime": 1.5,
      "cooldown": 45,
      "baseRadius": 5,
      "maxRadius": 8,
      "duration": 60,
      "allyEffects": [],
      "enemyEffects": [
        {
          "buffType": "Poison",
          "value": 0,
          "tickInterval": 1,
          "tickValue": 15
        },
        {
          "buffType": "AttackReduction",
          "value": 30,
          "isPercentage": true
        }
      ],
      "maxSlots": 2
    }
  ]
}
```

---

*Документ создан: 2026-04-03*
*Статус: Теоретические изыскания*
