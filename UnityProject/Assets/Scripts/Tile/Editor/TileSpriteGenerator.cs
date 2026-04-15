// ============================================================================
// TileSpriteGenerator.cs — Генератор простых спрайтов тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-16 — FIX: terrain 68×68 pixel bleed для устранения белой сетки,
//   objects PPU=160 (уменьшение в 5 раз), RGBA32 прозрачность, SpriteImportMode.Single
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace CultivationGame.TileSystem.Editor
{
    /// <summary>
    /// Генератор простых спрайтов для тайловой системы.
    /// Создаёт базовые цветные тайлы для тестирования.
    ///
    /// Terrain: 68×68 px, PPU=32 → 2.125 юнита (перекрытие 0.0625 юнита с каждой стороны,
    ///   устраняет белую сетку между тайлами). SpriteImportMode.Single.
    ///
    /// Objects: 64×64 px, PPU=160 → 0.4 юнита (в 5 раз меньше ячейки 2.0 юнита).
    ///   Прозрачный фон RGBA32. SpriteImportMode.Single.
    /// </summary>
    public static class TileSpriteGenerator
    {
        private const int TERRAIN_SIZE = 68;  // 64 + 4px pixel bleed (2px с каждой стороны)
        private const int OBJECT_SIZE = 64;    // Стандартный размер объекта
        private const int TERRAIN_PPU = 32;    // 68/32 = 2.125 юнита — перекрытие устраняет зазоры
        private const int OBJECT_PPU = 160;    // 64/160 = 0.4 юнита — в 5 раз меньше ячейки
        private const string OUTPUT_PATH = "Assets/Sprites/Tiles";

        [MenuItem("Tools/Generate Tile Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(OUTPUT_PATH))
            {
                Directory.CreateDirectory(OUTPUT_PATH);
            }

            // Terrain sprites (68×68, PPU=32 → 2.125 units, pixel bleed)
            GenerateTerrainSprite("terrain_grass", new Color(0.4f, 0.7f, 0.3f));
            GenerateTerrainSprite("terrain_dirt", new Color(0.6f, 0.4f, 0.2f));
            GenerateTerrainSprite("terrain_stone", new Color(0.5f, 0.5f, 0.55f));
            GenerateTerrainSprite("terrain_water_shallow", new Color(0.3f, 0.5f, 0.8f, 0.8f));
            GenerateTerrainSprite("terrain_water_deep", new Color(0.2f, 0.3f, 0.7f, 0.9f));
            GenerateTerrainSprite("terrain_sand", new Color(0.9f, 0.85f, 0.6f));
            GenerateTerrainSprite("terrain_snow", new Color(0.95f, 0.95f, 1f));
            GenerateTerrainSprite("terrain_ice", new Color(0.7f, 0.85f, 0.95f));
            GenerateTerrainSprite("terrain_lava", new Color(0.9f, 0.3f, 0.05f));
            GenerateTerrainSprite("terrain_void", new Color(0.1f, 0.1f, 0.1f));

            // Object sprites (64×64, PPU=160 → 0.4 units, 5x smaller than cell)
            GenerateObjectSprite("obj_tree", new Color(0.3f, 0.5f, 0.2f), ObjectShape.Tree);
            GenerateObjectSprite("obj_rock_small", new Color(0.6f, 0.55f, 0.5f), ObjectShape.SmallRock);
            GenerateObjectSprite("obj_rock_medium", new Color(0.5f, 0.45f, 0.4f), ObjectShape.MediumRock);
            GenerateObjectSprite("obj_bush", new Color(0.35f, 0.55f, 0.25f), ObjectShape.Bush);
            GenerateObjectSprite("obj_chest", new Color(0.6f, 0.4f, 0.2f), ObjectShape.Chest);
            GenerateObjectSprite("obj_ore_vein", new Color(0.7f, 0.5f, 0.2f), ObjectShape.OreVein);
            GenerateObjectSprite("obj_herb", new Color(0.2f, 0.6f, 0.3f), ObjectShape.Herb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Generated tile sprites at " + OUTPUT_PATH);
        }

        private static void GenerateTerrainSprite(string name, Color color)
        {
            // Текстура 68×68 RGBA32. Pixel bleed: вся площадь залита цветом,
            // крайние 2px — дублирование цвета соседних пикселей.
            // PPU=32 → 68/32 = 2.125 юнита. Ячейка = 2.0 юнита.
            // Перекрытие 0.0625 юнита с каждой стороны устраняет белую сетку.
            Texture2D texture = new Texture2D(TERRAIN_SIZE, TERRAIN_SIZE, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                for (int y = 0; y < TERRAIN_SIZE; y++)
                {
                    // Координаты внутри видимой области (64×64 центр)
                    int vx = Mathf.Clamp(x - 2, 0, 63);
                    int vy = Mathf.Clamp(y - 2, 0, 63);

                    // Вариация цвета через Perlin noise
                    float variation = Mathf.PerlinNoise(vx * 0.1f, vy * 0.1f) * 0.1f;
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
            // Объект рисуется в центре 64×64, фон — полностью прозрачный.
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
                importer.filterMode = FilterMode.Point;
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
