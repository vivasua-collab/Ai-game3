# 🔧 Чекпоинт: План внедрения генераторов экипировки

**Дата:** 2026-04-29 07:11 UTC  
**Редактировано:** 2026-04-29 07:53 UTC — цветовая дифференциация грейда/тира, интеграция в фабрику
**Статус:** 📋 План готов к выполнению  
**Цель:** Подключить runtime-генераторы (WeaponGenerator, ArmorGenerator) к конвейеру создания ScriptableObject, чтобы экипировка генерировалась процедурно и попадала в инвентарь/экипировку персонажа.

---

## 🔴 КРИТИЧЕСКИЙ РАЗРЫВ

### Текущее состояние

```
WeaponGenerator.Generate()     → GeneratedWeapon (DTO)    ──── ❌ НИКУДА
ArmorGenerator.Generate()      → GeneratedArmor  (DTO)    ──── ❌ НИКУДА
ConsumableGenerator.Generate() → GeneratedConsumable (DTO) ──── ❌ НИКУДА

AssetGeneratorExtended         ← equipment.json → EquipmentData (SO) ✅ РАБОТАЕТ
Phase16InventoryData           → CreateTestEquipment() → EquipmentData (SO) ✅ РАБОТАЕТ
```

**Генераторы создают DTO, но НИКТО не конвертирует DTO → EquipmentData SO.**

### Почему это критично

1. **WeaponGenerator/ArmorGenerator** — полностью функциональны (аудит P1-P7 подтверждён), но их выход никто не использует
2. **AssetGeneratorExtended** читает статичный `equipment.json` — генерация НЕ процедурная
3. **Phase16** создаёт 10 тестовых предметов вручную — этого мало для игры
4. Без моста DTO→SO невозможно:
   - Генерировать лут с монстров (процедурно)
   - Создавать предметы по параметрам (уровень, грейд, тир)
   - Масштабировать контент без ручного ввода JSON

---

## 🔍 РЕЗУЛЬТАТЫ АУДИТА КОДА (2026-04-29 08:30 UTC)

### Аудит моделей данных

| Файл | Статус | Проблемы |
|------|--------|----------|
| `ItemData.cs` | ✅ | weight + volume + allowNesting присутствуют (строчная модель v2.0) |
| `EquipmentData.cs` | ⚠️ | Нет equippedSprite, dodgePenalty, moveSpeedPenalty, qiFlowPenalty |
| `Enums.cs` | ✅ | EquipmentSlot (15 значений), EquipmentGrade (5), WeaponHandType (2) — всё актуально |
| `EquipmentController.cs` | ✅ | v2.0, 1 слот=1 предмет, 1H/2H логика, OnEquipmentEquipped/Unequipped события |
| `EquipmentInstance` | ✅ | grade, durability, Slot, HandType, Condition |

### Аудит генераторов

| Файл | Статус | DTO-поля инвентаря |
|------|--------|---------------------|
| `WeaponGenerator.cs` | ✅ | weight, volume, stackable, maxStack, allowNesting, category (P1 выполнен) |
| `ArmorGenerator.cs` | ✅ | weight, volume, stackable, maxStack, allowNesting, category (P2 выполнен) |
| `ConsumableGenerator.cs` | ✅ | weight, volume, allowNesting, category (P3/P5/P7 выполнены) |
| `GeneratorRegistry.cs` | ✅ | Есть GenerateWeapon/GenerateArmor/GenerateConsumable методы |

### Аудит визуала

| Файл | Статус | Проблемы |
|------|--------|----------|
| `PlayerVisual.cs` | ⚠️ | Статический SpriteRenderer, нет контейнера для equipment overlay |
| `CharacterSpriteController.cs` | ⚠️ | Только scale-based flipX, нет интеграции с equipment |

### Аудит Editor-генераторов

| Файл | Статус | Примечание |
|------|--------|------------|
| `AssetGeneratorExtended.cs` | ✅ | JSON→SO мост работает, CalculateVolume/CalculateNestingFlag корректны |
| `Phase16InventoryData.cs` | ✅ | Тестовые предметы + backpack + storage ring |

### Аудит инфраструктуры

| Файл | Статус | Примечание |
|------|--------|------------|
| `MaterialSystem.cs` | ✅ | 5 тиров, кэши, MaterialProperties |
| `StorageRingData.cs` | ✅ | Наследует EquipmentData, 4 кольца |
| `ChargerData.cs` | ✅ | Зарядник Ци |
| `Combatant.cs` | ✅ | Ссылается на equipped items |

