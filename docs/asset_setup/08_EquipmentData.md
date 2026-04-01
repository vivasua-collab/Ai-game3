# Настройка EquipmentData

**Путь:** `Assets/Data/Equipment/`

**Создание:** 
- Оружие: `Create → Cultivation → Equipment` (тип EquipmentData)
- Броня: `Create → Cultivation → Equipment` (тип EquipmentData)

---

## Структура полей (из ItemData.cs → EquipmentData)

### Basic Info (наследуется от ItemData)
| Поле | Тип | Описание |
|------|-----|----------|
| itemId | string | Уникальный ID |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание |
| icon | Sprite | Иконка |
| stackable | bool | false для экипировки |
| maxStack | int | 1 |
| weight | float | Вес (кг) |
| value | int | Стоимость (духовные камни) |

### Equipment
| Поле | Тип | Описание |
|------|-----|----------|
| slot | EquipmentSlot | Слот экипировки |
| damage | int | Урон (для оружия) |
| defense | int | Защита (для брони) |
| coverage | float (%) | Покрытие брони |
| damageReduction | float (%) | Снижение урона |
| dodgeBonus | float (%) | Бонус к уклонению |
| materialId | string | ID материала |
| materialTier | int (1-5) | Тир материала |
| grade | EquipmentGrade | Damaged, Common, Refined, Perfect, Transcendent |
| itemLevel | int (1-9) | Уровень предмета |

### Requirements
| Поле | Тип | Описание |
|------|-----|----------|
| requiredCultivationLevel | int | Мин. уровень культивации |
| statRequirements | List | Требования к характеристикам |

### Bonuses
| Поле | Тип | Описание |
|------|-----|----------|
| statBonuses | List | Бонусы к характеристикам |
| specialEffects | List | Особые эффекты |

---

## Enums

### EquipmentSlot
| Слот | Описание |
|------|----------|
| weapon_main | Основное оружие |
| weapon_off | Вторичное оружие |
| weapon_twohanded | Двуручное оружие |
| head_armor | Шлем (броня) |
| head_clothing | Головной убор (одежда) |
| torso_armor | Нагрудник (броня) |
| torso_clothing | Роба/туника (одежда) |
| legs_armor | Поножи (броня) |
| legs_clothing | Штаны (одежда) |
| feet_armor | Сабатоны (броня) |
| feet_clothing | Обувь (одежда) |
| hands_armor | Перчатки (броня) |
| hands_clothing | Перчатки (одежда) |
| amulet | Амулет |
| ring_left_1 | Кольцо левое 1 |
| ring_left_2 | Кольцо левое 2 |
| ring_right_1 | Кольцо правое 1 |
| ring_right_2 | Кольцо правое 2 |
| charger | Зарядник |

### EquipmentGrade
| Grade | Множитель | Цвет |
|-------|-----------|------|
| Damaged | ×0.5 | Красный |
| Common | ×1.0 | Белый |
| Refined | ×1.4 | Зелёный |
| Perfect | ×2.0 | Синий |
| Transcendent | ×3.0 | Золотой |

### WeaponType
| Тип | Описание |
|-----|----------|
| unarmed | Без оружия / когти |
| dagger | Кинжал |
| sword | Меч |
| greatsword | Двуручный меч |
| axe | Топор / молот |
| spear | Копьё |
| bow | Лук / арбалет |
| staff | Посох |

### DamageType
| Тип | Описание |
|-----|----------|
| Blunt | Дробящий |
| Slashing | Рубящий |
| Piercing | Колющий |

---

## Оружие 1: Кулаки

**Имя файла:** `Weapon_Fists`

```
=== Basic Info ===
itemId: weapon_unarmed_fists
nameRu: Кулаки
nameEn: Fists
description: Собственные кулаки. Базовое оружие без требований.
stackable: false
maxStack: 1
weight: 0.0
value: 0

=== Equipment ===
slot: hands
damage: 2 (1-3 range)
defense: 0
coverage: 0
damageReduction: 0
dodgeBonus: 0
materialId: null
materialTier: 0
grade: Common
itemLevel: 1

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)

=== Bonuses ===
statBonuses: (пусто)
specialEffects: (пусто)
```

---

## Оружие 2: Железный меч

**Имя файла:** `Weapon_IronSword`

