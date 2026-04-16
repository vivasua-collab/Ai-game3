# Чекпоинт: Анализ регрессии спрайтов после Harvest System

**Дата:** 2026-04-16 06:36:40 UTC
**Статус:** complete
**Цель:** Выяснить, почему качественные AI-спрайты деревьев/камней/руды перестали использоваться после внедрения Harvest System (чекпоинт 04_15)

---

## Описание проблемы

После внедрения Harvest System (чекпоинт 04_15 и доработки 04_16) деревья и другие объекты стали отображаться как «калечья» — низкокачественные программные спрайты вместо красивых сгенерированных AI-спрайтов.

---

## Хронология поломки

### Этап 1: ДО Harvest System — всё работало

**Рендер-цепочка (рабочая):**
1. `TileMapController.GenerateTestMap()` → генерирует карту с объектами `Tree_Oak`, `Tree_Pine`, `Rock_Small`, `OreVein` и т.д.
2. `TileMapController.RenderMap()` → для КАЖДОГО объекта вызывает `GetObjectTile(objectType)`
3. `GetObjectTile()` → маппит `Tree_Oak/Pine/Birch` → `treeTile`, `Rock_Small` → `rockSmallTile`, `OreVein` → `oreVeinTile`
4. `treeTile` создаётся через `EnsureTile(treeTile, "obj_tree", ...)` → `LoadTileSprite("obj_tree")` → **`Assets/Sprites/Tiles/obj_tree.png`** ✅
5. Спрайт загружен, рендерится через tilemap — красивый AI-спрайт

**Спрайты на диске (Assets/Sprites/Tiles/):**
| Файл | Размер | Описание |
|------|--------|----------|
| `obj_tree.png` | 10 KB | Обработанный AI-спрайт дерева (64×64, RGBA, прозрачность) |
| `obj_rock_small.png` | 6.5 KB | Обработанный AI-спрайт маленького камня |
| `obj_rock_medium.png` | 7.7 KB | Обработанный AI-спрайт среднего камня |
| `obj_ore_vein.png` | 9.3 KB | Обработанный AI-спрайт жилы руды |
| `obj_bush.png` | 8.9 KB | Обработанный AI-спрайт куста |
| `obj_herb.png` | 11.6 KB | Обработанный AI-спрайт травы |
| `obj_chest.png` | 8.9 KB | Обработанный AI-спрайт сундука |

**Все 7 файлов — реальные AI-спрайты, корректно загружались через `LoadTileSprite()`.**

### Этап 2: Внедрение Harvest System (чекпоинт 04_15)

**Ключевое изменение в `TileMapController.RenderMap()`:**
```csharp
// Строка 986-991: пропускать harvestable-объекты
if (obj.isHarvestable)
    continue;  // ← НЕ рендерить в tilemap!
```

**Результат:** Деревья (Tree_Oak/Pine/Birch), камни (Rock_Small/Medium), руда (OreVein), кусты (Bush_Berry), трава (Herb) — **перестали рендериться через tilemap**.

Вместо этого теперь их рендерит **HarvestableSpawner** как отдельные GameObject.

### Этап 3: КРИТИЧЕСКАЯ ОШИБКА — неверный маппинг спрайтов в HarvestableSpawner

