# 🏗️ Детальный план внедрения NPC

**Дата:** 2026-04-30  
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)  
**Статус:** complete  
**Источник:** checkpoints/04_30_npc_generation_and_placement.md

---

## 📊 Результаты аудита кода

### Существующие NPC-файлы и их состояние

| Файл | Строк | Статус | Решение |
|------|-------|--------|---------|
| `NPC/NPCController.cs` | 625 | ✅ Полный. `InitializeFromGenerated()` + `CreateFromGenerated()` работают. Но: нет регистрации в WorldController. | **РЕДАКТИРОВАТЬ** — +5 строк |
| `NPC/NPCData.cs` | 331 | ✅ Полный. NPCState, NPCSaveData, NPCAIState, DialogueOption. | **НЕ ТРОГАТЬ** |
| `NPC/NPCAI.cs` | 634 | ✅ Полный. Взвешенный выбор поведения + PersonalityTrait. Stub-методы движения (Wandering/Patrolling — пустые). | **РЕДАКТИРОВАТЬ** — +10 строк (NPCVisual feedback) |
| `NPC/RelationshipController.cs` | 534 | ✅ Полный. Attitude-совместимый. | **НЕ ТРОГАТЬ** |
| `Generators/NPCGenerator.cs` | 468 | ✅ Полный. Attitude + PersonalityTrait генерация. | **НЕ ТРОГАТЬ** |
| `Generators/GeneratorRegistry.cs` | 528 | ✅ Полный. GenerateNPC, GenerateEnemyForPlayer, LRU-кэш. | **НЕ ТРОГАТЬ** |
| `Data/SO/NPCPresetData.cs` | 203 | ✅ Полный. PersonalityTraitEntry (без коллизии). | **НЕ ТРОГАТЬ** |
| `Interaction/InteractionController.cs` | 407 | ✅ Полный. Сканирует `Interactable` через Physics2D. NPC НЕ является Interactable! | **НЕ ТРОГАТЬ** — но нужно создать NPCInteractable |
| `Interaction/DialogueSystem.cs` | 615 | ✅ Полный. Загрузка из Resources/JSON. | **НЕ ТРОГАТЬ** |
| `Examples/NPCAssemblyExample.cs` | 407 | ✅ Полный. Демо сборки L6 культиватора. | **НЕ ТРОГАТЬ** |
| `World/WorldController.cs` | 488 | ✅ Полный. `RegisterNPC()` есть, но NPCController его не вызывает. | **НЕ ТРОГАТЬ** — правка в NPCController |

### SceneBuilder оркестраторы

| Файл | Строк | Решение |
|------|-------|---------|
| `Editor/FullSceneBuilder.cs` | 244 | **РЕДАКТИРОВАТЬ** — +1 строка (Phase19) |
| `Editor/SceneBuilder/IScenePhase.cs` | 42 | **НЕ ТРОГАТЬ** — заморожен |
| `Editor/SceneBuilder/SceneBuilderConstants.cs` | 105 | **НЕ ТРОГАТЬ** — NPC-папки уже есть |
| `Editor/SceneBuilder/SceneBuilderUtils.cs` | 593 | **НЕ ТРОГАТЬ** — утилиты достаточно |
| `Editor/SceneBuilder/Phase06Player.cs` | 221 | **ШАБЛОН** — фаза создания Player с компонентами |
| `Editor/SceneBuilder/Phase15ConfigureTestLocation.cs` | 167 | **НЕ ТРОГАТЬ** — NPC добавляются в новой фазе |
| `Editor/EquipmentSceneSpawner.cs` | 278 | **ШАБЛОН** — Editor-спавнер с хоткеями |

### Отсутствующие файлы (GAP)

