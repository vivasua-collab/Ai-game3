# 🎮 Справка по командам Editor — Cultivation World Simulator

**Версия:** 1.0
**Дата:** 2026-04-29
**Файлы-источники:** `Assets/Scripts/Editor/*.cs`

---

## ⌨️ Горячие клавиши

| Комбинация | Действие | Файл |
|---|---|---|
| **Ctrl+G** | 3 случайных предмета рядом с Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+Shift+G** | 10 случайных предметов рядом с Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+Alt+G** | 5 предметов уровня 3 рядом с Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+F1** | 1 оружие → инвентарь Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+F2** | 1 броня → инвентарь Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+F3** | 3 случайных → инвентарь Player | `EquipmentSceneSpawner.cs` |

> **Примечание:** `_` в MenuItem отделяет путь от hotkey. `%` = Ctrl, `#` = Shift, `&` = Alt.

---

## 🔧 Tools / Full Scene Builder

Полная сборка сцены из 19 фаз (00–18).

| Пункт меню | Фаза | Описание |
|---|---|---|
| **Build All (One Click)** | — | Запуск всех нужных фаз автоматически |
| Phase 00: URP Asset Setup | 00 | URP Asset + Renderer2D + GraphicsSettings |
| Phase 01: Folders | 01 | Создание структуры папок |
| Phase 02: Tags and Layers | 02 | Теги, слои, Sorting Layers |
| Phase 03: Create Scene | 03 | Создание сцены MainScene |
| Phase 04: Camera and Light | 04 | Camera2D + Light2D |
| Phase 05: GameManager and Systems | 05 | GameManager, GeneratorRegistry, WorldController |
| Phase 06: Player | 06 | Player + все компоненты (Rigidbody, Inventory и т.д.) |
| Phase 07: UI | 07 | Canvas, HUD, EventSystem |
| Phase 08: Tilemap System | 08 | Grid, Tilemap, TilemapRenderer |
| Phase 09: Generate Assets from JSON | 09 | SO из JSON (CultivationLevels, Elements и т.д.) |
| Phase 10: Generate Tile Sprites | 10 | Спрайты тайлов (процедурные) |
| Phase 11: Generate Formation UI Prefabs | 11 | Префабы UI формаций |
| Phase 12: Import TMP Essentials | 12 | TextMeshPro ресурсы |
| Phase 13: Save Scene | 13 | Сохранение сцены |
| Phase 14: Create Tile Assets | 14 | TileData, ResourceNodeData |
| Phase 15: Configure Test Location | 15 | Тестовая локация (тайлы, объекты) |
| Phase 16: Inventory Data | 16 | ItemData, EquipmentData, BackpackData, StorageRingData |
| Phase 17: Inventory UI | 17 | Инвентарь UI (панели, слоты, перетаскивание) |
| Phase 18: Inventory Components | 18 | InventoryController + UI привязка |

**Источник:** `FullSceneBuilder.cs`

---

## ⚔️ Tools / Equipment

### Spawn In Scene (Runtime, без .asset файлов)

| Пункт меню | Hotkey | Описание |
|---|---|---|
| Random Loot x3 | Ctrl+G | 3 случайных предмета уровня 1 |
| Random Loot x10 | Ctrl+Shift+G | 10 случайных предметов уровня 1 |
| Random Loot L3 x5 | Ctrl+Alt+G | 5 предметов уровня 3 |
| Weapon (Random) | — | 1 случайное оружие |
| Armor (Random) | — | 1 случайная броня |

> Спавнит `ResourcePickup` с `EquipmentData` рядом с Player. Цвет маркера по редкости.

### Add to Inventory (Runtime, без .asset файлов)

| Пункт меню | Hotkey | Описание |
|---|---|---|
| Weapon | Ctrl+F1 | 1 оружие в инвентарь |
| Armor | Ctrl+F2 | 1 броня в инвентарь |
| Random Loot x3 | Ctrl+F3 | 3 случайных в инвентарь |

### Generate (Editor, создаёт .asset файлы на диске)

| Пункт меню | Кол-во SO | Описание |
|---|---|---|
| Generate Weapon Set (T1) | 36 | 12 подтипов × 3 грейда |
| Generate Weapon Set (All Tiers) | 180 | ×5 тиров |
| Generate Armor Set (T1) | 63 | 7×3 вес.класс × 3 грейда |
| Generate Armor Set (All Tiers) | 315 | ×5 тиров |
| Generate Full Set (T1) | 99 | Оружие + броня T1 |
| Generate Random Loot | 3 | 3 случайных .asset |
| Clear Generated Equipment | — | Удаление папки `Generated/` |

