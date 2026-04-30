# 🧑‍🤝‍🧑 Система NPC — Сборка и Архитектура

**Версия:** 1.0
**Дата:** 2025-05-01
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)
**Статус:** ✅ Актуально (соответствует коду)

---

## ⚠️ Назначение документа

> Этот документ описывает **как собирается NPC** — из каких частей состоит,
> какие скрипты за что отвечают, и как данные проходят от генератора до
> экземпляра в сцене. Для теоретических основ AI см. [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md),
> для инструкций по размещению в Editor см. [docs_asset_setup/19_NPCPlacement.md](../docs_asset_setup/19_NPCPlacement.md).

---

## 📋 Краткий обзор

NPC в проекте — это **GameObject** с набором компонентов, каждый из которых
отвечает за свою подсистему. Данные NPC проходят через **три слоя**:

```
┌─────────────────────────────────────────────────────────────────────┐
│  1. ГЕНЕРАЦИЯ (NPCGenerator / NPCPresetData)                       │
│     → GeneratedNPC (POCO) или .asset пресет                        │
│                                                                     │
│  2. ИНИЦИАЛИЗАЦИЯ (NPCController.InitializeFromGenerated)          │
│     → NPCState (runtime-состояние)                                 │
│                                                                     │
│  3. РАЗМЕЩЕНИЕ (NPCSceneSpawner / Phase19NPCPlacement)             │
│     → GameObject + компоненты в активной сцене                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🧱 Компоненты NPC (GameObject)

Каждый NPC в сцене состоит из **обязательных** компонентов:

```
NPC_[Имя] (GameObject)
├── Transform                  — позиция (Z=0 обязательно)
├── NPCController              — 🎯 главный контроллер, хранит NPCState
├── NPCAI                      — 🧠 принятие решений, поведение
├── BodyController             — 💪 система тела (Kenshi-style HP)
├── QiController               — ✨ система Ци (накопление, проводимость)
├── TechniqueController        — ⚔️ техники культивации
├── NPCVisual                  — 👁 визуал (спрайт, имя, HP-бар)
├── NPCInteractable            — 🖱 взаимодействие с Player
├── Rigidbody2D                — 🏃 физика (Dynamic, gravity=0)
├── CircleCollider2D (solid)   — 🔵 физический коллайдер (r=0.5)
└── CircleCollider2D (trigger) — 🔵 зона взаимодействия (r=1.5)
```

### Назначение каждого компонента

| Компонент | Скрипт | Ответственность |
|-----------|--------|----------------|
| **NPCController** | `Scripts/NPC/NPCController.cs` (640 строк) | Единый центр NPC. Хранит `NPCState`, инициализирует из `GeneratedNPC` или `NPCPresetData`, управляет смертью/уроном/отношениями/сохранением. Авторегистрируется в `WorldController` (GAP-4). |
| **NPCAI** | `Scripts/NPC/NPCAI.cs` (640 строк) | Принятие решений с взвешенными вероятностями. 15 AI-состояний. Модификаторы от `PersonalityTrait [Flags]`. Система угроз с затуханием (-2/tick каждые 5с). Визуальная обратная связь через NPCVisual. |
| **BodyController** | `Scripts/Body/BodyController.cs` | Kenshi-style система тела. Двойной HP (Red/Black) по 11 частей тела. Устанавливается через `SetCultivationLevel()`. |
| **QiController** | `Scripts/Qi/QiController.cs` | Накопление, проводимость, плотность Ци. Формула: `1000 × 1.1^totalSubLevels`. Conductivity = maxQi / 360. |
| **TechniqueController** | `Scripts/Combat/TechniqueController.cs` | Управление техниками культивации. Загружает техники по ID. |
| **NPCVisual** | `Scripts/NPC/NPCVisual.cs` (606 строк) | Спрайт по роли (или программный гуманоид 64×64). Плавающее имя (TextMeshPro) цветом по Attitude. HP-бар (Slider). Sorting Layer «Objects», order 50. Unlit-шейдер. |
| **NPCInteractable** | `Scripts/NPC/NPCInteractable.cs` (473 строк) | Расширяет `Interactable` для `InteractionController`. 12 типов взаимодействия: Talk, Trade, Attack, Gift, Flatter, Learn, Teach, Cultivate, Spar, Threaten, Insult, Ask. |
| **Rigidbody2D** | Built-in | Dynamic, Gravity Scale = 0, Freeze Rotation, Linear Damping = 5 |
| **CircleCollider2D** (solid) | Built-in | Физический коллайдер. isTrigger=false, radius=0.5 |
| **CircleCollider2D** (trigger) | Built-in | Зона взаимодействия. isTrigger=true, radius=1.5 (добавляется NPCInteractable) |

---

## 🔄 Пайплайн сборки NPC

### Способ 1: Через NPCGenerator (рекомендуется)

```
NPCGenerator.Generate(params, rng)
    ↓