| GAP | Что нужно | Приоритет |
|-----|----------|-----------|
| GAP-1 | Нет NPC-префаба → но **префаб не нужен для Editor-спавна**. Создаём GameObject напрямую. | — |
| GAP-2 | Нет NPCSceneSpawner (хоткеи/меню) | 🔴 Критичный |
| GAP-3 | Нет маппинга role → sprite | 🟡 Средний |
| GAP-4 | Нет регистрации NPC в WorldController | 🟡 Средний |
| GAP-5 | Нет NPCVisual (спрайт + имя + HP-бар) | 🔴 Критичный |
| GAP-6 | Нет NPCInteractable (NPC ↔ InteractionController) | 🔴 Критичный |
| GAP-7 | Нет Phase19 (автоматическое размещение на поляне) | 🟡 Средний |

---

## 🏛️ Архитектура внедрения

### Принцип: «Создаём GameObject напрямую, без префаба»

Phase06Player и EquipmentSceneSpawner создают объекты в сцене напрямую, без .prefab файлов.
NPC-спавнер работает аналогично:
1. Создать пустой GameObject
2. Добавить все нужные компоненты
3. Настроить через SerializedObject
4. Зарегистрировать Undo

`NPCController.CreateFromGenerated(prefab)` требует префаб, но мы **не используем этот метод**.
Вместо этого создаём GameObject и вызываем `NPCController.InitializeFromGenerated(generated)` напрямую.

### Порядок внедрения через оркестраторы

```
┌──────────────────────────────────────────────────────────────────┐
│ ШАГ 1: NPCVisual.cs (NEW, runtime)                              │
│   • Создаёт визуал NPC: спрайт по роли + имя + HP-бар           │
│   • Аналог PlayerVisual.cs                                       │
│   • Не зависит от других новых файлов                            │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 2: NPCInteractable.cs (NEW, runtime)                        │
│   • Extends Interactable для NPC                                 │
│   • Возвращает доступные взаимодействия (Talk, Trade, Attack...) │
│   • Зависит от NPCController (существует)                        │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 3: NPCController.cs (EDIT)                                   │
│   • +auto-registration в WorldController.Start()                 │
│   • +OnDestroy: unregister                                       │
│   • Зависит от WorldController (существует)                      │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 4: NPCSceneSpawner.cs (NEW, editor)                          │
│   • Хоткеи + меню для спавна NPC в сцену                         │
│   • Ctrl+N / Ctrl+Shift+N / Ctrl+F5...                          │
│   • Создаёт GameObject + компоненты + InitializeFromGenerated    │
│   • Зависит от NPCVisual, NPCInteractable, NPCController         │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 5: NPCAI.cs (EDIT, minor)                                    │
│   • +NPCVisual state feedback (цвет по Attitude)                 │
│   • Зависит от NPCVisual (шаг 1)                                 │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 6: Phase19NPCPlacement.cs (NEW, SceneBuilder)                │
│   • IScenePhase: размещение NPC на тестовой поляне               │
│   • 1 Merchant, 2 Guard, 1 Elder, 1 Cultivator, 2 Monster       │
│   • Зависит от NPCSceneSpawner (шаг 4)                           │
├──────────────────────────────────────────────────────────────────┤
│ ШАГ 7: FullSceneBuilder.cs (EDIT, 1 строка)                      │
│   • +new Phase19NPCPlacement() в PHASES                          │
│   • Зависит от Phase19 (шаг 6)                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📋 Детальная спецификация каждого шага

### ШАГ 1: NPCVisual.cs (NEW)

**Файл:** `Assets/Scripts/NPC/NPCVisual.cs`  
**Тип:** Runtime MonoBehaviour  
**Зависимости:** NPCController, NPCData (NPCAIState, Attitude)  
**Шаблон:** PlayerVisual.cs

```
Компоненты GameObject "Visual" (дочерний):
  ├─ SpriteRenderer — спрайт по роли (из Assets/Sprites/Characters/NPC/)
  │                  fallback: программный гуманоид (как PlayerVisual)
  ├─ Sorting Layer: "Objects", order: 50 (выше terrain, ниже Player)
  └─ Material: Sprite-Unlit-Default (виден без Light2D)

