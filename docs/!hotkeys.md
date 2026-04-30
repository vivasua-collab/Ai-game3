# ⌨️ Горячие клавиши — Cultivation World Simulator

**Создано:** 2026-04-30 08:09:40 UTC
**Редактировано:** 2026-04-30 08:09:40 UTC
**Версия:** 1.0

---

## Обзор

Все горячие клавиши работают **только в Unity Editor** (в режиме Edit или Play).
Символы в MenuItem: `%` = Ctrl, `#` = Shift, `&` = Alt.

---

## 🎒 Экипировка — Спавн в сцену

| Комбинация | Действие | Файл |
|---|---|---|
| **Ctrl+G** | 3 случайных предмета (L1) рядом с Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+Shift+G** | 10 случайных предметов (L1) рядом с Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+Alt+G** | 5 предметов уровня 3 рядом с Player | `EquipmentSceneSpawner.cs` |

> Спавнит `ResourcePickup` с `EquipmentData`. Цвет маркера по редкости.
> Не создаёт .asset файлов — предметы существуют только в сцене.

---

## 🎒 Экипировка — Прямо в инвентарь

| Комбинация | Действие | Файл |
|---|---|---|
| **Ctrl+F1** | 1 оружие T1 → инвентарь Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+F2** | 1 броня T1 → инвентарь Player | `EquipmentSceneSpawner.cs` |
| **Ctrl+F3** | 3 случайных предмета → инвентарь Player | `EquipmentSceneSpawner.cs` |

> Предметы добавляются в `InventoryController` на Player.
> Если инвентарь полон — предупреждение в Console.

---

## 👤 NPC — Спавн в сцену

| Комбинация | Действие | Файл |
|---|---|---|
| **Ctrl+N** | 1 случайный NPC рядом с Player | `NPCSceneSpawner.cs` |
| **Ctrl+Shift+N** | 5 NPC разных ролей рядом с Player | `NPCSceneSpawner.cs` |
| **Ctrl+F5** | 1 Merchant рядом с Player | `NPCSceneSpawner.cs` |
| **Ctrl+F6** | 1 Monster/Enemy рядом с Player | `NPCSceneSpawner.cs` |

> Создаёт GameObject с NPCController + NPCAI + BodyController + QiController + NPCVisual + NPCInteractable.
> Генерация данных через NPCGenerator или GeneratorRegistry.

---

## 🎮 Игровые клавиши (Runtime)

| Клавиша | Действие |
|---|---|
| **I** | Открыть / закрыть инвентарь |
| **ESC** | Закрыть инвентарь / отменить действие |
| **Левый клик** | Выбрать / перетащить предмет |
| **Правый клик** | Контекстное меню предмета |
| **Shift+клик** | Переместить в хранилище |
| **Ctrl+клик** | Разделить стек |

---

## 📋 Полная таблица горячих клавиш Editor

### Экипировка (EquipmentSceneSpawner)

| Клавиша | MenuItem | Описание |
|---|---|---|
| Ctrl+G | `Tools/Equipment/Spawn In Scene/Random Loot x3` | 3 предмета L1 в сцену |
| Ctrl+Shift+G | `Tools/Equipment/Spawn In Scene/Random Loot x10` | 10 предметов L1 в сцену |
| Ctrl+Alt+G | `Tools/Equipment/Spawn In Scene/Random Loot L3 x5` | 5 предметов L3 в сцену |
| — | `Tools/Equipment/Spawn In Scene/Weapon (Random)` | 1 оружие в сцену |
| — | `Tools/Equipment/Spawn In Scene/Armor (Random)` | 1 броня в сцену |
| Ctrl+F1 | `Tools/Equipment/Add to Inventory/Weapon` | 1 оружие → инвентарь |
| Ctrl+F2 | `Tools/Equipment/Add to Inventory/Armor` | 1 броня → инвентарь |
| Ctrl+F3 | `Tools/Equipment/Add to Inventory/Random Loot x3` | 3 предмета → инвентарь |

### NPC (NPCSceneSpawner)