### Ключевые находки

1. **EquipmentData не хватает полей** для полного маппинга из GeneratedArmor:
   - `dodgePenalty` (GeneratedArmor.dodgePenalty)
   - `moveSpeedPenalty` (GeneratedArmor.moveSpeedPenalty)  
   - `qiFlowPenalty` (GeneratedArmor.qiFlowPenalty)
   - `equippedSprite` (спецификация готова)
   
2. **WeaponSubtype → EquipmentSlot маппинг** нужен в фабрике:
   - Sword/Dagger/Axe/Mace/Wand/Hammer → WeaponMain
   - Greatsword/Spear/Bow/Staff → WeaponMain (handType=TwoHand)
   - Unarmed → WeaponMain (handType=OneHand)
   - Crossbow → WeaponMain (handType=TwoHand)

3. **ArmorSubtype → EquipmentSlot маппинг**:
   - Head → Head, Torso → Torso, Arms → Hands (нет Arms-слота!)
   - Hands → Hands, Legs → Legs, Feet → Feet, Full → Torso (как основная)

4. **Проблема Arms-слота**: ArmorSubtype.Arms защищает руки, но EquipmentSlot.Hands — это кисти, а не предплечья. Решение: Arms маппить на Hands (как ближайший видимый слот).

5. **Программная иконка**: Phase16 уже делает это — цветной квадрат с буквой. Можно переиспользовать.

---

## 📐 УТВЕРЖДЁННЫЕ РЕШЕНИЯ

### Д1: Единый EquipmentData SO (без подтипов)
- **Статус:** ✅ Утверждено кодом
- `WeaponData`/`ArmorData` НЕ существуют — всё через `EquipmentData`
- Различие: `category` (Weapon/Armor) + поля `damage`/`defense`
- Это значит: конвертер DTO→SO пишет в ОДИН тип (`EquipmentData`)

### Д2: Слоты экипировки — 7 видимых + 8 заглушек
- **Статус:** ✅ Утверждено кодом
- Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff — видимые
- Amulet, Ring×4, Charger, Hands, Back — скрытые-заглушки
- Двуручное: WeaponMain + блок WeaponOff

### Д3: Упрощённая модель — 1 слот = 1 предмет
- **Статус:** ✅ Решение 2026-04-29
- Слои (Матрёшка v1) упразднены
- Equipped-спрайт = 1 на слот (когда будет добавлен)

### Д4: Строчная модель инвентаря (weight + volume)
- **Статус:** ✅ P1-P7 выполнены и проверены аудитом
- `CanFitItem` проверяет ОБА ограничителя
- Формулы volume: оружие/броня = `clamp(weight, 1, 4)`, расходник = `0.1`

### Д5: Генерация «Матрёшка» — База × Материал × Грейд
- **Статус:** ✅ Реализовано в генераторах
- WeaponGenerator: 12 подтипов × 5 тиров × 5 грейдов = 300 комбинаций
- ArmorGenerator: 7 подтипов × 3 весовых класса × 5 тиров × 5 грейдов = 525 комбинаций

### Д6: equippedSprite — НЕТ в коде, К ДОБАВЛЕНИЮ
- **Статус:** ⏳ Спецификация готова (EQUIPPED_SPRITES_DRAFT.md)
- Поле `equippedSprite` нужно добавить в `EquipmentData`
- Equipped-спрайты = 45 штук (пока не сгенерированы)

### Д7: штрафы брони — прямые поля в EquipmentData
- **Статус:** ✅ Решение принято (Вариант A)
- Добавить: `dodgePenalty`, `moveSpeedPenalty`, `qiFlowPenalty`
- Обоснование: CombatManager использует coverage/damageReduction как прямые поля

### Д8: Arms → Hands маппинг
- **Статус:** ✅ Решение принято
- ArmorSubtype.Arms маппится на EquipmentSlot.Hands
- Наручи = ближайший видимый слот к предплечьям

---

## 📋 ПЛАН ВНЕДРЕНИЯ — 5 ЭТАПОВ

### Этап 1: Мост DTO → EquipmentData SO (КРИТИЧЕСКИЙ)

**Цель:** Создать конвертер `GeneratedWeapon`/`GeneratedArmor` → `EquipmentData` ScriptableObject

