# 🧑‍🤝‍🧑 NPC: Генерация и размещение на тестовой поляне

**Дата:** 2026-04-30
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)
**Статус:** in_progress

---

## 📐 Архитектура NPC (3 слоя)

```
┌─────────────────────────────────────────────────────┐
│ 1. Генерация (pure data)                            │
│    NPCGenerator.Generate() → GeneratedNPC (POCO)    │
│    NPCPresetData (ScriptableObject из JSON)         │
├─────────────────────────────────────────────────────┤
│ 2. Runtime (MonoBehaviour)                          │
│    NPCController.InitializeFromGenerated(GeneratedNPC)│
│    NPCController.ApplyPreset(NPCPresetData)          │
├─────────────────────────────────────────────────────┤
│ 3. Сцена (GameObject)                               │
│    NPCController.CreateFromGenerated() — фабрика     │
│    ⚠️ НЕ РЕАЛИЗОВАНО: NPCSpawner / Placement в сцене │
└─────────────────────────────────────────────────────┘
```

---

## 📁 Файлы NPC

### Генерация

| Файл | Назначение |
|------|-----------|
| `Scripts/Generators/NPCGenerator.cs` | Статический генератор → `GeneratedNPC` |
| `Scripts/Generators/GeneratorRegistry.cs` | Синглтон-реестр с LRU-кэшем (100 NPC) |
| `Scripts/Examples/NPCAssemblyExample.cs` | Пример сборки NPC (оружие+броня+техники) |

### Данные

| Файл | Назначение |
|------|-----------|
| `Scripts/NPC/NPCData.cs` | `NPCState`, `NPCSaveData`, `NPCAIState`, `DialogueOption` |
| `Scripts/Data/ScriptableObjects/NPCPresetData.cs` | SO-пресет (Cultivation/NPC Preset) |
| `Data/JSON/npc_presets.json` | 15 пресетов (villager, guard, merchant и т.д.) |

### Runtime

| Файл | Назначение |
|------|-----------|
| `Scripts/NPC/NPCController.cs` | Главный контроллер NPC (MonoBehaviour) |
| `Scripts/NPC/NPCAI.cs` | AI: взвешенный выбор поведения + личность |
| `Scripts/NPC/RelationshipController.cs` | Система отношений |
| `Scripts/Interaction/DialogueSystem.cs` | Узловой диалог с условиями и действиями |
| `Scripts/Interaction/InteractionController.cs` | 18 типов взаимодействия с NPC |

### Спрайты

| Путь | Количество |
|------|-----------|
| `Sprites/Characters/NPC/` | 12 PNG (guard, merchant, elder, disciple и т.д.) |

### Prefabs

| Путь | Статус |
|------|--------|
| `Prefabs/NPC/` | ⚠️ **ПУСТА** — нет NPC-префабов! |

---

## 🔑 Ключевые типы

### NPCGenerationParams — Вход генератора

```csharp
public class NPCGenerationParams
{
    public SpeciesData species;             // Вид (null = человек)
    public int cultivationLevel = 0;        // 0 = смертный, 1-10 = практик
    public NPCRole role = NPCRole.Passerby; // Роль
    public string locationId = "";          // Локация спавна
    public int count = 1;                   // Количество
    public int? seed = null;                // Seed (null = случайный)
}
```

### NPCRole — Роли

| Значение | Описание | Category | Attitude | Personality |
|----------|----------|----------|----------|-------------|
| `Monster` | Монстр | Temp | Hostile | Aggressive |
| `Guard` | Охранник | Plot | Neutral | Cautious + Loyal |
| `Merchant` | Торговец | Plot | Friendly | Cautious ± Ambitious |
| `Cultivator` | Культиватор | Plot | Neutral..Allied | Curious ± Ambitious |
| `Passerby` | Прохожий | Temp | Neutral | — |
| `Elder` | Старейшина | Unique | Friendly | Loyal + Cautious |
| `Disciple` | Ученик | Plot | Neutral | Curious |
| `Enemy` | Враг | Temp | Hatred | Aggressive ± Vengeful |

