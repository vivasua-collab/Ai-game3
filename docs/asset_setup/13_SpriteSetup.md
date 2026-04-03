# 🎨 Настройка спрайтов — Sprite Setup Guide

**Версия:** 1.0  
**Дата:** 2026-04-03  
**Проект:** Cultivation World Simulator

---

## 📋 Обзор

Сгенерировано **57 спрайтов** для всех игровых элементов:

| Категория | Количество | Путь |
|-----------|------------|------|
| Элементы | 8 | `Sprites/Elements/` |
| Техники | 8 | `Sprites/Techniques/` |
| Материалы | 8 | `Sprites/Items/` |
| Расходники | 7 | `Sprites/Items/` |
| Оружие | 11 | `Sprites/Equipment/` |
| Броня | 10 | `Sprites/Equipment/` |
| UI | 4 | `Sprites/UI/` |
| Персонаж | 1 | `Sprites/` |

---

## 🗂️ Структура папок

```
Assets/
└── Sprites/
    ├── Elements/              ← Иконки стихий
    │   ├── element_neutral.png
    │   ├── element_fire.png
    │   ├── element_water.png
    │   ├── element_earth.png
    │   ├── element_air.png
    │   ├── element_lightning.png
    │   ├── element_void.png
    │   └── element_poison.png
    │
    ├── Techniques/            ← Иконки типов техник
    │   ├── technique_melee.png
    │   ├── technique_ranged.png
    │   ├── technique_defense.png
    │   ├── technique_healing.png
    │   ├── technique_movement.png
    │   ├── technique_cultivation.png
    │   ├── technique_support.png
    │   └── technique_curse.png
    │
    ├── Items/                 ← Материалы и расходники
    │   ├── material_iron_ore.png
    │   ├── material_iron_ingot.png
    │   ├── material_spirit_iron.png
    │   ├── material_leather.png
    │   ├── material_cloth.png
    │   ├── material_jade.png
    │   ├── material_spirit_stone.png
    │   ├── material_star_metal.png
    │   ├── consumable_healing_pill.png
    │   ├── consumable_qi_pill.png
    │   ├── consumable_breakthrough_pill.png
    │   ├── consumable_bread.png
    │   ├── consumable_meat.png
    │   ├── consumable_antidote.png
    │   └── consumable_scroll.png
    │
    ├── Equipment/             ← Оружие и броня
    │   ├── weapon_sword_iron.png
    │   ├── weapon_sword_spirit.png
    │   ├── weapon_dagger_iron.png
    │   ├── weapon_axe_iron.png
    │   ├── weapon_spear_iron.png
    │   ├── weapon_staff_wood.png
    │   ├── weapon_staff_jade.png
    │   ├── weapon_bow_wood.png
    │   ├── weapon_greatsword_iron.png
    │   ├── weapon_crossbow_iron.png
    │   ├── weapon_claws.png
    │   ├── armor_helmet_iron.png
    │   ├── armor_torso_iron.png
    │   ├── armor_robe_cloth.png
    │   ├── armor_robe_spirit.png
    │   ├── armor_vest_leather.png
    │   ├── armor_greaves_iron.png
    │   ├── armor_boots_leather.png
    │   ├── armor_gloves_leather.png
    │   ├── armor_chainmail.png
    │   └── armor_hood_cloth.png
    │
    ├── UI/                    ← UI иконки
    │   ├── ui_health.png
    │   ├── ui_qi.png
    │   ├── ui_stamina.png
    │   └── ui_cultivation.png
    │
    └── player_sprite.png      ← Спрайт игрока
```

---

## 🔧 Шаг 1: Импорт спрайтов в Unity

### 1.1 Проверка папки

Спрайты уже находятся в проекте:
```
UnityProject/Local/Assets/Sprites/
```

### 1.2 Настройка импорта (для каждого спрайта)

1. Выделите спрайт в Project окне
2. В Inspector настройте:
   - **Texture Type:** Sprite (2D and UI)
   - **Sprite Mode:** Single
   - **Pixels Per Unit:** 100 (для 1024x1024)
   - **Filter Mode:** Bilinear
   - **Compression:** None (для качества) или High Quality

### 1.3 Массовая настройка

