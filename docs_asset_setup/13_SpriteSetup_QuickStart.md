# 🎨 Быстрая настройка спрайтов — Quick Start

**Создано:** 66 спрайтов в `Assets/Sprites/`

---

## ⚡ Быстрая настройка (3 шага)

### Шаг 1: Импорт в Unity

Спрайты уже в проекте. Откройте Unity и проверьте папку `Assets/Sprites/`.

### Шаг 2: Настройка текстур

1. Выделите все PNG файлы
2. Inspector:
   - Texture Type: **Sprite (2D and UI)**
   - Pixels Per Unit: **100**
   - Filter Mode: **Bilinear**
3. Нажмите **Apply**

### Шаг 3: Назначение ScriptableObject'ам

| Data Type | Folder | Sprite Prefix |
|-----------|--------|---------------|
| ElementData | `Data/Elements/` | `element_*` |
| TechniqueData | `Data/Techniques/` | `technique_*` |
| MaterialData | `Data/Materials/` | `material_*` |
| ItemData | `Data/Items/` | `consumable_*` |
| EquipmentData | `Data/Equipment/` | `weapon_*` / `armor_*` |

---

## 📁 Структура

```
Sprites/
├── Elements/      (8)   ← Стихии: neutral, fire, water, earth, air, lightning, void, poison
├── Techniques/    (11)  ← Типы: melee, ranged, defense, healing, movement, cultivation...
├── Materials/     (8)   ← Тиры: T1-T5 материалы
├── Consumables/   (7)   ← Расходники: pills, food, antidote
├── Weapons/       (11)  ← Оружие: fists, dagger, sword, greatsword, axe, spear, bow, staff...
├── Armor/         (10)  ← Броня: helmet, robe, plate, boots, gloves...
├── Cultivation/   (3)   ← Культивация: core, breakthrough, meditation
├── UI/            (4)   ← health, qi, stamina, cultivation
└── Player/        (1)   ← player_sprite.png
```

---

## 🎯 Маппинг техник

| Тип техники | Спрайт |
|-------------|--------|
| Combat (Melee) | technique_melee |
| Combat (Ranged) | technique_ranged |
| Defense | technique_defense |
| Healing | technique_healing |
| Movement | technique_movement |
| Cultivation | technique_cultivation |
| Support | technique_support |
| Curse/Poison | technique_curse |
| Sensory | technique_sensory |
| Formation | technique_formation |
| Poison | technique_poison |

---

## 🔗 Подробнее

Полная документация: [13_SpriteSetup.md](./13_SpriteSetup.md)

---

*Дата: 2026-04-03*
*Обновлено: 2026-04-11 15:43:14 UTC — количество 57→66, добавлен Poison, обновлён маппинг техник*
