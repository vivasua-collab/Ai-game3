# 📦 Кодовая база: Внедрение генераторов экипировки

**Дата:** 2026-04-29 07:53 UTC  
**Редактировано:** 2026-04-29 08:10 UTC  
**Связанный чекпоинт:** [04_29_equipment_generator_integration_plan.md](./04_29_equipment_generator_integration_plan.md)  
**Статус:** 📋 Код к реализации  

> Этот файл содержит весь код, планируемый к созданию/изменению.
> Решения и обоснования — в основном чекпоинте.

---

## 📁 Новые файлы

### 1. GradeColors.cs — Цветовая палитра Grade + Tier

**Путь:** `UnityProject/Assets/Scripts/Core/GradeColors.cs`  
**Решение:** Д12  
**Этап:** Подготовка

```csharp
// ============================================================================
// GradeColors.cs — Цветовая палитра грейдов и тиров (единая точка доступа)
// Cultivation World Simulator
// Создано: 2026-04-29 07:53 UTC
// ============================================================================
//
// ТРИ ОСИ ЦВЕТА:
//   1. Grade  → фон иконки, текст грейда   (Д9)
//   2. Tier   → индикатор материала, яркость (Д10)
//   3. Rarity → рамка слота (существующая система InventorySlotUI.GetRarityColor)
//
// Grade ≠ Rarity. Grade = качество предмета, Rarity = редкость.
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Core
{
    /// <summary>
    /// Цветовая палитра грейдов и тиров.
    /// Единая точка доступа для всех UI-компонентов.
    /// </summary>
    public static class GradeColors
    {
        // === GRADE COLORS (Д9) ===
        
        /// Цвет фона иконки по грейду
        public static Color GetGradeColor(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => new Color(0.545f, 0.271f, 0.075f), // Коричнево-красный
            EquipmentGrade.Common       => new Color(0.612f, 0.639f, 0.686f), // Серый
            EquipmentGrade.Refined      => new Color(0.133f, 0.773f, 0.369f), // Зелёный
            EquipmentGrade.Perfect      => new Color(0.231f, 0.510f, 0.965f), // Синий
            EquipmentGrade.Transcendent => new Color(0.961f, 0.620f, 0.043f), // Золотой
            _ => Color.gray
        };
        
        /// Hex-строка для грейда (для отладки и логов)
        public static string GetGradeHex(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => "#8B4513",
            EquipmentGrade.Common       => "#9CA3AF",
            EquipmentGrade.Refined      => "#22C55E",
            EquipmentGrade.Perfect      => "#3B82F6",
            EquipmentGrade.Transcendent => "#F59E0B",
            _ => "#808080"
        };
        
        /// Название грейда на русском
        public static string GetGradeNameRu(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => "Повреждённый",
            EquipmentGrade.Common       => "Обычный",
            EquipmentGrade.Refined      => "Улучшенный",
            EquipmentGrade.Perfect      => "Совершенный",
            EquipmentGrade.Transcendent => "Превосходящий",
            _ => "???"
        };
        
        // === TIER COLORS (Д10) ===
        
        /// Цвет индикатора тира материала
        public static Color GetTierColor(int tier) => tier switch
        {
            1 => new Color(0.545f, 0.616f, 0.686f), // Стальной серый (T1)
            2 => new Color(0.290f, 0.871f, 0.502f), // Светло-зелёный (T2)
            3 => new Color(0.376f, 0.647f, 0.980f), // Голубой (T3)
            4 => new Color(0.655f, 0.545f, 0.980f), // Фиолетовый (T4)
            5 => new Color(0.957f, 0.447f, 0.714f), // Розовый (T5)
            _ => Color.gray
        };
        
        /// Hex-строка для тира
        public static string GetTierHex(int tier) => tier switch
        {
            1 => "#8B9DAF",
            2 => "#4ADE80",
            3 => "#60A5FA",
            4 => "#A78BFA",
            5 => "#F472B6",
            _ => "#808080"
        };
        
        /// Название тира на русском
        public static string GetTierNameRu(int tier) => tier switch
        {
            1 => "Обычный",
            2 => "Качественный",
            3 => "Духовный",
            4 => "Небесный",
            5 => "Первородный",
            _ => "???"
        };
        
        // === COMBINED FORMULA (Д11) ===
        
        /// Цвет фона иконки = Grade × яркость по Tier
        public static Color GetIconBgColor(EquipmentGrade grade, int tier)
        {
            Color gradeColor = GetGradeColor(grade);
            float brightness = 0.7f + 0.3f * (Mathf.Clamp(tier, 1, 5) / 5f);
            return gradeColor * brightness;
        }
    }
}
```