Компонент "NameLabel" (дочерний):
  ├─ Canvas (WorldSpace, размер 3×0.5)
  ├─ TextMeshPro — имя NPC + уровень культивации
  │   Цвет по Attitude: Hostile=красный, Neutral=белый, Friendly=зелёный
  └─ Позиция: +1.2 юнита выше NPC (над головой)

HealthBar (дочерний):
  ├─ Canvas (WorldSpace)
  ├─ Slider (red/green) — CurrentHealth/MaxHealth
  └─ Позиция: +0.8 юнита выше NPC (под именем)

Методы:
  • SetSpriteByRole(NPCRole) — загрузка спрайта из Assets/Sprites/Characters/NPC/
  • SetAttitudeColor(Attitude) — цвет имени по отношению
  • UpdateHealthBar(int current, int max)
  • SetAIState(NPCAIState) — визуальная индикация состояния
```

**Маппинг role → sprite:**
```
Monster    → npc_rogue.png / npc_beast_cultivator.png
Guard      → npc_guard.png
Merchant   → npc_merchant.png
Cultivator → npc_disciple_male.png / npc_disciple_female.png
Elder      → npc_village_elder.png / npc_elder_master.png
Enemy      → npc_enemy_demonic.png / npc_rival.png
Disciple   → npc_disciple_male.png
Passerby   → npc_villager_male.png
```

**Объём:** ~250-300 строк  
**Сложность:** Средняя (копия PlayerVisual с NPC-спецификой)

---

### ШАГ 2: NPCInteractable.cs (NEW)

**Файл:** `Assets/Scripts/NPC/NPCInteractable.cs`  
**Тип:** Runtime MonoBehaviour (extends Interactable)  
**Зависимости:** InteractionController.Interactable, NPCController  
**Обоснование:** InteractionController сканирует Physics2D на `Interactable`. NPC должен реализовать этот интерфейс.

```
class NPCInteractable : Interactable
  ├─ [SerializeField] NPCController npcController
  │
  ├─ CanInteract(InteractionController player) → bool
  │   • npcController.IsAlive && !isInDialogue
  │
  ├─ GetAvailableInteractions(InteractionController player) → List<InteractionType>
  │   • Всегда: Talk
  │   • Если Merchant: Trade
  │   • Если attitude < Hostile: Attack
  │   • Если attitude >= Friendly: Gift, Flatter
  │   • Если Cultivator: Learn, Cultivate
  │   • Если Elder: Teach
  │
  ├─ Interact(InteractionController player, InteractionType type) → InteractionResult
  │   • Talk → вызвать DialogueSystem.StartDialogue()
  │   • Attack → начать бой (заглушка)
  │   • Gift → ModifyRelationship(+10)
  │   • и т.д.
  │
  └─ Требует: CircleCollider2D (trigger) на том же GameObject
       • radius = 1.5f (радиус взаимодействия)
       • layer = "Interactable" (8)
```

**Объём:** ~150-200 строк  
**Сложность:** Средняя

---

### ШАГ 3: NPCController.cs (EDIT)

**Файл:** `Assets/Scripts/NPC/NPCController.cs`  
**Изменения:** Добавить авторегистрацию в WorldController

```csharp
// В Start(), после ServiceLocator.Request<TimeController>:
// GAP-4: Авторегистрация в WorldController
ServiceLocator.Request<WorldController>(wc => wc.RegisterNPC(this));

