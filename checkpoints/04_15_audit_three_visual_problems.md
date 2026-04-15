# Чекпоинт: Аудит 3 визуальных проблем — углублённый аудит

**Дата:** 2026-04-15 16:28:00 UTC
**Редактировано:** 2026-04-15 16:45:00 UTC — углублённая трассировка кода с учётом пересоздания сцены
**Статус:** complete

---

## КОНТЕКСТ: Пользователь удаляет Assets и пересоздаёт сцену

Это критически важная информация:
1. При каждом пересоздании Unity генерирует .meta файлы заново
2. FullSceneBuilder Build All выполняется полностью (все 15 фаз)
3. TileSpriteGenerator.GenerateAllSprites() создаёт Tiles/ и генерирует программные PNG
4. Тайлы создаются из программных PNG, НЕ из AI-спрайтов

---

## УГЛУБЛЁННАЯ ТРАССИРОВКА ПОЛНОГО ПОТОКА ВЫПОЛНЕНИЯ

### FullSceneBuilder Build All — порядок фаз:

```
Phase 10: Generate Tile Sprites
  → IsGenerateSpritesNeeded() = !HasAssetsInFolder("Assets/Sprites/Tiles")
  → Если Tiles/ НЕ существует (а её нет при пересоздании) → EXECUTE
  → TileSpriteGenerator.GenerateAllSprites()
    → Создаёт Assets/Sprites/Tiles/ (если нет)
    → Генерирует terrain_*.png (68×68 RGBA32) и obj_*.png (64×64 RGBA32)
    → Реимпортирует через SaveTexture() с PPU=32/160, SpriteImportMode.Single
    → РЕЗУЛЬТАТ: Tiles/ заполнена программными спрайтами

Phase 14: Create Tile Assets
  → ExecuteCreateTileAssets()
    → Шаг 1: Проверка Tiles/terrain_grass.png → СУЩЕСТВУЕТ (создан Phase 10)
    → Шаг 1.5: ReimportTileSprites()
      → Сканирует Tiles/ и Tiles_AI/
      → Устанавливает PPU=32(terrain)/160(objects), alphaIsTransparency=true
      → ДЛЯ Tiles_AI/: PPU=32 для terrain_grass_ai.png (1024px) → 1024/32=32 юнита!
      → ДЛЯ Tiles/: PPU=32 для terrain_grass.png (68px) → 68/32=2.125 юнита
    → Шаг 3: CreateTerrainTileAsset("Tile_Grass", "terrain_grass", ...)
      → Ищет: "Assets/Sprites/Tiles/terrain_grass.png" ← ТОЛЬКО Tiles/!
      → НАХОДИТ программный спрайт (создан Phase 10)
      → tile.sprite = программный спрайт 68×68 PPU=32
    → Шаг 4: CreateObjectTileAsset("Tile_Tree", "obj_tree", ...)
      → Ищет: "Assets/Sprites/Tiles/obj_tree.png" ← ТОЛЬКО Tiles/!
      → НАХОДИТ программный спрайт 64×64 PPU=160
      → tile.sprite = программный спрайт
    → Шаг 5: AssignTileBasesToController()
      → Назначает все tile assets в [SerializeField] поля TileMapController
```

### Runtime (Start):

```
TileMapController.Start()
  → EnsureTileAssets()
    → grassTile уже назначен из FullSceneBuilder → НЕ null → ПРОПУСКАЕТ
    → ВСЕ [SerializeField] поля назначены → EnsureTile() не вызывается
  → GenerateTestMap()
    → Заполняет mapData типами TerrainType
    → RenderMap()
      → GetTerrainTile(TerrainType.Grass) → grassTile (с программным спрайтом)
      → terrainTilemap.SetTile(pos, grassTile) → РИСУЕТ программный спрайт
```

### КЛЮЧЕВОЙ ВЫВОД:

**AI-спрайты из Tiles_AI/ НИКОГДА НЕ ИСПОЛЬЗУЮТСЯ!**

Потому что:
1. Phase 10 генерирует программные спрайты в Tiles/ (ВСЕГДА при пересоздании)
2. CreateTerrainTileAsset/CreateObjectTileAsset ищут ТОЛЬКО в Tiles/
3. EnsureTileAssets() не вызывается — все поля уже назначены из Build All
4. ReimportTileSprites() реимпортирует Tiles_AI/ но результат не используется нигде

---

