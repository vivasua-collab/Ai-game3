# Чекпоинт: План реализации визуальных фиксов

**Дата:** 2026-04-16 11:00 UTC
**Фаза:** 4 (Реализация фиксов по аудиту 04_16_visual_regression_full_audit.md)
**Статус:** complete
**Цель:** Пошаговый план исправления 6 багов (3 КРИТ + 3 СРЕД) в порядке приоритета и зависимостей
**Реализовано:** 2026-04-16 — все 5 фиксов внедрены в код

---

## Сводка багов (из аудита)

| # | Серьёзность | Баг | Влияние | Зависит от |
|---|-------------|-----|---------|-----------|
| КРИТ-1 | 🔴 | Нет Unlit шейдера в HarvestableSpawner | ВСЕ harvestable = невидимые | — |
| КРИТ-2 | 🔴 | Игрок на "Default" слое | Игрок за деревьями | КРИТ-1 |
| КРИТ-3 | 🔴 | AI terrain 64×64 PPU=32 = нет bleed | Белые зазоры | — |
| СРЕД-1 | 🟡 | Нет .meta → дефолтный импорт | LoadAssetAtPath = null | КРИТ-1 |
| СРЕД-2 | 🟡 | PPU=100 → масштаб 2.8x → "кривые" | Пиксельный вид | СРЕД-1 |
| СРЕД-3 | 🟡 | OreVein пропущен в tilemap (by design) | Руда невидима | КРИТ-1 |

---

## Порядок реализации

### FIX-1: КРИТ-1 — Unlit шейдер в HarvestableSpawner (5 мин, максимальный эффект)

**Причина:** HarvestableSpawner.CreateHarvestableGameObject() НЕ устанавливает Sprite-Unlit-Default шейдер для SpriteRenderer. В URP 2D без Light2D спрайт с Sprite-Lit-Default = чёрный/невидимый. Это ГЛАВНАЯ причина всех проблем — в PlayerVisual и ResourceSpawner это уже исправлено (04_15), но при создании HarvestableSpawner (04_16) критический урок был забыт.

**Файл:** `HarvestableSpawner.cs`
**Метод:** `CreateHarvestableGameObject()`
**Строка:** После строки 235 (`sr.sortingOrder = GetSortingOrder(objData.objectType);`)

**Изменение:** Добавить 6 строк кода:
```csharp
// КРИТ-1 FIX: Unlit шейдер — рендерит БЕЗ Light2D (как в PlayerVisual и ResourceSpawner).
// Sprite-Lit-Default без Light2D → чёрные/невидимые спрайты.
// Sprite-Unlit-Default рендерит без освещения — всегда виден.
// Редактировано: 2026-04-16
Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
sr.material = new Material(spriteShader);
```

**Результат:** ВСЕ harvestable-объекты (деревья, камни, руда, кусты, травы) станут видимыми, включая процедурные fallback-спрайты.

**Побочный эффект:** СРЕД-3 (руда невидима) автоматически решится, потому что руда спавнится через HarvestableSpawner, который после этого фикса будет рендерить спрайты.

---

### FIX-2: КРИТ-2 — Sorting layer "Objects" для PlayerVisual (2 мин)

**Причина:** PlayerVisual.CreateVisual() устанавливает `mainSprite.sortingOrder = 10`, но НЕ устанавливает `sortingLayerName`. По умолчанию "Default". HarvestableSpawner ставит `sortingLayerName = "Objects"`. В Unity sortingLayer имеет приоритет над sortingOrder: "Objects" > "Default" → ВСЕ деревья поверх игрока.

**Файл:** `PlayerVisual.cs`
**Метод:** `CreateVisual()`
**Строка:** После строки 91 (`mainSprite.sortingOrder = 10;`)

**Изменение:** Добавить 1 строку:
```csharp
// КРИТ-2 FIX: Sorting layer "Objects" — чтобы игрок рендерился на том же слое,
// что и harvestable-объекты (деревья, камни). Иначе "Objects" > "Default" →
// игрок скрыт за деревьями, несмотря на sortingOrder=10.
// Редактировано: 2026-04-16
mainSprite.sortingLayerName = "Objects";
```

**Результат:** Игрок рендерится на "Objects" слое с sortingOrder=10 (выше деревьев=5, камней=3, руды=4).