```
=== Basic Info ===
itemId: weapon_sword_iron
nameRu: Железный меч
nameEn: Iron Sword
description: Надёжный железный меч. Базовое оружие воина.
stackable: false
maxStack: 1
weight: 2.5
value: 50

=== Equipment ===
slot: weapon_main
damage: 11 (8-14 range)
defense: 0
coverage: 0
damageReduction: 0
dodgeBonus: 0
materialId: iron
materialTier: 1
grade: Common
itemLevel: 1

=== Requirements ===
requiredCultivationLevel: 0
statRequirements:
  - statName: strength, minValue: 10
  - statName: agility, minValue: 8

=== Bonuses ===
statBonuses: (пусто)
specialEffects: (пусто)
```

---

## Оружие 3: Духовный меч

**Имя файла:** `Weapon_SpiritSword`

```
=== Basic Info ===
itemId: weapon_sword_spirit
nameRu: Духовный меч
nameEn: Spirit Sword
description: Меч из духовного железа. Проводит Ци и усиливает техники.
stackable: false
maxStack: 1
weight: 2.0
value: 850

=== Equipment ===
slot: weapon_main
damage: 24 (18-30 range)
defense: 0
coverage: 0
damageReduction: 0
dodgeBonus: 0
materialId: spirit_iron
materialTier: 3
grade: Common
itemLevel: 3

=== Requirements ===
requiredCultivationLevel: 3
statRequirements:
  - statName: strength, minValue: 25
  - statName: agility, minValue: 20

=== Bonuses ===
statBonuses:
  - statName: qiConductivity, bonus: 10, isPercentage: true
specialEffects:
  - effectName: Техники усилены, description: +10% к урону техник, triggerChance: 100
```

---

## Броня 1: Тканевая роба

**Имя файла:** `Armor_ClothRobe`

```
=== Basic Info ===
itemId: armor_torso_cloth_robe
nameRu: Тканевая роба
nameEn: Cloth Robe
description: Простая роба из ткани. Не мешает циркуляции Ци.
stackable: false
maxStack: 1
weight: 0.5
value: 20

=== Equipment ===
slot: torso_clothing
damage: 0
defense: 3
coverage: 60
damageReduction: 0
dodgeBonus: 0
materialId: cloth
materialTier: 1
grade: Common
itemLevel: 1

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)

=== Bonuses ===
statBonuses: (пусто)
specialEffects: (пусто)
```

---

## Броня 2: Духовная роба

**Имя файла:** `Armor_SpiritRobe`

```
=== Basic Info ===
itemId: armor_torso_spirit_robe
nameRu: Духовная роба
nameEn: Spirit Robe
description: Роба из духовного шёлка. Усиливает проводимость Ци и защищает.
stackable: false
maxStack: 1
weight: 0.4
value: 650

=== Equipment ===
slot: torso_clothing
damage: 0
defense: 15
coverage: 75
damageReduction: 8
dodgeBonus: 0
materialId: spirit_silk
materialTier: 3
grade: Common
itemLevel: 3

=== Requirements ===
requiredCultivationLevel: 3
statRequirements:
  - statName: strength, minValue: 5
  - statName: agility, minValue: 10

=== Bonuses ===
statBonuses:
  - statName: qiConductivity, bonus: 10, isPercentage: true
specialEffects: (пусто)
```

---

## Броня 3: Железный нагрудник

**Имя файла:** `Armor_IronBreastplate`

```
=== Basic Info ===
itemId: armor_torso_iron_plate
nameRu: Железный нагрудник
nameEn: Iron Breastplate
description: Железный нагрудник. Тяжёлая, но надёжная защита.
stackable: false
maxStack: 1
weight: 10.0
value: 220

=== Equipment ===
slot: torso_armor
damage: 0
defense: 25
coverage: 80
damageReduction: 15
dodgeBonus: -15
materialId: iron
materialTier: 1
grade: Common
itemLevel: 1

=== Requirements ===
requiredCultivationLevel: 0
statRequirements:
  - statName: strength, minValue: 16

=== Bonuses ===
statBonuses: (пусто)
specialEffects: (пусто)
```

---

## Сводная таблица оружия (20 предметов)

