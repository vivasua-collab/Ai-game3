# Чекпоинт: Генераторы (Generators)

**Дата:** 2026-03-31 10:22:36 UTC
**Фаза:** Generators
**Статус:** complete

## Выполненные задачи

- [x] Исправлен TechniqueGenerator.cs — множители Grade (1.2/1.4/1.6 вместо 1.3/1.6/2.0)
- [x] Исправлен TechniqueGenerator.cs — распределение Grade (60/28/10/2%)
- [x] Исправлен TechniqueGenerator.cs — формула capacity по документации
- [x] Добавлены timestamps во все файлы генераторов
- [x] Создан WeaponGenerator.cs
- [x] Создан ArmorGenerator.cs
- [x] Создан ConsumableGenerator.cs

## Ключевые исправления

### TechniqueGenerator.cs

**До (неверно):**
```csharp
private static readonly float[] GradeMultipliers = { 1.0f, 1.3f, 1.6f, 2.0f };
// Распределение: 50%, 30%, 15%, 5%
```

**После (по документации):**
```csharp
private static readonly float[] GradeMultipliers = { 1.0f, 1.2f, 1.4f, 1.6f };
// Распределение: 60%, 28%, 10%, 2%
```

### Формула Capacity (TECHNIQUE_SYSTEM.md §"Структурная ёмкость")

```
capacity = baseCapacity(type) × 2^(level-1) × masteryBonus
```

Базовая ёмкость по типам:
| Тип техники              | baseCapacity |
|--------------------------|--------------|
| Formation                | 80           |
| Defense                  | 72           |
| Support                  | 56           |
| Healing                  | 56           |
| Combat (melee_strike)    | 64           |
| Combat (melee_weapon)    | 48           |
| Combat (ranged_*)        | 32           |
| Movement                 | 40           |
| Curse                    | 40           |
| Poison                   | 40           |
| Sensory                  | 32           |

## Новые генераторы

### WeaponGenerator.cs
- Подтипы: Unarmed, Dagger, Sword, Greatsword, Axe, Spear, Bow, Staff, Hammer, Mace, Crossbow, Wand
- Классы: Light, Medium, Heavy, Ranged, Magic
- Типы урона: Slashing, Piercing, Blunt, Elemental
- Материалы: Тиры 1-5
- Грейды: Damaged, Common, Refined, Perfect, Transcendent

### ArmorGenerator.cs
- Подтипы: Head, Torso, Arms, Hands, Legs, Feet, Full
- Весовые классы: Light, Medium, Heavy
- Покрытие частей тела
- Проводимость Ци материалов

### ConsumableGenerator.cs
- Типы: Pill, Elixir, Food, Drink, Poison, Scroll, Talisman
- Эффекты: Healing, QiRestoration, Buff, Debuff, Cultivation, Permanent
- Побочные эффекты для таблеток
- Редкость: Common → Mythic

## Изменённые файлы

- `UnityProject/Assets/Scripts/Generators/TechniqueGenerator.cs` — исправлен по документации
- `UnityProject/Assets/Scripts/Generators/NPCGenerator.cs` — добавлены timestamps
- `UnityProject/Assets/Scripts/Generators/SeededRandom.cs` — добавлены timestamps
- `UnityProject/Assets/Scripts/Generators/WeaponGenerator.cs` — **НОВЫЙ**
- `UnityProject/Assets/Scripts/Generators/ArmorGenerator.cs` — **НОВЫЙ**
- `UnityProject/Assets/Scripts/Generators/ConsumableGenerator.cs` — **НОВЫЙ**

## Источники документации

- `docs/TECHNIQUE_SYSTEM.md` — система техник
- `docs/EQUIPMENT_SYSTEM.md` — система экипировки
- `docs/GENERATORS_SYSTEM.md` — система генерации
- `docs/INVENTORY_SYSTEM.md` — система инвентаря

## Следующие шаги

1. Тестирование генераторов в Unity
2. Создание ScriptableObject для материалов
3. Интеграция с системой лута
4. Балансировка параметров

---

*Чекпоинт создан: 2026-03-31 10:22:36 UTC*
