# Чекпоинт: Полный аудит визуальной регрессии v2

**Дата:** 2026-04-16 10:30 UTC
**Фаза:** 3 (Sprite regression — полный аудит после внедрения Variant B)
**Статус:** complete
**Цель:** Точный аудит всех визуальных проблем на основе хронологии чекпоинтов 04_14–04_16 и текущего кода. Определить, что работало вчера и что сломалось сегодня.

---

## ХРОНОЛОГИЯ — когда работало, когда сломалось

### 2026-04-14 — Фундамент (РАБОТАЛО)

| Чекпоинт | Событие | Результат |
|----------|---------|-----------|
| 04_14_critical_fixes_and_tiles (06:42) | 8/9 багов пофикшено, расширена tile система | ✅ Тайлы, объекты видны |
| 04_14_sprite_generation (14:11) | AI-спрайты сгенерированы (17 шт, 1024×1024 → 64×64) | ✅ Спрайты в Tiles/ и Tiles_AI/ |

**Ключевое:** Tiles/ содержит обработанные AI-спрайты 64×64 RGBA с прозрачностью. Tiles_AI/ — оригиналы 1024×1024 RGB без альфа.

### 2026-04-15 — Итерации визуальных фиксов (К КОНЦУ ДНЯ РАБОТАЛО)

| Время UTC | Чекпоинт | Что сделано | Результат |
|-----------|----------|-------------|-----------|
| 08:30 | 04_15_sprite_fixes_harvest_feedback | spriteBorder, player PPU=32, HarvestFeedbackUI | Частично |
| 11:18 | 04_15_visual_fixes_compilation_warnings | Terrain 66×66 PPU=32, alphaIsTransparency, player гуманоид | Частично |
| 11:52 | 04_15_visual_sprite_fixes | CS1061 фикс, spriteRect через TextureImporterSettings | Частично |
| 12:58 | 04_15_sprite_visual_fixes | Terrain 68×68 PPU=32, Objects PPU=160, cellGap=0 | Улучшение |
| 16:34 | 04_15_fix_three_visual_problems | **Light2D**, **Unlit shader**, **AI sprite processing** (process_ai_sprites.py), player position | ✅ Ключевой фикс |
| 17:14 | 04_15_fix_four_visual_problems_v2 | Player duplication, PPU=31 terrain, resource visibility, ItemData fallback | ✅ Работало |
| 17:51 | 04_15_consolidated_visual_fix | ReimportTileSprites PPU=32 Bilinear, dynamic scale, player sprite | ✅ Работало |
| 18:18 | 04_15_harvest_system_plan | **ПЛАН** создан, НЕ реализован | — |

**⭐ КРИТИЧЕСКИЕ фиксы 04_15, которые сделали всё видимым:**
1. **Light2D в сцене** (FullSceneBuilder Phase 04) — Sprite-Lit-Default спрайты стали видимы
2. **Unlit шейдер** для PlayerVisual и ResourceSpawner — видны даже без Light2D
3. **process_ai_sprites.py** — обработка AI-спрайтов: RGB→RGBA, flood-fill фон→прозрачный, 1024→64
4. **Dynamic scale** в ResourceSpawner — `targetWorldSize / spriteWorldSize`
5. **Player position** на центре карты (100, 80, 0)
6. **Player duplication fix** — SpawnPlayer() использует существующего Player

### 2026-04-16 — Harvest System implementation (ВСЁ СЛОМАЛОСЬ)

| Время UTC | Чекпоинт | Что сделано | Что сломано |
|-----------|----------|-------------|-------------|
| ~04:00 | 04_16_harvest_progress | Шаги 1-8 Harvest System реализованы | Компиляция сломана |
| 05:12 | 04_16_harvest_compile_fix | 18 ошибок CS1061/CS0234 пофикшено | Компиляция OK, но визуал сломан |
| 05:48 | 04_16_harvest_race_condition_fix | Awake+Start подписка | HarvestableSpawner спавнит, но спрайты уродливые |
| 06:36 | 04_16_sprite_regression_audit | Аудит — найдены неверные пути спрайтов | — |
| 07:55-08:03 | (внедрение) | FIX-R1/R4a/R4b/R4c/R2 — пути, depleted, подтипы | Спрайты ВСЁ ЕЩЁ не видны |

---

