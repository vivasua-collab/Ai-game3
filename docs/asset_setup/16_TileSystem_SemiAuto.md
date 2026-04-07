# Настройка Таиловой Системы (Полуавтомат)

**Создано:** 2026-04-07 14:24:05 UTC
**Редактировано:** 2026-04-07 14:24:05 UTC

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
| Настройка камеры | ✅ Автоматически |
| Создание освещения | ✅ Автоматически |
| Назначение спрайтов в TileMapController | ❌ Руками |
| Добавление Player | ❌ Руками |
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
├── Main Camera           ← Orthographic, Size: 20
├── Directional Light     ← Intensity: 1
├── Grid                  ← cellSize: (2, 2, 1)
│   ├── Terrain           ← Tilemap для поверхности
│   └── Objects           ← Tilemap для объектов
└── TileMapController     ← MonoBehaviour скрипт
    └── SpawnedObjects    ← Родитель для спавна (runtime)
```

---

## Шаг 3: Назначение спрайтов в TileMapController (ВРУКАМИ)

**Файл сцены откроется автоматически. Если нет — открой `Assets/Scenes/TestLocation.unity`**

1. Выбери объект **TileMapController** в Hierarchy
2. В Inspector найди секции **Terrain Tiles** и **Object Tiles**
3. Перетащи спрайты из `Assets/Sprites/Tiles/`:

### Terrain Tiles:

| Поле в Inspector | Спрайт из Project |
|-----------------|-------------------|
| Grass Tile | `terrain_grass` |
| Dirt Tile | `terrain_dirt` |
| Stone Tile | `terrain_stone` |
| Water Shallow Tile | `terrain_water_shallow` |
| Water Deep Tile | `terrain_water_deep` |
| Sand Tile | `terrain_sand` |
| Void Tile | `terrain_void` |

### Object Tiles:

| Поле в Inspector | Спрайт из Project |
|-----------------|-------------------|
| Tree Tile | `obj_tree` |
| Rock Small Tile | `obj_rock_small` |
| Rock Medium Tile | `obj_rock_medium` |
| Bush Tile | `obj_bush` |
| Chest Tile | `obj_chest` |

---

## Шаг 4: Настройка размеров карты (ВРУКАМИ, опционально)

Выбери **TileMapController** в Hierarchy:

```
Settings:
├── Default Width:  30     ← ширина в тайлах (60м)
├── Default Height: 20     ← высота в тайлах (40м)
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

## Шаг 5: Настройка коллизий (ВРУКАМИ)

### 5.1 Добавить TilemapCollider2D на Terrain:

1. Выбери объект **Grid → Terrain** в Hierarchy
2. **Add Component → Tilemap Collider 2D**
3. Настрой:
```
Tilemap Collider 2D:
└── Used By Composite: ☐ (отключено)
```

### 5.2 Опционально: Composite Collider:

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

## Шаг 6: Добавление Player (ВРУКАМИ)

Используй существующий Player или создай новый:

### Вариант А: Из существующего префаба

1. Перетащи `Assets/Prefabs/Player/Player.prefab` в Hierarchy
2. Установи Position: (30, 20, 0) — центр карты 30×20 тайлов

### Вариант Б: Через Scene Setup Tools

1. Открой **Window → Scene Setup Tools**
2. Нажми **Create Player GameObject**
3. Настрой позицию вручную

---

## Шаг 7: Создание Tile assets (ВРУКАМИ, опционально)

Для продвинутой настройки создай GameTile assets:

### 7.1 Terrain Tile:

1. Правый клик в Project: **Create → Cultivation → Terrain Tile**
2. Назови `Tile_Grass`
3. Настрой в Inspector:
```
TerrainTile:
├── Sprite: terrain_grass
├── Color: Белый
├── Terrain Type: Grass
├── Move Cost: 1
├── Is Passable: ☑
└── Flags: Passable
```

### 7.2 Object Tile:

1. Правый клик в Project: **Create → Cultivation → Object Tile**
2. Назови `Tile_Tree`
3. Настрой в Inspector:
```
ObjectTile:
├── Sprite: obj_tree
├── Color: Белый
├── Object Type: Tree_Oak
├── Width: 1
├── Height: 1
├── Durability: 200
├── Blocks Vision: ☑
├── Provides Cover: ☑
├── Is Interactable: ☐
├── Is Harvestable: ☑
├── Move Cost: 0
├── Is Passable: ☐
└── Flags: None
```