---

### 2. EquipmentSOFactory.cs — Фабрика DTO → EquipmentData SO

**Путь:** `UnityProject/Assets/Scripts/Editor/EquipmentSOFactory.cs`  
**Решение:** Д1, Д6, Д7, Д8, Д9, Д11  
**Этап:** 1

```csharp
// ============================================================================
// EquipmentSOFactory.cs — Фабрика создания EquipmentData SO из DTO генераторов
// Cultivation World Simulator
// Создано: 2026-04-29
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Generators;

namespace CultivationGame.Editor
{
    public static class EquipmentSOFactory
    {
        // === Weapon → EquipmentData ===
        public static EquipmentData CreateFromWeapon(GeneratedWeapon dto, string assetPath)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyWeaponToSO(so, dto);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
        
        public static void ApplyWeaponToSO(EquipmentData so, GeneratedWeapon dto)
        {
            // ItemData fields
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
            
            // EquipmentData fields
            so.slot = MapWeaponSlot(dto.subtype);
            so.handType = MapWeaponHandType(dto.subtype);
            so.damage = dto.baseDamage;
            so.defense = 0;
            so.grade = dto.grade;
            so.itemLevel = dto.itemLevel;
            so.materialId = dto.materialId;
            so.materialTier = dto.materialTier;
            
            // Bonuses
            so.statBonuses = ConvertBonuses(dto.bonuses);
            so.specialEffects = ConvertSpecialEffects(dto.specialEffects);
            
            // Icon (программная генерация с GradeColors)
            so.icon = GenerateWeaponIcon(dto);
            
            // Visuals
            so.equippedSprite = null; // Пока нет equipped-спрайтов
        }
        
        // === Armor → EquipmentData ===
        public static EquipmentData CreateFromArmor(GeneratedArmor dto, string assetPath)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyArmorToSO(so, dto);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
        
        public static void ApplyArmorToSO(EquipmentData so, GeneratedArmor dto)
        {
            // ItemData fields
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
            
            // EquipmentData fields
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
            
            // Bonuses
            so.statBonuses = ConvertArmorBonuses(dto.bonuses);
            
            // Icon
            so.icon = GenerateArmorIcon(dto);
            
            // Visuals
            so.equippedSprite = null;
        }
        
        // === Runtime SO creation (без AssetDatabase) ===
        public static EquipmentData CreateRuntimeFromWeapon(GeneratedWeapon dto)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyWeaponToSO(so, dto);
            return so;
        }
        
        public static EquipmentData CreateRuntimeFromArmor(GeneratedArmor dto)
        {
            var so = ScriptableObject.CreateInstance<EquipmentData>();
            ApplyArmorToSO(so, dto);
            return so;
        }
        
        // === Slot mapping (Д2, Д8) ===
        private static EquipmentSlot MapWeaponSlot(WeaponSubtype subtype) => subtype switch
        {
            WeaponSubtype.Unarmed => EquipmentSlot.WeaponMain,
            WeaponSubtype.Dagger => EquipmentSlot.WeaponMain,
            WeaponSubtype.Sword => EquipmentSlot.WeaponMain,
            WeaponSubtype.Greatsword => EquipmentSlot.WeaponMain,
            WeaponSubtype.Axe => EquipmentSlot.WeaponMain,
            WeaponSubtype.Spear => EquipmentSlot.WeaponMain,
            WeaponSubtype.Bow => EquipmentSlot.WeaponMain,
            WeaponSubtype.Staff => EquipmentSlot.WeaponMain,
            WeaponSubtype.Hammer => EquipmentSlot.WeaponMain,
            WeaponSubtype.Mace => EquipmentSlot.WeaponMain,
            WeaponSubtype.Crossbow => EquipmentSlot.WeaponMain,
            WeaponSubtype.Wand => EquipmentSlot.WeaponMain,
            _ => EquipmentSlot.WeaponMain
        };
        
        private static WeaponHandType MapWeaponHandType(WeaponSubtype subtype) => subtype switch
        {
            WeaponSubtype.Greatsword => WeaponHandType.TwoHand,
            WeaponSubtype.Spear => WeaponHandType.TwoHand,
            WeaponSubtype.Bow => WeaponHandType.TwoHand,
            WeaponSubtype.Staff => WeaponHandType.TwoHand,
            WeaponSubtype.Crossbow => WeaponHandType.TwoHand,
            _ => WeaponHandType.OneHand
        };
        
        private static EquipmentSlot MapArmorSlot(ArmorSubtype subtype) => subtype switch
        {
            ArmorSubtype.Head => EquipmentSlot.Head,
            ArmorSubtype.Torso => EquipmentSlot.Torso,
            ArmorSubtype.Arms => EquipmentSlot.Hands, // Arms→Hands (Д8)
            ArmorSubtype.Hands => EquipmentSlot.Hands,
            ArmorSubtype.Legs => EquipmentSlot.Legs,
            ArmorSubtype.Feet => EquipmentSlot.Feet,
            ArmorSubtype.Full => EquipmentSlot.Torso, // Full→Torso (основной слот)
            _ => EquipmentSlot.Torso
        };
        
        // === Icon generation (Д9, Д11) ===
        private static Sprite GenerateWeaponIcon(GeneratedWeapon dto)
        {
            // Попытка найти существующую иконку по имени
            string iconPath = $"Assets/Sprites/Equipment/Icons/weapon_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
            
            // Fallback: программная иконка (GradeColors + Tier-индикатор)
            Color bgColor = GradeColors.GetIconBgColor(dto.grade, dto.materialTier);
            Color borderColor = InventorySlotUI.GetRarityColor(MapRarityFromGrade(dto.grade));
            Color tierColor = GradeColors.GetTierColor(dto.materialTier);
            return CreateProceduralIcon(dto.subtype.ToString()[0], bgColor, borderColor, tierColor);
        }
        
        private static Sprite GenerateArmorIcon(GeneratedArmor dto)
        {
            string iconPath = $"Assets/Sprites/Equipment/Icons/armor_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
            
            Color bgColor = GradeColors.GetIconBgColor(dto.grade, dto.materialTier);
            Color borderColor = InventorySlotUI.GetRarityColor(MapRarityFromGrade(dto.grade));
            Color tierColor = GradeColors.GetTierColor(dto.materialTier);
            return CreateProceduralIcon(dto.subtype.ToString()[0], bgColor, borderColor, tierColor);
        }
        
        /// Программная иконка: Grade-цвет фон + Rarity-цвет рамка + Tier-индикатор 4×4
        private static Sprite CreateProceduralIcon(char letter, Color bgColor, Color borderColor, Color tierColor)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Color.clear;
            
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
        
        // === Helpers ===
        private static ItemRarity MapRarityFromGrade(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged => ItemRarity.Common,
            EquipmentGrade.Common => ItemRarity.Common,
            EquipmentGrade.Refined => ItemRarity.Uncommon,
            EquipmentGrade.Perfect => ItemRarity.Rare,
            EquipmentGrade.Transcendent => ItemRarity.Epic,
            _ => ItemRarity.Common
        };
        
        private static int CalculateWeaponValue(GeneratedWeapon dto)
            => Mathf.RoundToInt(dto.baseDamage * 10 * (1 + dto.materialTier * 0.5f));
        
        private static int CalculateArmorValue(GeneratedArmor dto)
            => Mathf.RoundToInt(dto.armor * 8 * (1 + dto.materialTier * 0.5f));
        
        private static List<StatBonus> ConvertBonuses(List<StatBonus> dtoBonuses)
        {
            var result = new List<StatBonus>();
            foreach (var b in dtoBonuses)
                result.Add(new StatBonus { statName = b.statName, bonus = b.value, isPercentage = b.isPercentage });
            return result;
        }
        
        private static List<StatBonus> ConvertArmorBonuses(List<ArmorBonus> dtoBonuses)
        {
            var result = new List<StatBonus>();
            foreach (var b in dtoBonuses)
                result.Add(new StatBonus { statName = b.statName, bonus = b.value, isPercentage = b.isPercentage });
            return result;
        }
        
        private static List<SpecialEffect> ConvertSpecialEffects(List<string> effects)
        {
            var result = new List<SpecialEffect>();
            foreach (var e in effects)
                result.Add(new SpecialEffect { effectName = e, description = e, triggerChance = 0.2f });
            return result;
        }
    }
}
#endif
```

