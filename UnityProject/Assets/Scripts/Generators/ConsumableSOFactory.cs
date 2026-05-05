// ============================================================================
// ConsumableSOFactory.cs — Фабрика создания ItemData SO из DTO расходников
// Cultivation World Simulator
// Создано: 2026-05-05 09:55:00 UTC — С-05: аналогично EquipmentSOFactory
// ============================================================================
//
// Мост DTO → ItemData SO:
//   GeneratedConsumable → ItemData (через CreateRuntimeFromConsumable / ApplyConsumableToSO)
//
// Runtime-методы (CreateRuntime*, Apply*) доступны ВЕЗДЕ.
// Editor-методы (CreateFrom* с .asset) — только в Unity Editor.
//
// ConsumableData не выделен в отдельный SO-класс (в отличие от EquipmentData),
// поэтому используем базовый ItemData. Эффекты расходника хранятся в ItemData.effects.
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CultivationGame.Generators
{
    /// <summary>
    /// Фабрика конвертации GeneratedConsumable DTO в ItemData ScriptableObject.
    /// Runtime-методы работают без UnityEditor. Editor-методы создают .asset файлы.
    /// </summary>
    public static class ConsumableSOFactory
    {
        // ================================================================
        //  RUNTIME: CONSUMABLE → ItemData (доступны везде)
        // ================================================================

        /// <summary>
        /// Создать runtime ItemData из DTO расходника (без сохранения на диск).
        /// </summary>
        public static ItemData CreateRuntimeFromConsumable(GeneratedConsumable dto)
        {
            var so = ScriptableObject.CreateInstance<ItemData>();
            ApplyConsumableToSO(so, dto);
            return so;
        }

        /// <summary>
        /// Заполнить поля ItemData из DTO расходника.
        /// </summary>
        public static void ApplyConsumableToSO(ItemData so, GeneratedConsumable dto)
        {
            if (so == null || dto == null) return;

            // Поля ItemData (базовые)
            so.itemId = dto.id;
            so.nameRu = dto.nameRu;
            so.nameEn = dto.nameEn;
            so.description = dto.description;
            so.category = ItemCategory.Consumable;
            so.rarity = dto.rarity;
            so.stackable = dto.stackable;
            so.maxStack = dto.maxStack;
            so.weight = dto.weight;
            so.volume = dto.volume;
            so.value = dto.value;
            so.hasDurability = false; // Расходники не имеют прочности
            so.maxDurability = 0;
            so.allowNesting = dto.allowNesting;
            so.requiredCultivationLevel = dto.requiredCultivationLevel;

            // Конвертация эффектов (ConsumableEffect → ItemEffect)
            so.effects = ConvertEffects(dto.effects, dto.sideEffects, dto.sideEffectChance);

            // Иконка (runtime: программная; editor: поиск + fallback)
            so.icon = ResolveConsumableIcon(dto);
        }

        // ================================================================
        //  EDITOR-ONLY: .asset файлы (AssetDatabase)
        // ================================================================

#if UNITY_EDITOR
        /// <summary>
        /// Создать ItemData SO из DTO расходника (с сохранением .asset).
        /// </summary>
        public static ItemData CreateFromConsumable(GeneratedConsumable dto, string assetPath)
        {
            var so = ScriptableObject.CreateInstance<ItemData>();
            ApplyConsumableToSO(so, dto);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
#endif

        // ================================================================
        //  КОНВЕРТАЦИЯ ЭФФЕКТОВ — runtime
        // ================================================================

        /// <summary>
        /// Конвертация эффектов расходника (основные + побочные) в List<ItemEffect>.
        /// Побочные эффекты помечаются суффиксом "_side" в effectType.
        /// </summary>
        private static List<ItemEffect> ConvertEffects(
            List<ConsumableEffect> mainEffects,
            List<ConsumableEffect> sideEffects,
            float sideEffectChance)
        {
            var result = new List<ItemEffect>();

            // Основные эффекты
            foreach (var e in mainEffects)
            {
                result.Add(new ItemEffect
                {
                    effectType = e.effectType,
                    value = e.isLongValue ? (float)e.valueLong : e.valueFloat,
                    duration = e.duration
                });
            }

            // Побочные эффекты (добавляются с пометкой)
            if (sideEffects != null && sideEffects.Count > 0)
            {
                foreach (var s in sideEffects)
                {
                    result.Add(new ItemEffect
                    {
                        effectType = $"side_{s.effectType}_chance{sideEffectChance:F2}",
                        value = s.isLongValue ? (float)s.valueLong : s.valueFloat,
                        duration = s.duration
                    });
                }
            }

            return result;
        }

        // ================================================================
        //  ИКОНКИ — runtime + editor
        // ================================================================

        /// <summary>
        /// Разрешение иконки расходника: editor → поиск AssetDatabase, runtime → программная
        /// </summary>
        private static Sprite ResolveConsumableIcon(GeneratedConsumable dto)
        {
#if UNITY_EDITOR
            // Попытка найти готовую иконку по имени (только в Editor)
            string iconPath = $"Assets/Sprites/Items/Icons/consumable_{dto.type.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
#endif
            // Fallback: программная иконка (Rarity-цвет рамка)
            return CreateRuntimeConsumableIcon(dto);
        }

        /// <summary>
        /// Runtime-иконка расходника (цвет рамки по Rarity, буква типа).
        /// </summary>
        private static Sprite CreateRuntimeConsumableIcon(GeneratedConsumable dto)
        {
            Color bgColor = GetConsumableBgColor(dto.type);
            Color borderColor = GetRarityBorderColor(dto.rarity);
            char letter = dto.type.ToString()[0];
            return CreateProceduralIcon(letter, bgColor, borderColor);
        }

        /// <summary>
        /// Фон иконки по типу расходника.
        /// </summary>
        private static Color GetConsumableBgColor(ConsumableType type) => type switch
        {
            ConsumableType.Pill     => new Color(0.9f, 0.7f, 0.9f),  // Розовый
            ConsumableType.Elixir   => new Color(0.7f, 0.9f, 0.7f),  // Зелёный
            ConsumableType.Food     => new Color(0.9f, 0.85f, 0.6f),  // Желтоватый
            ConsumableType.Drink    => new Color(0.6f, 0.8f, 0.95f),  // Голубой
            ConsumableType.Poison   => new Color(0.6f, 0.7f, 0.5f),  // Тёмно-зелёный
            ConsumableType.Scroll   => new Color(0.95f, 0.9f, 0.7f),  // Бежевый
            ConsumableType.Talisman => new Color(0.8f, 0.7f, 0.95f),  // Лиловый
            _ => Color.gray
        };

        /// <summary>
        /// Цвет рамки по Rarity.
        /// </summary>
        private static Color GetRarityBorderColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new Color(0.6f, 0.6f, 0.6f),    // Серый
            ItemRarity.Uncommon  => new Color(0.13f, 0.77f, 0.37f), // Зелёный
            ItemRarity.Rare      => new Color(0.23f, 0.51f, 0.97f), // Синий
            ItemRarity.Epic      => new Color(0.65f, 0.35f, 0.98f), // Фиолетовый
            ItemRarity.Legendary => new Color(0.96f, 0.62f, 0.04f), // Золотой
            ItemRarity.Mythic    => new Color(0.96f, 0.20f, 0.20f), // Красный
            _ => Color.gray
        };

        /// <summary>
        /// Программная иконка: цветной фон + Rarity-цвет рамка + буква типа.
        /// </summary>
        private static Sprite CreateProceduralIcon(char letter, Color bgColor, Color borderColor)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            // Прозрачный фон
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Рамка 2px — Rarity-цвет
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                        pixels[y * size + x] = borderColor;

            // Фон — Consumable-цвет
            for (int y = 2; y < size - 2; y++)
                for (int x = 2; x < size - 2; x++)
                    pixels[y * size + x] = bgColor;

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
