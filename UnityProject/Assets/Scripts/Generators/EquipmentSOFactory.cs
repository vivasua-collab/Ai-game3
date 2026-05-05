// ============================================================================
// EquipmentSOFactory.cs — Фабрика создания EquipmentData SO из DTO генераторов
// Cultivation World Simulator
// Создано: 2026-04-29 08:55:00 UTC
// Редактировано: 2026-04-29 09:30:00 UTC — рефакторинг: вынос runtime-методов из #if UNITY_EDITOR
// Редактировано: 2026-04-29 09:55:00 UTC — FIX: перенос из Editor/ → Generators/ (namespace CultivationGame.Editor → .Generators)
//   Причина: Editor/ папка компилируется в Assembly-CSharp-Editor, недоступный в runtime.
// Редактировано: 2026-04-29 12:03:16 UTC — исправление некорректной даты (05-01 → 04-29)
// Редактировано: 2026-05-07 10:30:00 UTC — ФАЗА 2: маппинг qiCostReduction/chargeSpeedBonus
// Редактировано: 2026-05-05 08:20:00 UTC — БАГ-3: +маппинг techniqueDamageBonus
// ============================================================================
//
// Мост DTO → EquipmentData SO:
//   GeneratedWeapon  → EquipmentData (через CreateFromWeapon / CreateRuntimeFromWeapon)
//   GeneratedArmor   → EquipmentData (через CreateFromArmor  / CreateRuntimeFromArmor)
//
// Runtime-методы (CreateRuntime*, Apply*, CreateProceduralIcon) доступны ВЕЗДЕ.
// Editor-методы (CreateFrom* с .asset) — только в Unity Editor.
//
// Иконки:
//   Editor: поиск AssetDatabase.LoadAssetAtPath → fallback CreateProceduralIcon
//   Runtime: всегда CreateProceduralIcon (GradeColors)
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
    /// Фабрика конвертации DTO генераторов в EquipmentData ScriptableObject.
    /// Runtime-методы работают без UnityEditor. Editor-методы создают .asset файлы.
    /// </summary>
    public static class EquipmentSOFactory
    {
        // ================================================================
        //  RUNTIME: WEAPON → EquipmentData (доступны везде)
        // ================================================================

        /// Создать runtime EquipmentData из DTO оружия (без сохранения на диск)
        public static EquipmentData CreateRuntimeFromWeapon(GeneratedWeapon dto)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyWeaponToSO(so, dto);
            return so;
        }

        /// Заполнить поля EquipmentData из DTO оружия
        public static void ApplyWeaponToSO(EquipmentData so, GeneratedWeapon dto)
        {
            // Поля ItemData
            so.itemId = dto.id;
            so.nameRu = dto.nameRu;
            so.nameEn = dto.nameEn;
            so.description = dto.description;
            so.category = ItemCategory.Weapon;
            so.rarity = MapRarityFromGrade(dto.grade);
            so.stackable = false;
            so.maxStack = 1;
            so.weight = dto.weight;
            so.volume = dto.volume;
            so.value = CalculateWeaponValue(dto);
            so.hasDurability = true;
            so.maxDurability = dto.maxDurability;
            so.allowNesting = NestingFlag.Any;
            so.requiredCultivationLevel = dto.requiredCultivationLevel;

            // Поля EquipmentData
            so.slot = MapWeaponSlot(dto.subtype);
            so.handType = MapWeaponHandType(dto.subtype);
            so.damage = dto.baseDamage;
            so.defense = 0;
            so.grade = dto.grade;
            so.itemLevel = dto.itemLevel;
            so.materialId = dto.materialId;
            so.materialTier = dto.materialTier;

            // Штрафы (оружие не имеет штрафов брони)
            so.moveSpeedPenalty = 0f;
            so.qiFlowPenalty = 0f;

            // ФАЗА 2: Бонусы техник от оружия — маппинг из GeneratedWeapon DTO
            so.qiCostReduction = dto.qiCostReduction;
            so.techniqueDamageBonus = dto.techniqueDamageBonus;  // БАГ-3: маппинг бонуса урона техник
            so.chargeSpeedBonus = dto.qiConductivity > 0 ? dto.qiConductivity * 0.1f : 0f; // Проводимость → ускорение накачки

            // Бонусы
            so.statBonuses = ConvertBonuses(dto.bonuses);
            so.specialEffects = ConvertSpecialEffects(dto.specialEffects);

            // Иконка (runtime: программная; editor: поиск + fallback)
            so.icon = ResolveWeaponIcon(dto);

            // Визуал
            so.equippedSprite = null; // Пока нет equipped-спрайтов
        }

        // ================================================================
        //  RUNTIME: ARMOR → EquipmentData (доступны везде)
        // ================================================================

        /// Создать runtime EquipmentData из DTO брони (без сохранения на диск)
        public static EquipmentData CreateRuntimeFromArmor(GeneratedArmor dto)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyArmorToSO(so, dto);
            return so;
        }

        /// Заполнить поля EquipmentData из DTO брони
        public static void ApplyArmorToSO(EquipmentData so, GeneratedArmor dto)
        {
            // Поля ItemData
            so.itemId = dto.id;
            so.nameRu = dto.nameRu;
            so.nameEn = dto.nameEn;
            so.description = dto.description;
            so.category = ItemCategory.Armor;
            so.rarity = MapRarityFromGrade(dto.grade);
            so.stackable = false;
            so.maxStack = 1;
            so.weight = dto.weight;
            so.volume = dto.volume;
            so.value = CalculateArmorValue(dto);
            so.hasDurability = true;
            so.maxDurability = dto.maxDurability;
            so.allowNesting = NestingFlag.Any;
            so.requiredCultivationLevel = dto.requiredCultivationLevel;

            // Поля EquipmentData
            so.slot = MapArmorSlot(dto.subtype);
            so.handType = WeaponHandType.OneHand; // Броня никогда не двуручная
            so.damage = 0;
            so.defense = dto.armor;
            so.coverage = dto.coverage;
            so.damageReduction = dto.damageReduction;
            so.dodgeBonus = dto.dodgePenalty; // Отрицательное значение = штраф
            so.moveSpeedPenalty = dto.moveSpeedPenalty;
            so.qiFlowPenalty = dto.qiFlowPenalty;
            so.grade = dto.grade;
            so.itemLevel = dto.itemLevel;
            so.materialId = dto.materialId;
            so.materialTier = dto.materialTier;

            // Бонусы
            so.statBonuses = ConvertArmorBonuses(dto.bonuses);

            // Иконка
            so.icon = ResolveArmorIcon(dto);

            // Визуал
            so.equippedSprite = null;
        }

        // ================================================================
        //  EDITOR-ONLY: .asset файлы (AssetDatabase)
        // ================================================================

