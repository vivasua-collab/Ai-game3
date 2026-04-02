# Настройка ItemData

**Путь:** `Assets/Data/Items/`

**Создание:** `Create → Cultivation → Item`

---

## Структура полей (из ItemData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| itemId | string | Уникальный ID |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание |
| icon | Sprite | Иконка |

### Classification
| Поле | Тип | Описание |
|------|-----|----------|
| category | ItemCategory | Категория (Consumable, Technique, Misc) |
| itemType | string | Тип (pill, food, medicine, scroll) |
| rarity | ItemRarity | Редкость |

### Stacking
| Поле | Тип | Описание |
|------|-----|----------|
| stackable | bool | Можно стакать |
| maxStack | int | Максимум в стаке |

### Size
| Поле | Тип | Описание |
|------|-----|----------|
| sizeWidth | int (1-2) | Ширина в сетке |
| sizeHeight | int (1-3) | Высота в сетке |

### Physical
| Поле | Тип | Описание |
|------|-----|----------|
| weight | float | Вес (кг) |
| value | int | Стоимость (духовные камни) |

### Durability
| Поле | Тип | Описание |
|------|-----|----------|
| hasDurability | bool | Имеет прочность |
| maxDurability | int | Макс. прочность |

### Effects
| Поле | Тип | Описание |
|------|-----|----------|
| effects | List<ItemEffect> | Эффекты при использовании |

### Requirements
| Поле | Тип | Описание |
|------|-----|----------|
| requiredCultivationLevel | int (0-10) | Мин. уровень культивации |
| statRequirements | List | Требования к характеристикам |

---

## Enums

### ItemCategory
| Категория | Описание |
|-----------|----------|
| Consumable | Расходники |
| Technique | Свитки техник |
| Misc | Разное |

### ItemRarity
| Rarity | Цвет | Шанс дропа |
|--------|------|------------|
| Common | Белый | 50% |
| Uncommon | Зелёный | 30% |
| Rare | Синий | 15% |
| Epic | Фиолетовый | 4% |
| Legendary | Оранжевый | 1% |
| Mythic | Золотой | 0.1% |

### ItemEffect (helper class)
| Поле | Тип | Описание |
|------|-----|----------|
| effectType | string | Тип эффекта (heal, restore_qi, reduce_fatigue, etc.) |
| value | float | Значение эффекта |
| duration | int | Длительность (0 = мгновенно, >0 = секунды) |

---

## Расходник 1: Лечебная пилюля

**Имя файла:** `Item_HealingPill`

```
=== Basic Info ===
itemId: item_healing_pill
nameRu: Лечебная пилюля
nameEn: Healing Pill
description: Базовая пилюля для восстановления здоровья. Восстанавливает 20 HP.

=== Classification ===
category: Consumable
itemType: pill
rarity: Common

=== Stacking ===
stackable: true
maxStack: 99

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.05
value: 50

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: heal, value: 20, duration: 0

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)
```

---

## Расходник 2: Пилюля Ци

**Имя файла:** `Item_QiPill`

```
=== Basic Info ===
itemId: item_qi_pill
nameRu: Пилюля Ци
nameEn: Qi Pill
description: Пилюля для восстановления Ци. Восполняет 100 единиц Ци.

=== Classification ===
category: Consumable
itemType: pill
rarity: Common

=== Stacking ===
stackable: true
maxStack: 99

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.05
value: 80

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: restore_qi, value: 100, duration: 0

=== Requirements ===
requiredCultivationLevel: 1
statRequirements: (пусто)
```

---

## Расходник 3: Пилюля выносливости

**Имя файла:** `Item_StaminaPill`

```
=== Basic Info ===
itemId: item_stamina_pill
nameRu: Пилюля выносливости
nameEn: Stamina Pill
description: Восстанавливает физические силы. Снимает усталость на 30%.

=== Classification ===
category: Consumable
itemType: pill
rarity: Common

=== Stacking ===
stackable: true
maxStack: 99

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.05
value: 40

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: reduce_fatigue, value: 30, duration: 0

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)
```

---

## Расходник 4: Пилюля прорыва

**Имя файла:** `Item_BreakthroughPill`

```
=== Basic Info ===
itemId: item_breakthrough_pill
nameRu: Пилюля прорыва
nameEn: Breakthrough Pill
description: Помогает при прорыве уровня. Увеличивает шанс успешного прорыва на 20%.

=== Classification ===
category: Consumable
itemType: pill
rarity: Rare

=== Stacking ===
stackable: true
maxStack: 20

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.1
value: 500

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: breakthrough_bonus, value: 20, duration: 60

=== Requirements ===
requiredCultivationLevel: 1
statRequirements:
  - statName: intelligence, minValue: 10
```

