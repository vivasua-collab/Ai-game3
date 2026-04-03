# 📋 FormationCoreData — Настройка ядер формаций

Эта инструкция описывает создание FormationCoreData assets для физических носителей формаций.

---

## 📊 Обзор

| Параметр | Описание |
|----------|----------|
| Типы ядер | 5 (Disk, Altar, Array, Totem, Seal) |
| Материалы | 7 (Stone, Jade, Iron, SpiritIron, Crystal, StarMetal, VoidMatter) |
| Уровни | 1-9 |
| Количество | ~30 базовых ядер |

---

## 🗂️ Структура папок

```
Assets/Data/FormationCores/
├── Disk/
│   ├── Core_Disk_L1_Stone.asset
│   ├── Core_Disk_L2_Jade.asset
│   └── ...
├── Altar/
│   ├── Core_Altar_L5_Stone.asset
│   └── ...
├── Array/
├── Totem/
└── Seal/
```

---

## 🔧 Типы ядер

### Disk (Диск) — портативный
- **Уровни:** L1-L6
- **Переносимость:** Да
- **Макс. слотов:** 3
- **Использование:** Мобильные формации

### Altar (Алтарь) — стационарный
- **Уровни:** L5-L9
- **Переносимость:** Нет
- **Макс. слотов:** 12
- **Использование:** Постоянные формации, базы

### Array (Массив) — напольный
- **Уровни:** L3-L8
- **Переносимость:** Нет (временно)
- **Макс. слотов:** 6
- **Использование:** Оборонительные формации

### Totem (Тотем)
- **Уровни:** L2-L7
- **Переносимость:** Да (ограниченно)
- **Макс. слотов:** 4
- **Использование:** Племенные формации

### Seal (Печать)
- **Уровни:** L4-L9
- **Переносимость:** Да (одноразово)
- **Макс. слотов:** 2
- **Использование:** Запечатывающие формации

---

## 🔧 Создание FormationCoreData (вручную)

### Шаг 1: Создать asset

1. Правый клик в папке `FormationCores/{Type}/`
2. Create → Cultivation → Formation Core
3. Назвать: `Core_{Type}_L{Level}_{Material}.asset`

### Шаг 2: Заполнить поля

#### Basic Info

| Поле | Описание | Пример |
|------|----------|--------|
| coreId | Уникальный ID | `disk_l2_jade` |
| nameRu | Название (RU) | `Нефритовый диск 2-го уровня` |
| nameEn | Название (EN) | `Jade Disk L2` |
| description | Описание | `Портативное ядро...` |
| icon | Спрайт | Перетащить из Assets/Icons/ |

#### Classification

| Поле | Значения |
|------|----------|
| coreType | Disk, Altar, Array, Totem, Seal |
| variant | Stone, Jade, Iron, SpiritIron, Crystal, StarMetal, VoidMatter |
| rarity | Common, Uncommon, Rare, Epic, Legendary, Mythic |

#### Capacity

| Поле | Описание | Пример |
|------|----------|--------|
| levelMin | Мин. уровень формации | 1 |
| levelMax | Макс. уровень формации | 6 |
| maxSlots | Макс. слотов для камней Ци | 3 |
| baseConductivity | Базовая проводимость | 10-100 |
| maxCapacity | Макс. ёмкость | 10000-1000000 |

#### Formation Types

| Поле | Описание |
|------|----------|
| supportedTypes | Поддерживаемые типы формаций (пусто = все) |
| recommendedType | Рекомендуемый тип |

#### Size

| Поле | Описание |
|------|----------|
| supportedSizes | Поддерживаемые размеры (пусто = все) |
| maxSize | Максимальный размер |

#### Requirements

| Поле | Описание | Пример |
|------|----------|--------|
| minCultivationLevel | Мин. уровень культивации | 1-9 |
| minConductivity | Мин. проводимость владельца | 0.5-5.0 |
| minFormationKnowledge | Мин. знания формаций | 0-100 |

#### Durability

| Поле | Описание | Пример |
|------|----------|--------|
| maxDurability | Макс. прочность | 100-1000 |
| durabilityPerActivation | Расход при активации | 1-10 |
| durabilityPerHour | Расход в час | 0-5 |

---

## 📐 Примеры ядер

### Disk L2 Jade

```
coreId: disk_l2_jade
nameRu: Нефритовый диск 2-го уровня
coreType: Disk
variant: Jade
rarity: Uncommon

levelMin: 1
levelMax: 6
maxSlots: 3
baseConductivity: 25
maxCapacity: 50000

supportedTypes: [] (все)
maxSize: Medium

minCultivationLevel: 2
minConductivity: 1.0

maxDurability: 200
durabilityPerActivation: 1
```

### Altar L7 StarMetal

```
coreId: altar_l7_starmetal
nameRu: Звёздно-металлический алтарь 7-го уровня
coreType: Altar
variant: StarMetal
rarity: Legendary

levelMin: 5
levelMax: 9
maxSlots: 12
baseConductivity: 80
maxCapacity: 5000000

supportedTypes: [Barrier, Amplification, Suppression, Summoning]
maxSize: Great

minCultivationLevel: 7
minConductivity: 3.0
minFormationKnowledge: 50

maxDurability: 2000
durabilityPerActivation: 0
durabilityPerHour: 1
```

---

## 🔑 Материалы и свойства

| Материал | Проводимость | Прочность | Редкость |
|----------|--------------|-----------|----------|
| Stone | 1x | 1x | Common |
| Jade | 1.5x | 0.8x | Uncommon |
| Iron | 1.2x | 1.5x | Uncommon |
| SpiritIron | 2x | 1.2x | Rare |
| Crystal | 3x | 0.5x | Epic |
| StarMetal | 4x | 2x | Legendary |
| VoidMatter | 5x | 3x | Mythic |

---

## 🎯 Рекомендации по созданию

### Минимальный набор

1. **Disk L1-L6** (по одному на уровень)
   - Материалы: Stone → Jade → SpiritIron
   - Для начальных и средних игроков

2. **Altar L5-L9** (по одному на уровень)
   - Материалы: Stone → Iron → StarMetal
   - Для продвинутых игроков и баз

### Расширенный набор

3. **Array L3-L8** (3-5 шт)
4. **Totem L2-L7** (3-5 шт)
5. **Seal L4-L9** (3-5 шт)

---

## ✅ Чеклист создания

- [ ] Создать папки для каждого типа ядра
- [ ] Создать FormationCoreData assets
- [ ] Заполнить все обязательные поля
- [ ) Добавить иконки
- [ ] Настроить требования
- [ ] Проверить совместимость с формациями
- [ ] Протестировать размещение

---

*Инструкция создана: 2026-04-03*
*Версия: 1.0*