### GeneratedNPC — Выход генератора

```
Identity:   id, nameRu, nameEn, role, category, age
Species:    species, soulType, morphology, bodyMaterial
Cultivation: cultivationLevel, cultivationSubLevel, coreQuality
Stats:      vitality, strength, agility, constitution, intelligence
Qi:         maxQi, currentQi, conductivity
Body:       bodyParts[] (GeneratedBodyPart)
Combat:     baseDamage, baseDefense
AI:         baseAttitude, basePersonality (PersonalityTrait [Flags]), aggressionLevel
Refs:       techniqueIds[], equipmentIds[]
```

### Формулы генерации

| Параметр | Формула |
|----------|---------|
| MaxQi | `1000 × 1.1^totalSubLevels`, totalSubLevels = (level-1)×10 + subLevel |
| Vitality | `VitalityByLevel[level] + rng(-2,3)` |
| BaseDamage | `DamageByLevel[level] + strength/2` |
| BaseDefense | `constitution + cultivationLevel × 5` |
| Age | `18 + cultivationLevel × 5 + rng(0,10)` |
| CoreQuality | Взвешенный random: Normal(35%), Refined(20%), Flawed(20%) и т.д. |

---

## 🛤️ Путь 1: NPC из генератора (runtime)

```csharp
// Через GeneratorRegistry (синглтон на сцене):
var registry = GeneratorRegistry.Instance;

// Один NPC
var npc = registry.GenerateNPC(new NPCGenerationParams
{
    role = NPCRole.Merchant,
    cultivationLevel = 3,
    locationId = "test_location"
});

// Враг для игрока
var enemy = registry.GenerateEnemyForPlayer(playerLevel: 1);

// По роли
var elder = registry.GenerateNPCByRole(NPCRole.Elder, cultivationLevel: 5);
```

### Создание GameObject из GeneratedNPC

```csharp
// NPCController.CreateFromGenerated — статическая фабрика
var controller = NPCController.CreateFromGenerated(
    generated,     // GeneratedNPC из генератора
    npcPrefab,     // GameObject-префаб (NPCController + NPCAI + BodyController + QiController)
    position,      // Vector3 позиция на карте
    parent         // Transform родителя (опционально)
);
```

**⚠️ ПРОБЛЕМА: Префаб `Assets/Prefabs/NPC/` — ПУСТ.** Фабрика требует префаб, но его нет.

---

## 🛤️ Путь 2: NPC из пресета (ScriptableObject)

```csharp
// Через AssetGeneratorExtended (Editor):
// Tools/Generate Assets/NPC Presets (15)
// → создаёт Assets/Data/NPCPresets/NPC_*.asset из JSON

// Применение пресета к NPCController:
var controller = gameObject.AddComponent<NPCController>();
controller.ApplyPreset();  // берёт preset из [SerializeField] поля
```

**15 пресетов в `npc_presets.json`:**
villager, guard, cultivator, bandit, wolf, spirit_tiger, merchant, sect_elder, disciple, innkeeper, blacksmith, alchemist, hermit, traveling_monk, beast_tamer

---

## ❌ ЧТО НЕ РЕАЛИЗОВАНО (GAP-анализ)

### GAP-1: Нет NPC-префаба ⚠️ КРИТИЧНО
- `Assets/Prefabs/NPC/` — пустая папка
- `NPCController.CreateFromGenerated()` требует префаб с компонентами:
  - `NPCController`
  - `NPCAI`
  - `BodyController`
  - `QiController`
  - `TechniqueController`
  - `SpriteRenderer`
  - `CircleCollider2D`
  - `Rigidbody2D`
- **Без префаба нельзя создать NPC через фабрику**