1. Выделите все спрайты в папке (Ctrl+A)
2. Inspector → Apply defaults:
   - Texture Type: Sprite (2D and UI)
   - Pixels Per Unit: 100
3. Нажмите **Apply**

---

## 🔧 Шаг 2: Создание Sprite Atlas (опционально)

Для оптимизации производительности:

### 2.1 Создание Atlas

1. `Assets/Create/2D/Sprite Atlas`
2. Назовите `GameSprites`
3. Перетащите папки в **Objects for Packing**:
   - Elements
   - Techniques
   - Items
   - Equipment
   - UI

### 2.2 Настройки Atlas

- **Type:** Master
- **Include in Build:** ✓
- **Allow Rotation:** ✗
- **Tight Packing:** ✓

---

## 🔧 Шаг 3: Назначение спрайтов ScriptableObject'ам

### 3.1 ElementData

Откройте каждый ElementData asset и назначьте:

| Element | Sprite |
|---------|--------|
| Neutral | `element_neutral` |
| Fire | `element_fire` |
| Water | `element_water` |
| Earth | `element_earth` |
| Air | `element_air` |
| Lightning | `element_lightning` |
| Void | `element_void` |
| Poison | `element_poison` |

**Путь:** `Assets/Data/Elements/`

### 3.2 TechniqueData

Для каждого TechniqueData назначьте спрайт по типу:

| TechniqueType | Sprite |
|---------------|--------|
| Combat (MeleeStrike) | `technique_melee` |
| Combat (RangedProjectile) | `technique_ranged` |
| Combat (RangedBeam) | `technique_ranged` |
| Combat (RangedAoe) | `technique_ranged` |
| Defense | `technique_defense` |
| Healing | `technique_healing` |
| Movement | `technique_movement` |
| Cultivation | `technique_cultivation` |
| Support | `technique_support` |
| Curse | `technique_curse` |
| Poison | `technique_curse` |
| Sensory | `technique_support` |

**Путь:** `Assets/Data/Techniques/`

### 3.3 ItemData (Материалы)

| Material | Sprite |
|----------|--------|
| Iron Ore | `material_iron_ore` |
| Iron Ingot | `material_iron_ingot` |
| Spirit Iron | `material_spirit_iron` |
| Leather | `material_leather` |
| Cloth | `material_cloth` |
| Jade | `material_jade` |
| Spirit Stone | `material_spirit_stone` |
| Star Metal | `material_star_metal` |

**Путь:** `Assets/Data/Materials/`

### 3.4 ItemData (Расходники)

| Consumable | Sprite |
|------------|--------|
| Healing Pill | `consumable_healing_pill` |
| Qi Pill | `consumable_qi_pill` |
| Breakthrough Pill | `consumable_breakthrough_pill` |
| Bread | `consumable_bread` |
| Meat | `consumable_meat` |
| Antidote | `consumable_antidote` |
| Technique Scroll | `consumable_scroll` |

**Путь:** `Assets/Data/Items/`

### 3.5 EquipmentData (Оружие)

| Weapon | Sprite |
|--------|--------|
| Fists/Claws | `weapon_claws` |
| Iron Dagger | `weapon_dagger_iron` |
| Iron Sword | `weapon_sword_iron` |
| Steel Sword | `weapon_sword_iron` (переименовать) |
| Spirit Sword | `weapon_sword_spirit` |
| Iron Greatsword | `weapon_greatsword_iron` |
| Iron Axe | `weapon_axe_iron` |
| Iron Spear | `weapon_spear_iron` |
| Wooden Bow | `weapon_bow_wood` |
| Iron Crossbow | `weapon_crossbow_iron` |
| Wooden Staff | `weapon_staff_wood` |
| Jade Staff | `weapon_staff_jade` |

**Путь:** `Assets/Data/Equipment/`

### 3.6 EquipmentData (Броня)

| Armor | Sprite |
|-------|--------|
| Iron Helmet | `armor_helmet_iron` |
| Cloth Hood | `armor_hood_cloth` |
| Cloth Robe | `armor_robe_cloth` |
| Spirit Robe | `armor_robe_spirit` |
| Leather Vest | `armor_vest_leather` |
| Iron Breastplate | `armor_torso_iron` |
| Chainmail | `armor_chainmail` |
| Iron Greaves | `armor_greaves_iron` |
| Leather Boots | `armor_boots_leather` |
| Leather Gloves | `armor_gloves_leather` |