GeneratedNPC (POCO — все данные NPC)
    ↓
NPCSceneSpawner.SpawnNPCInScene(role, level, position)
    ├── new GameObject("NPC_[Имя]")
    ├── AddComponent<NPCController>
    ├── AddComponent<NPCAI>
    ├── AddComponent<BodyController>
    ├── AddComponent<QiController>
    ├── AddComponent<TechniqueController>
    ├── AddComponent<NPCVisual>
    ├── AddComponent<NPCInteractable>
    ├── AddComponent<Rigidbody2D> + CircleCollider2D
    ├── controller.InitializeFromGenerated(generated)  ← ключевая точка
    ├── visual.SetSpriteByRole(role)
    ├── visual.UpdateVisualFromState()
    ├── interactable.SetNPCRole(role)
    └── SetInitialAIState(ai, role)
```

### Способ 2: Через NPCPresetData (.asset)

```
NPCPresetData (ScriptableObject .asset)
    ↓
NPCController.ApplyPreset()
    ├── state.Name = preset.nameTemplate
    ├── state.CultivationLevel = (CultivationLevel)preset.cultivationLevel
    ├── state.Attitude = ValueToAttitude(preset.baseDisposition)
    ├── bodyController.SetCultivationLevel(preset.cultivationLevel)
    └── qiController.SetCultivationLevel(preset.cultivationLevel, subLevel)
```

### Способ 3: Через Phase 19 (автоматический)

```
Phase19NPCPlacement.Execute()
    ├── для каждого NPC_PLACEMENTS[]:
    │   ├── NPCSceneSpawner.SpawnNPCInScene(role, level, position)
    │   └── if Guard: SetupGuardPatrol(controller, position)
    └── IsNeeded(): true только если нет объектов с тегом "NPC"
```

---

## 📦 GeneratedNPC — структура данных генератора

`GeneratedNPC` — это POCO (Plain Old C# Object), результат работы `NPCGenerator.Generate()`.

```csharp
public class GeneratedNPC
{
    // Identity
    public string id;              // "npc_[role]_[level]_[random]"
    public string nameRu;          // "Страж #437"
    public string nameEn;
    public NPCRole role;           // Monster, Guard, Merchant, Cultivator, Elder, Disciple, Enemy, Passerby
    public NPCCategory category;   // Temp, Plot, Unique
    public int age;                // 18 + level*5 + random(0..10)

    // Species
    public SpeciesData species;
    public SoulType soulType;      // Character, Beast, Spirit, ...
    public Morphology morphology;  // Humanoid, Quadruped, ...
    public BodyMaterial bodyMaterial; // Organic, Metal, ...

    // Cultivation
    public int cultivationLevel;   // 0-10
    public int cultivationSubLevel; // 0-9
    public CoreQuality coreQuality; // Fragmented..Transcendent

    // Stats
    public int vitality, strength, agility, constitution, intelligence;

    // Qi
    public long maxQi;             // 1000 × 1.1^totalSubLevels
    public long currentQi;
    public float conductivity;     // maxQi / 360

    // Body
    public List<GeneratedBodyPart> bodyParts; // 11 гуманоидных частей

    // Combat
    public int baseDamage, baseDefense;

    // Techniques
    public List<string> techniqueIds;

    // Equipment
    public List<string> equipmentIds;

    // AI
    public Attitude baseAttitude;            // Hatred..SwornAlly
    public PersonalityTrait basePersonality; // [Flags] Aggressive|Cautious|...
    public float aggressionLevel;            // 0..1
}
```

---

## 📐 NPCState — runtime-состояние

`NPCState` — это объект, хранящий текущее состояние NPC. Создаётся в `NPCController.Awake()`.

```csharp
public class NPCState
{
    // Identity
    public string NpcId;          // GUID
    public string Name;
    public int Age;

    // Cultivation
    public CultivationLevel CultivationLevel;  // None..Ten
    public int SubLevel;                       // 1-9
    public float CultivationProgress;          // 0-100%
    public MortalStage MortalStage;

    // Resources
    public long CurrentQi, MaxQi;
    public int CurrentHealth, MaxHealth;
    public float CurrentStamina, MaxStamina;

    // Body
    public float BodyStrength, BodyDefense, Constitution;
    public int Lifespan, MaxLifespan;

