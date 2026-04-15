# Чекпоинт: Исправление 4 новых визуальных проблем (итерация 2)

**Дата:** 2026-04-15 17:14:21 UTC
**Статус:** complete
**Основание:** checkpoints/04_15_fix_three_visual_problems.md (итерация 1)

---

## 4 НОВЫХ ПРОБЛЕМЫ (после итерации 1)

### Проблема 1: Задвоение спрайта персонажа ✅ ИСПРАВЛЕНО

**Симптом:** Два спрайта персонажа видны одновременно.

**Причина:** FullSceneBuilder Phase 06 создаёт Player с PlayerVisual. Затем TestLocationGameController.Start() → SpawnPlayer() → CreateBasicPlayer() создаёт ВТОРОГО Player с PlayerVisual. Оба PlayerVisual.Awake() создают визуал.

**Доказательство из лога:**
```
[PlayerVisual] Loaded AI sprite: Assets/Sprites/Characters/Player/player_variant1_cultivator.png
CultivationGame.Player.PlayerVisual:Awake()  ← ПЕРВЫЙ
...
[PlayerVisual] Loaded AI sprite: Assets/Sprites/Characters/Player/player_variant1_cultivator.png
CultivationGame.World.TestLocationGameController:CreateBasicPlayer()  ← ВТОРОЙ
```

**Исправление:** `TestLocationGameController.SpawnPlayer()` — сначала проверяет `GameObject.Find("Player")`. Если Player уже существует — использует его, не создаёт нового.

**Файл:** `TestLocationGameController.cs`

---

### Проблема 2: Белая сетка между тайлами ✅ ИСПРАВЛЕНО

**Симптом:** Белые линии между terrain-тайлами при использовании AI-спрайтов.

**Причина:** AI-спрайты 64×64 при PPU=32 = 2.0 юнита = ТОЧНО размер ячейки. Из-за floating-point погрешностей между тайлами образуются субпиксельные зазоры = белая сетка.

**Дополнительная причина:** `TileMapController.LoadTileSprite()` ищет в `Tiles_AI/` ПЕРВЫМ — находит 1024×1024 RGB спрайты с PPU=32 → 1024/32=32 юнита (ОГРОМНЫЙ тайл!).

**Исправление:**
1. PPU=31 вместо 32 для terrain: 64/31 = 2.065 юнита → лёгкое перекрытие → нет зазоров
2. `TileMapController.LoadTileSprite()` — убран `Tiles_AI/` из поиска
3. `FullSceneBuilder.ReimportTileSprites()` — PPU=31 для terrain
4. `TileSpriteGenerator` — PPU=31 для terrain
5. `TileMapController.CreateProceduralTileSprite()` — PPU=31 для terrain

**Файлы:** TileSpriteGenerator.cs, FullSceneBuilder.cs, TileMapController.cs

---

### Проблема 3: Спрайты ресурсов невидимы ✅ ИСПРАВЛЕНО

**Симптом:** Ресурсы спавнятся (113 штук), но ни один не виден.

**Причина (двойная):**
1. `spriteScale=0.16` — при PPU=160: 64/160=0.4 юнита × 0.16 = 0.064 юнита — МИКРОСКОПИЧЕСКИЙ размер
2. Sprite-Lit-Default шейдер без Light2D = чёрный (невидимый) спрайт

**Исправление:**
1. `spriteScale` = 1.0 (было 0.16) — при PPU=160: 64/160=0.4 юнита × 1.0 = 0.4 юнита
2. Добавлен Sprite-Unlit-Default шейдер в SpawnSingleResource()

**Файл:** ResourceSpawner.cs

---

### Проблема 4: ItemData не найден ✅ ИСПРАВЛЕНО

**Симптом:** Повторяющая ошибка: `[ResourcePickup] ItemData не найден для 'stone'`

**Причина:** ResourcePickup.TryAddToInventory() требует ItemData ScriptableObject для добавления в инвентарь. ItemData .asset файлы не созданы для ресурсов. Без ItemData → return false → ресурс не подобран, остаётся на карте.

**Исправление:** Добавлен `CreateTemporaryItemData(string id)` — создаёт runtime ItemData с маппингом resourceId → nameRu/category/rarity. Если ItemData .asset не найден — используется временный.

**Файл:** ResourcePickup.cs

---

## ИЗМЕНЁННЫЕ ФАЙЛЫ

| Файл | Изменение |
|------|-----------|
| `TestLocationGameController.cs` | SpawnPlayer() — использовать существующего Player |
| `TileSpriteGenerator.cs` | TERRAIN_PPU=31 |
| `FullSceneBuilder.cs` | ReimportTileSprites PPU=31 |
| `TileMapController.cs` | PPU=31 + убрать Tiles_AI/ из LoadTileSprite |
| `ResourceSpawner.cs` | spriteScale=1.0 + Unlit шейдер |
| `ResourcePickup.cs` | CreateTemporaryItemData() — fallback для ресурсов без ItemData |

---

## ЧТО ПРОВЕРИТЬ ПОСЛЕ ИСПРАВЛЕНИЙ

1. **Один спрайт персонажа?** Не должно быть задвоения
2. **Нет белой сетки?** Terrain-тайлы без зазоров
3. **Ресурсы видны?** Кристаллы/деревья/растения на карте
4. **Ресурсы подбираются?** Нет ошибки "ItemData не найден"

---

*Создано: 2026-04-15 17:14:21 UTC*