**Путь:** `Assets/Data/Equipment/`

---

## 🔧 Шаг 4: Назначение спрайта игроку

### 4.1 Через префаб

1. Откройте префаб `Assets/Prefabs/Player/Player.prefab`
2. Найдите компонент **Sprite Renderer**
3. Перетащите `player_sprite` в поле **Sprite**

### 4.2 Через скрипт (PlayerVisual)

Если используется динамическое назначение:

```csharp
// В PlayerVisual.cs
[SerializeField] private Sprite playerSprite;

void Start()
{
    GetComponent<SpriteRenderer>().sprite = playerSprite;
}
```

---

## 🔧 Шаг 5: UI элементы

### 5.1 HUD иконки

Назначьте для HUDController или UIManager:

| UI Element | Sprite | Использование |
|------------|--------|---------------|
| Health Bar Icon | `ui_health` | Image перед полоской HP |
| Qi Bar Icon | `ui_qi` | Image перед полоской Ци |
| Stamina Icon | `ui_stamina` | Image перед полоской выносливости |
| Cultivation Icon | `ui_cultivation` | Image возле уровня культивации |

### 5.2 Inventory иконки

Для InventoryUI назначьте спрайты предметам при отображении:

```csharp
// Пример в InventoryUI.cs
public void UpdateSlot(int slotIndex, ItemData item)
{
    if (item != null && item.icon != null)
    {
        slotIcons[slotIndex].sprite = item.icon;
        slotIcons[slotIndex].color = Color.white;
    }
    else
    {
        slotIcons[slotIndex].sprite = null;
        slotIcons[slotIndex].color = Color.clear;
    }
}
```

---

## 🔧 Шаг 6: Создание генератора спрайтов (Editor Script)

Для автоматического назначения спрайтов по именам:

```csharp
// Assets/Scripts/Editor/SpriteAssigner.cs
using UnityEditor;
using UnityEngine;

public class SpriteAssigner : EditorWindow
{
    [MenuItem("Tools/Assign Sprites to Data Assets")]
    public static void AssignAllSprites()
    {
        AssignElementSprites();
        AssignTechniqueSprites();
        AssignItemSprites();
        AssignEquipmentSprites();
        
        AssetDatabase.SaveAssets();
        Debug.Log("All sprites assigned!");
    }
    
    static void AssignElementSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:ElementData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ElementData data = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            
            // Поиск спрайта по имени элемента
            string spritePath = $"Assets/Sprites/Elements/element_{data.elementName.ToLower()}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            
            if (sprite != null)
            {
                data.icon = sprite;
                EditorUtility.SetDirty(data);
            }
        }
    }
    
    // Аналогично для других типов данных...
}
```

---

## 📊 Сводная таблица спрайтов

### Элементы (8)

| Файл | Цвет | Описание |
|------|------|----------|
| element_neutral.png | Белый | Нейтральный элемент |
| element_fire.png | Оранжевый | Огненная стихия |
| element_water.png | Синий | Водная стихия |
| element_earth.png | Коричневый | Земная стихия |
| element_air.png | Голубой | Воздушная стихия |
| element_lightning.png | Золотой | Молния |
| element_void.png | Фиолетовый | Пустота |
| element_poison.png | Зелёный | Яд |

### Типы техник (8)

| Файл | Описание |
|------|----------|
| technique_melee.png | Рукопашные техники |
| technique_ranged.png | Дальнобойные техники |
| technique_defense.png | Защитные техники |
| technique_healing.png | Исцеление |
| technique_movement.png | Перемещение |
| technique_cultivation.png | Культивация |
| technique_support.png | Поддержка/баффы |
| technique_curse.png | Проклятия/дебаффы |

### Материалы (8)

| Файл | Редкость | Описание |
|------|----------|----------|
| material_iron_ore.png | Common | Железная руда |
| material_iron_ingot.png | Common | Железный слиток |
| material_spirit_iron.png | Rare | Духовное железо |
| material_leather.png | Common | Кожа |
| material_cloth.png | Common | Ткань |
| material_jade.png | Rare | Нефрит |
| material_spirit_stone.png | Uncommon | Духовный камень |
| material_star_metal.png | Epic | Звёздный металл |

### Расходники (7)