---

### 3. EquipmentGeneratorMenu.cs — Editor-меню генерации

**Путь:** `UnityProject/Assets/Scripts/Editor/EquipmentGeneratorMenu.cs`  
**Этап:** 2

```csharp
// ============================================================================
// EquipmentGeneratorMenu.cs — Editor-меню генерации экипировки
// Cultivation World Simulator
// Создано: 2026-04-29
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Generators;

namespace CultivationGame.Editor
{
    public static class EquipmentGeneratorMenu
    {
        private const string OUTPUT_BASE = "Assets/Data/Equipment/Generated";
        
        [MenuItem("Tools/Equipment/Generate Weapon Set (T1)", false, 20)]
        public static void GenerateWeaponSetT1() => GenerateWeaponSet(1);
        
        [MenuItem("Tools/Equipment/Generate Weapon Set (All Tiers)", false, 21)]
        public static void GenerateWeaponSetAll() 
        { 
            for (int t = 1; t <= 5; t++) GenerateWeaponSet(t); 
        }
        
        [MenuItem("Tools/Equipment/Generate Armor Set (T1)", false, 22)]
        public static void GenerateArmorSetT1() => GenerateArmorSet(1);
        
        [MenuItem("Tools/Equipment/Generate Armor Set (All Tiers)", false, 23)]
        public static void GenerateArmorSetAll() 
        { 
            for (int t = 1; t <= 5; t++) GenerateArmorSet(t); 
        }
        
        [MenuItem("Tools/Equipment/Generate Full Set (T1)", false, 24)]
        public static void GenerateFullSetT1()
        {
            GenerateWeaponSet(1);
            GenerateArmorSet(1);
        }
        
        [MenuItem("Tools/Equipment/Generate Random Loot", false, 25)]
        public static void GenerateRandomLoot()
        {
            // 3 случайных предмета для уровня 1
            var rng = new SeededRandom();
            for (int i = 0; i < 3; i++)
            {
                if (rng.NextBool(0.5f))
                {
                    var weapon = WeaponGenerator.GenerateForLevel(1, rng);
                    // Создать SO через EquipmentSOFactory
                }
                else
                {
                    var armor = ArmorGenerator.GenerateForLevel(1, rng);
                    // Создать SO через EquipmentSOFactory
                }
            }
        }
        
        [MenuItem("Tools/Equipment/Clear Generated Equipment", false, 100)]
        public static void ClearGenerated() { /* удалить Generated/ папку */ }
        
        // === internals ===
        private static void GenerateWeaponSet(int tier)
        {
            string tierPath = $"{OUTPUT_BASE}/Weapons/T{tier}";
            EnsureDirectory(tierPath);
            
            var rng = new SeededRandom(12345 + tier);
            var subtypes = (WeaponSubtype[])System.Enum.GetValues(typeof(WeaponSubtype));
            var grades = new[] { EquipmentGrade.Common, EquipmentGrade.Refined, EquipmentGrade.Perfect };
            var materials = GetMaterialForTier(tier);
            
            int count = 0;
            foreach (var subtype in subtypes)
            {
                foreach (var grade in grades)
                {
                    var params_ = new WeaponGenerationParams
                    {
                        subtype = subtype,
                        itemLevel = Mathf.Clamp(tier * 2 - 1, 1, 9),
                        grade = grade,
                        materialTier = tier,
                        materialCategory = materials,
                        seed = rng.Next()
                    };
                    
                    var dto = WeaponGenerator.Generate(params_, new SeededRandom(params_.seed.Value));
                    string fileName = $"weapon_{dto.subtype}_{dto.materialTier}_{dto.grade}.asset";
                    string path = System.IO.Path.Combine(tierPath, fileName);
                    
                    EquipmentSOFactory.CreateFromWeapon(dto, path);
                    count++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"[EquipmentGenerator] Generated {count} weapons for T{tier}");
        }
        
        private static void GenerateArmorSet(int tier) { /* аналогично GenerateWeaponSet */ }
        
        private static MaterialCategory GetMaterialForTier(int tier) => tier switch
        {
            1 => MaterialCategory.Metal,
            2 => MaterialCategory.Metal,
            3 => MaterialCategory.Metal, // Spirit Iron
            4 => MaterialCategory.Crystal,
            5 => MaterialCategory.Void,
            _ => MaterialCategory.Metal
        };
        
        private static void EnsureDirectory(string path) { /* AssetDatabase.CreateFolder */ }
    }
}
#endif
```

