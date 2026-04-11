# 📋 FormationData — Настройка данных формаций

Эта инструкция описывает создание FormationData assets для системы формаций.

---

## 📊 Обзор

| Параметр | Описание |
|----------|----------|
| Количество формаций | 24 базовых + вариации |
| Типы формаций | 8 (Barrier, Trap, Amplification, Suppression, Gathering, Detection, Teleportation, Summoning) |
| Уровни | 1-9 |
| Размеры | 5 (Small, Medium, Large, Great, Heavy) |

---

## 🗂️ Структура папок

```
Assets/Data/Formations/
├── Barrier/
│   ├── F_Barrier_L1_Small.asset
│   ├── F_Barrier_L2_Small.asset
│   └── ...
├── Trap/
├── Amplification/
├── Suppression/
├── Gathering/
├── Detection/
├── Teleportation/
└── Summoning/
```

---

## 🔧 Создание FormationData (вручную)

### Шаг 1: Создать папки

В Project окне:
```
Assets → Create → Folder → "Data"
Assets/Data → Create → Folder → "Formations"
Assets/Data/Formations → Create → Folder → "Barrier"
... (повторить для каждого типа)
```

### Шаг 2: Создать asset

1. Правый клик в папке типа формации
2. Create → Cultivation → Formation Data
3. Назвать файл по шаблону: `F_{Type}_L{Level}_{Size}.asset`

### Шаг 3: Заполнить поля

#### Identity

| Поле | Описание | Пример |
|------|----------|--------|
| formationId | Уникальный ID | `barrier_l1_small` |
| displayName | Название (RU) | `Малый барьер` |
| nameEn | Название (EN) | `Small Barrier` |
| description | Описание | `Защитный барьер малого размера...` |
| icon | Спрайт | Перетащить из Assets/Icons/ |

#### Classification

| Поле | Значения |
|------|----------|
| formationType | Barrier, Trap, Amplification, Suppression, Gathering, Detection, Teleportation, Summoning |
| size | Small, Medium, Large, Great, Heavy |
| level | 1-9 |
| element | Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison |

#### Qi Costs

| Поле | Описание | Формула |
|------|----------|---------|
| contourQiOverride | Переопределение стоимости контура | 0 = авто: `80 × 2^(level-1)` |
| drawTime | Время прорисовки (сек) | 5-60 |
| cooldown | Кулдаун между использованиями | 60-600 |

#### Area

| Поле | Описание | Пример |
|------|----------|--------|
| creationRadius | Радиус прорисовки (м) | 10 |
| effectRadius | Радиус действия (м) | 50 |
| height | Высота (для 3D) | 5 |

#### Duration

| Поле | Описание |
|------|----------|
| requiresCore | Требует физическое ядро? |
| isPermanent | Постоянная (только с ядром) |
| baseDuration | Базовая длительность (сек) |

#### Effects

| Поле | Описание |
|------|----------|
| allyEffects | Эффекты на союзников |
| enemyEffects | Эффекты на врагов |
| effectTickInterval | Интервал применения эффектов |
| applyOnEnter | Применять при входе в зону |
| removeOnExit | Снимать при выходе из зоны |

#### Formation Effect

Для каждого эффекта:

| Поле | Описание | Значения |
|------|----------|----------|
| effectType | Тип эффекта | Buff, Debuff, Damage, Heal, Control, Shield, Summon |
| buffType | Тип баффа | Damage, Defense, Speed, CriticalChance, etc. |
| value | Значение | 10-100 |
| isPercentage | В процентах? | ✓ / ✗ |
| tickValue | Периодическое значение | 0-100 |
| tickInterval | Интервал тиков (сек) | 0-10 (float) |
| controlType | Тип контроля | Freeze, Slow, Root, Stun, Silence, Blind |
| controlDuration | Длительность контроля | 1-10 |

---

## 📐 Примеры формаций

### Barrier (Барьер)

```
formationId: barrier_l2_medium
displayName: Средний барьер
formationType: Barrier
size: Medium
level: 2

contourQi: 160 (авто: 80 × 2^(2-1))
contourQiOverride: 0 (авто)
capacity: 8000 (160 × 50)
effectRadius: 100m
baseDuration: 300s

allyEffects:
  - effectType: Shield
    value: 500
    isPercentage: false
```

### Trap (Ловушка)

```
formationId: trap_l3_small
displayName: Малая ловушка
formationType: Trap
size: Small
level: 3

contourQi: 320 (авто: 80 × 2^(3-1))
contourQiOverride: 0 (авто)
capacity: 3200 (320 × 10)
effectRadius: 20m
baseDuration: 600s

enemyEffects:
  - effectType: Damage
    tickValue: 50
    tickInterval: 2
  - effectType: Control
    controlType: Slow
    controlDuration: 5
```

### Amplification (Усиление)

```
formationId: amplification_l4_large
displayName: Большое усиление
formationType: Amplification
size: Large
level: 4

contourQi: 640 (авто)
contourQiOverride: 0 (авто)
capacity: 128000 (640 × 200)
effectRadius: 200m
baseDuration: 180s

allyEffects:
  - effectType: Buff
    buffType: Damage
    value: 25
    isPercentage: true
  - effectType: Buff
    buffType: Speed
    value: 15
    isPercentage: true
```

---

## 🔑 Ключевые формулы

### Стоимость контура
```
contourQi = 80 × 2^(level-1)
contourQiOverride: long (0 = auto, >0 = override)
```

### Ёмкость формации
```
capacity = contourQi × sizeMultiplier

sizeMultiplier:
- Small: 10
- Medium: 50
- Large: 200
- Great: 1000
- Heavy: 10000
```

### Интервал утечки
```
drainInterval (тики):
- L1-2: 60 (каждый час)
- L3-4: 40 (каждые 40 мин)
- L5-6: 20 (каждые 20 мин)
- L7-8: 10 (каждые 10 мин)
- L9: 5 (каждые 5 мин)
```

### Количество утечки
```
drainAmount (Ци/тик):
- Small: 1
- Medium: 3
- Large: 10
- Great: 30
- Heavy: 100
```

---

## 🎯 Рекомендации по балансу

### По уровню

| Уровень | Contour Qi | Сложность |
|---------|------------|-----------|
| 1-2 | 80-160 | Начальные |
| 3-4 | 320-640 | Средние |
| 5-6 | 1280-2560 | Продвинутые |
| 7-8 | 5120-10240 | Мастерские |
| 9 | 20480 | Легендарные |

### По типу

| Тип | Рекомендуемый уровень | Особенности |
|-----|----------------------|-------------|
| Barrier | 1-9 | Защита, длительные |
| Trap | 2-6 | Урон, скрытые |
| Amplification | 3-7 | Баффы, временные |
| Suppression | 4-8 | Дебаффы, область |
| Gathering | 3-7 | Ресурсы, пассивные |
| Detection | 1-5 | Разведка, дальние |
| Teleportation | 5-9 | Перемещение, дорогие |
| Summoning | 6-9 | Призыв, мощные |

---

## ✅ Чеклист создания

- [ ] Создать папки для каждого типа формации
- [ ] Создать FormationData assets (минимум 3 на тип)
- [ ] Заполнить все обязательные поля
- [ ] Добавить иконки
- [ ] Настроить эффекты
- [ ] Проверить формулы расчёта
- [ ] Протестировать в игре

---

*Инструкция создана: 2026-04-03*
*Обновлено: 2026-04-11 16:10:00 UTC — contourQiOverride int→long, tickInterval int→float*
*Версия: 1.1*