## ТОЧНАЯ ДИАГНОСТИКА — почему сломалось

### 🔴 КРИТ-1: HarvestableSpawner НЕ устанавливает Unlit шейдер

**Файл:** `HarvestableSpawner.cs` строки 221-235 (CreateHarvestableGameObject)

**Суть:** Это ГЛАВНАЯ причина. В 04_15 мы добавили `Sprite-Unlit-Default` шейдер в PlayerVisual и ResourceSpawner. Но при создании HarvestableSpawner в 04_16 этот критический урок был забыт.

| Компонент | Шейдер | Материал | Видимость |
|-----------|--------|----------|-----------|
| PlayerVisual | `Sprite-Unlit-Default` ✅ | new Material() | Виден |
| ResourceSpawner | `Sprite-Unlit-Default` ✅ | sharedMaterial (кэш) | Виден |
| **HarvestableSpawner** | **Sprite-Lit-Default (ДЕФОЛТ)** ❌ | **null (дефолтный)** | **НЕВИДИМЫЙ** |

**Процедурные fallback-спрайты** (из CreateProceduralSprite) — тоже невидимы, потому что шейдер Lit-Default без Light2D = чёрный.

**Влияние:** ВСЕ harvestable-объекты (деревья, камни, руда, кусты, травы) — невидимые или чёрные прямоугольники.

**Решение:** Добавить в CreateHarvestableGameObject():
```csharp
Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
Material mat = new Material(spriteShader);
sr.material = mat;
```

---

### 🔴 КРИТ-2: Игрок на слое "Default" вместо "Objects" — скрыт за деревьями

**Файл:** `PlayerVisual.cs` строка 91

**Суть:** `mainSprite.sortingOrder = 10`, НО `sortingLayerName` НЕ установлен → "Default".
HarvestableSpawner ставит `sortingLayerName = "Objects"` (строка 234).
Если "Objects" > "Default" в Sorting Layers, деревья рендерятся ПОВЕРХ игрока.

**Вчера работало** потому что:
- Деревья рендерились через objectTilemap (sortingLayer = "Objects", но без отдельного sortingOrder)
- Tilemap объекты имеют низкий sortingOrder

**Сегодня сломалось** потому что:
- Деревья теперь — отдельные GameObject через HarvestableSpawner
- sortingOrder деревьев = 5 (GetSortingOrder), но sortingLayer = "Objects"
- Игрок sortingOrder = 10, но sortingLayer = "Default"
- В Unity: sortingLayer имеет приоритет над sortingOrder!
- "Objects" > "Default" → ВСЕ harvestable-объекты ПОВЕРХ игрока

**Решение:** Добавить в PlayerVisual.CreateVisual():
```csharp
mainSprite.sortingLayerName = "Objects";
mainSprite.sortingOrder = 10; // Выше деревьев (5) и камней (3)
```

---

### 🔴 КРИТ-3: Белые зазоры между terrain-тайлами — при загрузке AI-спрайтов

**Файл:** `TileMapController.cs` CreateProceduralTileSprite() + LoadTileSprite()

**Суть:**
- Процедурные terrain-спрайты: 68×68 PPU=32 → **2.125u** → pixel bleed → ✅ нет зазоров
- AI terrain-спрайты из Tiles/: 64×64 PPU=32 → **2.0u** → РОВНО размер ячейки → ❌ зазоры

**Вчера** это было частично видно, но терпимо. Сегодня проблема актуальна, потому что:
- Если FullSceneBuilder.ReimportTileSprites() устанавливает PPU=32 для terrain, то AI-спрайты = 2.0u = нет bleed
- Если PPU=100 (дефолт без ReimportTileSprites), то AI-спрайты = 0.64u = крошечные

**Решения (по приоритету):**
1. **(Лучшее)** Обработать terrain-спрайты до 68×68 в process_ai_sprites.py — добавить 2px bleed с каждой стороны
2. **(Быстрое)** В EnsureTile(): для terrain-спрайтов проверять размер и добавлять bleed программно
3. **(Unity)** Установить PPU=31 для terrain (64/31=2.065u — лёгкое перекрытие)

---

### 🟡 СРЕД-1: Нет .meta файлов в Git — Unity назначает дефолтные настройки

**Папки:** `Assets/Sprites/Tiles/` (25 PNG, 0 .meta), `Assets/Sprites/Tiles_AI/` (25 PNG, 0 .meta)