---

### 4. LootGenerator.cs — Runtime генератор лута

**Путь:** `UnityProject/Assets/Scripts/Generators/LootGenerator.cs`  
**Этап:** 3

```csharp
// ============================================================================
// LootGenerator.cs — Runtime генератор лута
// Cultivation World Simulator
// Создано: 2026-04-29
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Generators
{
    public static class LootGenerator
    {
        /// Генерирует случайный предмет экипировки как runtime SO
        public static EquipmentData GenerateRandomEquipment(int playerLevel, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            
            if (rng.NextBool(0.5f))
            {
                var dto = WeaponGenerator.GenerateForLevel(playerLevel, rng);
                return EquipmentSOFactory.CreateRuntimeFromWeapon(dto);
            }
            else
            {
                var dto = ArmorGenerator.GenerateForLevel(playerLevel, rng);
                return EquipmentSOFactory.CreateRuntimeFromArmor(dto);
            }
        }
        
        /// Генерирует массив лута по уровню
        public static List<EquipmentData> GenerateLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var result = new List<EquipmentData>();
            
            for (int i = 0; i < count; i++)
                result.Add(GenerateRandomEquipment(playerLevel, rng));
            
            return result;
        }
        
        /// Генерирует расходники
        public static List<ItemData> GenerateConsumableLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            // TODO: ConsumableSOFactory (аналог EquipmentSOFactory для ItemData)
            return new List<ItemData>();
        }
    }
}
```