| ID | Название | Тип | Урон | Слот | Вес |
|----|----------|-----|------|------|-----|
| weapon_unarmed_fists | Кулаки | unarmed | 1-3 | hands | 0 |
| weapon_unarmed_claws | Когти | unarmed | 3-6 | hands_armor | 0.3 |
| weapon_dagger_iron | Железный кинжал | dagger | 4-8 | weapon_main/off | 0.5 |
| weapon_dagger_dragon | Кинжал из кости дракона | dagger | 8-15 | weapon_main/off | 0.4 |
| weapon_sword_iron | Железный меч | sword | 8-14 | weapon_main | 2.5 |
| weapon_sword_steel | Стальной меч | sword | 12-20 | weapon_main | 2.3 |
| weapon_sword_spirit | Духовный меч | sword | 18-30 | weapon_main | 2.0 |
| weapon_greatsword_iron | Железный двуручник | greatsword | 18-30 | weapon_twohanded | 6.0 |
| weapon_greatsword_starmetal | Звёздный двуручник | greatsword | 35-55 | weapon_twohanded | 5.5 |
| weapon_axe_iron | Железный топор | axe | 10-18 | weapon_main | 3.5 |
| weapon_axe_spirit | Духовный топор | axe | 20-35 | weapon_main | 3.2 |
| weapon_spear_iron | Железное копьё | spear | 12-20 | weapon_twohanded | 3.0 |
| weapon_spear_dragon | Копьё из кости дракона | spear | 25-40 | weapon_twohanded | 2.5 |
| weapon_bow_wood | Деревянный лук | bow | 6-12 | weapon_twohanded | 1.2 |
| weapon_bow_spiritwood | Лук из духовного дерева | bow | 15-28 | weapon_twohanded | 1.0 |
| weapon_staff_wood | Деревянный посох | staff | 3-7 | weapon_twohanded | 1.5 |
| weapon_staff_jade | Нефритовый посох | staff | 8-15 | weapon_twohanded | 1.8 |
| weapon_maul_iron | Железный молот | axe | 16-28 | weapon_twohanded | 7.0 |
| weapon_whip_leather | Кожаный хлыст | dagger | 4-10 | weapon_main | 0.8 |
| weapon_crossbow_iron | Железный арбалет | bow | 15-25 | weapon_twohanded | 4.0 |

---

## Сводная таблица брони (19 предметов)

| ID | Название | Слот | Защита | Покрытие | Вес |
|----|----------|------|--------|----------|-----|
| armor_helmet_iron | Железный шлем | head_armor | 12 | 75% | 2.5 |
| armor_helmet_spirit | Духовный шлем | head_armor | 20 | 85% | 2.0 |
| armor_hood_cloth | Тканевый капюшон | head_clothing | 2 | 40% | 0.2 |
| armor_torso_cloth_robe | Тканевая роба | torso_clothing | 3 | 60% | 0.5 |
| armor_torso_leather_vest | Кожаный жилет | torso_armor | 10 | 70% | 2.0 |
| armor_torso_iron_plate | Железный нагрудник | torso_armor | 25 | 80% | 10.0 |
| armor_torso_spirit_robe | Духовная роба | torso_clothing | 15 | 75% | 0.4 |
| armor_torso_chainmail | Кольчуга | torso_armor | 18 | 85% | 6.0 |
| armor_legs_cloth_pants | Тканевые штаны | legs_clothing | 2 | 50% | 0.4 |
| armor_legs_leather | Кожаные поножи | legs_armor | 8 | 55% | 1.5 |
| armor_legs_iron_greaves | Железные поножи | legs_armor | 15 | 65% | 5.0 |
| armor_feet_cloth_shoes | Тканевые туфли | feet_clothing | 1 | 30% | 0.2 |
| armor_feet_leather_boots | Кожаные сапоги | feet_armor | 5 | 45% | 0.8 |
| armor_feet_iron_sabatons | Железные сабатоны | feet_armor | 10 | 55% | 3.0 |
| armor_hands_cloth_gloves | Тканевые перчатки | hands_clothing | 1 | 35% | 0.1 |
| armor_hands_leather_gloves | Кожаные перчатки | hands_armor | 4 | 45% | 0.3 |
| armor_hands_iron_gauntlets | Железные перчатки | hands_armor | 8 | 55% | 1.5 |
| armor_hands_spirit_gloves | Духовные перчатки | hands_armor | 12 | 65% | 0.5 |
| armor_full_iron | Железный доспех | torso_armor | 35 | 90% | 18.0 |

---

## Важные правила

1. **Слои экипировки:** Armor и Clothing НЕ конфликтуют
2. **Двуручное оружие** занимает слот `weapon_twohanded`
3. **Кинжалы** можно использовать в обеих руках
4. **qiFlowPenalty** — штраф к проводимости Ци (отрицательный = бонус)
5. **coverage** — процент защиты части тела
6. **spirit_ материалы** дают бонус к проводимости Ци

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/equipment.json*