## ПРОБЛЕМА 1: Используются программные текстуры вместо AI-спрайтов

### Корневая причина (УТОЧНЕНА)

НЕ то что "AI-спрайты не загружаются". А то что:
1. `TileSpriteGenerator` генерирует программные PNG в Tiles/ → ЭТИ используются
2. `CreateTerrainTileAsset` ищет ТОЛЬКО в Tiles/ → находит программные
3. AI-спрайты в Tiles_AI/ существуют, но **код никогда к ним не обращается** при создании tile assets
4. `EnsureTileAssets()` (runtime fallback) НЕ вызывается, т.к. FullSceneBuilder уже назначил все поля

### Решение с МАКСИМАЛЬНОЙ вероятностью успеха

**Вариант 1 (самый надёжный): Заменить содержимое Tiles/ на обработанные AI-спрайты**
- Python-скрипт обрабатывает Tiles_AI/ (уменьшает 1024→64, добавляет прозрачность для obj_*)
- Результат сохраняется в Tiles/ (заменяет программные PNG)
- `TileSpriteGenerator.GenerateAllSprites()` НЕ вызывается если Tiles/ уже содержит PNG
- НО: `IsGenerateSpritesNeeded()` проверяет `HasAssetsInFolder("Assets/Sprites/Tiles")` → если Tiles/ не пуста → Phase 10 ПРОПУСКАЕТСЯ
- Значит: если Tiles/ содержит обработанные AI-спрайты → Phase 10 пропускается → AI-спрайты используются!

**ПРОБЛЕМА С ВАРИАНТОМ 1:** При удалении Assets и пересоздании Tiles/ будет ПУСТАЯ → Phase 10 запустится → перезапишет AI-спрайты программными!

**Вариант 2 (рекомендуемый — МАКСИМАЛЬНАЯ вероятность):**
Изменить `TileSpriteGenerator.GenerateAllSprites()` — вместо генерации программных текстур, КОПИРОВАТЬ (и обрабатывать) AI-спрайты в Tiles/:

```
GenerateAllSprites():
  1. Проверить Tiles_AI/ на наличие AI-спрайтов
  2. Если AI-спрайты есть:
     a. Создать Tiles/ если нет
     b. Для каждого AI-спрайта в Tiles_AI/:
        - Загрузить PNG
        - Уменьшить до 64×64 (Lanczos resize)
        - Для obj_*: добавить прозрачность (flood-fill от углов)
        - Сохранить в Tiles/
        - Реимпортировать как Sprite с правильным PPU
  3. Если AI-спрайтов нет:
     a. Fallback: текущая генерация программных спрайтов
```

**Почему это работает:**
- Phase 10 ВСЕГДА запускается при пересоздании (Tiles/ пуста)
- Но вместо программных спрайтов → используем обработанные AI-спрайты
- Phase 14 находит PNG в Tiles/ → tile assets с AI-спрайтами
- Код минимально изменён — только TileSpriteGenerator

**Вариант 3 (альтернативный): Удалить TileSpriteGenerator, изменить CreateTerrainTileAsset**
- CreateTerrainTileAsset ищет сначала в Tiles_AI/, потом в Tiles/
- Но при 1024×1024 PPU=32 → 32 юнита — НЕВЕРНО
- Нужно PPU=512 → 1024/512=2.0 юнита
- Но alphaIsTransparency=true не работает для RGB PNG → нужен альфа-канал

---

## ПРОБЛЕМА 2: Игрок не отображается

### Уточнённая трассировка

```
FullSceneBuilder.ExecutePlayer():
  → player.transform.position = Vector3.zero  ← (0,0,0)
  → PlayerVisual создан на Player
  → PlayerVisual.Awake() → CreateVisual()
    → Создаёт дочерний "Visual" с SpriteRenderer
    → localScale = (0.4, 0.4, 1)
    → LoadPlayerSprite() → ищет 4 пути → НИ ОДИН НЕ СУЩЕСТВУЕТ
    → Fallback: CreateCircleSprite()
      → 64×64 RGBA32, PPU=32 → 2.0 юнита
      → Scale 0.4 → видимый размер 0.8 юнита
      → Цвет: playerColor = (0.2, 0.8, 0.3) — ЗЕЛЁНЫЙ
    → sortingOrder = 10

Camera2DSetup.Start():
  → FindPlayerTarget() → находит Player по тегу/имени
  → SetFollowTarget(player.transform)
  → Камера следует за игроком → камера на (0,0,-10)
```

