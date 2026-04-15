# Чекпоинт: План исправления 3 визуальных проблем

**Дата:** 2026-04-15 16:34:00 UTC
**Статус:** pending
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

## ИСПРАВЛЕНИЯ (пошагово, в порядке выполнения)

### Исправление 2A: Добавить Global Light2D в сцену (КРИТИЧЕСКОЕ)

**Файл:** `FullSceneBuilder.cs` — Phase 04 (Camera & Light)

**Что сделать:** После создания Directional Light, добавить Light2D:
```csharp
// Добавить Global 2D Light — без него Sprite-Lit-Default шейдер
// рендерит все спрайты как чёрные (невидимые)
var light2DObj = new GameObject("GlobalLight2D");
light2DObj.transform.SetParent(lightObj.transform.parent);
var light2D = light2DObj.AddComponent<Light2D>();
light2D.lightType = Light2D.LightType.Global;
light2D.color = Color.white;
light2D.intensity = 1f;
```

**Почему:** URP 2D Renderer2D.asset активен → Sprite-Lit-Default используется по умолчанию → без Light2D ВСЕ спрайты невидимы. Это объясняет ВСЕ три проблемы: terrain не виден (белая сетка — это наоборот прозрачность/отсутствие света), игрок невидим, объекты невидимы.

**Альтернатива:** Если Light2D нельзя добавить программно (нет доступа к типу), использовать Sprite-Unlit-Default шейдер в PlayerVisual.cs.

**Риск:** Light2D может не быть доступен через скрипт если пакет 2D Lighting не установлен. Проверить: `using UnityEngine.Rendering.Universal;`

---

### Исправление 2B: Сменить позицию игрока на центр карты

**Файл:** `FullSceneBuilder.cs` — ExecutePlayer()

**Что изменить:**
```csharp
// Было:
player.transform.position = Vector3.zero;

// Стало:
// Центр карты 100×80 тайлов × 2м/тайл = (100, 80, 0) мировых координат
player.transform.position = new Vector3(100f, 80f, 0f);
```

**Почему:** Позиция (0,0) = Void (граница карты). Игрок на Void → камера видит чёрный экран.

---

### Исправление 2C: Контрастный цвет игрока

**Файл:** `PlayerVisual.cs`

**Что изменить:**
```csharp
// Было:
public Color playerColor = new Color(0.2f, 0.8f, 0.3f); // Зелёный — сливается с травой

// Стало:
public Color playerColor = new Color(1f, 0.3f, 0.15f); // Красно-оранжевый — контрастный
```

---

### Исправление 1A: TileSpriteGenerator — использовать AI-спрайты вместо программных

**Файл:** `TileSpriteGenerator.cs`

**Что сделать:** Переписать `GenerateAllSprites()` — загружать AI-спрайты из Tiles_AI/, обрабатывать (уменьшать, добавлять прозрачность), сохранять в Tiles/:

```csharp
[MenuItem("Tools/Generate Tile Sprites")]
public static void GenerateAllSprites()
{
    if (!Directory.Exists(OUTPUT_PATH))
        Directory.CreateDirectory(OUTPUT_PATH);

    string aiPath = "Assets/Sprites/Tiles_AI";

    // Если AI-спрайты есть — обработать и скопировать в Tiles/
    if (Directory.Exists(aiPath))
    {
        ProcessAISprites(aiPath);
    }
    else
    {
        // Fallback: программная генерация
        GenerateProceduralSprites();
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
}
```

**Новый метод ProcessAISprites():**
1. Сканировать Tiles_AI/ на PNG файлы
2. Для каждого файла:
   - Загрузить Texture2D через AssetDatabase
   - Уменьшить с 1024×1024 до 64×64 (через RenderTexture или новый Texture2D)
   - Для obj_*: добавить прозрачность (определить фон и заменить на Color.clear)
   - Сохранить как PNG в Tiles/
   - Реимпортировать с правильными настройками (PPU=32 terrain, PPU=160 objects)

**Проблема:** Уменьшение 1024→64 в Editor скрипте может быть сложным. Альтернатива — Python скрипт вне Unity.

---

### Исправление 1B (АЛЬТЕРНАТИВА 1A): Python-скрипт для обработки AI-спрайтов

**Файл:** `tools/process_ai_sprites.py` (НОВЫЙ)

**Что делает:**
1. Читает PNG из `UnityProject/Assets/Sprites/Tiles_AI/`
2. Для terrain_*: уменьшает 1024→64, сохраняет RGB
3. Для obj_*: уменьшает 1024→64, flood-fill от углов → прозрачный фон, сохраняет RGBA
4. Записывает в `UnityProject/Assets/Sprites/Tiles/`

**Запуск:** `python3 tools/process_ai_sprites.py` (вручную перед Build All)

**Плюс:** Не нужно менять TileSpriteGenerator — Python обрабатывает файлы ДО Unity
**Минус:** Нужно запускать вручную перед каждым пересозданием

