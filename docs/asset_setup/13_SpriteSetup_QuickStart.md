# 🎨 Быстрая настройка спрайтов — Quick Start

**Создано:** 57 спрайтов в `Assets/Sprites/`

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
├── Elements/      (8)   ← Стихии: fire, water, earth...
├── Techniques/    (8)   ← Типы: melee, ranged, defense...
├── Items/         (15)  ← Материалы + расходники
├── Equipment/     (21)  ← Оружие + броня
├── UI/            (4)   ← health, qi, stamina, cultivation
└── player_sprite.png     ← Персонаж
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

---

## 🔗 Подробнее

Полная документация: [13_SpriteSetup.md](./13_SpriteSetup.md)

---

*Дата: 2026-04-03*