---

## Шаг 8: Сохранение сцены (ВРУКАМИ)

1. **Ctrl + S** или **File → Save**
2. Сцена сохранится в `Assets/Scenes/TestLocation.unity`

---

## Шаг 9: Проверка

### Нажми Play:

**Console должна показать:**
```
Generated map: 30x20 tiles (60x40 meters)
```

**На сцене должно быть:**
- Зелёная трава (terrain_grass)
- Каменная площадка в центре (terrain_stone)
- Пруд в углу (terrain_water_shallow/deep)
- Деревья по карте (obj_tree)
- Камни (obj_rock_small/medium)
- Кусты (obj_bush)

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

---

## Настройка TileMapController через Inspector

При выборе TileMapController доступны поля:

### Tilemap References:
```
Terrain Tilemap:    ← ссылка на Grid/Terrain
Object Tilemap:     ← ссылка на Grid/Objects
Overlay Tilemap:    ← опционально для эффектов
```

### Terrain Tiles:
```
Grass Tile:         ← terrain_grass sprite
Dirt Tile:          ← terrain_dirt sprite
Stone Tile:         ← terrain_stone sprite
Water Shallow Tile: ← terrain_water_shallow sprite
Water Deep Tile:    ← terrain_water_deep sprite
Sand Tile:          ← terrain_sand sprite
Void Tile:          ← terrain_void sprite
```

### Object Tiles:
```
Tree Tile:          ← obj_tree sprite
Rock Small Tile:    ← obj_rock_small sprite
Rock Medium Tile:   ← obj_rock_medium sprite
Bush Tile:          ← obj_bush sprite
Chest Tile:         ← obj_chest sprite
```

### Settings:
```
Default Width: 30       ← ширина карты в тайлах
Default Height: 20      ← высота карты в тайлах
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

### События:

```csharp
controller.OnTileChanged += (tile) => {
    Debug.Log($"Tile changed: {tile.x}, {tile.y}");
};

controller.OnMapGenerated += (mapData) => {
    Debug.Log($"Map generated: {mapData.width}x{mapData.height}");
};
```

---

## Типичные ошибки

| Ошибка | Причина | Решение |
|--------|---------|---------|
| Спрайты не назначены | Пустые поля в Inspector | Перетащи спрайты в TileMapController |
| Карта пустая | Generate On Start отключен | Включи чекбокс или нажми Generate Test Map |
| Коллизии не работают | Нет TilemapCollider2D | Добавь компонент на Terrain |
| Камера не видит карту | Z позиция или Size | Camera Z: -10, Size: 20 |
| Спрайты размыты | Filter Mode не Point | Выбери спрайт → Filter Mode: Point |

---

## Структура файлов

```
Assets/
├── Scripts/Tile/
│   ├── TileEnums.cs              ← Перечисления
│   ├── TileData.cs               ← Данные тайла
│   ├── TileMapData.cs            ← Данные карты
│   ├── TileMapController.cs      ← Контроллер
│   ├── GameTile.cs               ← Custom TileBase
│   ├── CultivationGame.TileSystem.asmdef
│   └── Editor/
│       ├── TileSpriteGenerator.cs    ← Генератор спрайтов
│       └── TestLocationSetup.cs      ← Создание сцены
├── Sprites/Tiles/
│   ├── terrain_*.png             ← Спрайты поверхности
│   └── obj_*.png                 ← Спрайты объектов
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
| Настроить камеру | 🤖 Скрипт |
| Назначить спрайты | ✋ Руками |
| Добавить коллизии | ✋ Руками |
| Добавить Player | ✋ Руками |
| Создать GameTile assets | ✋ Руками |

---

## Размерности системы

| Параметр | Значение |
|----------|----------|
| Размер тайла | 2×2 м |
| Tilemap cellSize | (2, 2, 1) |
| Спрайт PPU | 64 |
| Размер тестовой карты | 30×20 тайлов |
| Размер тестовой карты | 60×40 м |
| Диапазон высот (Z) | -5 .. +5 |

---

*Документ создан: 2026-04-07 14:24:05 UTC*