    // Mental
    public float Willpower, Perception, Intelligence, Wisdom;

    // Personality
    public Attitude Attitude;                    // Hatred..SwornAlly
    public PersonalityTrait Personality;         // [Flags]
    public float[] ElementAffinities;            // индекс = Element enum

    // Status
    public bool IsAlive, IsInCombat, IsInSect;
    public string SectId, CurrentLocation;

    // AI State
    public NPCAIState CurrentAIState;
    public string TargetId;
    public float StateTimer;
}
```

### Формулы расчёта ресурсов (в NPCController)

| Ресурс | Формула |
|--------|---------|
| MaxHealth | `100 + (int)CultivationLevel × 500 + SubLevel × 50` |
| MaxQi | `1000 × 1.1^((level-1)×10 + subLevel)` (смертный: 100) |
| MaxLifespan | `80 + (int)CultivationLevel × 100` |
| Conductivity | `MaxQi / 360` |

---

## 🧠 NPCAI — система поведения

### 15 AI-состояний

| Состояние | Описание | Когда |
|-----------|----------|-------|
| Idle | Бездействие | По умолчанию, после отдыха |
| Wandering | Случайное блуждание | Monster, Enemy по умолчанию |
| Patrolling | Патруль по точкам | Guard по умолчанию |
| Following | Следование за целью | При приказе |
| Fleeing | Бегство | HP < 20% + cautiousness > aggressiveness |
| Attacking | Атака цели | Угроза + aggressiveness > cautiousness |
| Defending | Защита | — |
| Meditating | Медитация | — |
| Cultivating | Культивация (восстановление Ци) | Cultivator, Disciple по умолчанию |
| Resting | Отдых (восст. HP/Stamina) | После блуждания |
| Trading | Торговля | Merchant по умолчанию |
| Talking | Разговор | Социальный NPC |
| Working | Работа | Ambitious NPC |
| Searching | Поиск | — |
| Guarding | Охрана | — |

### PersonalityTrait [Flags] — влияние на поведение

| Флаг | Значение | Эффект на веса AI |
|------|----------|-------------------|
| Aggressive | 1 | +50% patrol, −30% rest |
| Cautious | 2 | +50% rest, −30% patrol |
| Treacherous | 4 | Если Attitude < Neutral: −50% talk, +40% patrol |
| Ambitious | 8 | +30% cultivate, +30% patrol, +20% work |
| Loyal | 16 | −50% idle, +30% work, +20% talk |
| Pacifist | 32 | −50% patrol, +30% rest, +20% cultivate |
| Curious | 64 | +40% wander, +30% talk |
| Vengeful | 128 | +30% patrol |

### Система угроз

- `AddThreat(sourceId, level)` — добавить угрозу (при получении урона: `damage × 0.5`)
- Затухание: каждые 5 сек, −2 threat/tick
- Угроза > 50 → `knownTargets.Add(sourceId)`, `OnTargetAcquired`
- Aggressiveness > Cautiousness → атака, иначе бегство

---

## 👁 NPCVisual — визуальное отображение

### Структура дочерних объектов

```
NPC_[Имя]
├── Visual (SpriteRenderer)          — спрайт NPC
│   ├── Sorting Layer: "Objects"
│   ├── Sorting Order: 50
│   └── Material: Sprite-Unlit-Default (виден без Light2D)
├── NameLabel (Canvas + TextMeshPro) — имя + уровень
│   ├── Sorting Layer: "UI", Order: 100
│   ├── Цвет: по Attitude (красный→белый→зелёный→золотой)
│   └── Billboard: поворот к камере каждый кадр
└── HealthBar (Canvas + Slider)      — полоска HP
    ├── Sorting Layer: "UI", Order: 99
    ├── Background: тёмно-красный
    ├── Fill: зелёный
    └── Billboard: поворот к камере каждый кадр
```

### Загрузка спрайтов

| Способ | Путь | Условие |
|--------|------|---------|
| Editor | `Assets/Sprites/Characters/NPC/npc_[role].png` | `#if UNITY_EDITOR` |
| Runtime | `Resources/Sprites/NPC/[role].ToLower()` | Build |
| Fallback | `CreateHumanoidSprite()` (64×64 программный) | Спрайт не найден |

**PPU = 64** — автоматически корректируется через `EnsureSpritePPU()`.

### Цвета имени по Attitude

