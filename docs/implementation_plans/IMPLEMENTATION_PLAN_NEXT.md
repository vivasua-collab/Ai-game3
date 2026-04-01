# 🚀 План внедрения: Следующие этапы

**Версия:** 2.0  
**Дата:** 2026-04-01  
**Статус:** Готов к выполнению

---

## 📊 Анализ текущего состояния

### ✅ Завершено (Phase 1-7 + Дополнительно)

| Категория | Компоненты | Файлов | Статус |
|-----------|------------|--------|--------|
| Core | Constants, Enums, GameSettings | 3 | ✅ |
| ScriptableObjects | CultivationLevelData, MortalStageData, ElementData, SpeciesData, TechniqueData, ItemData, NPCPresetData | 7 | ✅ |
| Combat | DamageCalculator, QiBuffer, DefenseProcessor, LevelSuppression, TechniqueCapacity, TechniqueController | 6 | ✅ |
| Body | BodyController, BodyPart, BodyDamage | 3 | ✅ |
| Qi | QiController | 1 | ✅ |
| World | WorldController, TimeController, LocationController, EventController, FactionController | 5 | ✅ |
| NPC | NPCController, NPCAI, NPCData, NPCGenerator, RelationshipController | 5 | ✅ |
| Inventory | InventoryController, EquipmentController, CraftingController, MaterialSystem | 4 | ✅ |
| Save | SaveManager, SaveDataTypes, SaveFileHandler | 3 | ✅ |
| UI | UIManager, HUDController, MenuUI, CombatUI, DialogUI | 5 | ✅ |
| Generators | TechniqueGenerator, WeaponGenerator, ArmorGenerator, ConsumableGenerator, NPCGenerator, SeededRandom | 6 | ✅ |
| Player | PlayerController, PlayerVisual | 2 | ✅ |
| **ИТОГО** | | **50 файлов** | ✅ |

### ❌ Отсутствует (требуется от пользователя)

| Компонент | Количество | Статус |
|-----------|------------|--------|
| CultivationLevelData .asset | 10 файлов | ❌ |
| MortalStageData .asset | 6 файлов | ❌ |
| ElementData .asset | 7 файлов | ❌ |
| SpeciesData .asset | 5+ файлов | ❌ |
| Сцена Main.unity | 1 файл | ❌ |
| Префаб Player.prefab | 1 файл | ❌ |
| Техники (примеры) | 10+ файлов | ❌ |

---

## 🎯 ЭТАП 1: Unity Assets Creation (ПРИОРИТЕТ 1)

**Исполнитель:** User (в Unity Editor)  
**Время:** 2-3 часа  
**Документация:** `docs/asset_setup/`

### 1.1 CultivationLevelData (10 уровней)

**Путь:** `Assets/Data/CultivationLevels/`

| Файл | Уровень | nameRu |
|------|---------|--------|
| Level1_AwakenedCore.asset | 1 | Пробуждённое Ядро |
| Level2_LifeFlow.asset | 2 | Течение Жизни |
| Level3_InternalFire.asset | 3 | Пламя Внутреннего Огня |
| Level4_BodySpiritUnion.asset | 4 | Объединение Тела и Духа |
| Level5_HeartOfHeaven.asset | 5 | Сердце Небес |
| Level6_VeilBreaker.asset | 6 | Разрыв Пелены |
| Level7_EternalRing.asset | 7 | Вечное Кольцо |
| Level8_VoiceOfHeaven.asset | 8 | Глас Небес |
| Level9_ImmortalCore.asset | 9 | Бессмертное Ядро |
| Level10_Ascension.asset | 10 | Вознесение |

**Инструкция:** `docs/asset_setup/01_CultivationLevelData.md`

### 1.2 MortalStageData (6 этапов)

**Путь:** `Assets/Data/MortalStages/`

| Файл | Этап | nameRu |
|------|------|--------|
| Stage1_Newborn.asset | Newborn | Новорождённый |
| Stage2_Child.asset | Child | Ребёнок |
| Stage3_Adult.asset | Adult | Взрослый |
| Stage4_Mature.asset | Mature | Зрелый |
| Stage5_Elder.asset | Elder | Старец |
| Stage9_Awakening.asset | Awakening | Пробуждение |

**Инструкция:** `docs/asset_setup/02_MortalStageData.md`

### 1.3 ElementData (7 элементов)

**Путь:** `Assets/Data/Elements/`

| Файл | Элемент | nameRu |
|------|---------|--------|
| Element_Neutral.asset | Neutral | Нейтральный |
| Element_Fire.asset | Fire | Огонь |
| Element_Water.asset | Water | Вода |
| Element_Earth.asset | Earth | Земля |
| Element_Air.asset | Air | Воздух |
| Element_Lightning.asset | Lightning | Молния |
| Element_Void.asset | Void | Пустота |

**Инструкция:** `docs/asset_setup/03_ElementData.md`

### 1.4 Сцена Main.unity

**Путь:** `Assets/Scenes/Main.unity`

