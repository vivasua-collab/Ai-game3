# Чекпоинт: Полный аудит визуальной регрессии

**Дата:** 2026-04-16 09:06 UTC
**Фаза:** 3 (Sprite regression — полный аудит после внедрения Variant B)
**Статус:** complete
**Цель:** Выяснить, почему после внедрения Variant B (фикс маппингов спрайтов) визуал ВСЁ ЕЩЁ сломан: белый фон, пропавшая руда, кривые деревья, невидимый игрок.

---

## Жалобы пользователя

1. AI-спрайты не показываются — всё сломано
2. Тайлы поверхности не покрывают сетку — виден белый фон
3. Тайлы руды — пропали
4. Деревья — кривые, некрасивые
5. Спрайт игрока — не виден

---

## АУДИТ — Найденные проблемы

### 🔴 BUG-VA1: Terrain-спрайты 64×64 при PPU=32 → белые зазоры между тайлами

**Файл:** `TileMapController.cs` строки 255-338 (CreateProceduralTileSprite)

**Суть:** Процедурные terrain-спрайты создаются 68×68 PPU=32 → 2.125 юнита (pixel bleed устраняет зазоры). НО загруженные AI terrain-спрайты из файлов — **64×64** при дефолтном PPU (Unity назначит PPU=100 или 100 при импорте).

| Спрайт | Размер | PPU (файл) | World Size | Ячейка Grid | Результат |
|--------|--------|------------|------------|-------------|-----------|
| Процедурный terrain | 68×68 | 32 | 2.125u | 2.0u | ✅ Перекрытие = нет зазоров |
| AI terrain из файла | 64×64 | 100 (default) | 0.64u | 2.0u | ❌ Недостаточный размер |
| AI terrain из файла | 64×64 | 32 | 2.0u | 2.0u | ❌ Ровно = зазоры (нет bleed) |

**Проблема:** Нет .meta файлов в `Assets/Sprites/Tiles/` — Unity НЕ импортировал эти спрайты. При первом импорте Unity назначит:
- Texture Type: **Default** (НЕ Sprite!)
- PPU: **100** (для 64×64 → 0.64 юнита — крошечный!)
- Sprite Mode: Single (но тип = Default, не Sprite 2D)

**Даже если тип исправить на Sprite:** при PPU=32, 64px = 2.0u — это РОВНО размер ячейки, нет pixel bleed → белые зазоры.

**Решение:** Нужно либо:
- (A) Обработать terrain-спрайты до 68×68 при сохранении (как процедурные), ИЛИ
- (B) Создать .meta файлы с правильными настройками: Texture Type = Sprite, PPU = 32, ИЛИ
- (C) Перекрыть tilemap чуть большими спрайтами через cellSize настройку

---

### 🔴 BUG-VA2: HarvestableSpawner НЕ устанавливает shader/material — спрайты невидимые/чёрные

**Файл:** `HarvestableSpawner.cs` строки 221-235 (CreateHarvestableGameObject)

**Суть:** В отличие от ResourceSpawner и PlayerVisual, которые устанавливают `Sprite-Unlit-Default` shader, HarvestableSpawner оставляет SpriteRenderer с **дефолтным материалом**.

| Компонент | Shader | Material | Видимость без Light2D |
|-----------|--------|----------|-----------------------|
| PlayerVisual | Sprite-Unlit-Default | new Material() | ✅ Виден |
| ResourceSpawner | Sprite-Unlit-Default | sharedMaterial | ✅ Виден |
| **HarvestableSpawner** | **Sprite-Lit-Default (дефолт)** | **null (дефолтный)** | **❌ ЧЁРНЫЙ/НЕВИДИМЫЙ** |

**Если в сцене НЕТ Light2D** (что вероятно — FullSceneBuilder не всегда запускается), ВСЕ harvestable-объекты рендерятся как чёрные прямоугольники или полностью невидимы.

**Решение:** Добавить установку Unlit-шейдера в CreateHarvestableGameObject(), как в ResourceSpawner.

---

### 🔴 BUG-VA3: Спрайт игрока на "Default" слое — может быть скрыт за "Objects"

**Файл:** `PlayerVisual.cs` строка 91

**Суть:** Player sprite: `sortingOrder = 10`, НО `sortingLayerName` НЕ установлен → **"Default"**.
HarvestableSpawner ставит `sortingLayerName = "Objects"` (строка 234).

Если "Objects" сортируется ВЫШЕ "Default" в Project Settings, деревья/камни рендерятся ПОВЕРХ игрока.

**Решение:** Установить `mainSprite.sortingLayerName = "Objects"` + `sortingOrder = 10` (выше деревьев).

---

### 🟡 BUG-VA4: Object-спрайты 64×64 PPU=100 → микроскопические в tilemap

**Файл:** `TileMapController.cs` EnsureTile() + LoadTileSprite()

**Суть:** AI object-спрайты (obj_tree_oak.png и т.д.) — 64×64 RGBA. При дефолтном импорте Unity назначит PPU=100 → worldSize = 0.64 юнита. Tilemap-ячейка = 2.0 юнита. Объект будет занимать 32% ячейки — крошечный!

Процедурные fallback: 64×64 PPU=160 → 0.4 юнита (тоже маленький, но хотя бы известный).

