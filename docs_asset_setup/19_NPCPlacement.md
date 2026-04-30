# Настройка размещения NPC (Вручную)

**Категория:** NPC
**Зависимости:** Phase06Player (Player), Phase09GenerateAssets (NPCPresets .asset), Phase02TagsLayers (тег «NPC», слой «NPC»)
**Редактировано:** 2026-04-30 08:09:40 UTC

---

## Обзор

Фаза 19 размещает NPC на тестовой поляне. Каждый NPC — это GameObject с набором компонентов, инициализируемый через `NPCGenerator` / `GeneratorRegistry`. Существует два способа создания NPC:

1. **Автоматический** — `Tools → Full Scene Builder → Phase 19: NPC Placement`
2. **Ручной** — через горячие клавиши или меню `Tools/NPC/`
3. **Полностью ручной** — создание GameObject и настройка компонентов в Inspector

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание 7 NPC на тестовой поляне | ✅ Автоматически (Phase19) |
| Генерация данных NPC (имя, статы, отношение) | ✅ Автоматически (NPCGenerator) |
| Добавление NPCController + InitializeFromGenerated | ✅ Автоматически |
| Добавление NPCAI + настройка начального состояния | ✅ Автоматически |
| Добавление BodyController + QiController | ✅ Автоматически |
| Добавление TechniqueController | ✅ Автоматически |
| Добавление NPCVisual + SetSpriteByRole | ✅ Автоматически |
| Добавление NPCInteractable | ✅ Автоматически |
| Настройка Rigidbody2D (Dynamic, gravity=0) | ✅ Автоматически |
| Настройка CircleCollider2D (solid, radius 0.5) | ✅ Автоматически |
| Назначение тега «NPC» и слоя «NPC» | ✅ Автоматически |
| Настройка точек патруля для Guard | ✅ Автоматически |
| Undo-поддержка (откат через Ctrl+Z) | ✅ Автоматически |
| Добавление уникальных диалогов | ❌ Руками |
| Настройка торгового ассортимента для Merchant | ❌ Руками |
| Добавление кастомных точек патруля | ❌ Руками (см. Шаг 7) |
| Настройка faction / allegiance | ❌ Руками |

---

## Шаг 1: Быстрый спавн через горячие клавиши

### Спавн NPC рядом с Player

| Горячая клавиша | Действие |
|---|---|
| **Ctrl+N** | 1 случайный NPC рядом с Player |
| **Ctrl+Shift+N** | 5 NPC разных ролей рядом с Player |
| **Ctrl+F5** | 1 Merchant рядом с Player |
| **Ctrl+F6** | 1 Monster рядом с Player |

### Спавн через меню

`Tools → NPC → Spawn In Scene → ...`

| Пункт меню | Роль | Уровень культивации |
|---|---|---|
| Random NPC | Случайная (Passerby) | 0 |
| 5 Random NPCs | Merchant, Guard, Cultivator, Elder, Monster | 2, 3, 4, 5, 1 |
| Merchant | Merchant | 2 |
| Monster | Monster | 1 |
| Guard | Guard | 2 |
| Elder | Elder | 5 |
| Enemy | Enemy | 1 |
| Cultivator | Cultivator | 3 |
| Disciple | Disciple | 1 |

### Удаление NPC

| Пункт меню | Действие |
|---|---|
| `Tools/NPC/Clear All NPCs` | Удалить все NPC из сцены |

> Спавн через hotkey/меню создаёт NPC в радиусе 3 единиц от Player.
> NPC получают уровень культивации по умолчанию для каждой роли.

---

## Шаг 2: Автоматическое размещение через Phase 19

**Меню:** `Tools → Full Scene Builder → Phase 19: NPC Placement`

**Предусловие:** Фазы 02, 06, 09 уже выполнены.

**Результат:** Создаётся 7 NPC на фиксированных позициях:

| Роль | Уровень | Позиция (X, Y) | Локация |
|---|---|---|---|
| Merchant | 2 | (3, -2) | Центр деревни |
| Guard | 3 | (8, 0) | Вход |
| Guard | 2 | (-5, 3) | Патруль |
| Elder | 5 | (-3, -4) | Дом старейшины |
| Cultivator | 4 | (5, 5) | Площадка культивации |
| Monster | 1 | (-10, -8) | Окраина |
| Monster | 2 | (12, -10) | Окраина |

**IsNeeded():** Проверяет наличие объектов с тегом «NPC». Если на сцене уже есть NPC — фаза пропускается.

---

## Шаг 3: Полностью ручное создание NPC

Если нужно создать NPC с полной кастомизацией:

### 3.1 Создать GameObject

1. В Hierarchy → Правый клик → Create Empty
2. Назвать: `NPC_[Имя]` (например, `NPC_ТорговецЛи`)
3. Установить позицию в Inspector

### 3.2 Назначить тег и слой

- **Tag:** `NPC`
- **Layer:** `NPC` (если существует, иначе Default)

### 3.3 Добавить компоненты

Обязательные компоненты (в порядке добавления):