**Новый файл:** `UnityProject/Assets/Scripts/Editor/EquipmentSOFactory.cs`

```csharp
// ============================================================================
// EquipmentSOFactory.cs — Фабрика создания EquipmentData SO из DTO генераторов
// Cultivation World Simulator
// Создано: 2026-04-29
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
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
            
            // Icon (программная генерация)
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
        
        // === Slot mapping ===
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
        
        // === Icon generation ===
        private static Sprite GenerateWeaponIcon(GeneratedWeapon dto)
        {
            // Попытка найти существующую иконку по имени
            string iconPath = $"Assets/Sprites/Equipment/Icons/weapon_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
            
            // Fallback: программная иконка (цветной квадрат + буква)
            return CreateProceduralIcon(dto.subtype.ToString()[0], new Color(0.8f, 0.3f, 0.2f));
        }
        
        private static Sprite GenerateArmorIcon(GeneratedArmor dto)
        {
            string iconPath = $"Assets/Sprites/Equipment/Icons/armor_{dto.subtype.ToString().ToLower()}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;
            
            return CreateProceduralIcon(dto.subtype.ToString()[0], new Color(0.3f, 0.5f, 0.8f));
        }
        
        private static Sprite CreateProceduralIcon(char letter, Color color)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Color.clear;
            
            // Заливка фона
            for (int y = 4; y < size - 4; y++)
                for (int x = 4; x < size - 4; x++)
                    pixels[y * size + x] = color;
            
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

**Подзадачи:**
- [ ] 1.1 Создать `EquipmentSOFactory.cs` с методами CreateFromWeapon/CreateFromArmor
- [ ] 1.2 Маппинг WeaponSubtype → EquipmentSlot (12→WeaponMain)
- [ ] 1.3 Маппинг ArmorSubtype → EquipmentSlot (7→соответствующие слоты, Arms→Hands)
- [ ] 1.4 Маппинг WeaponSubtype → handType (5 двуручных)
- [ ] 1.5 Добавить поля dodgePenalty/moveSpeedPenalty/qiFlowPenalty в EquipmentData (Д7)
- [ ] 1.6 Добавить equippedSprite в EquipmentData (Д6)
- [ ] 1.7 Программная иконка: поиск по имени + fallback (цветной квадрат + буква)
- [ ] 1.8 Runtime-методы CreateRuntimeFromWeapon/CreateRuntimeFromArmor (без AssetDatabase)
- [ ] 1.9 Тест: WeaponGenerator.Generate() → EquipmentSOFactory.CreateFromWeapon() → EquipmentData SO

### Этап 2: Editor-меню «Генерация экипировки» (ИНТЕГРАЦИЯ)

**Цель:** Добавить в Unity Editor меню для процедурной генерации экипировки

**Новый файл:** `UnityProject/Assets/Scripts/Editor/EquipmentGeneratorMenu.cs`

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
        
        private static void GenerateArmorSet(int tier) { /* аналогично */ }
        
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

**Подзадачи:**
- [ ] 2.1 Создать `EquipmentGeneratorMenu.cs` с 6 пунктами меню
- [ ] 2.2 Generate Weapon Set: цикл по подтипам × грейдам × тирам
- [ ] 2.3 Generate Armor Set: цикл по подтипам × весовым классам × грейдам
- [ ] 2.4 Generate Full Set: объединение Weapon + Armor
- [ ] 2.5 Generate Random Loot: случайные предметы по уровню
- [ ] 2.6 Clear Generated: удаление папки Generated/
- [ ] 2.7 Структура папок: `Assets/Data/Equipment/Generated/{Weapons,Armor}/T{1-5}/`

### Этап 3: Runtime-генерация лута (ИГРОВАЯ ЛОГИКА)

**Цель:** Генерировать экипировку в рантайме (лут с монстров, награды, торговцы)

**Новый файл:** `UnityProject/Assets/Scripts/Generators/LootGenerator.cs`

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

**Решение по runtime SO:** Подход B — `ScriptableObject.CreateInstance<EquipmentData>()` в рантайме.
Иконка = программная (цветной квадрат + буква). Для MVP достаточно.

**Подзадачи:**
- [ ] 3.1 Создать `LootGenerator.cs` с методами GenerateRandomEquipment/GenerateLoot
- [ ] 3.2 Runtime-методы в EquipmentSOFactory (CreateRuntimeFromWeapon/Armor) — БЕЗ AssetDatabase
- [ ] 3.3 Программная иконка для runtime SO (переделать CreateProceduralIcon для runtime)
- [ ] 3.4 Интеграция с GeneratorRegistry — добавить кэш equipment SO
- [ ] 3.5 (ОТДЕЛЬНО) LootTable ScriptableObject — таблица лута (предметы + шансы)
- [ ] 3.6 (ОТДЕЛЬНО) Интеграция с CombatManager: после боя → GenerateLoot → добавить в инвентарь

### Этап 4: Добавить equippedSprite и штрафы в EquipmentData (ВИЗУАЛ + БАЛАНС)

**Цель:** Расширить EquipmentData для полноценного маппинга из DTO

**Изменение:** `EquipmentData.cs` — добавить поля:

```csharp
[Header("Penalties")]
[Tooltip("Штраф уклонения (%) — отрицательный")]
[Range(-50f, 0f)]
public float dodgePenalty = 0f;