**Суть:** .meta файлы создаются Unity при первом импорте. Их отсутствие в Git означает:
- При `git clone` + открытие Unity → первый импорт с дефолтными настройками
- Texture Type = **Default** (НЕ Sprite) → `AssetDatabase.LoadAssetAtPath<Sprite>()` = **null**
- PPU = 100, alphaIsTransparency = false

**НО:** FullSceneBuilder.ReimportTileSprites() ИСПРАВЛЯЕТ настройки при запуске. Проблема только если:
1. Сцена запускается БЕЗ FullSceneBuilder (напрямую)
2. ReimportTileSprites не покрывает Tiles_AI/ (уже убран из сканирования — ОК)

**Вчера работало** потому что EnsureTileAssets() → CreateProceduralTileSprite() создавал видимые процедурные fallback.

**Сегодня** — HarvestableSpawner.LoadSpriteFromAssets() тоже имеет fallback на CreateProceduralSprite(), но без Unlit шейдера (КРИТ-1).

**Решение:**
1. После фикса КРИТ-1 (Unlit шейдер), процедурные fallback-спрайты станут видимыми
2. Для постоянного решения: добавить ReimportTileSprites() вызов в TileMapController.EnsureTileAssets() (не только в FullSceneBuilder)
3. Или: создавать .meta файлы с правильными настройками программно при первом запуске

---

### 🟡 СРЕД-2: Деревья "кривые" — причина в PPU и масштабе

**Файл:** `HarvestableSpawner.cs` CalculateSpriteScale()

**Сценарий 1: AI-спрайт НЕ загружен (LoadAssetAtPath = null)**
→ Fallback на CreateProceduralSprite() с PPU=32
→ bounds.size = 2.0u
→ scale = 1.8/2.0 = 0.9
→ Спрайт 64×64 отрисовывается как 64×64 → видна пиксельная графика
→ **Это нормально, но "некрасиво"** — программные спрайты не детализированные

**Сценарий 2: AI-спрайт загружен с PPU=100 (дефолт)**
→ bounds.size = 0.64u
→ scale = 1.8/0.64 = 2.8125
→ Спрайт 64×64 растягивается в 2.8 раза → размытый, пиксельный, "кривой"

**Сценарий 3: AI-спрайт загружен с PPU=32 (после ReimportTileSprites)**
→ bounds.size = 2.0u
→ scale = 1.8/2.0 = 0.9
→ **Нормально** — AI-спрайт 64×64 при 0.9 масштабе выглядит хорошо

**Вывод:** "Кривые" деревья = следствие сценария 2 (PPU=100 дефолт). После фикса СРЕД-1 (правильный импорт) проблема исчезнет.

---

### 🟡 СРЕД-3: Руда "пропала" — следствие КРИТ-1 и КРИТ-2

**Файл:** `TileMapController.cs` строка 1009

**Суть:** RenderMap() пропускает `isHarvestable` объекты — это BY DESIGN (чекпоинт 04_15 §6.2). Руда должна рендериться через HarvestableSpawner, а не через tilemap.

**Но HarvestableSpawner:**
1. Не устанавливает Unlit шейдер → спрайты невидимые (КРИТ-1)
2. Даже если видимые — рендерятся на "Objects" слое поверх игрока (КРИТ-2)

**Решение:** После фикса КРИТ-1 руда станет видимой. КРИТ-2 — чтобы игрок не был скрыт.

---

## Сводная таблица багов (приоритизированная)