---

## Расходник 5: Хлеб

**Имя файла:** `Item_Bread`

```
=== Basic Info ===
itemId: item_food_bread
nameRu: Хлеб
nameEn: Bread
description: Простой хлеб. Утоляет голод.

=== Classification ===
category: Consumable
itemType: food
rarity: Common

=== Stacking ===
stackable: true
maxStack: 20

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.3
value: 5

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: reduce_hunger, value: 30, duration: 0

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)
```

---

## Расходник 6: Мясо

**Имя файла:** `Item_Meat`

```
=== Basic Info ===
itemId: item_food_meat
nameRu: Мясо
nameEn: Meat
description: Приготовленное мясо. Хорошо насыщает.

=== Classification ===
category: Consumable
itemType: food
rarity: Common

=== Stacking ===
stackable: true
maxStack: 20

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.5
value: 15

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: reduce_hunger, value: 60, duration: 0

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)
```

---

## Расходник 7: Противоядие

**Имя файла:** `Item_Antidote`

```
=== Basic Info ===
itemId: item_antidote
nameRu: Противоядие
nameEn: Antidote
description: Снимает большинство отравлений.

=== Classification ===
category: Consumable
itemType: medicine
rarity: Uncommon

=== Stacking ===
stackable: true
maxStack: 30

=== Size ===
sizeWidth: 1
sizeHeight: 1

=== Physical ===
weight: 0.1
value: 100

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: remove_poison, value: 1, duration: 0

=== Requirements ===
requiredCultivationLevel: 0
statRequirements: (пусто)
```

---

## Свиток: Свиток техники

**Имя файла:** `Item_TechniqueScroll`

```
=== Basic Info ===
itemId: scroll_technique_common
nameRu: Свиток техники
nameEn: Technique Scroll
description: Свиток с описанием техники. Позволяет изучить новую технику.

=== Classification ===
category: Technique
itemType: scroll
rarity: Uncommon

=== Stacking ===
stackable: false
maxStack: 1

=== Size ===
sizeWidth: 1
sizeHeight: 2

=== Physical ===
weight: 0.2
value: 200

=== Durability ===
hasDurability: false
maxDurability: 0

=== Effects ===
effects:
  - effectType: learn_technique, value: 1, duration: 0

=== Requirements ===
requiredCultivationLevel: 1
statRequirements: (пусто)
```

---

## Сводная таблица расходников (7 предметов)

| ID | Название | Тип | Эффект | Значение | Стак | Вес | Цена |
|----|----------|-----|--------|----------|------|-----|------|
| item_healing_pill | Лечебная пилюля | pill | heal | 20 HP | 99 | 0.05 | 50 |
| item_qi_pill | Пилюля Ци | pill | restore_qi | 100 Ци | 99 | 0.05 | 80 |
| item_stamina_pill | Пилюля выносливости | pill | reduce_fatigue | 30% | 99 | 0.05 | 40 |
| item_breakthrough_pill | Пилюля прорыва | pill | breakthrough_bonus | +20% (60 сек) | 20 | 0.1 | 500 |
| item_food_bread | Хлеб | food | reduce_hunger | 30 | 20 | 0.3 | 5 |
| item_food_meat | Мясо | food | reduce_hunger | 60 | 20 | 0.5 | 15 |
| item_antidote | Противоядие | medicine | remove_poison | 1 | 30 | 0.1 | 100 |

---

## Сводная таблица свитков (1 предмет)

| ID | Название | Размер | Вес | Цена |
|----|----------|--------|-----|------|
| scroll_technique_common | Свиток техники | 1×2 | 0.2 | 200 |

---

## Типы эффектов (effectType)

| Тип | Описание | duration |
|-----|----------|----------|
| heal | Восстановление HP | 0 |
| restore_qi | Восстановление Ци | 0 |
| reduce_fatigue | Снижение усталости (%) | 0 |
| reduce_hunger | Снижение голода | 0 |
| remove_poison | Снятие отравления | 0 |
| breakthrough_bonus | Бонус к прорыву (%) | >0 (секунды) |
| learn_technique | Изучение техники | 0 |

---

## Важные правила

1. **Стакаемые предметы** (stackable=true) не могут иметь уникальных модификаторов
2. **duration=0** означает мгновенный эффект
3. **duration>0** означает эффект на время (в секундах)
4. **Размер 1×2** для свитков (вертикальный формат)
5. **requiredCultivationLevel=0** доступен с начала игры

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/items.json*
