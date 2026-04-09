# Настройка Таиловой Системы (Полуавтомат)

**Создано:** 2026-04-07 14:24:05 UTC
**Редактировано:** 2026-04-09 11:15:00 UTC
**Версия:** 1.1 — обновлено для TileBase, добавлены TestLocationGameController и DestructibleObjectController

**Инструменты:** `Tools → Generate Tile Sprites` и `Tools → Setup Test Location Scene`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание спрайтов тайлов (terrain, objects) | ✅ Автоматически |
| Создание сцены TestLocation | ✅ Автоматически |
| Создание Grid | ✅ Автоматически |
| Создание Tilemap (Terrain) | ✅ Автоматически |
| Создание Tilemap (Objects) | ✅ Автоматически |
| Создание TileMapController | ✅ Автоматически |
| Создание TestLocationGameController | ✅ Автоматически |
| Создание DestructibleObjectController | ✅ Автоматически |
| Создание UI (HUD, слайдеры, тексты) | ✅ Автоматически |
| Настройка камеры | ✅ Автоматически |
| Создание освещения | ✅ Автоматически |
| Создание GameTile assets | ❌ Руками |
| Назначение TileBase в TileMapController | ❌ Руками |
| Добавление Player префаба | ❌ Руками |
| Настройка коллизий | ❌ Руками |

---

## Шаг 1: Генерация спрайтов (АВТОМАТИЧЕСКИ)

**Действия:**
1. Открой меню: **Tools → Generate Tile Sprites**
2. Дождись сообщения в Console: `Generated tile sprites at Assets/Sprites/Tiles`

**Результат в Project:**
```
Assets/Sprites/Tiles/
├── terrain_grass.png          ← Трава
├── terrain_dirt.png           ← Земля
├── terrain_stone.png          ← Камень
├── terrain_water_shallow.png  ← Мелкая вода
├── terrain_water_deep.png     ← Глубокая вода
├── terrain_sand.png           ← Песок
├── terrain_snow.png           ← Снег
├── terrain_void.png           ← Пустота
├── obj_tree.png               ← Дерево
├── obj_rock_small.png         ← Маленький камень
├── obj_rock_medium.png        ← Средний камень
├── obj_bush.png               ← Куст
└── obj_chest.png              ← Сундук
```

**Параметры спрайтов:**
- Размер: 64×64 пикселя
- Pixels Per Unit: 64
- Filter Mode: Point

---

## Шаг 2: Создание тестовой сцены (АВТОМАТИЧЕСКИ)

**Действия:**
1. Открой меню: **Tools → Setup Test Location Scene**
2. Дождись сообщения в Console: `Created test location scene at Assets/Scenes/TestLocation.unity`
3. Сцена откроется автоматически

**Результат в Hierarchy:**
```
TestLocation (сцена)
├── Main Camera           ← Orthographic, Size: 15
├── Directional Light     ← Intensity: 1
├── Grid                  ← cellSize: (2, 2, 1)
│   ├── Terrain           ← Tilemap для поверхности
│   └── Objects           ← Tilemap для объектов
├── TileMapController     ← Управление картой
├── GameController        ← Управление игрой
│   └── DestructibleObjectController ← Разрушаемые объекты
└── GameUI                ← Canvas с HUD
    ├── HUD               ← Панель со статусом
    │   ├── LocationText  ← Название локации
    │   ├── PositionText  ← Координаты
    │   ├── HealthBar     ← HP
    │   ├── HealthText    ← HP текст
    │   ├── QiBar         ← Ци
    │   ├── QiText        ← Ци текст
    │   └── StaminaBar    ← Выносливость
    └── Instructions      ← Панель управления
```

---

## Шаг 3: Создание GameTile Assets (ВРУКАМИ) ⚠️ ВАЖНО

**TileMapController использует `TileBase` (GameTile), а не Sprite напрямую!**

### 3.1 Создать папку для тайлов:

```
Assets/Tiles/
├── Terrain/
└── Objects/
```

### 3.2 Создать TerrainTile assets:

Для каждого типа поверхности:

1. Правый клик в `Assets/Tiles/Terrain/`
2. **Create → Cultivation → TerrainTile**
3. Назови по типу: `Tile_Grass`, `Tile_Stone`, и т.д.
4. В Inspector назначь:

| Asset Name | Sprite | Terrain Type | Move Cost | Is Passable | Flags |
|------------|--------|--------------|-----------|-------------|-------|
| Tile_Grass | terrain_grass | Grass | 1.0 | ☑ | Passable |
| Tile_Dirt | terrain_dirt | Dirt | 1.0 | ☑ | Passable |
| Tile_Stone | terrain_stone | Stone | 1.0 | ☑ | Passable |
| Tile_WaterShallow | terrain_water_shallow | Water_Shallow | 2.0 | ☑ | Passable, Swimable |
| Tile_WaterDeep | terrain_water_deep | Water_Deep | 0.0 | ☐ | Swimable, Flyable |
| Tile_Sand | terrain_sand | Sand | 1.2 | ☑ | Passable |
| Tile_Void | terrain_void | Void | 0.0 | ☐ | None |

### 3.3 Создать ObjectTile assets:

Для каждого типа объекта:

1. Правый клик в `Assets/Tiles/Objects/`
2. **Create → Cultivation → ObjectTile**
3. Назови по типу: `Tile_Tree`, `Tile_RockSmall`, и т.д.
4. В Inspector назначь:

| Asset Name | Sprite | Object Type | Durability | Blocks Vision | Harvestable |
|------------|--------|-------------|------------|---------------|-------------|
| Tile_Tree | obj_tree | Tree_Oak | 200 | ☑ | ☑ |
| Tile_RockSmall | obj_rock_small | Rock_Small | 100 | ☐ | ☑ |
| Tile_RockMedium | obj_rock_medium | Rock_Medium | 300 | ☑ | ☑ |
| Tile_Bush | obj_bush | Bush | 50 | ☐ | ☐ |
| Tile_Chest | obj_chest | Chest | 50 | ☐ | ☐ |

---

## Шаг 4: Назначение TileBase в TileMapController (ВРУКАМИ)

1. Выбери объект **TileMapController** в Hierarchy
2. В Inspector найди секции **Terrain Tiles** и **Object Tiles**
3. Перетащи GameTile assets из `Assets/Tiles/`:

### Terrain Tiles (TileBase):

| Поле в Inspector | GameTile Asset |
|-----------------|----------------|
| Grass Tile | `Tile_Grass` |
| Dirt Tile | `Tile_Dirt` |
| Stone Tile | `Tile_Stone` |
| Water Shallow Tile | `Tile_WaterShallow` |
| Water Deep Tile | `Tile_WaterDeep` |
| Sand Tile | `Tile_Sand` |
| Void Tile | `Tile_Void` |

### Object Tiles (TileBase):

| Поле в Inspector | GameTile Asset |
|-----------------|----------------|
| Tree Tile | `Tile_Tree` |
| Rock Small Tile | `Tile_RockSmall` |
| Rock Medium Tile | `Tile_RockMedium` |
| Bush Tile | `Tile_Bush` |
| Chest Tile | `Tile_Chest` |

---

## Шаг 5: Настройка TestLocationGameController (ВРУКАМИ)

Выбери **GameController** в Hierarchy. В Inspector:

### References:
```
Tile Map Controller:    ← Автоматически назначен
Player Spawn Point:     ← Опционально (создай Empty GameObject)
Player Prefab:          ← Переташи Player.prefab (если есть)
```

### UI References (автоматически найдены по именам):
```
Health Bar:    ← Автоматически (имя: "HealthBar")
Qi Bar:        ← Автоматически (имя: "QiBar")
Stamina Bar:   ← Автоматически (имя: "StaminaBar")
Health Text:   ← Автоматически (имя: "HealthText")
Qi Text:       ← Автоматически (имя: "QiText")
Location Text: ← Автоматически (имя: "LocationText")
Position Text: ← Автоматически (имя: "PositionText")
```

### Settings:
```
Spawn Player On Start: ☑  ← Автоматический спавн игрока
Show Debug Info: ☑        ← Показывать позицию в тайлах
```

---

## Шаг 6: Настройка DestructibleObjectController (ВРУКАМИ)

Выбери **GameController → DestructibleObjectController** (компонент). В Inspector:

### References:
```
Tile Map Controller:  ← Автоматически назначен
Object Tilemap:       ← Переташи Grid/Objects
Resource Pickup Prefab: ← Опционально (для дропа)
```

### Settings:
```
Spawn Resource Pickups: ☑  ← Создавать объекты дропа
Destructible Layer:        ← Слой для разрушаемых объектов
```

---

## Шаг 7: Настройка размеров карты (ВРУКАМИ, опционально)

Выбери **TileMapController** в Hierarchy:

```
Settings:
├── Default Width:  50     ← ширина в тайлах (100м)
├── Default Height: 50     ← высота в тайлах (100м)
└── Generate On Start: ☑   ← авто-генерация при старте
```

**Размеры карты:**
| Ширина × Высота (тайлы) | Размер в метрах |
|------------------------|-----------------|
| 25 × 25 | 50 × 50 м |
| 50 × 50 | 100 × 100 м |
| 100 × 100 | 200 × 200 м |
| 250 × 250 | 500 × 500 м |

---