| Файл | Тип | Эффект |
|------|-----|--------|
| consumable_healing_pill.png | Пилюля | +20 HP |
| consumable_qi_pill.png | Пилюля | +100 Qi |
| consumable_breakthrough_pill.png | Пилюля | +20% к прорыву |
| consumable_bread.png | Еда | -30 голод |
| consumable_meat.png | Еда | -60 голод |
| consumable_antidote.png | Лекарство | Снимает яд |
| consumable_scroll.png | Свиток | Изучение техники |

### Оружие (11)

| Файл | Тип | Описание |
|------|-----|----------|
| weapon_sword_iron.png | Sword | Железный меч |
| weapon_sword_spirit.png | Sword | Духовный меч |
| weapon_dagger_iron.png | Dagger | Железный кинжал |
| weapon_axe_iron.png | Axe | Железный топор |
| weapon_spear_iron.png | Spear | Железное копьё |
| weapon_staff_wood.png | Staff | Деревянный посох |
| weapon_staff_jade.png | Staff | Нефритовый посох |
| weapon_bow_wood.png | Bow | Деревянный лук |
| weapon_greatsword_iron.png | Greatsword | Двуручный меч |
| weapon_crossbow_iron.png | Crossbow | Арбалет |
| weapon_claws.png | Unarmed | Когти |

### Броня (10)

| Файл | Слот | Описание |
|------|------|----------|
| armor_helmet_iron.png | Head | Железный шлем |
| armor_hood_cloth.png | Head | Тканевый капюшон |
| armor_robe_cloth.png | Torso | Тканевая роба |
| armor_robe_spirit.png | Torso | Духовная роба |
| armor_vest_leather.png | Torso | Кожаный жилет |
| armor_torso_iron.png | Torso | Железный нагрудник |
| armor_chainmail.png | Torso | Кольчуга |
| armor_greaves_iron.png | Legs | Железные поножи |
| armor_boots_leather.png | Feet | Кожаные сапоги |
| armor_gloves_leather.png | Hands | Кожаные перчатки |

### UI (4)

| Файл | Использование |
|------|---------------|
| ui_health.png | Иконка HP в HUD |
| ui_qi.png | Иконка Qi в HUD |
| ui_stamina.png | Иконка выносливости |
| ui_cultivation.png | Иконка культивации |

---

## 🔄 Регенерация спрайтов

Если нужно регенерировать спрайты, используйте AI Image Generation:

### Через CLI

```bash
# Пример генерации нового спрайта
z-ai image -p "Game icon for cultivation game, [описание], fantasy style, game asset, icon style, high quality" -o "./UnityProject/Local/Assets/Sprites/[путь].png" -s 1024x1024
```

### Параметры промпта

```
Game icon for cultivation game,
[предмет/элемент],
[детали: цвет, эффекты],
fantasy style,
game asset,
icon style,
high quality
```

---

## ⚠️ Важные замечания

1. **Формат:** Все спрайты 1024x1024 PNG
2. **Стиль:** Fantasy, icon style
3. **Фон:** Полупрозрачный/белый (требуется обработка для прозрачности)
4. **Названия:** Строгие имена файлов для автоматического назначения

### Обработка прозрачности

Если спрайты имеют белый фон вместо прозрачного:

1. Откройте в графическом редакторе
2. Удалите белый фон
3. Сохраните как PNG с прозрачностью

Или используйте скрипт:

```csharp
// Tools → Remove White Background
[MenuItem("Tools/Sprites/Remove White Background")]
static void RemoveWhiteBackground()
{
    string[] guids = Selection.assetGUIDs;
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        // Обработка прозрачности...
    }
}
```

---

## 📝 Чек-лист проверки

- [ ] Все спрайты импортированы в Unity
- [ ] Texture Type = Sprite (2D and UI)
- [ ] Pixels Per Unit = 100
- [ ] ElementData имеют назначенные иконки
- [ ] TechniqueData имеют назначенные иконки
- [ ] ItemData имеют назначенные иконки
- [ ] EquipmentData имеют назначенные иконки
- [ ] Player имеет назначенный спрайт
- [ ] UI элементы используют правильные иконки
- [ ] Sprite Atlas создан (опционально)

---

*Документ создан: 2026-04-03*
*Для проекта: Cultivation World Simulator*