**HarvestableSpawner.GetSpritePath()** — строки 298-312:
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/tree_oak",    // ← ФАЙЛА НЕТ!
TileObjectType.Tree_Pine  => "Sprites/Tiles/tree_pine",   // ← ФАЙЛА НЕТ!
TileObjectType.Tree_Birch => "Sprites/Tiles/tree_birch",  // ← ФАЙЛА НЕТ!
TileObjectType.Rock_Small  => "Sprites/Tiles/rock_small",  // ← ФАЙЛА НЕТ!
TileObjectType.Rock_Medium => "Sprites/Tiles/rock_medium", // ← ФАЙЛА НЕТ!
TileObjectType.OreVein    => "Sprites/Tiles/ore_vein",    // ← ФАЙЛА НЕТ!
TileObjectType.Bush       => "Sprites/Tiles/bush",        // ← ФАЙЛА НЕТ!
TileObjectType.Bush_Berry => "Sprites/Tiles/bush_berry",  // ← ФАЙЛА НЕТ!
TileObjectType.Herb       => "Sprites/Tiles/herb",        // ← ФАЙЛА НЕТ!
```

**Реальные имена файлов на диске:**
```
Assets/Sprites/Tiles/obj_tree.png         ← "obj_tree", НЕ "tree_oak"
Assets/Sprites/Tiles/obj_rock_small.png   ← "obj_rock_small", НЕ "rock_small"
Assets/Sprites/Tiles/obj_rock_medium.png  ← "obj_rock_medium", НЕ "rock_medium"
Assets/Sprites/Tiles/obj_ore_vein.png     ← "obj_ore_vein", НЕ "ore_vein"
Assets/Sprites/Tiles/obj_bush.png         ← "obj_bush", НЕ "bush"
Assets/Sprites/Tiles/obj_herb.png         ← "obj_herb", НЕ "herb"
```

**НИ ОДИН путь в HarvestableSpawner не совпадает с реальными файлами.** Все загрузки возвращают `null`, и срабатывает fallback на `CreateProceduralSprite()` — отсюда «калечья».

### Этап 4: Для сравнения — ResourceSpawner работает ПРАВИЛЬНО

**ResourceSpawner.LoadResourceSprite()** — строки 599-613:
```csharp
"ore"         => "obj_ore_vein",    // ✅ ВЕРНО
"stone"       => "obj_rock_medium", // ✅ ВЕРНО
"wood"        => "obj_tree",        // ✅ ВЕРНО
"herb"        => "obj_herb",        // ✅ ВЕРНО
"berries"     => "obj_bush",        // ✅ ВЕРНО
```

ResourceSpawner использует **префикс `obj_`**, который совпадает с реальными файлами. Именно поэтому автоподбираемые ресурсы (qi_crystal, mushroom и т.д.) выглядят нормально.

### Этап 5: Дополнительная проблема — отсутствующие спрайты для подтипов

Для `Tree_Oak`, `Tree_Pine`, `Tree_Birch` в проекте существует **ТОЛЬКО ОДИН** спрайт `obj_tree.png`. TileMapController решал это просто: все три типа маппились на один и тот же `treeTile`.

HarvestableSpawner же пытается загрузить **три разных** спрайта, ни одного из которых не существует.

---

## Корневая причина (итог)

| # | Проблема | Где | Влияние |
|---|----------|-----|---------|
| **R1** | **Неверный маппинг имён спрайтов** — без префикса `obj_` | `HarvestableSpawner.GetSpritePath()` строки 298-312 | 100% harvestable-объектов используют процедурный fallback |
| **R2** | **Неучёт подтипов деревьев** — отдельные пути для Tree_Oak/Pine/Birch при одном файле `obj_tree.png` | `HarvestableSpawner.GetSpritePath()` | Нет шансов загрузить даже если исправить префикс |
| **R3** | **Удаление рендера через tilemap** — `RenderMap()` пропускает `isHarvestable` | `TileMapController.RenderMap()` строка 990 | Старый рабочий путь рендера полностью отключён |

---

## План исправления

### Вариант A: Исправить маппинг в HarvestableSpawner (рекомендуемый)

Заменить `GetSpritePath()` на маппинг, совпадающий с реальными файлами:

| TileObjectType | Текущий (сломанный) | Правильный |
|---|---|---|
| Tree_Oak | `Sprites/Tiles/tree_oak` | `Sprites/Tiles/obj_tree` |
| Tree_Pine | `Sprites/Tiles/tree_pine` | `Sprites/Tiles/obj_tree` |
| Tree_Birch | `Sprites/Tiles/tree_birch` | `Sprites/Tiles/obj_tree` |
| Rock_Small | `Sprites/Tiles/rock_small` | `Sprites/Tiles/obj_rock_small` |
| Rock_Medium | `Sprites/Tiles/rock_medium` | `Sprites/Tiles/obj_rock_medium` |
| OreVein | `Sprites/Tiles/ore_vein` | `Sprites/Tiles/obj_ore_vein` |
| Bush | `Sprites/Tiles/bush` | `Sprites/Tiles/obj_bush` |
| Bush_Berry | `Sprites/Tiles/bush_berry` | `Sprites/Tiles/obj_bush` |
| Herb | `Sprites/Tiles/herb` | `Sprites/Tiles/obj_herb` |

Также исправить `GetDepletedSpritePath()` — спрайтов stump/rock_depleted/ore_depleted/bush_depleted тоже **не существует** на диске. Нужен fallback или программная генерация depleted-спрайтов.

Также исправить `LoadSpriteFromAssets()` — путь формируется как `Assets/{relativePath}.png`, что даёт `Assets/Sprites/Tiles/obj_tree.png` ✅. Но при Build — `Resources.Load<Sprite>("Tiles/obj_tree")`, а Resources/Sprites/ не содержит тайловых спрайтов — ещё одна потенциальная проблема.

### Вариант B: Генерация недостающих спрайтов

Создать отдельные AI-спрайты для каждого подтипа:
- `obj_tree_oak.png`, `obj_tree_pine.png`, `obj_tree_birch.png`
- `obj_bush_berry.png`
- `obj_stump.png`, `obj_rock_depleted.png`, `obj_ore_depleted.png`, `obj_bush_depleted.png`

Это идеальный вариант, но требует AI-генерации 8+ новых спрайтов.

### Вариант C: Гибридный (A + частично B)

1. Немедленно: исправить маппинг по варианту A — вернуть AI-спрайты
2. Позже: сгенерировать недостающие подтипы (variant B)
3. Depleted-спрайты: программные (серо-коричневые фильтры над обычными спрайтами)

---

## Затронутые файлы

| Файл | Проблема | Что менять |
|---|---|---|
| `HarvestableSpawner.cs:298-312` | `GetSpritePath()` — неверные пути | Заменить на `obj_*` маппинг |
| `HarvestableSpawner.cs:318-332` | `GetDepletedSpritePath()` — несуществующие пути | Fallback или программные depleted |
| `TileMapController.cs:990` | `if (obj.isHarvestable) continue;` — отключён tilemap-рендер | Оставить (объекты теперь GameObject) |
| `HarvestableSpawner.cs:337-354` | `LoadSpriteFromAssets()` — путь формируется корректно для Editor, но Resources.Load не найдёт спрайты в Build | Перенести спрайты в Resources или использовать Addressables |

---

*Создано: 2026-04-16 06:26:27 UTC*
*Редактировано: 2026-04-16 06:36:40 UTC*