**Решение:** .meta файлы с PPU=32 для object-спрайтов, ИЛИ обработка до нужного размера.

---

### 🟡 BUG-VA5: Нет .meta файлов → Unity не импортировал спрайты корректно

**Папка:** `Assets/Sprites/Tiles/`

**Суть:** В папке 25 .png файлов, но **0 .meta файлов**. Это означает:
- Unity НИКОГДА не видел эти файлы в проекте
- При первом импорте назначит дефолтные настройки (Texture Type = Default, НЕ Sprite)
- `AssetDatabase.LoadAssetAtPath<Sprite>()` вернёт `null` — файл не типа Sprite!

**Это объясняет почему AI-спрайты "не показываются"** — они физически не могут быть загружены как Sprite.

**Решение:** Нужно обеспечить корректный импорт:
- (A) Открыть Unity Editor → выбрать спрайты → Texture Type = Sprite (2D and UI) → Apply
- (B) Или создать .meta файлы программно с правильными настройками
- (C) Или использовать скрипт AssetDatabase.Refresh() + AssetImporter для автоматизации

---

### 🟡 BUG-VA6: Деревья "кривые" — возможная причина в масштабе

**Файл:** `HarvestableSpawner.cs` строки 227-231 (CalculateSpriteScale)

**Суть:** Если AI-спрайт загружен (PPU=100, bounds.size=0.64), CalculateSpriteScale вычислит:
```
scale = targetWorldSize(1.8) / spriteWorldSize(0.64) = 2.8125
```

При scale=2.8 спрайт 64×64 растягивается до ~180×180 пикселей — пиксельный, "кривой".

Если PPU=32 (после ручного исправления), bounds.size=2.0:
```
scale = 1.8 / 2.0 = 0.9
```
Это нормально.

**Проблема в том, что спрайты 64×64 слишком малы для качественного отображения при масштабировании.** Tiles_AI/ содержит оригиналы 1024×1024, но они RGB без альфа-канала.

---

### 🟢 BUG-VA7: OreVein руда "пропала" — harvestable-объекты не рендерятся через tilemap

**Файл:** `TileMapController.cs` строки 1008-1010

**Суть:** RenderMap() пропускает isHarvestable объекты:
```csharp
if (obj.isHarvestable)
    continue;  // Не рендерить в tilemap
```

Вся руда (OreVein) — isHarvestable=true. Она НЕ рендерится через tilemap, а рендерится через HarvestableSpawner. Но из-за BUG-VA2 (нет шейдера) — невидима.

**Решение:** После фикса BUG-VA2 руда станет видимой через HarvestableSpawner.

---

## Сводная таблица багов

| # | Серьёзность | Баг | Где | Влияние |
|---|-------------|-----|-----|---------|
| VA1 | 🔴 КРИТ | Terrain 64×64 PPU≠32 → белые зазоры | TileMapController + Sprites/Tiles | Белый фон между тайлами |
| VA2 | 🔴 КРИТ | Нет шейдера у HarvestableSpawner | HarvestableSpawner.cs:221-235 | ВСЕ harvestable = чёрные/невидимые |
| VA3 | 🔴 КРИТ | Игрок на "Default" слое | PlayerVisual.cs:91 | Игрок за деревьями |
| VA4 | 🟡 СРЕДН | Object PPU=100 → микроскопические | TileMapController + Sprites/Tiles | Мелкие объекты в tilemap |
| VA5 | 🟡 СРЕДН | Нет .meta → Unity не импортировал | Sprites/Tiles/*.png | LoadAssetAtPath = null |
| VA6 | 🟡 СРЕДН | 64×64 масштабирование → пиксельный вид | HarvestableSpawner + Sprites | "Кривые" деревья |
| VA7 | 🟢 НИЗК | OreVein skipped в tilemap (by design) | TileMapController.cs:1009 | Руда невидима пока VA2 не пофиксен |

---

## Корневые причины (иерархия)

```
Белый фон (VA1)
├── Terrain-спрайты 64×64 вместо 68×68
├── Нет .meta файлов → PPU=100 вместо 32
└── При PPU=32 всё равно 2.0u = точно ячейка (нет bleed)

Невидимые harvestable (VA2 + VA7)
├── Нет Sprite-Unlit-Default шейдера
├── Нет Light2D в сцене
└── OreVein пропущен в tilemap (by design)

Невидимый игрок (VA3)
├── sortingLayerName = "Default" (не "Objects")
└── Objects слой > Default → игрок за деревьями

Кривые деревья (VA4 + VA6)
├── PPU=100 → 0.64u → scale=2.8 → растянутый
└── 64×64 исходник → мало для качественного отображения
```

---

## Рекомендуемый порядок исправления

1. **VA5 + VA1**: Создать .meta файлы с правильными настройками (Texture Type=Sprite, PPU=32) + обработать terrain до 68×68
2. **VA2**: Добавить Unlit-шейдер в HarvestableSpawner (как в ResourceSpawner)
3. **VA3**: Установить sortingLayerName = "Objects" для PlayerVisual
4. **VA6**: Регенерировать спрайты с более высоким разрешением (128×128 или 256×256) для качества

---

*Создано: 2026-04-16 09:06 UTC*