**Папка вывода:** `Assets/Data/Equipment/Generated/{Weapons,Armor}/T{1-5}/`
**Источник:** `EquipmentGeneratorMenu.cs`, `EquipmentSceneSpawner.cs`

---

## 📦 Tools / Generate Assets

### Базовые (AssetGenerator.cs — из JSON)

| Пункт меню | Кол-во | Папка |
|---|---|---|
| All Assets from JSON | — | Запуск всех ниже |
| Cultivation Levels (10) | 10 | `Data/CultivationLevels/` |
| Elements (7) | 7 | `Data/Elements/` |
| Mortal Stages (6) | 6 | `Data/MortalStages/` |
| Clear All Generated | — | Удаление |

### Расширенные (AssetGeneratorExtended.cs — из JSON)

| Пункт меню | Кол-во | Папка |
|---|---|---|
| All Extended Assets (122) | 122 | Запуск всех ниже |
| Techniques (34) | 34 | `Data/Techniques/` |
| NPC Presets (15) | 15 | `Data/NPC/` |
| Equipment (39) | 39 | `Data/Equipment/` |
| Items (8) | 8 | `Data/Items/` |
| Materials (17) | 17 | `Data/Materials/` |
| Storage Rings (4) | 4 | `Data/StorageRings/` |
| Validate All Assets | — | Проверка целостности |
| Clear Extended Assets | — | Удаление |

### Формации (FormationAssetGenerator.cs)

| Пункт меню | Кол-во | Папка |
|---|---|---|
| Formation Assets (All) | 54 | Запуск всех ниже |
| Formation Data (24) | 24 | `Data/Formations/` |
| Formation Core Data (30) | 30 | `Data/FormationCores/` |
| Validate Formation Assets | — | Проверка |
| Clear Formation Assets | — | Удаление |

---

## 🏛️ Tools / Formation UI

| Пункт меню | Описание |
|---|---|
| Generate All UI Prefabs | Все префабы UI формаций |
| Generate Formation List Item | Элемент списка формаций |
| Generate Active Formation Item | Активная формация |
| Generate Placement Preview | Превью размещения |

**Источник:** `FormationUIPrefabsGenerator.cs`

---

## 🗺️ Tools / Generate Tile Sprites

| Пункт меню | Описание |
|---|---|
| Generate Tile Sprites | Процедурные спрайты тайлов |

**Источник:** `TileSpriteGenerator.cs`

---

## 🏠 Tools / Setup Test Location Scene

| Пункт меню | Описание |
|---|---|
| Setup Test Location Scene | Настройка тестовой локации |
| Setup Harvestable Layer | Слой собираемых объектов |

**Источник:** `TestLocationSetup.cs`, `HarvestableLayerSetup.cs`

---

## 🪟 Window / Scene Setup Tools

| Пункт меню | Описание |
|---|---|
| Scene Setup Tools | Окно EditorWindow для настройки сцены |

**Источник:** `SceneSetupTools.cs`

---

## ⚠️ Tools / Scene Patch Builder (DEPRECATED)

| Пункт меню | Описание |
|---|---|
| Apply All Pending Patches | ❌ Устарело — используйте Full Scene Builder |
| Validate Current Scene | ❌ Устарело |
| Show Applied Patches | ❌ Устарело |
| Reset Patch History | ❌ Устарело |

> **Не используйте** — заменено на Full Scene Builder.

---

## 📋 Шпаргалка по редкости (цвета маркеров лута)

| Редкость | Цвет | Вероятность |
|---|---|---|
| Common | Серый | Часто |
| Uncommon | Зелёный | Обычно |
| Rare | Синий | Редко |
| Epic | Фиолетовый | Очень редко |
| Legendary | Оранжевый | Экстремально редко |
| Mythic | Красный | Мифически редко |

---

## 🔄 Типичный рабочий процесс

### Первый запуск (чистый проект)
1. `Tools → Full Scene Builder → Build All (One Click)`
2. Дождаться завершения всех 19 фаз
3. Нажать **Play** — проверить сцену

### Быстрый тест генерации эквипа
1. Нажать **Ctrl+G** — 3 предмета рядом с Player
2. Подойти к маркеру — автовыбор по триггеру
3. Или **Ctrl+F1** — оружие сразу в инвентарь

### Полная генерация .asset файлов
1. `Tools → Equipment → Generate Full Set (T1)` — 99 SO
2. Или `All Tiers` — 495 SO (оружие + броня всех тиров)

### Пересоздание сцены
1. `Tools → Equipment → Clear Generated Equipment` (если нужно)
2. `Tools → Full Scene Builder → Build All (One Click)`

---

*Документ создан: 2026-04-29*
*Проект: Cultivation World Simulator (Unity 6.3 URP 2D)*
