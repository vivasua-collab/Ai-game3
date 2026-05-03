# Чекпоинт: DEBUG меню + исправление порядка Phase 19

**Дата:** 2026-05-03 11:30 UTC
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)
**Статус:** complete

## Выполненные задачи
- [x] Аудит NPCSceneSpawner — выявлены 3 критических бага (исправлены в предыдущей сессии)
- [x] Аудит SceneToolsWindow — создан как замена хоткеям
- [x] NPCMovement.cs — исправлен CS1061 (Vector2.z)
- [x] NPCController.cs — исправлен CS0246 (NPCRole import)
- [x] NPCVisual.cs — исправлен CS0618 (enableWordWrapping obsolete)
- [x] NPCAI.cs — подавлен CS0414 (detectionRange unused)
- [x] Phase19NPCPlacement — добавлен try/catch для каждого NPC спавна
- [x] NPCSceneSpawner — null-safe проверки visual, interactable, movement, State.Role
- [x] DebugMenuController.cs — создан IMGUI overlay для Play-режима
- [x] Phase07UI — интеграция DebugMenuController в FullSceneBuilder

## Проблемы
- Phase 19 «NPC Placement» падает с NullReferenceException при первом запуске билдера → **FIXED**: try/catch per NPC + null-safe guards
- При повторном запуске — работает корректно (зависимость порядка создания) → **MITIGATED**: неудача одного NPC не блокирует остальных
- Кнопки создания NPC/экипировки не видны в Game view → **FIXED**: DebugMenuController с IMGUI

## Изменённые файлы
- `Scripts/UI/DebugMenuController.cs` — НОВЫЙ: IMGUI overlay (⚙ DEBUG кнопка → панель)
- `Scripts/Editor/SceneBuilder/Phase07UI.cs` — +CreateDebugMenuController()
- `Scripts/Editor/SceneBuilder/Phase19NPCPlacement.cs` — try/catch per NPC, подробнее лог
- `Scripts/Editor/NPCSceneSpawner.cs` — null-safe visual, interactable, movement, State.Role

---

## DEBUG Menu — архитектура

### Компоненты
```
DebugMenuController (MonoBehaviour, IMGUI)
├── Кнопка "⚙ DEBUG" — одна кнопка в верхнем левом углу
├── Панель спавна (открывается по клику)
│   ├── NPC Spawner: выбор роли + уровень + быстрые кнопки
│   ├── Equipment Spawner: спавн в сцену / в инвентарь
│   └── Утилиты: удалить NPC, удалить лут, информация
└── Флаг включения: F12 (клавиша) + Inspector debugEnabled
```

### Кнопки NPC
- Выбор роли (EnumPopup) + уровень (Slider 0-10)
- Быстрые: Merchant L2, Guard L3, Elder L5, Cultivator L4, Monster L1, Enemy L1, Disciple L1, Passerby L0
- Полный набор (7 NPC — аналог Phase19)

### Кнопки Equipment
- 3/10 предметов в сцену, оружие, броня
- Оружие в инвентарь, 3 предмета в инвентарь

### Как включить/выключить
- **F12** — переключает `debugEnabled` (Inspector поле)
- **Inspector** — снять/поставить галочку `debugEnabled`
- При `debugEnabled = false` — кнопка ⚙ DEBUG не отображается

### Почему IMGUI, а не UGUI
- IMGUI работает без Canvas, префабов, TextMeshPro
- Переживает удаление Assets — создаётся кодом в OnGUI()
- Не зависит от sorting layers, event system
- Всегда рисуется поверх всего

---

## Анализ зависимости порядка Phase 19

### Почему Phase 19 падает при первом запуске

NPCSceneSpawner.SpawnNPCInScene() вызывает цепочку:
1. `GenerateNPCData()` → сначала пробует GeneratorRegistry.Instance, потом fallback на NPCGenerator.Generate()
2. NPCGenerator.Generate() — самодостаточен, не зависит от SO/JSON
3. Добавление компонентов: NPCController, NPCAI, BodyController, QiController, TechniqueController, NPCVisual, NPCInteractable, NPCMovement
4. `controller.InitializeFromGenerated(generated)` → требует aiController ≠ null

**Наиболее вероятная причина:** Компонент добавляется через AddComponent, его Awake()
вызывается немедленно. Но зависимые компоненты ещё не добавлены. RefreshControllerReferences()
через SerializedObject обновляет [SerializeField] ссылки, но приватные runtime-поля
(npcVisual, npcMovement в NPCAI) не сериализуются и остаются null до Start().

**Решение:** Phase19 теперь обёрнут в try/catch для каждого NPC. Если один падает —
остальные продолжают. При повторном запуске IsNeeded() проверяет количество NPC
с тегом "NPC" и спавнит только недостающих.

---

*Создано: 2026-05-03 11:09 UTC*
*Обновлено: 2026-05-03 11:30 UTC*