| Клавиша | MenuItem | Описание |
|---|---|---|
| Ctrl+N | `Tools/NPC/Spawn In Scene/Random NPC` | 1 случайный NPC в сцену |
| Ctrl+Shift+N | `Tools/NPC/Spawn In Scene/5 Random NPCs` | 5 NPC разных ролей в сцену |
| Ctrl+F5 | `Tools/NPC/Spawn In Scene/Merchant` | 1 Merchant в сцену |
| Ctrl+F6 | `Tools/NPC/Spawn In Scene/Monster` | 1 Monster в сцену |
| — | `Tools/NPC/Spawn In Scene/Guard` | 1 Guard в сцену |
| — | `Tools/NPC/Spawn In Scene/Elder` | 1 Elder в сцену |
| — | `Tools/NPC/Spawn In Scene/Enemy` | 1 Enemy в сцену |
| — | `Tools/NPC/Spawn In Scene/Cultivator` | 1 Cultivator в сцену |
| — | `Tools/NPC/Spawn In Scene/Disciple` | 1 Disciple в сцену |
| — | `Tools/NPC/Clear All NPCs` | Удалить все NPC из сцены |

### Экипировка — Генерация .asset файлов (без hotkey)

| MenuItem | Кол-во SO | Описание |
|---|---|---|
| `Tools/Equipment/Generate/Weapon Set (T1)` | 36 | 12 подтипов × 3 грейда |
| `Tools/Equipment/Generate/Weapon Set (All Tiers)` | 180 | ×5 тиров |
| `Tools/Equipment/Generate/Armor Set (T1)` | 63 | 7×3 вес.класс × 3 грейда |
| `Tools/Equipment/Generate/Armor Set (All Tiers)` | 315 | ×5 тиров |
| `Tools/Equipment/Generate/Full Set (T1)` | 99 | Оружие + броня T1 |
| `Tools/Equipment/Generate/Random Loot` | 3 | 3 случайных .asset |
| `Tools/Equipment/Clear Generated Equipment` | — | Удаление папки `Generated/` |

---

## 🎨 Цвета маркеров по редкости

| Редкость | Цвет | RGB |
|---|---|---|
| Common | Серый | (0.7, 0.7, 0.7) |
| Uncommon | Зелёный | (0.3, 0.9, 0.3) |
| Rare | Синий | (0.2, 0.5, 1.0) |
| Epic | Фиолетовый | (0.7, 0.2, 1.0) |
| Legendary | Оранжевый | (1.0, 0.6, 0.1) |
| Mythic | Красный | (1.0, 0.15, 0.15) |

---

## 🏗️ Full Scene Builder — Фазы (без hotkey)

Запускаются через меню: `Tools → Full Scene Builder → ...`

| # | Фаза | Описание |
|---|---|---|
| 00 | URP Asset Setup | URP Asset + Renderer2D + GraphicsSettings |
| 01 | Folders | Создание структуры папок |
| 02 | Tags and Layers | Теги, слои, Sorting Layers |
| 03 | Create Scene | Создание сцены MainScene |
| 04 | Camera and Light | Camera2D + Light2D |
| 05 | GameManager and Systems | GameManager + системные компоненты |
| 06 | Player | Player + все компоненты |
| 07 | UI | Canvas, HUD, EventSystem |
| 08 | Tilemap System | Grid, Tilemap, TileMapController |
| 09 | Generate Assets from JSON | SO из JSON |
| 10 | Generate Tile Sprites | Спрайты тайлов (процедурные) |
| 11 | Generate Formation UI Prefabs | Префабы UI формаций |
| 12 | Import TMP Essentials | TextMeshPro ресурсы |
| 13 | Save Scene | Сохранение сцены |
| 14 | Create Tile Assets | TileData, ResourceNodeData |
| 15 | Configure Test Location | Тестовая локация |
| 16 | Inventory Data | BackpackData + StorageRingData + volume |
| 17 | Inventory UI | InventoryScreen + панели + wiring |
| 18 | Inventory Components | SpiritStorage + StorageRing на Player |
| 19 | NPC Placement | 7 NPC на тестовой поляне |

---

## 🔄 Типичный рабочий процесс

### Быстрый тест эквипа
1. **Ctrl+G** — 3 предмета рядом с Player
2. Подойти к маркеру — автовыбор по триггеру
3. Или **Ctrl+F1** — оружие сразу в инвентарь

### Быстрый тест NPC
1. **Ctrl+N** — 1 случайный NPC рядом с Player
2. Или **Ctrl+Shift+N** — 5 NPC разных ролей
3. Подойти к NPC — автовзаимодействие

### Полная сборка сцены
1. `Tools → Full Scene Builder → Build All (One Click)`
2. Дождаться завершения всех 20 фаз (00–19)

---

*Документ создано: 2026-04-30 08:09:40 UTC*
*Проект: Cultivation World Simulator (Unity 6.3 URP 2D)*