```
NPC_[Name]:
├── Transform (position, Z=0)
├── Rigidbody2D
│   ├── Body Type: Dynamic
│   ├── Gravity Scale: 0
│   ├── Freeze Rotation: ☑
│   └── Linear Damping: 5
├── CircleCollider2D (solid)
│   ├── Is Trigger: ☐
│   └── Radius: 0.5
├── NPCController
├── NPCAI
├── BodyController
├── QiController
├── TechniqueController
├── NPCVisual
└── NPCInteractable
```

---

## Шаг 4: Настройка NPCController

**Скрипт:** `Scripts/NPC/NPCController.cs`

### 4.1 Инициализация из GeneratedNPC (рекомендуется)

```csharp
// В Editor или через скрипт:
var generated = NPCGenerator.Generate(parameters, rng);
controller.InitializeFromGenerated(generated);
```

### 4.2 Поля NPCController (через Inspector после InitializeFromGenerated)

| Поле | Тип | Описание |
|---|---|---|
| npcName | string | Имя NPC (русское) |
| role | NPCRole | Роль (Merchant, Guard и т.д.) |
| cultivationLevel | int | Уровень культивации |
| baseAttitude | Attitude | Начальное отношение к Player |
| personalityFlags | PersonalityTrait | Черты характера (flags) |
| factionId | string | ID фракции |

### 4.3 Enum NPCRole

| Значение | Описание |
|---|---|
| Merchant | Торговец |
| Guard | Стражник |
| Elder | Старейшина |
| Cultivator | Культиватор |
| Disciple | Ученик |
| Monster | Монстр |
| Enemy | Враг |
| Passerby | Прохожий |

### 4.4 Enum Attitude (отношение к Player)

| Значение | Диапазон | Описание |
|---|---|---|
| Hatred | -100..-51 | Атака без предупреждения |
| Hostile | -50..-21 | Атака если спровоцирован |
| Unfriendly | -20..-10 | Избегание |
| Neutral | -9..9 | Безразличие |
| Friendly | 10..49 | Помощь, торговля |
| Allied | 50..79 | Лояльность |
| SwornAlly | 80..100 | Самопожертвование |

---

## Шаг 5: Настройка NPCAI

**Скрипт:** `Scripts/NPC/NPCAI.cs`

### Параметры AI (через SerializedObject)

| Поле | Тип | По умолчанию | Описание |
|---|---|---|---|
| decisionInterval | float | 1.0 | Интервал принятия решений (сек) |
| aggroRange | float | 10.0 | Дальность обнаружения врагов |
| fleeHealthThreshold | float | 0.2 | Порог HP для бегства (20%) |

### Начальные AI-состояния по роли

| Роль | Состояние | Описание поведения |
|---|---|---|
| Merchant | Trading | Стоит на месте, торгует |
| Guard | Patrolling | Патрулирует точки |
| Elder | Idle | Стоит, диалог |
| Cultivator | Cultivating | Медитирует |
| Disciple | Cultivating | Медитирует |
| Monster | Wandering | Случайно бродит |
| Enemy | Wandering | Случайно бродит |
| Passerby | Idle | Стоит или бродит |

### Enum NPCAIState

| Значение | Описание |
|---|---|
| Idle | Стоит на месте |
| Patrolling | Патрулирует между точками |
| Wandering | Случайное перемещение |
| Trading | Торговля с игроком |
| Cultivating | Медитация / культивация |
| Following | Следование за целью |
| Fleeing | Бегство от угрозы |
| Attacking | Атака цели |
| Dead | Мёртв |

---

## Шаг 6: Настройка NPCVisual

**Скрипт:** `Scripts/NPC/NPCVisual.cs`

### Назначение спрайта по роли

```csharp
visual.SetSpriteByRole(role);
```

Метод автоматически создаёт цветной круг-маркер по роли NPC.

### Цвета по роли (по умолчанию)

| Роль | Цвет | RGB |
|---|---|---|
| Merchant | Зелёный | (0.2, 0.8, 0.2) |
| Guard | Синий | (0.2, 0.4, 0.9) |
| Elder | Фиолетовый | (0.6, 0.2, 0.8) |
| Cultivator | Голубой | (0.2, 0.6, 0.9) |
| Disciple | Бирюзовый | (0.1, 0.7, 0.7) |
| Monster | Красный | (0.9, 0.2, 0.1) |
| Enemy | Тёмно-красный | (0.7, 0.1, 0.1) |
| Passerby | Серый | (0.6, 0.6, 0.6) |

### Sorting Layer

- **Sorting Layer:** `Objects`
- **Sorting Order:** `10`

> Для назначения кастомных спрайтов вместо кругов-маркеров:
> Выбрать NPC → NPCVisual → перетащить спрайт в поле `roleSprite`.

---

## Шаг 7: Настройка точек патруля (для Guard)

Guard автоматически получают 4 точки патруля (квадрат вокруг начальной позиции).

### Автоматические точки патруля

```
BasePos → (BasePos + 3, 0) → (BasePos + 3, 3) → (BasePos, 3) → BasePos → ...
```