### ПОЧЕМУ игрок не виден:

1. **Позиция (0,0,0) = Void тайл** → чёрный фон вокруг
2. **Зелёный на зелёном** → если камера сместится на траву, игрок сливается
3. **НО!** Процедурный спрайт с Color.white tint → greenColor → должен быть виден даже на Void (зелёный на чёрном)
4. **Возможная причина**: URP шейдер `Sprite-Lit-Default` требует 2D Lights! Без источников 2D света спрайт НЕ ВИДЕН в URP 2D!

### НОВАЯ ГИПОТЕЗА: URP 2D без 2D Lights = невидимые спрайты

Проверка:
- FullSceneBuilder создаёт `Directional Light` (3D) — НЕ 2D Light
- URP 2D renderer использует `Light2D` компоненты, не 3D Light
- `Sprite-Lit-Default` шейдер рендерит спрайт ТОЛЬКО если есть 2D Light
- Без 2D Light → спрайт полностью тёмный (чёрный = невидимый на чёрном фоне)

**ЭТО МОЖЕТ БЫТЬ ГЛАВНОЙ ПРИЧИНОЙ!**

Проверяю: PlayerVisual ищет шейдер `Universal Render Pipeline/2D/Sprite-Lit-Default`
Если URP не настроен → fallback на `Sprites/Default` → виден
Если URP настроен как 2D renderer → `Sprite-Lit-Default` найден → НО НЕТ 2D LIGHT → НЕВИДИМ!

### Решение с МАКСИМАЛЬНОЙ вероятностью (для Проблемы 2)

**Шаг 1:** Добавить Light2D в сцену (если URP 2D renderer)
- `Light2D` компонент типа `Global` с белым цветом
- Это осветит ВСЕ спрайты использующие Sprite-Lit-Default

**Шаг 2:** Сменить позицию игрока на центр карты
- `player.transform.position = new Vector3(100f, 80f, 0f)`
- Центр = каменный алтарь, видимый на любом фоне

**Шаг 3:** Использовать Sprite-Unlit-Default вместо Sprite-Lit-Default
- `Universal Render Pipeline/2D/Sprite-Unlit-Default` — рендерит БЕЗ света
- Это гарантированно показывает спрайт

**Шаг 4:** Контрастный цвет — `new Color(1f, 0.2f, 0.1f)` (красный)

### Приоритет исправлений для Проблемы 2:
1. **КРИТИЧЕСКОЕ:** Сменить шейдер на Unlit или добавить Light2D
2. Сменить позицию на центр карты
3. Контрастный цвет

---

## ПРОБЛЕМА 3: Белый фон вместо прозрачности

### Уточнённая трассировка

При пересоздании сцены (FullSceneBuilder Build All):

1. Phase 10 генерирует программные PNG в Tiles/:
   - terrain_*.png: 68×68 RGBA32, ВСЯ площадь залита цветом (нет прозрачных пикселей)
   - obj_*.png: 64×64 RGBA32, фон=прозрачный (Color.clear), объект нарисован в центре

2. SaveTexture() для каждого PNG:
   - `alphaIsTransparency = true` — установлено
   - `textureCompression = Uncompressed`
   - `spriteImportMode = SpriteImportMode.Single`

3. Phase 14: CreateObjectTileAsset() загружает obj_tree.png из Tiles/
   - Спрайт = программный, 64×64, PPU=160
   - Прозрачный фон (Color.clear) — ДОЛЖЕН быть прозрачным!

**Если программные спрайты с Color.clear + RGBA32 + alphaIsTransparency=true всё ещё показывают белый фон, причина может быть:**

A. **Sprite-Lit-Default шейдер без Light2D** — спрайты рендерятся как чёрные/белые
B. **Tilemap renderer issue** — TilemapRenderer.Mode.Individual может игнорировать прозрачность
C. **GameTile.GetTileData()** — устанавливает `tileData.color = Color.white` — это tint, не должно убирать прозрачность
D. **PNG не сохранился корректно** — EncodeToPNG() мог не сохранить alpha

### Проверка гипотезы A (URP 2D без Light2D):

Если URP 2D renderer активен и НЕТ Light2D:
- Sprite-Lit-Default → спрайт чёрный (нет освещения)
- На чёрном фоне спрайт с прозрачностью = ЧЁРНЫЙ силуэт на чёрном = НЕВИДИМЫЙ
- Но пользователь видит БЕЛЫЙ фон, не чёрный → возможно это НЕ URP проблема