---

## 📝 Изменения в существующих файлах

### 5. EquipmentData.cs — Добавить поля

**Путь:** `UnityProject/Assets/Scripts/Data/ScriptableObjects/EquipmentData.cs`  
**Решение:** Д6, Д7  
**Этап:** Подготовка

```csharp
// Добавить ПЕРЕД [Header("Bonuses")]:
[Header("Penalties")]
[Tooltip("Штраф скорости перемещения (%) — отрицательный")]
[Range(-50f, 0f)]
public float moveSpeedPenalty = 0f;

[Tooltip("Штраф проводимости Ци (%) — может быть отрицательным или положительным")]
[Range(-30f, 30f)]
public float qiFlowPenalty = 0f;

[Header("Visuals")]
[Tooltip("Спрайт надетой экипировки (overlay на персонаже)")]
public Sprite equippedSprite;
```

**⚠️ dodgePenalty НЕ добавляется** — используется существующее `dodgeBonus` (отрицательные значения).

---

### 6. InventorySlotUI.cs — Подсветка фона по грейду

**Путь:** `UnityProject/Assets/Scripts/UI/Inventory/InventorySlotUI.cs`  
**Решение:** Д9  
**Этап:** UI

```csharp
// В метод UpdateDisplay(), после установки рамки по Rarity:
if (background != null && itemData is EquipmentData eqData)
{
    Color gradeTint = GradeColors.GetGradeColor(eqData.grade);
    gradeTint.a = 0.15f;
    background.color = gradeTint;
}
```

---

### 7. TooltipPanel.cs — Цвет текста грейда и тира

**Путь:** `UnityProject/Assets/Scripts/UI/Inventory/TooltipPanel.cs`  
**Решение:** Д9, Д10  
**Этап:** UI

```csharp
// В метод FillEquipmentFields() (или аналогичный):
if (gradeText != null && equipmentData != null)
    gradeText.color = GradeColors.GetGradeColor(equipmentData.grade);

if (tierText != null && equipmentData != null)
    tierText.color = GradeColors.GetTierColor(equipmentData.materialTier);
```

---

### 8. Phase16InventoryData.cs — Использовать GradeColors в иконках

**Путь:** `UnityProject/Assets/Scripts/Editor/SceneBuilder/Phase16InventoryData.cs`  
**Этап:** 5

```csharp
// В метод CreateTestEquipment():
// Заменить iconColor на GradeColors:
Color iconBg = GradeColors.GetIconBgColor(grade, materialTier);
Color iconBorder = GetRarityBorderColor(rarity);
Color tierIndicator = GradeColors.GetTierColor(materialTier);

data.icon = GenerateTestIcon(fileName, iconBg, iconBorder, tierIndicator);
```

---

*Создано: 2026-04-29 07:53 UTC*  
*Редактировано: 2026-04-29 08:10 UTC — вынесено из основного чекпоинта*
