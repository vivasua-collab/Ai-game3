# Чекпоинт: План исправления 3 визуальных проблем

**Дата:** 2026-04-15 16:34:00 UTC
**Редактировано:** 2026-04-15 17:01:06 UTC — все исправления реализованы
**Статус:** complete
**Основание:** checkpoints/04_15_audit_three_visual_problems.md

---

## ПОДТВЕРЖДЁННЫЕ КОРНЕВЫЕ ПРИЧИНЫ

| # | Проблема | Корневая причина | Уверенность |
|---|----------|-----------------|-------------|
| 1 | Terrain = программные текстуры, не AI | TileSpriteGenerator пишет в Tiles/, CreateTerrainTileAsset ищет только в Tiles/ | 100% |
| 2 | Игрок не отображается | URP 2D renderer БЕЗ Light2D → Sprite-Lit-Default = невидимый + позиция (0,0)=Void | 90% |
| 3 | Белый фон объектов | AI-спрайты RGB 1024×1024 без альфа-канала; ResourceSpawner загружает их из Tiles_AI/ | 85% |

**ДОКАЗАТЕЛЬСТВО URP 2D:** Файл `Assets/Settings/Renderer2D.asset` существует → URP 2D renderer активен → `Sprite-Lit-Default` шейдер требует Light2D, которого НЕТ в сцене.

---

## ИСПРАВЛЕНИЯ — СТАТУС ВЫПОЛНЕНИЯ

### ✅ Исправление 2A: Добавить Global Light2D в сцену (КРИТИЧЕСКОЕ)

**Файл:** `FullSceneBuilder.cs` — Phase 04 (Camera & Light)
**Статус:** ВЫПОЛНЕНО (2026-04-15 16:53:48 UTC)

**Что сделано:**
- Добавлен код создания `GlobalLight2D` объекта после Directional Light
- Light2D добавляется через Reflection (`System.Type.GetType`) — безопасно даже если пакет 2D не установлен
- Настраивается: lightType=Global, intensity=1f, color=white
- `IsCameraLightNeeded()` обновлён — проверяет и Light2D
- Если Light2D не найден через Reflection — предупреждение в консоль

**Альтернатива:** PlayerVisual теперь использует `Sprite-Unlit-Default` шейдер (см. Fix 2C)

---

### ✅ Исправление 2B: Сменить позицию игрока на центр карты

**Файл:** `FullSceneBuilder.cs` — ExecutePlayer()
**Статус:** ВЫПОЛНЕНО (2026-04-15 16:53:48 UTC)

**Что сделано:**
- `player.transform.position = Vector3.zero` → `new Vector3(100f, 80f, 0f)`
- Центр карты 100×80 тайлов × 2м/тайл = (100, 80, 0)

---

### ✅ Исправление 2C: Контрастный цвет игрока + Unlit шейдер

**Файл:** `PlayerVisual.cs`
**Статус:** ВЫПОЛНЕНО (2026-04-15 16:53:48 UTC)

**Что сделано:**
- Цвет: `new Color(0.2f, 0.8f, 0.3f)` (зелёный) → `new Color(1f, 0.3f, 0.15f)` (красно-оранжевый)
- Шейдер: `Sprite-Lit-Default` → сначала `Sprite-Unlit-Default` (рендерит БЕЗ Light2D)
- Fallback: `Sprite-Lit-Default` → `Sprites/Default`

---

### ✅ Исправление 1A: TileSpriteGenerator — обработка AI-спрайтов вместо программных

**Файл:** `TileSpriteGenerator.cs` — полная переработка (версия 2.0)
**Статус:** ВЫПОЛНЕНО (2026-04-15 16:53:48 UTC)

**Что сделано:**
- `GenerateAllSprites()` теперь проверяет `Tiles_AI/` в первую очередь
- Новый метод `ProcessAISprites()`:
  - Загружает AI-спрайты (1024×1024 RGB)
  - Для obj_*: удаляет фон через flood-fill от углов → прозрачный
  - Уменьшает 1024→64 через RenderTexture (Lanczos)
  - Сохраняет обработанные RGBA64 PNG в Tiles/
  - Настраивает импорт с правильным PPU