| Attitude | Цвет | RGB |
|----------|------|-----|
| Hatred | Красный | (1.0, 0.0, 0.0) |
| Hostile | Красный | (1.0, 0.2, 0.2) |
| Unfriendly | Оранжевый | (1.0, 0.5, 0.2) |
| Neutral | Белый | (1.0, 1.0, 1.0) |
| Friendly | Зелёный | (0.3, 1.0, 0.3) |
| Allied | Голубой | (0.2, 0.8, 1.0) |
| SwornAlly | Золотой | (1.0, 0.85, 0.0) |

---

## 🖱 NPCInteractable — типы взаимодействия

12 типов взаимодействия, доступных через `InteractionController`:

| Тип | Условие доступности | Δ Relation |
|-----|---------------------|------------|
| Talk | Всегда | +1 |
| Trade | NPCRole = Merchant | 0 |
| Attack | Attitude < Neutral | −50 |
| Gift | Attitude ≥ Friendly | +10 |
| Flatter | Attitude ≥ Friendly | +5 |
| Learn | Cultivator или Elder | +5 |
| Teach | Elder | +20 |
| Cultivate | Cultivator или Elder | +5 |
| Spar | Guard, Cultivator или Disciple | +3 |
| Threaten | Всегда | −15 |
| Insult | Всегда | −20 |
| Ask | Всегда | +1 |

> **Заглушки:** Большинство взаимодействий — stub-реализации, меняющие только отношение.
> Полная реализация (торговля, диалоги, спарринг) — в будущих итерациях.

---

## 💾 Сохранение NPC

`NPCController` поддерживает сериализацию через `GetSaveData()` / `LoadSaveData()`:

```
NPCSaveData (сериализуемый):
├── NpcId, Name, Age
├── CultivationLevel, SubLevel, Progress, MortalStage
├── Resources: CurrentQi, MaxQi, CurrentHealth, MaxHealth, Stamina
├── Body: Strength, Defense, Constitution, Lifespan, MaxLifespan
├── Mental: Willpower, Perception, Intelligence, Wisdom
├── Personality: AttitudeValue (int), PersonalityFlags (int), ElementAffinities[]
├── Skills: SkillLevelEntry[] (serializable array вместо Dictionary)
├── Status: IsAlive, SectId, CurrentLocation
└── AI: CurrentAIState (int), TargetId
```

> **SkillLevelData** — обёртка для сериализации `Dictionary<string,float>` в массив
> `SkillLevelEntry[]` (т.к. JsonUtility не сериализует Dictionary).

---

## 📊 Отношения и Attitude

### Attitude (отношение к Player, числовое -100..+100)

| Диапазон | Attitude | Поведение |
|----------|----------|-----------|
| 80..100 | SwornAlly | Самопожертвование |
| 50..79 | Allied | Лояльность |
| 10..49 | Friendly | Помощь, торговля |
| −9..9 | Neutral | Безразличие |
| −20..−10 | Unfriendly | Избегание |
| −50..−21 | Hostile | Атака при провокации |
| −100..−51 | Hatred | Атака без предупреждения |

### RelationshipController

Отдельный компонент (`Scripts/NPC/RelationshipController.cs`, 534 строки):
- Хранит `Dictionary<string, RelationshipRecord>` — отношения ко всем целям
- Затухание: через игровое время (TimeController), 7 дней до начала, −1/день к нейтральному
- Флаги family/sworn/master/disciple — **без затухания**
- `CalculateAttitude(targetId)` — замена устаревшего `GetDisposition()`

---

## 🗂 NPCPresetData — ScriptableObject-пресеты

Файл: `Scripts/Data/ScriptableObjects/NPCPresetData.cs` (203 строки)
JSON: `Assets/Data/JSON/npc_presets.json` (937 строк, 15 пресетов)
Документация: [docs_asset_setup/07_NPCPresetData.md](../docs_asset_setup/07_NPCPresetData.md)

15 пресетов: villager, guard, cultivator, bandit, wolf, spirit_tiger, merchant, sect_elder,
disciple, innkeeper, blacksmith, alchemist, hermit, traveling_monk, beast_tamer.

---

## 📂 Файловая структура NPC

### Runtime-скрипты (`Scripts/NPC/`)

| Файл | Строки | Назначение |
|------|--------|------------|
| `NPCController.cs` | 640 | Главный контроллер |
| `NPCAI.cs` | 640 | AI и поведение |
| `NPCVisual.cs` | 606 | Визуал |
| `NPCInteractable.cs` | 473 | Взаимодействие |
| `NPCData.cs` | 331 | NPCState, NPCSaveData, NPCAIState |
| `RelationshipController.cs` | 534 | Отношения |

### Generator (`Scripts/Generators/`)