**Также:** Для shadowSprite — добавить `shadowSprite.sortingLayerName = "Objects";` и `shadowSprite.sortingOrder = 9;` (тень чуть ниже игрока, но выше деревьев).

---

### FIX-3: КРИТ-3 — Terrain pixel bleed через PPU=31 (5 мин)

**Причина:** AI terrain-спрайты 64×64 при PPU=32 = 2.0 юнита = ровно размер ячейки. Нет pixel bleed → субпиксельные зазоры (белая сетка). Процедурные terrain-спрайты 68×68 при PPU=32 = 2.125u — bleed есть, зазоров нет.

**Вариант C (выбранный — быстрый и безопасный):** Установить PPU=31 для terrain-спрайтов. Тогда 64/31 = 2.0645u — лёгкое перекрытие 0.032u с каждой стороны. Этого достаточно для устранения зазоров.

**Файл:** `TileMapController.cs`
**Метод:** `CreateProceduralTileSprite()`
**Изменение:** Заменить PPU для terrain:
```csharp
// Было:
int ppu = isObject ? 160 : 32;
// Стало:
// КРИТ-3 FIX: Terrain PPU=31 вместо 32 — 64/31=2.065u (перекрытие 0.032u).
// Устраняет белые зазоры между тайлами при использовании AI-спрайтов 64×64.
// Процедурные 68×68 при PPU=31 = 68/31=2.194u — тоже с bleed, без зазоров.
// Редактировано: 2026-04-16
int ppu = isObject ? 160 : 31;
```

**Также:** Обновить EnsureSpriteImportSettings (FIX-4) чтобы устанавливать PPU=31 для terrain.

**Альтернатива (если PPU=31 не сработает):** Обновить process_ai_sprites.py — добавлять 2px bleed для terrain-спрайтов (64→68). Но это требует перегенерации всех тайлов.

---

### FIX-4: СРЕД-1 — EnsureSpriteImportSettings в HarvestableSpawner (10 мин)

**Причина:** Нет .meta файлов в Git → при git clone Unity импортирует с дефолтными настройками: Texture Type = Default (НЕ Sprite), PPU = 100. `AssetDatabase.LoadAssetAtPath<Sprite>()` возвращает null при Texture Type ≠ Sprite.

**Файл:** `HarvestableSpawner.cs`
**Новый метод:** `EnsureSpriteImportSettings(string assetPath, bool isObject)`

**Изменение:** Перед LoadSpriteFromAssets — принудительный реимпорт с правильными настройками:

```csharp
#if UNITY_EDITOR
/// <summary>
/// СРЕД-1 FIX: Убедиться, что спрайт импортирован с правильными настройками.
/// Без .meta в Git Unity использует дефолтные: TextureType=Default (НЕ Sprite), PPU=100.
/// LoadAssetAtPath<Sprite>() возвращает null при TextureType≠Sprite.
/// Редактировано: 2026-04-16
/// </summary>
private void EnsureSpriteImportSettings(string assetPath, bool isObject)
{
    var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
    if (importer == null) return;

    // КРИТ-3 FIX: Terrain PPU=31 (64/31=2.065u — pixel bleed). Objects PPU=160.
    int targetPPU = isObject ? 160 : 31;
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
        importer.filterMode = isObject ? FilterMode.Point : FilterMode.Bilinear;
        UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
        Debug.Log($"[HarvestableSpawner] Спрайт реимпортирован: {assetPath} → PPU={targetPPU}, alphaIsTransparency=true");
    }
}
#endif
```

**Вызов:** В LoadSpriteFromAssets(), перед `AssetDatabase.LoadAssetAtPath<Sprite>()`:
```csharp
// СРЕД-1 FIX: Убедиться, что спрайт импортирован корректно
// Редактировано: 2026-04-16
bool isObject = relativePath.Contains("obj_");
EnsureSpriteImportSettings(fullPath, isObject);
// Перезагружаем после возможного реимпорта
sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
```

**Результат:** LoadAssetAtPath<Sprite>() больше не вернёт null из-за неправильных настроек импорта. AI-спрайты загрузятся корректно.

---

### FIX-5: СРЕД-2 → auto-resolved after FIX-4