[Tooltip("Штраф скорости (%) — отрицательный")]
[Range(-50f, 0f)]
public float moveSpeedPenalty = 0f;

[Tooltip("Штраф проводимости Ци (%) — может быть отрицательным или положительным")]
[Range(-30f, 30f)]
public float qiFlowPenalty = 0f;

[Header("Visuals")]
[Tooltip("Спрайт надетой экипировки (overlay на персонаже)")]
public Sprite equippedSprite;
```

**Примечание:** Поле `dodgeBonus` уже существует в EquipmentData и содержит отрицательные значения для штрафов. Новое поле `dodgePenalty` — отдельное, явное поле. 

**⚠️ РЕШЕНИЕ:** НЕ добавлять `dodgePenalty` отдельно — использовать существующее `dodgeBonus` (оно уже принимает отрицательные значения, и EquipmentController его так и обрабатывает). Добавить только `moveSpeedPenalty`, `qiFlowPenalty` и `equippedSprite`.

**Подзадачи:**
- [ ] 4.1 Добавить `moveSpeedPenalty` в EquipmentData.cs (float, Range(-50, 0))
- [ ] 4.2 Добавить `qiFlowPenalty` в EquipmentData.cs (float, Range(-30, 30))
- [ ] 4.3 Добавить `equippedSprite` в EquipmentData.cs (Sprite)
- [ ] 4.4 Обновить EquipmentSOFactory — заполнение новых полей из DTO
- [ ] 4.5 (ОТДЕЛЬНО) AI-генерация 45 equipped-спрайтов по спецификации из EQUIPPED_SPRITES_DRAFT.md
- [ ] 4.6 (ОТДЕЛЬНО) Создать `EquipmentVisualController.cs` — наложение equipped-спрайтов на персонажа

### Этап 5: Обновить Phase16 — подключить генераторы (ЗАМЕНА ТЕСТОВЫХ ДАННЫХ)

**Цель:** Заменить 10 вручную созданных тестовых предметов на процедурно сгенерированные

**Изменение:** `Phase16InventoryData.cs` — метод `CreateTestEquipment()`:

**Было:** 10 захардкоженных предметов  
**Стало:** Вызов `WeaponGenerator.Generate()` + `ArmorGenerator.Generate()` + `EquipmentSOFactory.CreateFromWeapon/Armor()`

**Подзадачи:**
- [ ] 5.1 Переписать `CreateTestEquipment()` — использовать генераторы
- [ ] 5.2 Сгенерировать базовый набор: 5 оружия + 5 брони (T1, Common, Level 1)
- [ ] 5.3 Сгенерировать улучшенный набор: 3 оружия + 3 брони (T3, Refined, Level 3-5)
- [ ] 5.4 Сгенерировать 5 рюкзаков + 4 кольца (оставить как есть — они не через генераторы)

---

## 🔄 ЗАВИСИМОСТИ

```
Подготовка (4.1-4.3: EquipmentData поля) ─── НЕЗАВИСИМА, делается ПЕРВОЙ
         │
Этап 1 (EquipmentSOFactory) ──── ФУНДАМЕНТ, зависит от подготовки
         │
         ├── Этап 2 (Editor-меню) ─── зависит от 1
         ├── Этап 3 (LootGenerator) ── зависит от 1
         ├── Этап 5 (Phase16 замена) ── зависит от 1
         └── Этап 4.4 (Factory update) ── зависит от 1 + 4.1-4.3