### GAP-2: Нет NPC Spawner / Placement
- Ни Phase15 (Test Location), ни TestLocationSetup не размещают NPC
- Нет Editor-инструмента для спавна NPC в сцену (аналог EquipmentSceneSpawner)
- NPC нельзя добавить на «поляну» через меню или хоткей

### GAP-3: Нет NPC-спрайта на префабе
- 12 PNG спрайтов есть в `Sprites/Characters/NPC/`
- Но нет маппинга role → sprite для автоматического назначения

### GAP-4: Нет регистрации NPC в WorldController
- `WorldController.RegisterNPC()` существует
- Но фабрика `CreateFromGenerated()` НЕ вызывает регистрацию
- NPC не будет виден в `WorldController.GetNPCsInLocation()`

### GAP-5: Нет визуального маркера / надписи имени
- Нет floating name label над NPC
- Нет health bar над NPC

---

## ✅ ПЛАН РЕАЛИЗАЦИИ

### Шаг 1: Создать NPC-префаб (Editor-скрипт)
Создать `NPCPrefabGenerator.cs` — генерирует префаб со всеми компонентами:
- `NPCController` + `NPCAI` + `BodyController` + `QiController` + `TechniqueController`
- `SpriteRenderer` (белый кружок-заглушка, как у EquipmentSceneSpawner)
- `CircleCollider2D` (trigger для взаимодействия)
- `Rigidbody2D` (Dynamic, gravity=0, freezeRotation)
- Слой "NPC" (7), тег "NPC"

### Шаг 2: Создать NPCSceneSpawner.cs (аналог EquipmentSceneSpawner)
Горячие клавиши и меню для спавна NPC в сцену:
- **Ctrl+N** — 1 случайный NPC рядом с Player
- **Ctrl+Shift+N** — 5 NPC разных ролей
- **Ctrl+F5** — 1 торговец в инвентарь (не нужно, убрать)
- Меню: `Tools/NPC/Spawn In Scene/...`

Роли для спавна:
- Merchant (дружелюбный, стоит на месте)
- Guard (нейтральный, патрулирует)
- Cultivator (нейтральный, культивирует)
- Elder (дружелюбный, стоит)
- Monster (враждебный, блуждает)
- Enemy (враждебный, атакует)

### Шаг 3: Расширить Phase15 (или создать Phase19)
Добавить размещение NPC на тестовой поляне:
- 1 Merchant — центр деревни
- 2 Guard — патруль
- 1 Elder — у дома старейшины
- 1 Cultivator — медитирует
- 2-3 Monster — на окраине карты

### Шаг 4: Регистрация в WorldController
- После `CreateFromGenerated()` → `WorldController.RegisterNPC()`
- В `NPCController.Start()` — авторегистрация если `WorldController` на сцене

### Шаг 5: Маппинг role → sprite
Автоматическое назначение спрайта из `Sprites/Characters/NPC/` по роли:
```
Monster    → npc_beast_cultivator.png / npc_rogue.png
Guard      → npc_guard.png
Merchant   → npc_merchant.png
Cultivator → npc_disciple_male.png / npc_disciple_female.png
Elder      → npc_village_elder.png / npc_elder_master.png
Enemy      → npc_enemy_demonic.png / npc_rival.png
Passerby   → npc_villager_male.png
```

---

## 🎮 Существующие хоткеи (контекст)

| Клавиша | Действие | Файл |
|---------|----------|------|
| Ctrl+G | 3 предмета лута | EquipmentSceneSpawner |
| Ctrl+Shift+G | 10 предметов | EquipmentSceneSpawner |
| Ctrl+F1/F2/F3 | Эквип в инвентарь | EquipmentSceneSpawner |
| **Ctrl+N** | 🆕 1 NPC рядом с Player | **НЕ РЕАЛИЗОВАНО** |
| **Ctrl+Shift+N** | 🆕 5 NPC разных ролей | **НЕ РЕАЛИЗОВАНО** |

---

*Документ создан: 2026-04-30 06:20 UTC*
*Чекпоинт: 04_30_npc_generation_and_placement.md*
