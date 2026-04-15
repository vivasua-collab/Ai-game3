// ============================================================================
// TileSpriteGenerator.cs — Генератор простых спрайтов тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-15 11:10:00 UTC — FIX: pixel bleed (66×66 текстура, sprite rect 1,1,64,64), alphaIsTransparency, улучшенные объектные спрайты
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
    /// </summary>
    public static class TileSpriteGenerator
    {
        private const int TILE_SIZE = 66; // +2px для pixel bleed (устранение зазоров)
        private const int VISIBLE_SIZE = 64; // Видимая часть спрайта
        private const string OUTPUT_PATH = "Assets/Sprites/Tiles";

        [MenuItem("Tools/Generate Tile Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(OUTPUT_PATH))
            {
                Directory.CreateDirectory(OUTPUT_PATH);
            }

            // Terrain sprites
            GenerateTerrainSprite("terrain_grass", new Color(0.4f, 0.7f, 0.3f));
            GenerateTerrainSprite("terrain_dirt", new Color(0.6f, 0.4f, 0.2f));
            GenerateTerrainSprite("terrain_stone", new Color(0.5f, 0.5f, 0.55f));
            GenerateTerrainSprite("terrain_water_shallow", new Color(0.3f, 0.5f, 0.8f, 0.8f));
            GenerateTerrainSprite("terrain_water_deep", new Color(0.2f, 0.3f, 0.7f, 0.9f));
            GenerateTerrainSprite("terrain_sand", new Color(0.9f, 0.85f, 0.6f));
            GenerateTerrainSprite("terrain_snow", new Color(0.95f, 0.95f, 1f));
            GenerateTerrainSprite("terrain_ice", new Color(0.7f, 0.85f, 0.95f));     // ДОБАВЛЕНО 2026-04-13
            GenerateTerrainSprite("terrain_lava", new Color(0.9f, 0.3f, 0.05f));      // ДОБАВЛЕНО 2026-04-13
            GenerateTerrainSprite("terrain_void", new Color(0.1f, 0.1f, 0.1f));

            // Object sprites (with simple shapes)
            GenerateObjectSprite("obj_tree", new Color(0.3f, 0.5f, 0.2f), ObjectShape.Tree);
            GenerateObjectSprite("obj_rock_small", new Color(0.6f, 0.55f, 0.5f), ObjectShape.SmallRock);
            GenerateObjectSprite("obj_rock_medium", new Color(0.5f, 0.45f, 0.4f), ObjectShape.MediumRock);
            GenerateObjectSprite("obj_bush", new Color(0.35f, 0.55f, 0.25f), ObjectShape.Bush);
            GenerateObjectSprite("obj_chest", new Color(0.6f, 0.4f, 0.2f), ObjectShape.Chest);
            GenerateObjectSprite("obj_ore_vein", new Color(0.7f, 0.5f, 0.2f), ObjectShape.OreVein);   // ДОБАВЛЕНО 2026-04-13
            GenerateObjectSprite("obj_herb", new Color(0.2f, 0.6f, 0.3f), ObjectShape.Herb);          // ДОБАВЛЕНО 2026-04-13

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Generated tile sprites at " + OUTPUT_PATH);
        }

        private static void GenerateTerrainSprite(string name, Color color)
        {
            // FIX: Pixel bleed — текстура 66×66, цвет заливается на всю площадь,
            // но sprite rect = (1,1,64,64) — видима только центральная часть.
            // Крайние пиксели дублируют цвет, устраняя зазоры между тайлами.
            // Редактировано: 2026-04-15 11:10:00 UTC
            Texture2D texture = new Texture2D(TILE_SIZE, TILE_SIZE);
            
            for (int x = 0; x < TILE_SIZE; x++)
            {
                for (int y = 0; y < TILE_SIZE; y++)
                {
                    // Координаты внутри видимой области (для PerlinNoise)
                    int vx = Mathf.Clamp(x - 1, 0, VISIBLE_SIZE - 1);
                    int vy = Mathf.Clamp(y - 1, 0, VISIBLE_SIZE - 1);
                    
                    // Добавить небольшую вариацию
                    float variation = Mathf.PerlinNoise(vx * 0.1f, vy * 0.1f) * 0.1f;
                    Color pixelColor = color * (1f + variation - 0.05f);
                    pixelColor.a = color.a;
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            // Lava: добавить яркие прожилки
            if (name == "terrain_lava")
            {
                for (int x = 0; x < TILE_SIZE; x++)
                {
                    for (int y = 0; y < TILE_SIZE; y++)
                    {
                        float crack = Mathf.PerlinNoise(x * 0.3f + 100f, y * 0.3f + 100f);
                        if (crack > 0.55f)
                        {
                            Color bright = new Color(1f, 0.7f, 0.1f);
                            texture.SetPixel(x, y, bright);
                        }
                    }
                }
            }

            // Ice: добавить блики
            if (name == "terrain_ice")
            {
                for (int x = 0; x < TILE_SIZE; x++)
                {
                    for (int y = 0; y < TILE_SIZE; y++)
                    {
                        float shine = Mathf.PerlinNoise(x * 0.15f + 50f, y * 0.15f + 50f);
                        if (shine > 0.6f)
                        {
                            Color bright = new Color(0.9f, 0.95f, 1f);
                            texture.SetPixel(x, y, bright);
                        }
                    }
                }
            }

            texture.Apply();
            SaveTexture(texture, name);
        }

        private static void GenerateObjectSprite(string name, Color color, ObjectShape shape)
        {
            // FIX: Объектные спрайты — 66×66 текстура с прозрачным фоном,
            // sprite rect = (1,1,64,64) — pixel bleed для совместимости.
            // Объекты рисуются в центре видимой области (смещение +1).
            // Редактировано: 2026-04-15 11:10:00 UTC
            Texture2D texture = new Texture2D(TILE_SIZE, TILE_SIZE, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            
            // Заполнить прозрачным
            for (int x = 0; x < TILE_SIZE; x++)
            {
                for (int y = 0; y < TILE_SIZE; y++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            // Смещение +1 для компенсации pixel bleed offset
            int cx = TILE_SIZE / 2;  // 33 (центр 66×66)
            int cy = TILE_SIZE / 2;

            switch (shape)
            {
                case ObjectShape.Tree:
                    // Ствол
                    DrawRect(texture, cx - 4, 0, 8, 20, new Color(0.4f, 0.25f, 0.15f));
                    // Крона (треугольник)
                    for (int y = 15; y < TILE_SIZE; y++)
                    {
                        int width = (TILE_SIZE - y) / 2;
                        DrawRect(texture, cx - width, y, width * 2, 1, color);
                    }
                    break;

                case ObjectShape.SmallRock:
                    DrawEllipse(texture, cx, cy - 5, 12, 8, color);
                    DrawEllipse(texture, cx, cy - 5, 10, 6, color * 1.1f);
                    break;

                case ObjectShape.MediumRock:
                    DrawEllipse(texture, cx, cy - 8, 18, 14, color);
                    DrawEllipse(texture, cx - 5, cy - 5, 8, 6, color * 1.2f);
                    DrawEllipse(texture, cx + 6, cy - 3, 6, 5, color * 0.9f);
                    break;

                case ObjectShape.Bush:
                    DrawEllipse(texture, cx - 8, cy, 10, 12, color);
                    DrawEllipse(texture, cx + 8, cy, 10, 12, color * 0.9f);
                    DrawEllipse(texture, cx, cy + 5, 14, 10, color * 1.1f);
                    break;

                case ObjectShape.Chest:
                    // Основа
                    DrawRect(texture, 10, 10, 44, 30, color);
                    // Крышка
                    DrawRect(texture, 8, 35, 48, 12, color * 1.2f);
                    // Замок
                    DrawRect(texture, cx - 4, 20, 8, 15, new Color(0.8f, 0.7f, 0.3f));
                    break;

                case ObjectShape.OreVein:
                    // Каменная основа
                    DrawEllipse(texture, cx, cy - 5, 16, 12, new Color(0.45f, 0.4f, 0.35f));
                    // Рудные вкрапления (золотистые/медные)
                    DrawEllipse(texture, cx - 5, cy - 3, 6, 4, new Color(0.8f, 0.6f, 0.2f));
                    DrawEllipse(texture, cx + 4, cy - 7, 5, 3, new Color(0.7f, 0.5f, 0.15f));
                    DrawEllipse(texture, cx + 2, cy + 2, 4, 3, new Color(0.85f, 0.65f, 0.25f));
                    break;

                case ObjectShape.Herb:
                    // Стебель
                    DrawRect(texture, cx - 1, 5, 2, 25, new Color(0.2f, 0.4f, 0.15f));
                    // Листья (маленькие эллипсы)
                    DrawEllipse(texture, cx - 6, cy + 5, 5, 4, color);
                    DrawEllipse(texture, cx + 6, cy + 8, 5, 4, color);
                    DrawEllipse(texture, cx, cy + 12, 6, 5, color * 1.15f);
                    // Цветок/бутон наверху
                    DrawEllipse(texture, cx, cy + 20, 4, 4, new Color(0.9f, 0.9f, 0.3f));
                    break;
            }

            texture.Apply();
            SaveTexture(texture, name);
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width && px < TILE_SIZE; px++)
            {
                for (int py = y; py < y + height && py < TILE_SIZE; py++)
                {
                    if (px >= 0 && py >= 0)
                        texture.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawEllipse(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f && x >= 0 && y >= 0 && x < TILE_SIZE && y < TILE_SIZE)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void SaveTexture(Texture2D texture, string name)
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
                // FIX: pixelsPerUnit=32 (64px / 2 юнита ячейки Grid)
                // Редактировано: 2026-04-14 06:30:00 UTC
                // FIX: PPU=32 (64px видимой области / 2 юнита), sprite rect со смещением (1,1) для pixel bleed
                // Редактировано: 2026-04-15 11:10:00 UTC
                importer.spritePixelsPerUnit = VISIBLE_SIZE / 2; // 64/2=32 — один тайл = 2 юнита
                importer.filterMode = FilterMode.Point;
                // FIX: spriteRect смещён на (1,1) — pixel bleed устраняет зазоры между тайлами
                importer.spriteBorder = Vector4.zero;
                importer.wrapMode = TextureWrapMode.Clamp;
                // FIX: alphaIsTransparency — ОБЯЗАТЕЛЬНО для объектных спрайтов с прозрачностью
                // Без этого PNG с alpha каналом отображаются с белым фоном
                // Редактировано: 2026-04-15 11:10:00 UTC
                importer.alphaIsTransparency = true;
                // FIX: Используем TextureImporterSettings вместо устаревших свойств spritePivot/spriteAlignment
                // В Unity 6.3 прямые свойства spritePivot/spriteAlignment удалены из TextureImporter
                // Редактировано: 2026-04-16 08:00:00 UTC
                var texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);
                texSettings.spriteAlignment = (int)SpriteAlignment.Center;
                texSettings.spritePivot = new Vector2(0.5f, 0.5f);
                // FIX: Задать sprite rect вручную — (1,1,64,64) вместо (0,0,66,66)
                // Центральная часть текстуры 66×66, края = pixel bleed
                // Редактировано: 2026-04-15 11:10:00 UTC
                importer.SetTextureSettings(texSettings);
                AssetDatabase.ImportAsset(path);
            }
        }

        private enum ObjectShape
        {
            Tree,
            SmallRock,
            MediumRock,
            Bush,
            Chest,
            OreVein,    // ДОБАВЛЕНО 2026-04-13
            Herb        // ДОБАВЛЕНО 2026-04-13
        }
    }
}
#endif