"Кривые" деревья = следствие PPU=100 (дефолт) → scale=2.8 → растянутый вид. После FIX-4 PPU=160 для objects → scale=1.8/0.4=4.5, но при targetWorldSize=1.8f объект будет размером 1.8 юнита (как задумано). Визуально — корректный масштаб.

**Действие:** Верификация — убедиться, что CalculateSpriteScale() даёт адекватный результат после FIX-4:
- AI-спрайт 64×64 при PPU=160 → bounds.size = 0.4u → scale = 1.8/0.4 = 4.5
- Итоговый размер = 0.4 * 4.5 = 1.8u ✅ (нормальный)

**Код не нужно менять.** Проблема решается фиксом СРЕД-1.

---

### FIX-6: TileMapController.EnsureTileAssets() — PPU=31 для terrain

**Связано с FIX-3.** EnsureTileAssets() в TileMapController тоже создаёт процедурные спрайты через CreateProceduralTileSprite(). После изменения PPU на 31 в CreateProceduralTileSprite, процедурные fallback-спрайты тоже получат bleed.

**Также:** Аналогичный EnsureSpriteImportSettings нужно добавить в TileMapController.LoadTileSprite() для terrain-спрайтов:

```csharp
#if UNITY_EDITOR
/// <summary>
/// КРИТ-3 FIX: Убедиться, что terrain-спрайт импортирован с PPU=31.
/// 64/31=2.065u — лёгкое перекрытие, устраняет белые зазоры.
/// Редактировано: 2026-04-16
/// </summary>
private void EnsureTerrainSpriteImportSettings(string assetPath, bool isObject)
{
    var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
    if (importer == null) return;

    int targetPPU = isObject ? 160 : 31;
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
        importer.filterMode = isObject ? FilterMode.Point : FilterMode.Bilinear;
        UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
        Debug.Log($"[TileMapController] Спрайт реимпортирован: {assetPath} → PPU={targetPPU}");
    }
}
#endif
```

**Вызов:** В LoadTileSprite(), перед `AssetDatabase.LoadAssetAtPath<Sprite>()`.

---

## Сводная таблица изменений

| FIX | Файл | Метод | Тип изменения | Зависит от |
|-----|------|-------|---------------|-----------|
| FIX-1 | HarvestableSpawner.cs | CreateHarvestableGameObject() | +6 строк (Unlit shader) | — |
| FIX-2 | PlayerVisual.cs | CreateVisual() | +2 строки (sortingLayer) | — |
| FIX-3 | TileMapController.cs | CreateProceduralTileSprite() | Изменить PPU=32→31 | — |
| FIX-4 | HarvestableSpawner.cs | LoadSpriteFromAssets() + новый метод | +30 строк (EnsureSpriteImportSettings) | — |
| FIX-6 | TileMapController.cs | LoadTileSprite() + новый метод | +25 строк (EnsureTerrainSpriteImportSettings) | FIX-3 |

---

## Граф зависимостей

```
FIX-1 (Unlit shader) ────────→ решает КРИТ-1 + СРЕД-3 (руда)
FIX-2 (Sorting layer) ───────→ решает КРИТ-2
FIX-3 (PPU=31 terrain) ──────→ решает КРИТ-3
FIX-4 (Sprite import) ───────→ решает СРЕД-1 + СРЕД-2 (авто)
FIX-6 (Terrain import PPU=31)→ дополняет FIX-3

Все 5 фиксов НЕЗАВИСИМЫ — можно делать параллельно или в любом порядке.
Рекомендуемый порядок: FIX-1 → FIX-2 → FIX-3 → FIX-4 → FIX-6
(от максимального эффекта к минимальному)
```

---

## Верификация (после всех фиксов)

1. ✅ Деревья, камни, руда, кусты, травы — ВИДНЫ (КРИТ-1)
2. ✅ Игрок ВИДЕН и НЕ скрыт за деревьями (КРИТ-2)
3. ✅ Нет белых зазоров между terrain-тайлами (КРИТ-3)
4. ✅ AI-спрайты загружаются корректно, не "кривые" (СРЕД-1 + СРЕД-2)
5. ✅ Руда видна (СРЕД-3 — by design через HarvestableSpawner)

---

*Создано: 2026-04-16 11:00 UTC*