```

**Рекомендуемый порядок:**
1. **Подготовка** → Добавить поля в EquipmentData (moveSpeedPenalty, qiFlowPenalty, equippedSprite)
2. **Этап 1** → EquipmentSOFactory (фабрика DTO→SO, критический)
3. **Этап 5** → Переписать Phase16 (видимый результат для тестирования)
4. **Этап 2** → Editor-меню (удобство для дизайнеров)
5. **Этап 3** → Runtime LootGenerator (игровая логика, самый сложный)
6. **Этап 4.5-4.6** → equipped-спрайты + EquipmentVisualController (отдельная задача)

---

## 📊 МАТРИЦА ИЗМЕНЕНИЙ

| Файл | Что делаем | Строк | Этап |
|------|-----------|-------|------|
| `EquipmentData.cs` | +moveSpeedPenalty, +qiFlowPenalty, +equippedSprite | ~12 | Подготовка |
| `EquipmentSOFactory.cs` | НОВЫЙ: конвертер DTO→SO (Editor + Runtime) | ~200 | 1 |
| `EquipmentGeneratorMenu.cs` | НОВЫЙ: Editor-меню генерации | ~150 | 2 |
| `LootGenerator.cs` | НОВЫЙ: Runtime генерация лута | ~60 | 3 |
| `Phase16InventoryData.cs` | Переписать CreateTestEquipment | ~40 | 5 |
| `EquipmentVisualController.cs` | НОВЫЙ: Overlay Layering (отдельно) | ~80 | 4.6 |

**Итого:** ~540 строк нового кода, 2 файла изменений.

---

## ⚠️ ОТКРЫТЫЕ ВОПРОСЫ — РЕШЁННЫЕ

### В1: dodgePenalty/moveSpeedPenalty/qiFlowPenalty — куда класть?
- ✅ **РЕШЕНО:** dodgePenalty → в существующее поле `dodgeBonus` (отрицательное значение)
- ✅ **РЕШЕНО:** moveSpeedPenalty → новое прямое поле в EquipmentData
- ✅ **РЕШЕНО:** qiFlowPenalty → новое прямое поле в EquipmentData
- Обоснование: CombatManager использует coverage/damageReduction как прямые поля

### В2: Сколько SO генерировать на старте?
- ✅ **РЕШЕНО:** Подход C — Runtime SO через LootGenerator. Editor-меню для предгенерации.

### В3: Иконки для генерируемых предметов?
- ✅ **РЕШЕНО:** Поиск по имени (weapon_{subtype}.png) → fallback на программную генерацию

### В4: Arms-слот в ArmorSubtype → EquipmentSlot
- ✅ **РЕШЕНО:** Arms маппится на Hands (ближайший видимый слот)

---

## ✅ КРИТЕРИИ ПРИЁМКИ

1. **EquipmentData** имеет новые поля: moveSpeedPenalty, qiFlowPenalty, equippedSprite
2. **EquipmentSOFactory** корректно конвертирует GeneratedWeapon → EquipmentData SO
3. **EquipmentSOFactory** корректно конвертирует GeneratedArmor → EquipmentData SO
4. Сгенерированные SO имеют ВСЕ инвентарные поля (weight, volume, stackable, maxStack, allowNesting)
5. Маппинг WeaponSubtype→EquipmentSlot корректный (5 двуручных = TwoHand)
6. Маппинг ArmorSubtype→EquipmentSlot корректный (Arms→Hands)
7. Editor-меню «Generate Weapon Set» создаёт SO в Assets/Data/Equipment/Generated/
8. Runtime LootGenerator создаёт EquipmentData через ScriptableObject.CreateInstance
9. Phase16 использует генераторы вместо ручного CreateTestEquipment
10. equippedSprite = null по умолчанию (спрайты — отдельная задача)
11. Компиляция без ошибок

---

## 🎨 ЦВЕТОВАЯ ДИФФЕРЕНЦИАЦИЯ ГРЕЙДА И ТИРА

**Дата решения:** 2026-04-29  
**Контекст:** Предметы имеют 3 визуальные оси: Grade (качество), Tier (материал), Rarity (редкость). Каждая ось должна иметь свою цветовую систему, чтобы игрок мгновенно понимал ценность предмета.

### Проблема

**Сейчас в коде:**
- `ItemRarity` → `InventorySlotUI.GetRarityColor()` ✅ 6 цветов (серый → красный)
- `EquipmentGrade` → цветов НЕТ ❌ (только emoji в EQUIPMENT_SYSTEM.md: 🔴⚪🟢🔵🟡)
- `MaterialTier` → цветов НЕТ ❌
- `Phase16InventoryData.GetRarityBorderColor()` → дублирует ItemRarity цвета ✅

**Grade ≠ Rarity.** Grade — качество предмета (Damaged→Transcendent), Rarity — общая редкость (Common→Mythic). Они маппятся друг в друга (MapRarityFromGrade), но это **разные оси восприятия**. Для UI нужно ЧЁТКО разделять:
- **Рамка слота** = Rarity (уже работает)
- **Фон иконки / текст грейда** = Grade (НОВОЕ)
- **Яркость / свечение** = Tier (НОВОЕ)

### Д9: Цветовая палитра Grade (Грейд = качество предмета)

**Статус:** ✅ Утверждено

Источник: EQUIPMENT_SYSTEM.md §2.1 — цвета из документа (🔴⚪🟢🔵🟡) + уточнение для UI

| Grade | Emoji | Hex | Unity Color | RGB (0-1) | Применение |
|-------|-------|-----|-------------|-----------|------------|
| **Damaged** | 🔴 | `#8B4513` | Коричнево-красный | (0.545, 0.271, 0.075) | Фон иконки, текст грейда, худший предмет |
| **Common** | ⚪ | `#9CA3AF` | Серый | (0.612, 0.639, 0.686) | Фон иконки, базовый предмет |
| **Refined** | 🟢 | `#22C55E` | Зелёный | (0.133, 0.773, 0.369) | Фон иконки, рамка текста |
| **Perfect** | 🔵 | `#3B82F6` | Синий | (0.231, 0.510, 0.965) | Фон иконки, подсветка |
| **Transcendent** | 🟡 | `#F59E0B` | Золотой | (0.961, 0.620, 0.043) | Фон иконки, свечение |