## Шаг 8: Настройка коллизий (ВРУКАМИ)

### 8.1 Добавить TilemapCollider2D на Terrain:

1. Выбери объект **Grid → Terrain** в Hierarchy
2. **Add Component → Tilemap Collider 2D**
3. Настрой:
```
Tilemap Collider 2D:
└── Used By Composite: ☐ (отключено)
```

### 8.2 Опционально: Composite Collider:

Для оптимизации коллизий:

1. На том же объекте **Add Component → Composite Collider 2D**
2. В **Tilemap Collider 2D** включи: `Used By Composite: ☑`
3. Настрой Composite:
```
Composite Collider 2D:
├── Is Trigger: ☐
├── Used By Effector: ☐
├── Offset: (0, 0)
└── Geometry Type: Outlines
```

---

## Шаг 9: Добавление Player (ВРУКАМИ)

### Вариант А: Из существующего префаба

1. Переташи `Assets/Prefabs/Player/Player.prefab` в поле **Player Prefab** в TestLocationGameController
2. Игрок заспавнится автоматически при старте

### Вариант Б: Без префаба (временно)

TestLocationGameController создаст базового игрока автоматически:
- Rigidbody2D (Dynamic, gravity = 0)
- CircleCollider2D (radius = 0.4)
- PlayerController, BodyController, QiController
- Временный спрайт (голубой круг)

---

## Шаг 10: Сохранение сцены (ВРУКАМИ)

1. **Ctrl + S** или **File → Save**
2. Сцена сохранится в `Assets/Scenes/TestLocation.unity`

---

## Шаг 11: Проверка

### Нажми Play:

**Console должна показать:**
```
Generated map: 50x50 tiles (100x100 meters)
[TestLocationGameController] Player spawned at (50, 50, 0)
[DestructibleObjectController] Cached XX destructible objects
```

**На сцене должно быть:**
- Зелёная трава (terrain_grass)
- Каменная площадка в центре (terrain_stone)
- Пруд в углу (terrain_water_shallow/deep)
- Деревья по карте (obj_tree)
- Камни (obj_rock_small/medium)
- Кусты (obj_bush)
- Игрок (голубой круг или префаб)

### Визуальная проверка:

| Элемент | Цвет | Расположение |
|---------|------|--------------|
| Трава | Зелёный | Основа карты |
| Камень | Серый | Центр (7×7 тайлов) |
| Вода мелкая | Голубой, светлый | Край пруда |
| Вода глубокая | Синий, тёмный | Центр пруда |
| Деревья | Зелёный + коричневый | Случайно |
| Камни | Серый | Случайно |
| Кусты | Светло-зелёный | Случайно |
| Игрок | Голубой круг | Центр карты |

---

## Настройка TileMapController через Inspector

При выборе TileMapController доступны поля:

### Tilemap References:
```
Terrain Tilemap:    ← ссылка на Grid/Terrain (автоматически)
Object Tilemap:     ← ссылка на Grid/Objects (автоматически)
Overlay Tilemap:    ← опционально для эффектов
```

### Terrain Tiles (TileBase):
```
Grass Tile:         ← Tile_Grass asset
Dirt Tile:          ← Tile_Dirt asset
Stone Tile:         ← Tile_Stone asset
Water Shallow Tile: ← Tile_WaterShallow asset
Water Deep Tile:    ← Tile_WaterDeep asset
Sand Tile:          ← Tile_Sand asset
Void Tile:          ← Tile_Void asset
```

### Object Tiles (TileBase):
```
Tree Tile:          ← Tile_Tree asset
Rock Small Tile:    ← Tile_RockSmall asset
Rock Medium Tile:   ← Tile_RockMedium asset
Bush Tile:          ← Tile_Bush asset
Chest Tile:         ← Tile_Chest asset
```

### Settings:
```
Default Width: 50       ← ширина карты в тайлах
Default Height: 50      ← высота карты в тайлах
Generate On Start: ☑    ← генерация при запуске
```

---

## API для использования в коде

### Получить тайл по координатам:

```csharp
using CultivationGame.TileSystem;

// Получить контроллер
var controller = FindFirstObjectByType<TileMapController>();

// Получить тайл
TileData tile = controller.GetTile(x, y);
TileData tile = controller.GetTileAtWorld(worldPos);

// Проверить проходимость
bool canWalk = tile.IsPassable();

// Изменить поверхность
controller.SetTerrain(x, y, TerrainType.Water_Deep);

// Добавить объект
controller.AddObject(x, y, TileObjectType.Tree_Oak);
```

### Разрушение объектов:

```csharp
using CultivationGame.TileSystem;

// Получить контроллер
var destructible = FindFirstObjectByType<DestructibleObjectController>();

// Нанести урон объекту
int actualDamage = destructible.DamageObjectAtTile(x, y, 50, DamageType.Physical);

// Проверить прочность
var (current, max) = destructible.GetObjectDurability(x, y);
```

### События:

```csharp
// TileMapController
controller.OnTileChanged += (tile) => {
    Debug.Log($"Tile changed: {tile.x}, {tile.y}");
};

controller.OnMapGenerated += (mapData) => {
    Debug.Log($"Map generated: {mapData.width}x{mapData.height}");
};

// DestructibleObjectController
destructible.OnObjectDestroyed += (info) => {
    Debug.Log($"Object destroyed: {info.ObjectType}");
};

destructible.OnResourceDropped += (drop) => {
    Debug.Log($"Resource dropped: {drop.ResourceId} x{drop.Amount}");
};
```

---

## Типичные ошибки

| Ошибка | Причина | Решение |
|--------|---------|---------|
| Карта пустая | TileBase не назначены | Создай GameTile assets и назначь в TileMapController |
| Тайлы не отображаются | Sprite не назначен в GameTile | Назначь спрайт в GameTile asset |
| Карта не генерируется | Generate On Start отключен | Включи чекбокс или нажми Generate Test Map |
| Коллизии не работают | Нет TilemapCollider2D | Добавь компонент на Terrain |
| Камера не видит карту | Z позиция или Size | Camera Z: -10, Size: 15 |
| Спрайты размыты | Filter Mode не Point | Выбери спрайт → Filter Mode: Point |
| Игрок не спавнится | Нет Player Prefab | Добавь префаб или включи авто-создание |

---

## Структура файлов

```
Assets/
├── Scripts/Tile/
│   ├── TileEnums.cs              ← Перечисления
│   ├── TileData.cs               ← Данные тайла (v1.1)
│   ├── TileMapData.cs            ← Данные карты
│   ├── TileMapController.cs      ← Контроллер карты
│   ├── GameTile.cs               ← Custom TileBase
│   ├── DestructibleObjectController.cs ← Разрушаемые объекты
│   ├── ResourcePickup.cs         ← Подбор ресурсов
│   ├── CultivationGame.TileSystem.asmdef
│   └── Editor/
│       ├── TileSpriteGenerator.cs    ← Генератор спрайтов
│       └── TestLocationSetup.cs      ← Создание сцены (v1.1)
├── Scripts/Data/
│   └── TerrainConfig.cs          ← Конфигурация типов поверхности
├── Scripts/World/
│   └── TestLocationGameController.cs ← Контроллер тестовой локации
├── Sprites/Tiles/
│   ├── terrain_*.png             ← Спрайты поверхности
│   └── obj_*.png                 ← Спрайты объектов
├── Tiles/
│   ├── Terrain/                  ← TerrainTile assets
│   └── Objects/                  ← ObjectTile assets
└── Scenes/
    └── TestLocation.unity        ← Тестовая сцена
```

---

## Шпаргалка: Что руками, что скриптом

| Задача | Способ |
|--------|--------|
| Создать спрайты тайлов | 🤖 Скрипт |
| Создать сцену | 🤖 Скрипт |
| Создать Grid + Tilemap | 🤖 Скрипт |
| Создать TileMapController | 🤖 Скрипт |
| Создать TestLocationGameController | 🤖 Скрипт |
| Создать DestructibleObjectController | 🤖 Скрипт |
| Создать UI | 🤖 Скрипт |
| Настроить камеру | 🤖 Скрипт |
| Создать GameTile assets | ✋ Руками |
| Назначить TileBase | ✋ Руками |
| Добавить коллизии | ✋ Руками |
| Добавить Player префаб | ✋ Руками |

---

## Размерности системы

| Параметр | Значение |
|----------|----------|
| Размер тайла | 2×2 м |
| Tilemap cellSize | (2, 2, 1) |
| Спрайт PPU | 64 |
| Размер тестовой карты | 50×50 тайлов |
| Размер тестовой карты | 100×100 м |
| Диапазон высот (Z) | -5 .. +5 |

---

## Контроллеры сцены

### TileMapController
- Управляет генерацией и отображением карты
- Хранит TileMapData с данными всех тайлов
- Предоставляет API для доступа и изменения тайлов

### TestLocationGameController
- Управляет спавном игрока
- Связывает UI с системами игрока
- Показывает debug информацию (позиция в тайлах)

### DestructibleObjectController
- Управляет разрушением объектов
- Создаёт дроп ресурсов при разрушении
- Кэширует разрушаемые объекты для производительности

---

*Документ создан: 2026-04-07 14:24:05 UTC*
*Обновлено: 2026-04-09 11:15:00 UTC — версия 1.1*