- Fallback: `GenerateMissingProceduralSprites()` — только для недостающих файлов
- Terrain размер 64×64 (было 68×68) — PPU=32 → 2.0 юнита = ТОЧНО в ячейку → НЕТ белой сетки
- Object размер 64×64 — PPU=160 → 0.4 юнита

---

### ✅ Исправление 3A: ResourceSpawner — убрать Tiles_AI/ из поиска

**Файл:** `ResourceSpawner.cs` — LoadResourceSprite()
**Статус:** ВЫПОЛНЕНО (2026-04-15 16:53:48 UTC)

**Что сделано:**
- Убран `Assets/Sprites/Tiles_AI/{spriteName}.png` из searchPaths
- Оставлен только `Assets/Sprites/Tiles/{spriteName}.png` — обработанные AI-спрайты с прозрачностью

---

### ✅ Python-скрипт для предобработки AI-спрайтов

**Файл:** `tools/process_ai_sprites.py` (НОВЫЙ)
**Статус:** ВЫПОЛНЕНО И ПРОВЕРЕНО (2026-04-15 17:01:06 UTC)

**Что делает:**
- Читает PNG из `UnityProject/Assets/Sprites/Tiles_AI/`
- Для obj_*: конвертирует RGB→RGBA, flood-fill фон → прозрачный, уменьшает 1024→64
- Для terrain_*: уменьшает 1024→64, конвертирует в RGBA
- Записывает в `UnityProject/Assets/Sprites/Tiles/`

**Результат тестового запуска:** 17/17 спрайтов обработано, 0 ошибок. Все 64×64 RGBA.

---

## ИЗМЕНЁННЫЕ ФАЙЛЫ

| Файл | Изменение |
|------|-----------|
| `FullSceneBuilder.cs` | Light2D + IsCameraLightNeeded + позиция игрока |
| `PlayerVisual.cs` | Цвет + Unlit шейдер |
| `TileSpriteGenerator.cs` | Полная переработка v2.0 — AI-спрайты приоритет |
| `ResourceSpawner.cs` | Убран Tiles_AI/ из поиска |
| `tools/process_ai_sprites.py` | НОВЫЙ — Python предобработка |
| `Assets/Sprites/Tiles/*.png` | 17 обработанных AI-спрайтов (64×64 RGBA) |

---

## ЧТО ПРОВЕРИТЬ ПОСЛЕ ИСПРАВЛЕНИЙ В UNITY

1. **Light2D добавлен?** В Hierarchy: GlobalLight2D с Light2D компонентом
2. **Игрок виден?** Красно-оранжевый гуманоид на каменном алтаре
3. **Terrain из AI-спрайтов?** Трава/камень/вода с текстурами, не цветные квадраты
4. **Объекты с прозрачностью?** Деревья/камни БЕЗ белого фона
5. **Белая сетка между тайлами?** Должна исчезнуть (AI-спрайты 64×64 PPU=32 = точно 2.0 юнита)

---

## ПОРЯДОК ВЫПОЛНЕНИЯ В UNITY

1. Удалить папку Assets локально (как обычно)
2. **ОПЦИОНАЛЬНО:** Запустить `python3 tools/process_ai_sprites.py` для предобработки AI-спрайтов
3. Открыть Unity → запустить `Tools → Full Scene Builder → Build All (One Click)`
4. Build All автоматически:
   - Phase 04: Создаст GlobalLight2D (если Light2D доступен)
   - Phase 06: Создаст игрока на позиции (100, 80, 0)
   - Phase 10: Обработает AI-спрайты → сохранит в Tiles/
   - Phase 14: Создаст tile assets из обработанных AI-спрайтов

---

*Создано: 2026-04-15 16:34:00 UTC*
*Редактировано: 2026-04-15 17:01:06 UTC — все исправления реализованы*