**Обоснование цветов:**
- **Damaged = коричнево-красный**, не чистый красный (красный = Mythic rarity). Коричневый = ржавчина, повреждение
- **Common = серый** — стандартный, ничем не выделяющийся
- **Refined = зелёный** — улучшенный, позитивный сигнал
- **Perfect = синий** — мастерский, премиальный
- **Transcendent = золотой** — высший, сияющий (xianxia: золотое ядро = высшая ступень)

**Отличие от Rarity-цветов:**
| Ось | Damaged | Common | Refined | Perfect | Transcendent |
|-----|---------|--------|---------|---------|--------------|
| **Grade** | Коричневый | Серый | Зелёный | Синий | Золотой |
| **Rarity** | — | Серый | Зелёный | Синий/Фиол. | Фиолет./Золот. |

Grade и Rarity **частично совпадают** (серый→зелёный→синий), но Grade не имеет фиолетового и красного — высший грейд = золотой, высшая редкость = красный.

### Д10: Цветовая палитра Tier (Тир = уровень материала)

**Статус:** ✅ Утверждено

Источник: EQUIPMENT_SYSTEM.md §3.1 — 5 тиров материалов

| Tier | Материалы | Hex | Unity Color | RGB (0-1) | Значение |
|------|-----------|-----|-------------|-----------|----------|
| **T1** | Iron, Leather, Cloth, Wood, Bone | `#8B9DAF` | Стальной серый | (0.545, 0.616, 0.686) | Обычный |
| **T2** | Steel, Silk, Silver | `#4ADE80` | Светло-зелёный | (0.290, 0.871, 0.502) | Качественный |
| **T3** | Spirit Iron, Jade, Cold Iron | `#60A5FA` | Голубой | (0.376, 0.647, 0.980) | Духовный |
| **T4** | Star Metal, Dragon Bone, Elemental Core | `#A78BFA` | Фиолетовый | (0.655, 0.545, 0.980) | Небесный |
| **T5** | Void Matter, Chaos Matter, Primordial | `#F472B6` | Розовый | (0.957, 0.447, 0.714) | Первородный |

**Обоснование:** Тир материала определяет «магический уровень» — от простого железа до первородной материи. Палитра идёт по спектру: серый → зелёный → голубой → фиолетовый → розовый. Это НЕ совпадает с Grade и Rarity, позволяя ортогональную дифференциацию.