#if UNITY_EDITOR
        /// Создать EquipmentData SO из DTO оружия (с сохранением .asset)
        public static EquipmentData CreateFromWeapon(GeneratedWeapon dto, string assetPath)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyWeaponToSO(so, dto);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        /// Создать EquipmentData SO из DTO брони (с сохранением .asset)
        public static EquipmentData CreateFromArmor(GeneratedArmor dto, string assetPath)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyArmorToSO(so, dto);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
#endif

        // ================================================================
        //  МАППИНГ СЛОТОВ (Д2, Д8) — runtime
        // ================================================================

        /// Маппинг WeaponSubtype → EquipmentSlot (все → WeaponMain)
        private static EquipmentSlot MapWeaponSlot(WeaponSubtype subtype) => subtype switch
        {
            WeaponSubtype.Unarmed     => EquipmentSlot.WeaponMain,
            WeaponSubtype.Dagger      => EquipmentSlot.WeaponMain,
            WeaponSubtype.Sword       => EquipmentSlot.WeaponMain,
            WeaponSubtype.Greatsword  => EquipmentSlot.WeaponMain,
            WeaponSubtype.Axe         => EquipmentSlot.WeaponMain,
            WeaponSubtype.Spear       => EquipmentSlot.WeaponMain,
            WeaponSubtype.Bow         => EquipmentSlot.WeaponMain,
            WeaponSubtype.Staff       => EquipmentSlot.WeaponMain,
            WeaponSubtype.Hammer      => EquipmentSlot.WeaponMain,
            WeaponSubtype.Mace        => EquipmentSlot.WeaponMain,
            WeaponSubtype.Crossbow    => EquipmentSlot.WeaponMain,
            WeaponSubtype.Wand        => EquipmentSlot.WeaponMain,
            _ => EquipmentSlot.WeaponMain
        };

        /// Маппинг WeaponSubtype → WeaponHandType (5 двуручных)
        private static WeaponHandType MapWeaponHandType(WeaponSubtype subtype) => subtype switch
        {
            WeaponSubtype.Greatsword  => WeaponHandType.TwoHand,
            WeaponSubtype.Spear       => WeaponHandType.TwoHand,
            WeaponSubtype.Bow         => WeaponHandType.TwoHand,
            WeaponSubtype.Staff       => WeaponHandType.TwoHand,
            WeaponSubtype.Crossbow    => WeaponHandType.TwoHand,
            _ => WeaponHandType.OneHand
        };

        /// Маппинг ArmorSubtype → EquipmentSlot (Arms→Hands, Д8)
        private static EquipmentSlot MapArmorSlot(ArmorSubtype subtype) => subtype switch
        {
            ArmorSubtype.Head  => EquipmentSlot.Head,
            ArmorSubtype.Torso => EquipmentSlot.Torso,
            ArmorSubtype.Arms  => EquipmentSlot.Hands, // Наручи → Hands (Д8)
            ArmorSubtype.Hands => EquipmentSlot.Hands,
            ArmorSubtype.Legs  => EquipmentSlot.Legs,
            ArmorSubtype.Feet  => EquipmentSlot.Feet,
            ArmorSubtype.Full  => EquipmentSlot.Torso, // Полная → Torso (основной слот)
            _ => EquipmentSlot.Torso
        };

        // ================================================================
        //  ИКОНКИ (Д9, Д10, Д11) — runtime + editor
        // ================================================================

        /// Разрешение иконки оружия: editor → поиск AssetDatabase, runtime → программная
        private static Sprite ResolveWeaponIcon(GeneratedWeapon dto)
        {
#if UNITY_EDITOR
            // Попытка найти готовую иконку по имени (только в Editor)
            string iconPath = $"Assets/Sprites/Equipment/Icons/weapon_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
#endif
            // Fallback: программная иконка (GradeColors + Tier-индикатор)
            return CreateRuntimeWeaponIcon(dto);
        }

        /// Разрешение иконки брони: editor → поиск AssetDatabase, runtime → программная
        private static Sprite ResolveArmorIcon(GeneratedArmor dto)
        {
#if UNITY_EDITOR
            string iconPath = $"Assets/Sprites/Equipment/Icons/armor_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
#endif
            return CreateRuntimeArmorIcon(dto);
        }

        /// Runtime-иконка оружия (GradeColors, без AssetDatabase)
        private static Sprite CreateRuntimeWeaponIcon(GeneratedWeapon dto)
        {
            Color bgColor = GradeColors.GetIconBgColor(dto.grade, dto.materialTier);
            Color borderColor = GetRarityBorderColor(MapRarityFromGrade(dto.grade));
            Color tierColor = GradeColors.GetTierColor(dto.materialTier);
            return CreateProceduralIcon(dto.subtype.ToString()[0], bgColor, borderColor, tierColor);
        }

        /// Runtime-иконка брони (GradeColors, без AssetDatabase)
        private static Sprite CreateRuntimeArmorIcon(GeneratedArmor dto)
        {
            Color bgColor = GradeColors.GetIconBgColor(dto.grade, dto.materialTier);
            Color borderColor = GetRarityBorderColor(MapRarityFromGrade(dto.grade));
            Color tierColor = GradeColors.GetTierColor(dto.materialTier);
            return CreateProceduralIcon(dto.subtype.ToString()[0], bgColor, borderColor, tierColor);
        }

        /// Программная иконка: Grade-цвет фон + Rarity-цвет рамка + Tier-индикатор 4×4
        private static Sprite CreateProceduralIcon(char letter, Color bgColor, Color borderColor, Color tierColor)
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

            // Фон — Grade-цвет (затемнённый по Tier)
            for (int y = 2; y < size - 2; y++)
                for (int x = 2; x < size - 2; x++)
                    pixels[y * size + x] = bgColor;

            // Tier-индикатор 4×4 в правом нижнем углу
            for (int y = 2; y < 6; y++)
                for (int x = size - 6; x < size - 2; x++)
                    pixels[y * size + x] = tierColor;

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// Цвет рамки по Rarity
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

        // ================================================================
        //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ — runtime
        // ================================================================

        /// Маппинг EquipmentGrade → ItemRarity (для совместимости с существующей системой)
        private static ItemRarity MapRarityFromGrade(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => ItemRarity.Common,
            EquipmentGrade.Common       => ItemRarity.Common,
            EquipmentGrade.Refined      => ItemRarity.Uncommon,
            EquipmentGrade.Perfect      => ItemRarity.Rare,
            EquipmentGrade.Transcendent => ItemRarity.Epic,
            _ => ItemRarity.Common
        };

        /// Расчёт стоимости оружия
        private static int CalculateWeaponValue(GeneratedWeapon dto)
            => Mathf.RoundToInt(dto.baseDamage * 10 * (1 + dto.materialTier * 0.5f));

        /// Расчёт стоимости брони
        private static int CalculateArmorValue(GeneratedArmor dto)
            => Mathf.RoundToInt(dto.armor * 8 * (1 + dto.materialTier * 0.5f));

        /// Конвертация бонусов оружия (Generators.StatBonus → Data.StatBonus)
        private static List<CultivationGame.Data.ScriptableObjects.StatBonus> ConvertBonuses(
            List<CultivationGame.Generators.StatBonus> dtoBonuses)
        {
            var result = new List<CultivationGame.Data.ScriptableObjects.StatBonus>();
            foreach (var b in dtoBonuses)
                result.Add(new CultivationGame.Data.ScriptableObjects.StatBonus
                {
                    statName = b.statName,
                    bonus = b.value,
                    isPercentage = b.isPercentage
                });
            return result;
        }

        /// Конвертация бонусов брони (ArmorBonus → Data.StatBonus)
        private static List<CultivationGame.Data.ScriptableObjects.StatBonus> ConvertArmorBonuses(
            List<ArmorBonus> dtoBonuses)
        {
            var result = new List<CultivationGame.Data.ScriptableObjects.StatBonus>();
            foreach (var b in dtoBonuses)
                result.Add(new CultivationGame.Data.ScriptableObjects.StatBonus
                {
                    statName = b.statName,
                    bonus = b.value,
                    isPercentage = b.isPercentage
                });
            return result;
        }

        /// Конвертация особых эффектов (List<string> → List<SpecialEffect>)
        private static List<SpecialEffect> ConvertSpecialEffects(List<string> effects)
        {
            var result = new List<SpecialEffect>();
            foreach (var e in effects)
                result.Add(new SpecialEffect { effectName = e, description = e, triggerChance = 0.2f });
            return result;
        }
    }
}