**Алгоритм flood-fill для прозрачности:**
```python
from PIL import Image
from collections import deque

def remove_background(img, tolerance=30):
    """Flood-fill от углов для удаления фона"""
    rgba = img.convert('RGBA')
    pixels = rgba.load()
    w, h = rgba.size
    visited = set()
    queue = deque()

    # Стартовые точки — 4 угла
    for x, y in [(0,0), (w-1,0), (0,h-1), (w-1,h-1)]:
        queue.append((x, y))

    # Цвет углового пикселя = цвет фона
    bg_color = pixels[0, 0][:3]

    while queue:
        x, y = queue.popleft()
        if (x, y) in visited: continue
        if x < 0 or x >= w or y < 0 or y >= h: continue
        visited.add((x, y))

        r, g, b, a = pixels[x, y]
        # Если пиксель близок к фоновому цвету → прозрачный
        if abs(r - bg_color[0]) < tolerance and \
           abs(g - bg_color[1]) < tolerance and \
           abs(b - bg_color[2]) < tolerance:
            pixels[x, y] = (r, g, b, 0)  # сделать прозрачным
            queue.extend([(x+1,y), (x-1,y), (x,y+1), (x,y-1)])

    return rgba
```

---

### Исправление 3A: ResourceSpawner — убрать загрузку AI-спрайтов без прозрачности

**Файл:** `ResourceSpawner.cs`

**Что изменить:** В `LoadResourceSprite()` — НЕ загружать AI-спрайты если у них нет альфа-канала, ИЛИ всегда использовать программные fallback:

**Вариант A (простой):** Убрать загрузку из Tiles_AI/:
```csharp
string[] searchPaths = new string[]
{
    // Убрано: $"Assets/Sprites/Tiles_AI/{spriteName}.png" — RGB без alpha
    $"Assets/Sprites/Tiles/{spriteName}.png"  // Только обработанные
};
```

**Вариант B (надёжный):** Проверять наличие альфа-канала:
```csharp
var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
if (sprite != null && sprite.texture.format == TextureFormat.RGBA32)
    return sprite;
// RGB текстуры пропускаем — нет прозрачности
```

---

### Исправление 3B (связано с 1A/1B): Обработанные AI-спрайты в Tiles/

Если Исправление 1A или 1B выполнено — обработанные AI-спрайты с прозрачностью лежат в Tiles/ → ResourceSpawner.LoadResourceSprite() найдёт их в Tiles/ → прозрачность будет работать.

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

| Шаг | Исправление | Файл | Влияние |
|-----|-------------|------|---------|
| 1 | 2A: Global Light2D | FullSceneBuilder.cs | ВСЕ спрайты становятся видимыми |
| 2 | 2B: Позиция игрока | FullSceneBuilder.cs | Игрок виден на траве/камне |
| 3 | 2C: Контрастный цвет | PlayerVisual.cs | Игрок виден на фоне |
| 4 | 1B: Python обработка AI | tools/process_ai_sprites.py | AI-спрайты готовы к использованию |
| 5 | 3A: Убрать Tiles_AI/ из ResourceSpawner | ResourceSpawner.cs | Нет белого фона ресурсов |
| 6 | Проверка | — | Запуск Build All → проверить |

**Шаг 1 (Light2D) — КРИТИЧЕСКИЙ.** Без него все остальные исправления могут не сработать.

---

## ЗАВИСИМОСТИ МЕЖДУ ИСПРАВЛЕНИЯМИ

```
2A (Light2D) ← блокирует ВСЁ остальное
   ↓
2B (позиция) + 2C (цвет) ← независимы друг от друга
   ↓
1B (Python) ← независимо от 2A-2C, но нужно для AI-спрайтов
   ↓
3A (ResourceSpawner) ← зависит от 1B (нужны обработанные спрайты в Tiles/)
```

---

## ЧТО ПРОВЕРИТЬ ПОСЛЕ ИСПРАВЛЕНИЙ

1. **Light2D добавлен?** В Hierarchy: GlobalLight2D с Light2D компонентом
2. **Игрок виден?** Красно-оранжевый гуманоид на каменном алтаре
3. **Terrain из AI-спрайтов?** Трава/камень/вода с текстурами, не цветные квадраты
4. **Объекты с прозрачностью?** Деревья/камни БЕЗ белого фона
5. **Белая сетка между тайлами?** Должна исчезнуть (AI-спрайты 64×64 = точно 2.0 юнита)

---

## РИСКИ И ОТКАТ

| Риск | Вероятность | Откат |
|------|-------------|-------|
| Light2D тип не доступен программно | 10% | Использовать Sprite-Unlit-Default шейдер |
| Python скрипт повредит AI-спрайты | 5% | Оригиналы в git, git checkout восстановит |
| Уменьшение 1024→64 ухудшит качество | 30% | Сохранить оригиналы, использовать PPU=512 вместо уменьшения |
| Объекты слишком маленькие при PPU=160 | 20% | Уменьшить PPU до 64 (64/64=1.0 юнита) |

---

## ФАЙЛЫ ДЛЯ ИЗМЕНЕНИЯ

1. `UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs` — Light2D + позиция игрока
2. `UnityProject/Assets/Scripts/Player/PlayerVisual.cs` — цвет игрока
3. `UnityProject/Assets/Scripts/Tile/ResourceSpawner.cs` — убрать Tiles_AI/ из поиска
4. `UnityProject/Assets/Scripts/Tile/Editor/TileSpriteGenerator.cs` — опционально (если делаем 1A)
5. `tools/process_ai_sprites.py` — НОВЫЙ Python скрипт (если делаем 1B)

---

*Создано: 2026-04-15 16:34:00 UTC*