| # | Серьёзность | Баг | Где | Влияние | Зависит от |
|---|-------------|-----|-----|---------|-----------|
| КРИТ-1 | 🔴 | Нет Unlit шейдера в HarvestableSpawner | HarvestableSpawner.cs:222 | ВСЕ harvestable = невидимые | — |
| КРИТ-2 | 🔴 | Игрок на "Default" слое | PlayerVisual.cs:91 | Игрок за деревьями | КРИТ-1 |
| КРИТ-3 | 🔴 | AI terrain 64×64 PPU=32 = нет bleed | TileMapController + Sprites/Tiles | Белые зазоры | — |
| СРЕД-1 | 🟡 | Нет .meta → дефолтный импорт | Sprites/Tiles/*.png | LoadAssetAtPath = null | КРИТ-1 |
| СРЕД-2 | 🟡 | PPU=100 → масштаб 2.8x → "кривые" деревья | HarvestableSpawner | Пиксельный вид | СРЕД-1 |
| СРЕД-3 | 🟡 | OreVein пропущен в tilemap (by design) | TileMapController.cs:1009 | Руда невидима | КРИТ-1 |

---

## Корневые причины (иерархия)

```
ВСЁ НЕВИДИМО (КРИТ-1)
├── HarvestableSpawner → Sprite-Lit-Default без Light2D → чёрные/невидимые
├── Деревья, камни, руда, кусты, травы — ВСЕ harvestable
└── Процедурные fallback-спрайты — тоже невидимые (тот же шейдер)

ИГРОК ЗА ДЕРЕВЬЯМИ (КРИТ-2)
├── PlayerVisual → sortingLayerName = "Default"
├── HarvestableSpawner → sortingLayerName = "Objects"
└── Objects > Default → игрок скрыт

БЕЛЫЕ ЗАЗОРЫ (КРИТ-3)
├── AI terrain 64×64 при PPU=32 = 2.0u = ровно ячейка
├── Нет pixel bleed → субпиксельные зазоры
└── Процедурные 68×68 = 2.125u = bleed ✅ (но AI лучше выглядят)

КРИВЫЕ ДЕРЕВЬЯ (СРЕД-1 + СРЕД-2)
├── LoadAssetAtPath<Sprite>() = null (PPU=100, не Sprite тип)
├── Fallback → программные спрайты (не красивые, но видимые после КРИТ-1)
└── Или: AI спрайт загружен с PPU=100 → scale=2.8 → растянутый
```

---

## ПЛАН ИСПРАВЛЕНИЯ (пошаговый, безопасный)

### Шаг 1: КРИТ-1 — Unlit шейдер в HarvestableSpawner (5 минут, максимальный эффект)

**Файл:** `HarvestableSpawner.cs` CreateHarvestableGameObject()

**Добавить после строки 235 (sr.sortingOrder):**
```csharp
// КРИТ-1: Unlit шейдер — рендерит БЕЗ Light2D (как в ResourceSpawner)
Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
sr.material = new Material(spriteShader);
```

**Результат:** ВСЕ harvestable-объекты (деревья, камни, руда, кусты, травы) станут видимыми.

### Шаг 2: КРИТ-2 — Sorting layer для игрока (2 минуты)

**Файл:** `PlayerVisual.cs` CreateVisual()

**Добавить после строки 91 (mainSprite.sortingOrder = 10):**
```csharp
mainSprite.sortingLayerName = "Objects";
```

**Результат:** Игрок рендерится на "Objects" слое с sortingOrder=10 (выше деревьев=5, камней=3).

### Шаг 3: КРИТ-3 — Terrain-спрайты с pixel bleed (15 минут)

**Вариант A (рекомендуемый):** Обновить process_ai_sprites.py — добавлять 2px bleed для terrain:
- Читать 64×64, создавать 68×68
- Копировать крайние пиксели на 2px бордюр
- Сохранять как 68×68

**Вариант B (быстрый):** В TileMapController.EnsureTile() — для terrain проверять размер загруженного спрайта и при 64×64 пересоздавать с bleed.

**Вариант C (Unity-side):** Установить PPU=31 для terrain (64/31=2.065u — лёгкое перекрытие). Это делалось в 04_15_fix_four_visual_problems_v2, но было отменено в consolidated fix.

### Шаг 4: СРЕД-1 — Корректный импорт спрайтов (10 минут)

**Файл:** `TileMapController.cs` EnsureTileAssets()

**Добавить:** Перед LoadTileSprite() — принудительный реимпорт с правильными настройками:
```csharp
#if UNITY_EDITOR
private void EnsureSpriteImportSettings(string spriteName)
{
    string path = $"Assets/Sprites/Tiles/{spriteName}.png";
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
    if (importer == null) return;
    
    bool isObject = spriteName.StartsWith("obj_");
    int targetPPU = isObject ? 160 : 32;
    bool needsReimport = importer.textureType != UnityEditor.TextureImporterType.Sprite
        || importer.spritePixelsPerUnit != targetPPU
        || importer.alphaIsTransparency != true;
    
    if (needsReimport)
    {
        importer.textureType = UnityEditor.TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = targetPPU;
        importer.alphaIsTransparency = true;
        importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
        importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
        UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
    }
}
#endif
```

### Шаг 5: Верификация (ручная — пользователь)

1. Открыть Unity → дождаться компиляции
2. Запустить сцену
3. Проверить: деревья/камни/руда ВИДНЫ (КРИТ-1)
4. Проверить: игрок ВИДЕН и НЕ скрыт за деревьями (КРИТ-2)
5. Проверить: нет белых зазоров между terrain-тайлами (КРИТ-3)
6. Проверить: деревья выглядят нормально, не "кривые" (СРЕД-2)

---

## ЧЕКПОИНТЫ — что было корректным, что привело к ошибкам

| Чекпоинт | Дата | Статус | Оценка |
|----------|------|--------|--------|
| 04_14_sprite_generation | 04-14 14:11 | ✅ complete | ✅ Корректный — AI-спрайты созданы, интегрированы |
| 04_14_critical_fixes_and_tiles | 04-14 06:42 | ✅ complete | ✅ Корректный — баги пофикшены, tile система расширена |
| 04_15_sprite_fixes_harvest_feedback | 04-15 08:30 | ✅ complete | ✅ Корректный — HarvestFeedbackUI, spriteBorder |
| 04_15_visual_fixes_compilation_warnings | 04-15 11:18 | ✅ complete | ⚠️ Частичный — terrain 66×66, потом переработано |
| 04_15_visual_sprite_fixes | 04-15 11:52 | 🔄 in_progress | ⚠️ Не закрыт — CS1061 фикс |
| 04_15_sprite_visual_fixes | 04-15 12:58 | ✅ complete | ✅ Корректный — 68×68 PPU=32, PPU=160 objects |
| 04_15_fix_three_visual_problems | 04-15 16:34 | ✅ complete | ✅ **КЛЮЧЕВОЙ** — Light2D, Unlit shader, AI processing |
| 04_15_fix_four_visual_problems_v2 | 04-15 17:14 | ✅ complete | ✅ Корректный — duplication, PPU=31, resource vis |
| 04_15_consolidated_visual_fix | 04-15 17:51 | ✅ complete | ✅ Корректный — ReimportTileSprites, dynamic scale |
| 04_15_harvest_system_plan | 04-15 18:18 | ✅ complete | ✅ План — НЕ реализован в этот день |
| 04_16_harvest_progress | 04-16 | ✅ complete | ⚠️ Реализация сломала визуал |
| 04_16_harvest_compile_fix | 04-16 05:12 | ✅ complete | ✅ Корректный — 18 ошибок пофикшено |
| 04_16_harvest_race_condition_fix | 04-16 05:48 | ✅ complete | ✅ Корректный — race condition пофиксен |
| 04_16_sprite_regression_audit | 04-16 06:36 | ✅ complete | ✅ Корректный — пути найдены |
| 04_16_visual_regression_full_audit | 04-16 09:06 | ✅ complete | ✅ Корректный — 7 багов найдено (этот файл — v2) |

**Вывод:** Ни один чекпоинт не содержал "ошибочных" действий. Проблема в том, что при создании HarvestableSpawner в 04_16 был забыт критический урок 04_15 — **Unlit шейдер обязателен** для URP 2D без гарантированного Light2D.

---

## РАБОЧИЙ ЧЕКПОИНТ — на что откатываться

**Последний рабочий:** После `04_15_consolidated_visual_fix` (2026-04-15 17:51 UTC)

**Что работало:**
- ✅ Игрок виден (Unlit shader, позиция центр карты)
- ✅ Terrain покрывает сетку (68×68 процедурные с bleed, AI с PPU=32)
- ✅ Объекты в tilemap видны (AI-спрайты + fallback)
- ✅ Ресурсы видны (ResourceSpawner + Unlit shader + dynamic scale)
- ✅ Нет задвоения игрока

**Что НЕ работало (но было терпимо):**
- ⚠️ Белые зазоры при использовании AI terrain (64×64 = 2.0u = нет bleed)
- ⚠️ Руда/деревья/камни рендерились через tilemap (без коллайдеров, без добычи)

---

*Создано: 2026-04-16 09:06 UTC*
*Редактировано: 2026-04-16 10:30 UTC — v2: полная хронология, точные корневые причины, приоритизированный план*
