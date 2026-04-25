# 📁 Asset Setup — Инструкции по настройке Unity Editor

Навигатор по инструкциям создания .asset файлов через Unity Editor.

---

## 📋 Перечень инструкций

### Базовые данные

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [01_CultivationLevelData.md](./01_CultivationLevelData.md) | 10 уровней культивации | Автоматически |
| [02_MortalStageData.md](./02_MortalStageData.md) | 6 этапов смертного | Автоматически |
| [03_ElementData.md](./03_ElementData.md) | 7 элементов (стихий) | Автоматически |

### Игровой контент

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [06_TechniqueData.md](./06_TechniqueData.md) | 34 техники | Автоматически |
| [07_NPCPresetData.md](./07_NPCPresetData.md) | 15 пресетов NPC | Автоматически |
| [08_EquipmentData.md](./08_EquipmentData.md) | 39 единиц экипировки | Автоматически |
| [09_EnemySetup.md](./09_EnemySetup.md) | 27 типов врагов | Автоматически |
| [10_QuestSetup.md](./10_QuestSetup.md) | 15 квестов | Автоматически |
| [11_ItemData.md](./11_ItemData.md) | 8 расходников | Автоматически |
| [12_MaterialData.md](./12_MaterialData.md) | 17 материалов | Автоматически |

### Формации и ядра

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [14_FormationData.md](./14_FormationData.md) | 24+ формаций | Автоматически |
| [15_FormationCoreData.md](./15_FormationCoreData.md) | 30+ ядер формаций | Автоматически |

### Полный генератор сцены (One-Click)

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| **FullSceneBuilder** (см. ниже) | Вся сцена + ассеты за 1 клик | Автоматически |

### Сцены и игрок

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [04_BasicScene.md](./04_BasicScene.md) | Базовая сцена | Вручную |
| [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md) | Базовая сцена | Полуавтомат |
| [05_PlayerSetup.md](./05_PlayerSetup.md) | Player prefab | Вручную |
| [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md) | Player prefab | Полуавтомат |
| [05_PlayerSetup_Animation.md](./05_PlayerSetup_Animation.md) | Анимации игрока | Вручную |

### Графика

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [13_SpriteSetup.md](./13_SpriteSetup.md) | 57 спрайтов | Вручную |
| [13_SpriteSetup_QuickStart.md](./13_SpriteSetup_QuickStart.md) | 57 спрайтов | Полуавтомат |

### Тайловая система

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [16_TileSystem_SemiAuto.md](./16_TileSystem_SemiAuto.md) | Тайлы + тестовая локация | Полуавтомат |

### Инвентарь

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [17_InventoryData.md](./17_InventoryData.md) | 4 рюкзака + 4 кольца хранения + volume/allowNesting | Вручную |
| [17_InventoryData_SemiAuto.md](./17_InventoryData_SemiAuto.md) | 4 рюкзака + 4 кольца хранения + volume/allowNesting | Полуавтомат |
| [18_InventoryUI.md](./18_InventoryUI.md) | InventoryScreen + BodyDoll + Backpack + Tooltip + DragDrop + ContextMenu + SpiritStorage + StorageRing (вручную wiring) | Вручную |
| [18_InventoryUI_SemiAuto.md](./18_InventoryUI_SemiAuto.md) | InventoryScreen + BodyDoll + Backpack + Tooltip + DragDrop + ContextMenu + SpiritStorage + StorageRing — ~150 wiring автоматически | Полуавтомат |

---

## 🤖 Инструменты автоматизации

### ⚡ FullSceneBuilder — One-Click Builder

**Скрипт:** `Editor/FullSceneBuilder.cs`
**Меню:** `Tools → Full Scene Builder → Build All (One Click)`

Инкрементальный генератор полной сцены. Выполняет 18 фаз, каждая идемпотентна
(повторный запуск безопасен — пропускает уже выполненные фазы).

#### Фазы

| # | Фаза | Что делает |
|---|------|------------|
| 01 | Folders | Создаёт 26 папок проекта |
| 02 | Tags & Layers | Добавляет 5 тегов + 7 слоёв |
| 03 | Create Scene | Создаёт Main.unity |
| 04 | Camera & Light | Камера (ortho) + Directional Light |
| 05 | GameManager | GameManager + GameInitializer + Systems (World, Time, Location, Event, Faction, Save, GeneratorRegistry) |
| 06 | Player | Player с 8 компонентами (Controller, Body, Qi, Inventory, Equipment, Technique, Sleep, Interaction) + Rigidbody2D + Collider |
| 07 | UI | Canvas (ScreenSpaceOverlay) + HUD + EventSystem + InputSystemUIInputModule |
| 08 | Tilemap | Grid + Terrain/Objects Tilemaps + TileMapController + TestLocationGameController + DestructibleObjectController |
| 09 | Generate Assets | ScriptableObject'ы из JSON (CultivationLevels, Elements, MortalStages, Techniques, NPCPresets, Equipment, Items, Materials, Formations, FormationCores) + валидация |
| 10 | Tile Sprites | 8 terrain + 5 object спрайтов (64×64 px) |
| 11 | Formation UI Prefabs | Префабы UI для формаций |
| 12 | TMP Essentials | Проверка/импорт TMP Essentials |
| 13 | Save Scene | Сохранение сцены |
| 14 | Tile Assets | TerrainTile + ObjectTile для 15+ типов |
| 15 | Test Location | Камера + коллайдеры + HarvestableSpawner |
| 16 | Inventory Data | BackpackData (4 рюкзака) + StorageRingData (4 кольца) + volume/allowNesting для ItemData |
| 17 | Inventory UI | InventoryScreen + BodyDoll (7 слотов + wiring) + BackpackPanel (3×4 + wiring) + Tooltip + DragDrop + ContextMenu + SpiritStorage + StorageRing — ~150 wiring операций автоматически |
| 18 | Inventory Components | SpiritStorageController + StorageRingController на Player + подключение UI |