### Ручная настройка точек патруля

1. Выбрать NPC Guard в Hierarchy
2. Найти компонент `NPCAI` в Inspector
3. Добавить точки патруля в массив `patrolPoints`

```csharp
// Через код:
var ai = controller.GetComponent<NPCAI>();
ai.SetPatrolPoints(new Vector3[] {
    new Vector3(0, 0, 0),
    new Vector3(5, 0, 0),
    new Vector3(5, 5, 0),
    new Vector3(0, 5, 0)
});
```

---

## Шаг 8: Настройка NPCInteractable

**Скрипт:** `Scripts/NPC/NPCInteractable.cs`

### Параметры взаимодействия

| Параметр | Значение по умолчанию | Описание |
|---|---|---|
| Interaction Radius | 1.5 | Дальность взаимодействия |
| Auto Interact | false | Автовзаимодействие при входе в зону |

### Назначение роли для interactable

```csharp
interactable.SetNPCRole(role);
```

> При приближении Player к NPC (в радиус 1.5) появится подсказка взаимодействия.

---

## Шаг 9: Изменение позиций NPC в Phase 19

Если нужно изменить позиции или состав NPC, размещаемых фазой 19:

1. Открыть файл: `Assets/Scripts/Editor/SceneBuilder/Phase19NPCPlacement.cs`
2. Найти массив `NPC_PLACEMENTS`
3. Изменить позиции, роли или уровни:

```csharp
private static readonly (NPCRole role, int level, float x, float y)[] NPC_PLACEMENTS =
{
    (NPCRole.Merchant,   2,   3f,  -2f),  // Центр деревни
    (NPCRole.Guard,      3,   8f,   0f),  // Вход
    (NPCRole.Guard,      2,  -5f,   3f),  // Патруль
    (NPCRole.Elder,      5,  -3f,  -4f),  // Дом старейшины
    (NPCRole.Cultivator, 4,   5f,   5f),  // Площадка культивации
    (NPCRole.Monster,    1, -10f,  -8f),  // Окраина
    (NPCRole.Monster,    2,  12f, -10f),  // Окраина
};
```

### Добавление нового NPC в фазу 19

Добавить строку в массив `NPC_PLACEMENTS`:

```csharp
(NPCRole.Disciple,    1,   0f,   5f),  // Новичок у входа
```

### Удаление NPC из фазы 19

Удалить соответствующую строку из массива.

---

## Шаг 10: Проверка работоспособности

### Автоматическая проверка (Phase 19)

1. `Tools → Full Scene Builder → Phase 19: NPC Placement`
2. В Console должно быть: `[Phase19] ✅ Размещение завершено: 7/7 NPC`

### Ручная проверка

1. Нажать **Play**
2. На сцене должны появиться цветные круги (NPC) в указанных позициях
3. Подойти к NPC — должна появиться подсказка взаимодействия
4. Guard должен патрулировать между 4 точками
5. Monster должен случайно бродить

### Проверка через горячие клавиши

1. Нажать **Ctrl+N** — 1 случайный NPC рядом с Player
2. Нажать **Ctrl+Shift+N** — 5 NPC разных ролей
3. В Console: `[NPCSpawner] Спавн: [Имя] ([Роль] L[Уровень]) поз.[X,Y,Z] Att=[Отношение]`

---

## Иерархия создаваемых объектов

```
MainScene
├── ...
├── NPC_ТорговецВан (Merchant L2)
│   ├── NPCController
│   ├── NPCAI (Trading)
│   ├── BodyController
│   ├── QiController
│   ├── TechniqueController
│   ├── NPCVisual (зелёный круг)
│   ├── NPCInteractable
│   ├── Rigidbody2D (Dynamic, gravity=0)
│   └── CircleCollider2D (solid, r=0.5)
├── NPC_СтражникЧжан (Guard L3)
│   ├── ... (те же компоненты)
│   └── NPCAI (Patrolling, 4 точки)
├── NPC_СтражникВэй (Guard L2)
├── NPC_СтарейшинаЛи (Elder L5)
├── NPC_КультиваторЧэнь (Cultivator L4)
├── NPC_ДикийЗверь (Monster L1)
└── NPC_ТёмныйЗверь (Monster L2)
```

---

## Типичные проблемы

| Проблема | Причина | Решение |
|---|---|---|
| NPC не появляется | Нет тега «NPC» | Выполнить Phase 02 |
| NPC проваливается | Gravity Scale ≠ 0 | Установить Gravity Scale: 0 |
| NPC не двигается | Rigidbody2D отсутствует | Добавить Rigidbody2D (Dynamic) |
| Guard не патрулирует | Нет точек патруля | Настроить SetPatrolPoints() |
| NPC генерирует ошибку | NPCGenerator не инициализирован | Выполнить Phase 05 + 09 |
| NPC пересекает стены | Нет solid коллайдера | Добавить CircleCollider2D (isTrigger: false) |
| Имена NPC одинаковые | Одинаковый seed генерации | Нормально — генерация случайная |

---

*Документ создано: 2026-04-30 08:09:40 UTC*