// Новый метод OnDestroy (или дополнение если существует):
private void OnDestroy()
{
    // Отписка от WorldController
    if (WorldController.Instance != null)
    {
        // WorldController.Instance.UnregisterNPC() — если Instance доступен
    }
}
```

**Объём:** +5-10 строк  
**Сложность:** Низкая  
**Риск:** Минимальный — ServiceLocator.Request безопасен

---

### ШАГ 4: NPCSceneSpawner.cs (NEW)

**Файл:** `Assets/Scripts/Editor/NPCSceneSpawner.cs`  
**Тип:** Editor-only static class  
**Зависимости:** NPCGenerator, GeneratorRegistry, NPCController, NPCVisual, NPCInteractable  
**Шаблон:** EquipmentSceneSpawner.cs

```
Хоткеи:
  Ctrl+N          — 1 случайный NPC рядом с Player
  Ctrl+Shift+N    — 5 NPC разных ролей рядом с Player
  Ctrl+F5         — 1 Merchant рядом с Player
  Ctrl+F6         — 1 Enemy/Monster рядом с Player

Меню:
  Tools/NPC/Spawn In Scene/Random NPC          → 1 случайный
  Tools/NPC/Spawn In Scene/5 Random NPCs       → 5 разных ролей
  Tools/NPC/Spawn In Scene/Merchant            → Merchant L1
  Tools/NPC/Spawn In Scene/Guard               → Guard L2
  Tools/NPC/Spawn In Scene/Elder               → Elder L5
  Tools/NPC/Spawn In Scene/Monster             → Monster L1
  Tools/NPC/Spawn In Scene/Enemy               → Enemy L1
  Tools/NPC/Clear All NPCs                     → Удалить все NPC из сцены

Метод SpawnNPCInScene(NPCRole role, int level, Vector3 position):
  1. Создать GameObject "NPC_{nameRu}"
  2. Position = position, Z = 0
  3. Layer = "NPC" (7) — если есть, иначе Default
  4. Tag = "NPC"
  5. Добавить компоненты:
     • NPCController
     • NPCAI
     • BodyController
     • QiController
     • TechniqueController
     • NPCVisual
     • NPCInteractable
     • CircleCollider2D (solid, radius=0.5f) — физика
     • CircleCollider2D (trigger, radius=1.5f) — взаимодействие, layer Interactable
     • Rigidbody2D (Dynamic, gravity=0, freezeRotation)
  6. Настроить NPCAI через SerializedObject:
     • decisionInterval = 1f
     • aggroRange = 10f
     • fleeHealthThreshold = 0.2f
  7. Сгенерировать GeneratedNPC через GeneratorRegistry:
     • Если на сцене есть GeneratorRegistry — использовать Instance
     • Иначе — создать временный SeededRandom и вызвать NPCGenerator.Generate()
  8. Вызвать controller.InitializeFromGenerated(generated)
  9. Настроить NPCVisual через SerializedObject (role)
  10. Undo.RegisterCreatedObjectUndo
```

**Объём:** ~300-350 строк  
**Сложность:** Средняя (копия EquipmentSceneSpawner с NPC-спецификой)

---

### ШАГ 5: NPCAI.cs (EDIT, minor)

**Файл:** `Assets/Scripts/NPC/NPCAI.cs`  
**Изменения:** Добавить визуальную обратную связь через NPCVisual

```csharp
// Добавить поле:
private NPCVisual npcVisual;

// В Awake(), после npcController = GetComponent<NPCController>():
npcVisual = GetComponent<NPCVisual>();

// В ChangeState(), после state.CurrentAIState = newState:
if (npcVisual != null)
    npcVisual.SetAIState(newState);