**Структура:**
```
Main (сцена)
├── Main Camera
├── Directional Light
├── GameManager
│   ├── WorldController
│   ├── TimeController
│   ├── LocationController
│   ├── EventController
│   ├── FactionController
│   └── SaveManager
├── Player
├── EventSystem
└── GameUI (Canvas)
```

**Инструкция:** `docs/asset_setup/04_BasicScene.md`

### 1.5 Префаб Player.prefab

**Путь:** `Assets/Prefabs/Player/Player.prefab`

**Компоненты:**
- PlayerController
- BodyController
- QiController
- InventoryController
- EquipmentController
- TechniqueController

**Инструкция:** `docs/asset_setup/05_PlayerSetup.md`

---

## 🎯 ЭТАП 2: Combat System Integration (ПРИОРИТЕТ 2)

**Исполнитель:** AI (код) + User (тестирование)  
**Время:** 3-4 часа

### 2.1 CombatManager (новый компонент)

**Задача:** Создать центральный менеджер боя

**Файл:** `Scripts/Combat/CombatManager.cs`

**Функции:**
- Инициация боя
- Управление очередностью ходов
- Координация атак и защит
- Завершение боя

### 2.2 Интеграция DamageCalculator ↔ BodyController

**Задача:** Связать расчёт урона с системой тела

**Изменения:**
```csharp
// В CombatManager:
public void ApplyDamageToTarget(GameObject target, DamageInfo damage)
{
    var bodyController = target.GetComponent<BodyController>();
    if (bodyController != null)
    {
        var result = DamageCalculator.CalculateFinalDamage(damage, target);
        bodyController.TakeDamageRandom(result.FinalDamage);
    }
}
```

### 2.3 Интеграция QiBuffer ↔ QiController

**Задача:** Добавить буфер Ци в QiController

**Изменения в QiController:**
```csharp
// Добавить поле:
private QiBuffer qiBuffer;

// В RecalculateStats():
qiBuffer = new QiBuffer(maxQiCapacity, qiDensity);

// Метод поглощения урона:
public DamageResult AbsorbDamage(float damage, Element element)
{
    return qiBuffer.AbsorbDamage(damage, element, currentQi);
}
```

### 2.4 HitDetector (новый компонент)

**Задача:** Определение попаданий

**Файл:** `Scripts/Combat/HitDetector.cs`

**Функции:**
- Определение цели атаки
- Проверка Line of Sight
- Определение части тела

---

## 🎯 ЭТАП 3: Generator System Integration (ПРИОРИТЕТ 3)

**Исполнитель:** AI (код) + User (тестирование)  
**Время:** 2-3 часа

### 3.1 GeneratorRegistry (новый компонент)

**Задача:** Централизованный доступ к генераторам

**Файл:** `Scripts/Generators/GeneratorRegistry.cs`

```csharp
public class GeneratorRegistry : MonoBehaviour
{
    public static GeneratorRegistry Instance { get; private set; }
    
    public TechniqueGenerator TechniqueGenerator { get; private set; }
    public WeaponGenerator WeaponGenerator { get; private set; }
    public ArmorGenerator ArmorGenerator { get; private set; }
    public ConsumableGenerator ConsumableGenerator { get; private set; }
    public NPCGenerator NPCGenerator { get; private set; }
    
    // Инициализация с сидом мира
    public void Initialize(long worldSeed) { ... }
}
```

### 3.2 Интеграция с WorldController

**Задача:** Инициализация генераторов при старте мира

**Изменения в WorldController:**
```csharp
private void InitializeWorld()
{
    // ... существующий код ...
    
    // Инициализация генераторов
    var registry = GetComponent<GeneratorRegistry>();
    if (registry != null)
    {
        registry.Initialize(worldSeed);
    }
}
```

### 3.3 Интеграция NPCGenerator ↔ NPCController

**Задача:** Создание NPC через генератор

**Новый метод в NPCController:**
```csharp
public static NPCController CreateFromGenerated(NPCGenerateResult generated, Transform parent)
{
    // Создание GameObject
    // Добавление компонентов
    // Настройка параметров
}
```

---

## 🎯 ЭТАП 4: Stat Development System (ПРИОРИТЕТ 4)

**Исполнитель:** AI (код)  
**Время:** 2-3 часа

### 4.1 StatDevelopment.cs (новый компонент)

**Задача:** Система развития характеристик

**Файл:** `Scripts/Core/StatDevelopment.cs`

**Механика:**
```
Действие → AddDelta(stat, amount) → VirtualDelta[stat] += amount
    ↓
Сон 4+ часов → ConsolidateSleep(hours)
    ↓
if (delta >= threshold) Stat += 1
```

### 4.2 Интеграция с PlayerController

**Добавить поля:**
```csharp
[SerializeField] private StatDevelopment statDevelopment;

public void AddStatExperience(StatType stat, float amount)
{
    statDevelopment?.AddDelta(stat, amount);
}
```

### 4.3 SleepSystem.cs (новый компонент)

**Задача:** Обработка сна и консолидации

**Файл:** `Scripts/Player/SleepSystem.cs`

---