### Проверка гипотезы B (Tilemap renderer):

TilemapRenderer рендерит каждый тайл. Для объектов:
- GameTile.GetTileData() → tileData.sprite = sprite, tileData.color = Color.white
- Sprite с RGBA32 + прозрачный фон + color=white tint → ДОЛЖЕН быть прозрачным
- НО если TilemapRenderer использует Sprite-Lit-Default → может рендерить как opaque

### НОВАЯ ГИПОТЕЗА: Проблема в AI-спрайтах, не в программных

Пользователь говорит "текстуры элементов имеют БЕЛЫЙ фон". "Элементы" может означать:
- Объекты на тайлах (деревья, камни) → отображаются через Tilemap → программные спрайты
- Ресурсы на карте → ResourceSpawner → ПЫТАЕТСЯ загрузить AI-спрайты
- Если ResourceSpawner.LoadResourceSprite() загружает AI-спрайт из Tiles_AI/ → 1024×1024 RGB БЕЗ прозрачности → БЕЛЫЙ ФОН!

**Проверяю:**
- ResourceSpawner.LoadResourceSprite() ищет в Tiles_AI/ ПЕРВЫМ → находит 1024×1024 RGB PNG
- AssetDatabase.LoadAssetAtPath<Sprite>() → работает если .meta существует и textureType=Sprite
- ReimportTileSprites() реимпортировал Tiles_AI/ с alphaIsTransparency=true → НО RGB PNG не имеет alpha!
- Результат: AI-спрайт с белым фоном → spriteScale=0.16 → видимый маленький объект с белым квадратом

### Решение с МАКСИМАЛЬНОЙ вероятностью (для Проблемы 3)

**Обработать AI-спрайты Python-скриптом:**
1. Для obj_* спрайтов: конвертировать RGB→RGBA, flood-fill фон→прозрачный, уменьшить 1024→64
2. Для terrain_* спрайтов: просто уменьшить 1024→64
3. Сохранить обработанные файлы в Tiles_AI/ (заменить оригиналы)

**АЛЬТЕРНАТИВНО:** Убрать LoadResourceSprite() из ResourceSpawner — всегда использовать программные спрайты (гарантированная прозрачность)

---

## ФИНАЛЬНЫЕ ВАРИАНТЫ ИСПРАВЛЕНИЙ (по вероятности успеха)

### Вариант A: "Минимальные изменения — максимальный эффект" ⭐⭐⭐ РЕКОМЕНДУЕТСЯ

1. **Python-скрипт** — обработать AI-спрайты (уменьшить + прозрачность) → заменить в Tiles_AI/
2. **TileSpriteGenerator.cs** — вместо программной генерации, копировать/обрабатывать AI-спрайты в Tiles/
3. **PlayerVisual.cs** — сменить шейдер на Sprite-Unlit-Default + контрастный цвет + позиция центра
4. **FullSceneBuilder.cs** — ExecutePlayer() позиция = центр карты

**Вероятность успеха: 90%** — решает все 3 проблемы

### Вариант B: "Только код, без AI-спрайтов" ⭐⭐

1. **PlayerVisual.cs** — Unlit шейдер + красный цвет + центр карты
2. **ResourceSpawner.cs** — убрать LoadResourceSprite(), всегда программные спрайты
3. **FullSceneBuilder.cs** — позиция игрока на центр

**Вероятность успеха: 70%** — игрок виден, но объекты остаются программными

### Вариант C: "AI-спрайты + правильный PPU" ⭐

1. Обработать AI-спрайты (добавить RGBA alpha)
2. Изменить PPU=512 для terrain, PPU=2560 для objects
3. Исправить CreateTerrainTileAsset — искать в Tiles_AI/

**Вероятность успеха: 60%** — сложнее, больше точек отказа

---

## ВАЖНО: ПРОВЕРИТЬ ПЕРЕД ИСПРАВЛЕНИЕМ

1. **URP 2D renderer активен?** Если да — нужен Light2D или Unlit шейдер
2. **Какой шейдер реально используется?** Sprite-Lit-Default или Sprites/Default?
3. **Программные спрайты (в Tiles/) с прозрачностью?** Проверить obj_tree.png → есть ли alpha канал

*Создано: 2026-04-15 16:28:00 UTC*
*Редактировано: 2026-04-15 16:45:00 UTC — углублённая трассировка + новые гипотезы*