**Применение Tier-цвета:**
- Маленький индикатор/точка на иконке предмета
- Цвет подстроки «T1»-«T5» в тултипе
- Свечение (glow) для T4-T5 (для привлечения внимания к редким материалам)

### Д11: Комбинированная формула цвета иконки

**Статус:** ✅ Утверждено

Для программных иконок (fallback, когда нет sprite) — фон иконки = Grade-цвет, затемнённый по Tier.

```
iconBgColor = GradeColor[grade] × (0.7 + 0.3 × (tier / 5))
```

- T1: 70% яркости Grade-цвета (тусклый)
- T3: 88% яркости
- T5: 100% яркости (полный)

Рамка иконки = Rarity-цвет (существующая система).

**Пример:**
- Iron Sword, Common grade, T1: фон = серый × 0.76 = тёмно-серый, рамка = серый
- Spirit Sword, Refined grade, T3: фон = зелёный × 0.88, рамка = зелёный
- Dragon Bone Sword, Perfect grade, T4: фон = синий × 0.94, рамка = синий
- Void Matter Sword, Transcendent, T5: фон = золотой × 1.0, рамка = фиолетовый

### Д12: Статический класс GradeColors — единая точка доступа

**Статус:** ✅ Утверждено

**Новый файл:** `UnityProject/Assets/Scripts/Core/GradeColors.cs`

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
        
        /// Цвет рамки иконки = Rarity (делегирует в InventorySlotUI)
        /// Использование: InventorySlotUI.GetRarityColor(item.rarity)
    }
}
```

### Интеграция в EquipmentSOFactory

Программная иконка теперь использует **GradeColors**:

```csharp
// Было (в EquipmentSOFactory.CreateProceduralIcon):
return CreateProceduralIcon(dto.subtype.ToString()[0], new Color(0.8f, 0.3f, 0.2f));

// Стало:
Color bgColor = GradeColors.GetIconBgColor(dto.grade, dto.materialTier);
Color borderColor = InventorySlotUI.GetRarityColor(MapRarityFromGrade(dto.grade));
return CreateProceduralIcon(dto.subtype.ToString()[0], bgColor, borderColor);
```

### Интеграция в TooltipPanel

```csharp
// В TooltipPanel — показать грейд + тир цветом:
if (equipmentData != null)
{
    gradeText.color = GradeColors.GetGradeColor(equipmentData.grade);
    tierText.color = GradeColors.GetTierColor(equipmentData.materialTier);
}
```

### Интеграция в InventorySlotUI

```csharp
// Фон слота — подсветка по грейду (слабая, 15% прозрачности):
if (equipmentData != null)
{
    Color gradeTint = GradeColors.GetGradeColor(equipmentData.grade);
    gradeTint.a = 0.15f;
    background.color = gradeTint;
}
```

---

## 📐 УТВЕРЖДЁННЫЕ РЕШЕНИЯ — ПОЛНЫЙ СПИСОК

### Д1–Д8: (без изменений, см. выше)

### Д9: Цветовая палитра Grade
- **Статус:** ✅ Утверждено 2026-04-29
- 5 цветов: Коричневый → Серый → Зелёный → Синий → Золотой
- Отличается от Rarity (нет фиолетового/красного)
- Применение: фон иконки, текст грейда

### Д10: Цветовая палитра Tier
- **Статус:** ✅ Утверждено 2026-04-29
- 5 цветов: Стальной → Зелёный → Голубой → Фиолетовый → Розовый
- Ортогональна Grade и Rarity
- Применение: индикатор материала, яркость иконки, glow T4-T5

### Д11: Комбинированная формула цвета иконки
- **Статус:** ✅ Утверждено 2026-04-29
- `iconBg = GradeColor × (0.7 + 0.3 × tier/5)`
- Рамка = Rarity-цвет (существующая система)
- Результат: Damaged T1 = тёмный коричневый, Transcendent T5 = яркий золотой

### Д12: Статический класс GradeColors
- **Статус:** ✅ Утверждено 2026-04-29
- Новый файл `Core/GradeColors.cs`
- Единая точка доступа для всех UI-компонентов
- Методы: GetGradeColor, GetTierColor, GetIconBgColor, GetGradeNameRu, GetTierNameRu

---

## 📊 ОБНОВЛЁННАЯ МАТРИЦА ИЗМЕНЕНИЙ

| Файл | Что делаем | Строк | Этап |
|------|-----------|-------|------|
| `EquipmentData.cs` | +moveSpeedPenalty, +qiFlowPenalty, +equippedSprite | ~12 | Подготовка |
| `GradeColors.cs` | НОВЫЙ: цветовая палитра Grade+Tier | ~90 | Подготовка |
| `EquipmentSOFactory.cs` | НОВЫЙ: конвертер DTO→SO (Editor + Runtime), GradeColors в иконках | ~220 | 1 |
| `EquipmentGeneratorMenu.cs` | НОВЫЙ: Editor-меню генерации | ~150 | 2 |
| `LootGenerator.cs` | НОВЫЙ: Runtime генерация лута | ~60 | 3 |
| `Phase16InventoryData.cs` | Переписать CreateTestEquipment, GradeColors в иконках | ~60 | 5 |
| `InventorySlotUI.cs` | Добавить GradeColors подсветку фона | ~8 | UI |
| `TooltipPanel.cs` | Добавить Grade+Tier цвет текста | ~10 | UI |
| `EquipmentVisualController.cs` | НОВЫЙ: Overlay Layering (отдельно) | ~80 | 4.6 |

**Итого:** ~690 строк нового кода, 4 файла изменений.

---

## 🔄 ОБНОВЛЁННЫЕ ЗАВИСИМОСТИ

```
Подготовка ─────────────────────────────────────────────────────┐
  ├── 4.1-4.3: EquipmentData поля                              │
  └── GradeColors.cs (Д12) ─────────────────── НОВАЯ           │
         │                                                      │
