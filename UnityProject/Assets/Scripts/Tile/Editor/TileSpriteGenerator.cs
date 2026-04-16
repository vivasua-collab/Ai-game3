// ============================================================================
// TileSpriteGenerator.cs — Генератор спрайтов тайлов с поддержкой AI-спрайтов
// Cultivation World Simulator
// Версия: 2.0
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-15 17:31:49 UTC — FIX 2A: terrain 68×68 PPU=32 Bilinear,
//   убрана обработка AI terrain (белая сетка), obj_* AI-спрайты сохраняются.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace CultivationGame.TileSystem.Editor
{
    /// <summary>
    /// Генератор спрайтов для тайловой системы.
    ///
    /// Версия 3.0 — раздельная обработка:
    ///   Terrain: ТОЛЬКО процедурные 68×68 PPU=32 Bilinear → 2.125 юнита →
    ///     перекрытие ячейки (2.0 юнита) = нет белой сетки.
    ///     AI terrain-спрайты УДАЛЕНЫ (вызывали белую сетку при 64×64 PPU=31).
    ///
    ///   Objects: AI-спрайты из Tiles_AI/ (обработка: resize + прозрачность).
    ///     64×64 px, PPU=160 → 0.4 юнита.
    ///     Прозрачный фон RGBA32.
    /// </summary>
    public static class TileSpriteGenerator
    {
        // FIX 2A: Terrain 68×68 PPU=32 → 68/32 = 2.125 юнита → 0.0625 юнита перекрытие с каждой стороны.
        // Перекрытие устраняет белую сетку между тайлами (pixel bleed approach).
        // AI terrain спрайты удалены — 64×64 при любом PPU давали белую сетку.
        // Редактировано: 2026-04-15 17:31:49 UTC
        private const int TERRAIN_SIZE = 68;   // 68×68 — на 4 пикселя больше ячейки для pixel bleed
        private const int OBJECT_SIZE = 64;    // 64×64 — стандартный размер объекта
        // FIX-V2-3: Terrain PPU=30 (было 31) — 68/30 = 2.267 юнита → pixel bleed.
        // При PPU=31: 64/31=2.065u — только 1.6% перекрытия, недостаточно для Bilinear.
        // 64/30=2.133u — 6.7% перекрытие, надёжно устраняет белую сетку.
        // Должно совпадать с TileMapController, HarvestableSpawner, FullSceneBuilder.
        // Редактировано: 2026-04-16 11:37 UTC
        private const int TERRAIN_PPU = 30;    // 68/30 = 2.267 юнита — 13.3% перекрытие устраняет зазоры
        private const int OBJECT_PPU = 160;    // 64/160 = 0.4 юнита — в 5 раз меньше ячейки
        private const string OUTPUT_PATH = "Assets/Sprites/Tiles";
        private const string AI_SPRITES_PATH = "Assets/Sprites/Tiles_AI";

        [MenuItem("Tools/Generate Tile Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(OUTPUT_PATH))
                Directory.CreateDirectory(OUTPUT_PATH);

            // FIX 2A: Обрабатываем ТОЛЬКО obj_* AI-спрайты (terrain — процедурные!)
            // AI terrain вызывал белую сетку — удалён. Terrain генерируется программно.
            // Редактировано: 2026-04-15 17:31:49 UTC
            bool usedAI = false;
            if (Directory.Exists(AI_SPRITES_PATH))
            {
                // Фильтруем только obj_* файлы
                string[] allAiFiles = Directory.GetFiles(AI_SPRITES_PATH, "*.png");
                var objFiles = new System.Collections.Generic.List<string>();
                foreach (var f in allAiFiles)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(f);
                    if (name.StartsWith("obj_"))
                        objFiles.Add(f);
                }
                if (objFiles.Count > 0)
                {
                    Debug.Log($"[TileSpriteGenerator] Найдено {objFiles.Count} AI obj_* спрайтов. Обработка...");
                    usedAI = ProcessAISprites(objFiles.ToArray());
                }
            }

            // Генерация процедурных terrain-спрайтов (68×68 PPU=32 Bilinear)
            GenerateMissingProceduralSprites();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TileSpriteGenerator] Спрайты сгенерированы (AI: {usedAI}) в {OUTPUT_PATH}");
        }

        // ====================================================================
        //  AI-СПРАЙТЫ: обработка и копирование в Tiles/
        // ====================================================================

        /// <summary>
        /// Обработать AI-спрайты: уменьшить 1024→64, добавить прозрачность для obj_*,
        /// сохранить в Tiles/ с правильными настройками импорта.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static bool ProcessAISprites(string[] aiFiles)
        {
            int processed = 0;
            int failed = 0;

            foreach (string aiPath in aiFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(aiPath);
                bool isObject = fileName.StartsWith("obj_");

                try
                {
                    // Загружаем AI-спрайт как текстуру
                    Texture2D sourceTex = LoadAITexture(aiPath);
                    if (sourceTex == null)
                    {
                        Debug.LogWarning($"[TileSpriteGenerator] Не удалось загрузить AI-спрайт: {aiPath}");
                        failed++;
                        continue;
                    }

                    // Для obj_*: удаляем фон (flood fill от углов → прозрачный)
                    if (isObject)
                    {
                        RemoveBackground(sourceTex, tolerance: 0.12f);
                    }

                    // Уменьшаем до целевого размера
                    int targetSize = isObject ? OBJECT_SIZE : TERRAIN_SIZE;
                    Texture2D resizedTex = ResizeTexture(sourceTex, targetSize, targetSize);

                    // Сохраняем в Tiles/
                    string outputPath = Path.Combine(OUTPUT_PATH, fileName + ".png");
                    SaveTextureToPNG(resizedTex, outputPath);

                    // Настраиваем импорт с правильным PPU
                    SetupSpriteImport(outputPath, isObject);

                    // Освобождаем временные текстуры
                    if (resizedTex != sourceTex)
                        Object.DestroyImmediate(resizedTex);
                    Object.DestroyImmediate(sourceTex);

                    processed++;
                    Debug.Log($"[TileSpriteGenerator] AI-спрайт обработан: {fileName} (isObject={isObject})");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[TileSpriteGenerator] Ошибка обработки {fileName}: {ex.Message}");
                    failed++;
                }
            }

            Debug.Log($"[TileSpriteGenerator] AI-спрайты: обработано {processed}, ошибок {failed}");
            return processed > 0;
        }

        /// <summary>
        /// Загрузить AI-спрайт из файла как Texture2D.
        /// Используем LoadImage() — работает с RGB PNG, результат RGBA32.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static Texture2D LoadAITexture(string assetPath)
        {
            // Метод 1: Пробуем через AssetDatabase (быстрее, но может дать compressed текстуру)
            var sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sourceSprite != null && sourceSprite.texture != null)
            {
                var srcTex = sourceSprite.texture;
                // Создаём копию в RGBA32 (оригинал может быть compressed)
                var copy = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
                copy.SetPixels(srcTex.GetPixels());
                copy.Apply();
                return copy;
            }

            // Метод 2: Читаем PNG файл напрямую и загружаем через LoadImage
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                return null;

            byte[] fileData = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(fileData); // LoadImage автоматически определяет размер и формат
            return tex;
        }

        /// <summary>
        /// Уменьшить текстуру до целевого размера.
        /// Использует RenderTexture с билинейной фильтрацией для качественного масштабирования.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            // Если размер уже совпадает — просто возвращаем копию
            if (source.width == newWidth && source.height == newHeight)
            {
                var copy = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                copy.SetPixels(source.GetPixels());
                copy.Apply();
                return copy;
            }

            // RenderTexture для качественного масштабирования
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            rt.filterMode = FilterMode.Bilinear;

            Graphics.Blit(source, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            result.filterMode = FilterMode.Point;
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        /// <summary>
        /// Удалить фон у AI-спрайта объекта через flood fill от углов.
        /// AI-спрайты — RGB 1024×1024, фон = однотонный (обычно белый или чёрный).
        /// Flood fill от 4 углов: все подключённые пиксели с цветом ≈ угловому → alpha=0.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static void RemoveBackground(Texture2D tex, float tolerance)
        {
            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();

            // Цвет фона = средний цвет угловых пикселей
            Color bgSum = Color.clear;
            int cornerCount = 0;
            Color[] cornerColors = new Color[] {
                pixels[0],                    // левый нижний
                pixels[w - 1],                // правый нижний
                pixels[(h - 1) * w],          // левый верхний
                pixels[(h - 1) * w + w - 1]   // правый верхний
            };
            foreach (var c in cornerColors)
            {
                bgSum += c;
                cornerCount++;
            }
            Color bgColor = bgSum / cornerCount;

            // Flood fill от углов — BFS
            bool[] visited = new bool[w * h];
            Queue<int> queue = new Queue<int>();

            // Стартовые точки — 4 угла + приграничные пиксели для надёжности
            queue.Enqueue(0);
            queue.Enqueue(w - 1);
            queue.Enqueue((h - 1) * w);
            queue.Enqueue((h - 1) * w + w - 1);

            // Также добавляем пиксели по краям (для сложных фонов)
            for (int x = 0; x < w; x += w / 16)
            {
                queue.Enqueue(x);                    // нижний край
                queue.Enqueue((h - 1) * w + x);      // верхний край
            }
            for (int y = 0; y < h; y += h / 16)
            {
                queue.Enqueue(y * w);                 // левый край
                queue.Enqueue(y * w + w - 1);         // правый край
            }

            float tolR = tolerance;
            float tolG = tolerance;
            float tolB = tolerance;

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (idx < 0 || idx >= pixels.Length) continue;
                if (visited[idx]) continue;
                visited[idx] = true;

                Color c = pixels[idx];
                // Проверяем, похож ли пиксель на фоновый цвет
                if (Mathf.Abs(c.r - bgColor.r) <= tolR &&
                    Mathf.Abs(c.g - bgColor.g) <= tolG &&
                    Mathf.Abs(c.b - bgColor.b) <= tolB)
                {
                    // Делаем прозрачным, сохраняя RGB для антиалиасинга
                    pixels[idx] = new Color(c.r, c.g, c.b, 0f);

                    int x = idx % w;
                    int y = idx / w;
                    // Соседи: вверх, вниз, влево, вправо
                    if (x > 0) queue.Enqueue(idx - 1);
                    if (x < w - 1) queue.Enqueue(idx + 1);
                    if (y > 0) queue.Enqueue(idx - w);
                    if (y < h - 1) queue.Enqueue(idx + w);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
        }

        /// <summary>
        /// Сохранить Texture2D как PNG файл.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static void SaveTextureToPNG(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Настроить импорт спрайта с правильным PPU и настройками.
        /// FIX 2A: Bilinear для terrain (сглаживает границы), Point для objects.
        /// Редактировано: 2026-04-15 17:31:49 UTC
        /// </summary>
        private static void SetupSpriteImport(string path, bool isObject)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = isObject ? OBJECT_PPU : TERRAIN_PPU;
                // FIX 2A: Terrain — Bilinear (устраняет белую сетку), Objects — Point (резкие края)
                importer.filterMode = isObject ? FilterMode.Point : FilterMode.Bilinear;
                importer.spriteBorder = Vector4.zero;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        // ====================================================================
        //  ПРОГРАММНЫЕ СПРАЙТЫ (fallback если AI-спрайтов нет)
        // ====================================================================

        /// <summary>
        /// Генерирует только те программные спрайты, которых не хватает в Tiles/.
        /// Не перезаписывает существующие (обработанные AI-спрайты).
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static void GenerateMissingProceduralSprites()
        {
            // Terrain sprites (64×64, PPU=32 → 2.0 units)
            GenerateTerrainSpriteIfMissing("terrain_grass", new Color(0.4f, 0.7f, 0.3f));
            GenerateTerrainSpriteIfMissing("terrain_dirt", new Color(0.6f, 0.4f, 0.2f));
            GenerateTerrainSpriteIfMissing("terrain_stone", new Color(0.5f, 0.5f, 0.55f));
            GenerateTerrainSpriteIfMissing("terrain_water_shallow", new Color(0.3f, 0.5f, 0.8f, 0.8f));
            GenerateTerrainSpriteIfMissing("terrain_water_deep", new Color(0.2f, 0.3f, 0.7f, 0.9f));
            GenerateTerrainSpriteIfMissing("terrain_sand", new Color(0.9f, 0.85f, 0.6f));
            GenerateTerrainSpriteIfMissing("terrain_snow", new Color(0.95f, 0.95f, 1f));
            GenerateTerrainSpriteIfMissing("terrain_ice", new Color(0.7f, 0.85f, 0.95f));
            GenerateTerrainSpriteIfMissing("terrain_lava", new Color(0.9f, 0.3f, 0.05f));
            GenerateTerrainSpriteIfMissing("terrain_void", new Color(0.1f, 0.1f, 0.1f));

            // Object sprites (64×64, PPU=160 → 0.4 units)
            GenerateObjectSpriteIfMissing("obj_tree", new Color(0.3f, 0.5f, 0.2f), ObjectShape.Tree);
            GenerateObjectSpriteIfMissing("obj_rock_small", new Color(0.6f, 0.55f, 0.5f), ObjectShape.SmallRock);
            GenerateObjectSpriteIfMissing("obj_rock_medium", new Color(0.5f, 0.45f, 0.4f), ObjectShape.MediumRock);
            GenerateObjectSpriteIfMissing("obj_bush", new Color(0.35f, 0.55f, 0.25f), ObjectShape.Bush);
            GenerateObjectSpriteIfMissing("obj_chest", new Color(0.6f, 0.4f, 0.2f), ObjectShape.Chest);
            GenerateObjectSpriteIfMissing("obj_ore_vein", new Color(0.7f, 0.5f, 0.2f), ObjectShape.OreVein);
            GenerateObjectSpriteIfMissing("obj_herb", new Color(0.2f, 0.6f, 0.3f), ObjectShape.Herb);
        }

        /// <summary>
        /// Генерировать программный terrain-спрайт только если файл не существует.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static void GenerateTerrainSpriteIfMissing(string name, Color color)
        {
            string path = Path.Combine(OUTPUT_PATH, name + ".png");
            if (File.Exists(path)) return; // Не перезаписываем AI-спрайты

            GenerateTerrainSprite(name, color);
        }

        /// <summary>
        /// Генерировать программный object-спрайт только если файл не существует.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private static void GenerateObjectSpriteIfMissing(string name, Color color, ObjectShape shape)
        {
            string path = Path.Combine(OUTPUT_PATH, name + ".png");
            if (File.Exists(path)) return; // Не перезаписываем AI-спрайты

            GenerateObjectSprite(name, color, shape);
        }

        private static void GenerateTerrainSprite(string name, Color color)
        {
            // FIX 2A: Текстура 68×68 вместо 64×64 — pixel bleed для устранения белой сетки.
            // 68/32 = 2.125 юнита → 0.0625 юнита перекрытие с каждой стороны ячейки (2.0).
            // Это надёжно закрывает субпиксельные зазоры между тайлами.
            // Редактировано: 2026-04-15 17:31:49 UTC
            Texture2D texture = new Texture2D(TERRAIN_SIZE, TERRAIN_SIZE, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear; // FIX 2A: Bilinear — сглаживает границы между тайлами
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                for (int y = 0; y < TERRAIN_SIZE; y++)
                {
                    // Вариация цвета через Perlin noise
                    float variation = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.1f;
                    Color pixelColor = color * (1f + variation - 0.05f);
                    pixelColor.a = color.a;
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            // Lava: добавить яркие прожилки
            if (name == "terrain_lava")
            {
                for (int x = 0; x < TERRAIN_SIZE; x++)
                {
                    for (int y = 0; y < TERRAIN_SIZE; y++)
                    {
                        float crack = Mathf.PerlinNoise(x * 0.3f + 100f, y * 0.3f + 100f);
                        if (crack > 0.55f)
                        {
                            texture.SetPixel(x, y, new Color(1f, 0.7f, 0.1f));
                        }
                    }
                }
            }

            // Ice: добавить блики
            if (name == "terrain_ice")
            {
                for (int x = 0; x < TERRAIN_SIZE; x++)
                {
                    for (int y = 0; y < TERRAIN_SIZE; y++)
                    {
                        float shine = Mathf.PerlinNoise(x * 0.15f + 50f, y * 0.15f + 50f);
                        if (shine > 0.6f)
                        {
                            texture.SetPixel(x, y, new Color(0.9f, 0.95f, 1f));
                        }
                    }
                }
            }

            texture.Apply();
            SaveTexture(texture, name, isObject: false);
        }

        private static void GenerateObjectSprite(string name, Color color, ObjectShape shape)
        {
            // Текстура 64×64 RGBA32 с прозрачным фоном.
            // PPU=160 → 64/160 = 0.4 юнита (в 5 раз меньше ячейки 2.0).
            Texture2D texture = new Texture2D(OBJECT_SIZE, OBJECT_SIZE, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color transparent = new Color(0, 0, 0, 0);

            // Заполнить прозрачным
            for (int x = 0; x < OBJECT_SIZE; x++)
            {
                for (int y = 0; y < OBJECT_SIZE; y++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            int cx = OBJECT_SIZE / 2;  // 32
            int cy = OBJECT_SIZE / 2;  // 32

            switch (shape)
            {
                case ObjectShape.Tree:
                    // Ствол
                    DrawRect(texture, cx - 3, 8, 6, 24, new Color(0.4f, 0.25f, 0.15f));
                    // Крона (треугольник)
                    for (int y = 24; y < 56; y++)
                    {
                        int width = (56 - y) / 3;
                        DrawRect(texture, cx - width, y, width * 2, 1, color);
                    }
                    break;

                case ObjectShape.SmallRock:
                    DrawEllipse(texture, cx, cy - 2, 8, 6, color);
                    DrawEllipse(texture, cx, cy - 2, 6, 4, color * 1.1f);
                    break;

                case ObjectShape.MediumRock:
                    DrawEllipse(texture, cx, cy - 4, 12, 9, color);
                    DrawEllipse(texture, cx - 4, cy - 2, 5, 4, color * 1.2f);
                    DrawEllipse(texture, cx + 4, cy - 1, 4, 3, color * 0.9f);
                    break;

                case ObjectShape.Bush:
                    DrawEllipse(texture, cx - 5, cy, 7, 8, color);
                    DrawEllipse(texture, cx + 5, cy, 7, 8, color * 0.9f);
                    DrawEllipse(texture, cx, cy + 3, 10, 7, color * 1.1f);
                    break;

                case ObjectShape.Chest:
                    DrawRect(texture, cx - 8, cy - 6, 16, 12, color);
                    DrawRect(texture, cx - 9, cy + 4, 18, 4, color * 1.2f);
                    DrawRect(texture, cx - 2, cy - 3, 4, 6, new Color(0.8f, 0.7f, 0.3f));
                    break;

                case ObjectShape.OreVein:
                    DrawEllipse(texture, cx, cy - 3, 10, 8, new Color(0.45f, 0.4f, 0.35f));
                    DrawEllipse(texture, cx - 3, cy - 2, 4, 3, new Color(0.8f, 0.6f, 0.2f));
                    DrawEllipse(texture, cx + 3, cy - 4, 3, 2, new Color(0.7f, 0.5f, 0.15f));
                    DrawEllipse(texture, cx + 1, cy + 1, 3, 2, new Color(0.85f, 0.65f, 0.25f));
                    break;

                case ObjectShape.Herb:
                    DrawRect(texture, cx - 1, 10, 2, 20, new Color(0.2f, 0.4f, 0.15f));
                    DrawEllipse(texture, cx - 4, cy + 2, 4, 3, color);
                    DrawEllipse(texture, cx + 4, cy + 4, 4, 3, color);
                    DrawEllipse(texture, cx, cy + 7, 5, 4, color * 1.15f);
                    DrawEllipse(texture, cx, cy + 14, 3, 3, new Color(0.9f, 0.9f, 0.3f));
                    break;
            }

            texture.Apply();
            SaveTexture(texture, name, isObject: true);
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            int texSize = texture.width;
            for (int px = x; px < x + width && px < texSize; px++)
            {
                for (int py = y; py < y + height && py < texSize; py++)
                {
                    if (px >= 0 && py >= 0)
                        texture.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawEllipse(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
        {
            int texSize = texture.width;
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f && x >= 0 && y >= 0 && x < texSize && y < texSize)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void SaveTexture(Texture2D texture, string name, bool isObject)
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Path.Combine(OUTPUT_PATH, name + ".png");
            File.WriteAllBytes(path, bytes);

            // Импортировать как спрайт
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = isObject ? OBJECT_PPU : TERRAIN_PPU;
                // FIX 2A: Terrain — Bilinear (устраняет белую сетку), Objects — Point
                importer.filterMode = isObject ? FilterMode.Point : FilterMode.Bilinear;
                importer.spriteBorder = Vector4.zero;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private enum ObjectShape
        {
            Tree,
            SmallRock,
            MediumRock,
            Bush,
            Chest,
            OreVein,
            Herb
        }
    }
}
#endif