## 🎯 ЭТАП 5: UI Enhancement (ПРИОРИТЕТ 5)

**Исполнитель:** AI (код) + User (Unity UI)  
**Время:** 3-4 часа

### 5.1 HUD улучшения

**Новые элементы:**
- Полоска опыта культивации
- Индикатор уровня культивации
- Панель быстрых слотов техник
- Миникарта (заглушка)

### 5.2 Inventory UI

**Файл:** `Scripts/UI/InventoryUI.cs`

**Функции:**
- Отображение сетки инвентаря
- Drag & Drop предметов
- Контекстное меню

### 5.3 Character Panel UI

**Файл:** `Scripts/UI/CharacterPanelUI.cs`

**Функции:**
- Отображение частей тела
- Состояние HP
- Экипировка

---

## 🎯 ЭТАП 6: Testing & Balance (ПРИОРИТЕТ 6)

**Исполнитель:** AI (анализ) + User (тестирование)  
**Время:** 2-4 часа

### 6.1 Unit Tests

**Файл:** `Scripts/Tests/CombatTests.cs`

**Тесты:**
- DamageCalculator: корректность формул
- QiBuffer: поглощение урона
- LevelSuppression: таблица подавления

### 6.2 Integration Tests

**Сценарии:**
1. Создание NPC через генератор
2. Бой Player vs NPC
3. Прорыв уровня культивации
4. Сохранение/загрузка

### 6.3 Balance Verification

**Проверка формул:**
- Урон техник по уровням
- Ёмкость ядра по под-уровням
- Время медитации

---

## 📋 Зависимости между этапами

```
Этап 1 (Assets)
    ↓
Этап 2 (Combat) ←── зависит от 1
    ↓
Этап 3 (Generators) ←── зависит от 1, 2
    ↓
Этап 4 (Stats) ←── зависит от 1, 2
    ↓
Этап 5 (UI) ←── зависит от 1-4
    ↓
Этап 6 (Testing) ←── зависит от 1-5
```

---

## 📊 Оценка времени

| Этап | Исполнитель | Время | Зависимость |
|------|-------------|-------|-------------|
| 1. Assets Creation | User | 2-3 часа | — |
| 2. Combat Integration | AI + User | 3-4 часа | Этап 1 |
| 3. Generator Integration | AI + User | 2-3 часа | Этап 1, 2 |
| 4. Stat Development | AI | 2-3 часа | Этап 1, 2 |
| 5. UI Enhancement | AI + User | 3-4 часа | Этап 1-4 |
| 6. Testing & Balance | AI + User | 2-4 часа | Этап 1-5 |
| **ИТОГО** | | **14-21 час** | |

---

## ✅ Чек-лист готовности

### Этап 1 (User):

**CultivationLevelData:**
- [ ] Level1_AwakenedCore.asset
- [ ] Level2_LifeFlow.asset
- [ ] Level3_InternalFire.asset
- [ ] Level4_BodySpiritUnion.asset
- [ ] Level5_HeartOfHeaven.asset
- [ ] Level6_VeilBreaker.asset
- [ ] Level7_EternalRing.asset
- [ ] Level8_VoiceOfHeaven.asset
- [ ] Level9_ImmortalCore.asset
- [ ] Level10_Ascension.asset

**MortalStageData:**
- [ ] Stage1_Newborn.asset
- [ ] Stage2_Child.asset
- [ ] Stage3_Adult.asset
- [ ] Stage4_Mature.asset
- [ ] Stage5_Elder.asset
- [ ] Stage9_Awakening.asset

**ElementData:**
- [ ] Element_Neutral.asset
- [ ] Element_Fire.asset
- [ ] Element_Water.asset
- [ ] Element_Earth.asset
- [ ] Element_Air.asset
- [ ] Element_Lightning.asset
- [ ] Element_Void.asset

**Scene & Prefabs:**
- [ ] Main.unity с GameManager
- [ ] Player.prefab

### Этап 2 (AI):
- [ ] CombatManager.cs
- [ ] HitDetector.cs
- [ ] Интеграция DamageCalculator
- [ ] Интеграция QiBuffer

### Этап 3 (AI):
- [ ] GeneratorRegistry.cs
- [ ] Интеграция с WorldController
- [ ] Метод NPCController.CreateFromGenerated

### Этап 4 (AI):
- [ ] StatDevelopment.cs
- [ ] SleepSystem.cs
- [ ] Интеграция с PlayerController

### Этап 5 (AI + User):
- [ ] InventoryUI.cs
- [ ] CharacterPanelUI.cs
- [ ] HUD улучшения

### Этап 6 (AI + User):
- [ ] CombatTests.cs
- [ ] Integration Tests
- [ ] Balance Verification

---

## 🚀 Следующий шаг

**НЕМЕДЛЕННО:** Создать .asset файлы в Unity Editor

**Инструкция:** `docs/asset_setup/README.md`

**После завершения Этапа 1:**
Сообщить о готовности assets для начала Этапа 2 (Combat Integration)

---

*План создан: 2026-04-01*
*Версия: 2.0*