Этап 1 (EquipmentSOFactory) ──── ФУНДАМЕНТ                    │
  ├── использует GradeColors.GetIconBgColor()                   │
  └── зависит от EquipmentData полей                            │
         │                                                      │
         ├── Этап 2 (Editor-меню) ─── зависит от 1             │
         ├── Этап 3 (LootGenerator) ── зависит от 1            │
         ├── Этап 5 (Phase16 замена) ── зависит от 1           │
         ├── UI: InventorySlotUI+TooltipPanel ── от GradeColors │
         └── Этап 4.5-4.6 (спрайты+визуал) ── отдельно        │
```

**Обновлённый порядок:**
1. **Подготовка** → EquipmentData поля + GradeColors.cs
2. **Этап 1** → EquipmentSOFactory (с GradeColors)
3. **Этап 5** → Phase16 (видимый результат)
4. **UI** → InventorySlotUI + TooltipPanel (подсветка Grade/Tier)
5. **Этап 2** → Editor-меню
6. **Этап 3** → Runtime LootGenerator
7. **Этап 4.5-4.6** → equipped-спрайты + EquipmentVisualController

---

## ⚠️ ОТКРЫТЫЕ ВОПРОСЫ — РЕШЁННЫЕ

### В1–В4: (без изменений, см. выше)

### В5: Цвет грейда = цвет редкости?
- ✅ **РЕШЕНО:** НЕТ, разные палитры (Д9 vs ItemRarity). Grade = коричневый/серый/зелёный/синий/золотой. Rarity = серый/зелёный/синий/фиолет/золотой/красный.
- Grade НЕ имеет фиолетового и красного (это Rarity).
- Grade ИМЕЕТ коричневый (Damaged), которого нет в Rarity.

### В6: Нужен ли отдельный индикатор Tier на иконке?
- ✅ **РЕШЕНО:** Да — маленькая точка/квадрат 4×4px в правом нижнем углу иконки, цвет = Tier. Реализуется в CreateProceduralIcon.

---

## ✅ ОБНОВЛЁННЫЕ КРИТЕРИИ ПРИЁМКИ

1–11. (без изменений, см. выше)
12. **GradeColors.cs** предоставляет GetGradeColor/GetTierColor/GetIconBgColor
13. Программная иконка использует GradeColors.GetIconBgColor(grade, tier) для фона
14. Тир-индикатор (4×4px точка) отображается на программной иконке
15. TooltipPanel показывает грейд цветом GradeColors.GetGradeColor()
16. TooltipPanel показывает тир цветом GradeColors.GetTierColor()
17. InventorySlotUI подсвечивает фон слота цветом грейда (15% прозрачности)

---

*Создано: 2026-04-29 07:11 UTC*  
*Редактировано: 2026-04-29 07:53 UTC — цветовая дифференциация грейда/тира (Д9-Д12), интеграция в фабрику и UI, обновлённая матрица изменений*