#### Как использовать

1. Откройте Unity Editor с проектом
2. Убедитесь, что все JSON-файлы на месте в `Assets/Data/JSON/` (14 файлов)
3. Нажмите: **Tools → Full Scene Builder → Build All (One Click)**
4. Дождитесь завершения (диалог с результатом)

#### Запуск отдельных фаз

Каждую фазу можно запустить отдельно через меню:
```
Tools → Full Scene Builder → Phase 01: Folders
Tools → Full Scene Builder → Phase 02: Tags and Layers
...
Tools → Full Scene Builder → Phase 13: Save Scene
Tools → Full Scene Builder → Phase 14: Create Tile Assets
Tools → Full Scene Builder → Phase 15: Configure Test Location
Tools → Full Scene Builder → Phase 16: Inventory Data
Tools → Full Scene Builder → Phase 17: Inventory UI
Tools → Full Scene Builder → Phase 18: Inventory Components
```

#### Повторный запуск

Безопасен. Фаза проверяет `IsPhaseNeeded()` и пропускает, если результат уже есть.
Например, если GameManager уже существует — Phase 05 пропустится.

#### При ошибке в фазе

Появится диалог: «Фаза упала. Продолжить?» — Да/Стоп.
Остальные фазы выполнятся или прервутся по выбору.

---

### Меню Unity Editor

| Меню | Скрипт | Назначение |
|------|--------|------------|
| `Tools → Full Scene Builder → Build All` | `Editor/FullSceneBuilder.cs` | **Всё за 1 клик (18 фаз)** |
| `Tools → Full Scene Builder → Phase NN` | `Editor/FullSceneBuilder.cs` | Отдельная фаза |
| `Window → Asset Generator` | `Editor/AssetGenerator.cs` | Базовые данные |
| `Window → Asset Generator Extended` | `Editor/AssetGeneratorExtended.cs` | Контент |
| `Tools → Generate Assets → Formation Assets` | `Editor/FormationAssetGenerator.cs` | Формации |
| `Window → Scene Setup Tools` | `Editor/SceneSetupTools.cs` | Сцена + Player |
| `Tools → Generate Tile Sprites` | `Tile/Editor/TileSpriteGenerator.cs` | Спрайты тайлов |
| `Tools → Setup Test Location Scene` | `Tile/Editor/TestLocationSetup.cs` | Тестовая локация |

---

## 🚀 Порядок настройки (быстрый старт)

### ⚡ Вариант 1: One-Click (рекомендуется)

1. **Всё сразу** — `Tools → Full Scene Builder → Build All (One Click)`

Генератор создаст папки, теги, слои, сцену, камеры, GameManager, Player, UI, Tilemap,
сгенерирует все ассеты из JSON, спрайты тайлов, префабы формаций и проверит TMP.

### Вариант 2: Пошаговый

1. **Базовые данные** — `Window → Asset Generator` → Generate All
2. **Контент** — `Window → Asset Generator Extended` → All Extended Assets
3. **Формации** — `Tools → Generate Assets → Formation Assets`
4. **Сцена** — см. [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md)
5. **Player** — см. [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md)
6. **Инвентарь (данные)** — см. [17_InventoryData_SemiAuto.md](./17_InventoryData_SemiAuto.md)
7. **Инвентарь (UI)** — см. [18_InventoryUI_SemiAuto.md](./18_InventoryUI_SemiAuto.md)

---

## 📚 Где искать информацию

| Тема | Документ |
|------|----------|
| Архитектура систем | `docs/ARCHITECTURE.md` |
| Формулы расчёта | `docs/ALGORITHMS.md` |
| Система Ци | `docs/QI_SYSTEM.md` |
| Боевая система | `docs/COMBAT_SYSTEM.md` |
| Спецификация инвентаря | `docs_temp/INVENTORY_UI_DRAFT.md` |
| Аудит флагов инвентаря | `docs_temp/INVENTORY_FLAGS_AUDIT.md` |
| Enums проекта | `UnityProject/Assets/Scripts/Core/Enums.cs` |
| Примеры кода | `docs_examples/` |

---

## ⚠️ Важно

- **SemiAuto** = автоматизация + ручные шаги после
- Все числовые значения вводить без запятых
- Checkbox (✓/✗) = true/false

---

*Обновлено: 2026-04-25 16:15:00 MSK*