```

**Объём:** +10 строк  
**Сложность:** Низкая  
**Риск:** Минимальный — null-safe

---

### ШАГ 6: Phase19NPCPlacement.cs (NEW)

**Файл:** `Assets/Scripts/Editor/SceneBuilder/Phase19NPCPlacement.cs`  
**Тип:** IScenePhase  
**Зависимости:** NPCSceneSpawner (шаг 4)  
**Шаблон:** Phase15ConfigureTestLocation.cs

```
class Phase19NPCPlacement : IScenePhase
  Name: "NPC Placement"
  MenuPath: "Phase 19: NPC Placement"
  Order: 19

  IsNeeded():
    • Если на сцене НЕТ ни одного объекта с тегом "NPC" → return true
    • Иначе → return false

  Execute():
    1. Найти Player позицию (центр карты)
    2. Найти границы карты через TileMapController

    3. Спавн NPC на тестовой поляне:
       ┌────────────────────────────────────────────────┐
       │  1 Merchant (L2)  — центр деревни (+3, -2)     │
       │  1 Guard (L3)     — вход (+8, 0)               │
       │  1 Guard (L2)     — патруль (-5, +3)           │
       │  1 Elder (L5)     — дом старейшины (-3, -4)    │
       │  1 Cultivator (L4)— площадка культивации (+5,+5)│
       │  1 Monster (L1)   — окраина (-10, -8)          │
       │  1 Monster (L2)   — окраина (+12, -10)         │
       └────────────────────────────────────────────────┘

    4. Вызвать NPCSceneSpawner.SpawnNPCInScene() для каждого
    5. Установить patrol points для Guard
    6. Установить начальный AI state:
       • Merchant → Trading
       • Guard → Patrolling
       • Elder → Idle
       • Cultivator → Cultivating
       • Monster → Wandering
```

**Объём:** ~120-150 строк  
**Сложность:** Низкая (вызовы NPCSceneSpawner)

---

### ШАГ 7: FullSceneBuilder.cs (EDIT, 1 строка)

**Файл:** `Assets/Scripts/Editor/FullSceneBuilder.cs`  
**Изменения:** Добавить Phase19 в массив PHASES

```csharp
// В массив PHASES добавить последним элементом:
new Phase19NPCPlacement(),
```

Также добавить MenuItem:
```csharp
[MenuItem("Tools/Full Scene Builder/Phase 19: NPC Placement", false, 119)]
public static void RunPhase19() { RunSinglePhase(19); }
```

**Объём:** +3 строки  
**Сложность:** Тривиальная  
**Риск:** Нет

---

## 📦 Итого: новые и редактируемые файлы

### Новые файлы (4)

| # | Файл | Тип | Строк (оценка) |
|---|------|-----|----------------|
| 1 | `Scripts/NPC/NPCVisual.cs` | Runtime | ~280 |
| 2 | `Scripts/NPC/NPCInteractable.cs` | Runtime | ~180 |
| 3 | `Scripts/Editor/NPCSceneSpawner.cs` | Editor | ~330 |
| 4 | `Scripts/Editor/SceneBuilder/Phase19NPCPlacement.cs` | Editor/SceneBuilder | ~140 |

### Редактируемые файлы (3)

| # | Файл | Изменение | Строк |
|---|------|-----------|-------|
| 1 | `Scripts/NPC/NPCController.cs` | +WorldController registration | +8 |
| 2 | `Scripts/NPC/NPCAI.cs` | +NPCVisual feedback | +10 |
| 3 | `Scripts/Editor/FullSceneBuilder.cs` | +Phase19 в PHASES | +3 |

### Файлы БЕЗ изменений (9)

NPCData.cs, NPCPresetData.cs, GeneratorRegistry.cs, NPCGenerator.cs,  
RelationshipController.cs, InteractionController.cs, DialogueSystem.cs,  
WorldController.cs, NPCAssemblyExample.cs

---

## ⚡ Порядок реализации

```
Шаг 1 ──► Шаг 2 ──► Шаг 3 ──► Шаг 4 ──► Шаг 5 ──► Шаг 6 ──► Шаг 7
NPCVisual  NPCInter  NPCContr  NPCSpaw   NPCAI     Phase19   FullScene
(runtime)  (runtime) (edit)    (editor)  (edit)    (SB-new)  (SB-edit)
                                              │
                                              └── можно параллельно с шагом 4