| Файл | Строки | Назначение |
|------|--------|------------|
| `NPCGenerator.cs` | 468 | Генерация GeneratedNPC |

### Editor (`Scripts/Editor/`)

| Файл | Строки | Назначение |
|------|--------|------------|
| `NPCSceneSpawner.cs` | 294 | Спавн в сцену (hotkeys + menu) |
| `SceneBuilder/Phase19NPCPlacement.cs` | 113 | Авто-размещение (FullSceneBuilder) |

### Data

| Файл | Строки | Назначение |
|------|--------|------------|
| `Data/ScriptableObjects/NPCPresetData.cs` | 203 | SO пресет |
| `Data/JSON/npc_presets.json` | 937 | 15 пресетов |
| `Examples/NPCAssemblyExample.cs` | 407 | Демо сборки L6 Cultivator |

### Core Enums (`Scripts/Core/Enums.cs`)

- `CultivationLevel` (None..Ten, 1-10)
- `Attitude` (Hatred..SwornAlly)
- `PersonalityTrait [Flags]` (Aggressive=1..Vengeful=128)
- `NPCRole` (Monster, Guard, Merchant, Cultivator, Passerby, Elder, Disciple, Enemy)
- `NPCCategory` (Temp, Plot, Unique)
- `[Obsolete] Disposition` — заменён на Attitude + PersonalityTrait (Fix-07)

---

## 🔗 Связанная документация

### Основная документация (docs/)

| Документ | Описание | Связь |
|----------|----------|-------|
| [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | AI NPC: Spinal Controller, Behaviour Tree, Neural Router | Теоретическая основа AI |
| [GENERATORS_SYSTEM.md](./GENERATORS_SYSTEM.md) | Генераторы: NPC, техники, предметы | NPCGenerator |
| [QI_SYSTEM.md](./QI_SYSTEM.md) | Ци: накопление, проводимость, формула MaxQi | QiController |
| [BODY_SYSTEM.md](./BODY_SYSTEM.md) | Kenshi-style система тела | BodyController |
| [COMBAT_SYSTEM.md](./COMBAT_SYSTEM.md) | Боевая система, 11-слойный пайплайн | TakeDamage, TechniqueController |
| [EQUIPMENT_SYSTEM.md](./EQUIPMENT_SYSTEM.md) | Экипировка NPC | equipmentIds |
| [FACTION_SYSTEM.md](./FACTION_SYSTEM.md) | Фракции и отношения | factionId |
| [SAVE_SYSTEM.md](./SAVE_SYSTEM.md) | Сохранение/загрузка | NPCSaveData |
| [!hotkeys.md](./!hotkeys.md) | Горячие клавиши | NPC hotkeys (Ctrl+N и др.) |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура проекта | SceneBuilder, модули |

### Инструкции для Unity Editor (docs_asset_setup/)

| Документ | Описание |
|----------|----------|
| [19_NPCPlacement.md](../docs_asset_setup/19_NPCPlacement.md) | Размещение NPC: hotkeys, Phase 19, ручное создание |
| [07_NPCPresetData.md](../docs_asset_setup/07_NPCPresetData.md) | Пресеты NPC: поля, примеры, .asset файлы |
| [07_NPCPresetData_SemiAuto.md](../docs_asset_setup/07_NPCPresetData_SemiAuto.md) | Полу-авто генерация пресетов |

### Чекпоинты

| Чекпоинт | Описание |
|----------|----------|
| [04_30_npc_generation_and_placement.md](../checkpoints/04_30_npc_generation_and_placement.md) | GAP-анализ и план внедрения NPC |
| [04_30_npc_implementation_plan.md](../checkpoints/04_30_npc_implementation_plan.md) | Детальный 7-шаговый план (все шаги выполнены) |
| [03_31_npc_integration.md](../checkpoints/03_31_npc_integration.md) | Интеграция NPCGenerator→NPCController |
| [04_10_fix07_npc_quest_dialogue.md](../checkpoints/04_10_fix07_npc_quest_dialogue.md) | Fix-07: Attitude+PersonalityTrait, SaveData, угрозы |

---

## 📝 История изменений

| Дата | Изменение |
|------|-----------|
| 2026-03-30 | Начальная реализация NPCController, NPCAI, NPCData |
| 2026-04-11 | Fix-07: Disposition→Attitude+PersonalityTrait, SaveData, угрозы, lifespan |
| 2026-04-30 | GAP-4: авторегистрация в WorldController; NPCVisual, NPCInteractable, NPCSceneSpawner, Phase19NPCPlacement |

---

*Документ создан: 2025-05-01*