```

**Критический путь:** Шаг 1 → Шаг 4 → Шаг 6 → Шаг 7  
**Шаги 2, 3, 5** — можно делать параллельно, но до шага 4

---

## 🔒 Риски и ограничения

1. **NPCController.CreateFromGenerated(prefab)** — НЕ используется. Мы создаём GameObject напрямую и вызываем `InitializeFromGenerated()`. Если позже потребуется runtime-спавн через префаб — нужно будет создать .prefab через NPCPrefabGenerator.

2. **NPCAI движения** — `ExecuteWandering()`, `ExecutePatrolling()` — пустые (stub). NPC не будет реально двигаться. Для тестовой поляны это допустимо. Реальное движение требует NavMesh2D или кастомный pathfinding — отдельная задача.

3. **DialogueSystem интеграция** — NPCInteractable.Talk вызывает DialogueSystem.StartDialogue(), но система диалогов требует узлы в Resources/Dialogues/. Без узлов диалог не начнётся. Для теста — достаточно заглушки.

4. **ServiceLocator.Request<WorldController>** — WorldController НЕ регистрирует себя в ServiceLocator (закомментировано в Awake). Поэтому `ServiceLocator.Request<WorldController>` может не сработать. Альтернатива: `FindFirstObjectByType<WorldController>()`.

---

*Документ создан: 2026-04-30 06:41 UTC*  
*Чекпоинт: 04_30_npc_implementation_plan.md*

---

## ✅ РЕЗУЛЬТАТЫ ВНЕДРЕНИЯ (2026-04-30 08:00 UTC)

Все 7 шагов внедрения выполнены.

### Новые файлы (4)

| # | Файл | Строк | Статус |
|---|------|-------|--------|
| 1 | `Scripts/NPC/NPCVisual.cs` | ~320 | ✅ Создан |
| 2 | `Scripts/NPC/NPCInteractable.cs` | ~320 | ✅ Создан |
| 3 | `Scripts/Editor/NPCSceneSpawner.cs` | ~250 | ✅ Создан |
| 4 | `Scripts/Editor/SceneBuilder/Phase19NPCPlacement.cs` | ~110 | ✅ Создан |

### Редактированные файлы (3)

| # | Файл | Изменение | Статус |
|---|------|-----------|--------|
| 1 | `Scripts/NPC/NPCController.cs` | +WorldController registration (Start/OnDestroy) | ✅ |
| 2 | `Scripts/NPC/NPCAI.cs` | +NPCVisual feedback (npcVisual field + ChangeState) | ✅ |
| 3 | `Scripts/Editor/FullSceneBuilder.cs` | +Phase19 в PHASES + MenuItem | ✅ |

### GAP статусы

| GAP | Описание | Статус |
|-----|----------|--------|
| GAP-1 | Нет NPC-префаба | ✅ Не нужен — GameObject создаётся напрямую |
| GAP-2 | Нет NPCSceneSpawner | ✅ Создан NPCSceneSpawner.cs |
| GAP-3 | Нет role→sprite маппинга | ✅ Реализован в NPCVisual.SetSpriteByRole() |
| GAP-4 | Нет регистрации в WorldController | ✅ Добавлена в NPCController.Start() + OnDestroy() |
| GAP-5 | Нет визуального маркера | ✅ Создан NPCVisual.cs (спрайт+имя+HP-бар) |
| GAP-6 | Нет NPCInteractable | ✅ Создан NPCInteractable.cs |
| GAP-7 | Нет Phase19 | ✅ Создан Phase19NPCPlacement.cs |

### Хоткеи NPC

| Клавиша | Действие |
|---------|----------|
| Ctrl+N | 1 случайный NPC рядом с Player |
| Ctrl+Shift+N | 5 NPC разных ролей |
| Ctrl+F5 | 1 Merchant |
| Ctrl+F6 | 1 Monster |

### Известные ограничения

1. NPCAI движения — stub (Wandering/Patrolling пустые)
2. DialogueSystem — заглушка в NPCInteractable.Talk
3. NPCController.OnDestroy — может быть вызван в Editor при перезагрузке сцены
