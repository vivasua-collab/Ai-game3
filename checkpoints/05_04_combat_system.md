# Чекпоинт: Система боя — полный план внедрения

**Дата:** 2026-05-04 04:10 UTC
**Статус:** in_progress

👉 Кодовая база: [05_04_combat_system_code.md](05_04_combat_system_code.md)

## Выполненные задачи

- [x] Остановлен и заблокирован DEV сервер (package.json: `echo ⛔ && exit 1`)
- [x] Очищено окружение от Next.js файлов
- [x] Код сверен с GitHub — откачено до `d536592`
- [x] Прочитана документация: COMBAT_SYSTEM.md, ALGORITHMS.md, TECHNIQUE_SYSTEM.md
- [x] Прочитан весь существующий код боя (17 файлов Combat/)
- [x] Прочитан PlayerController, NPCController, CombatUI
- [x] Проведён полный аудит боевой системы
- [x] Разработана архитектура системы накачки техник (Charge System)

---

## 🏗️ АРХИТЕКТУРА: Система боя — полный план внедрения

### Ключевой принцип: «Вывернутая» система техник

Пользователь описал модель: **СНАЧАЛА нажимается клавиша техники → ПОТОМ идёт «накачка» (подготовка) → ТОЛЬКО ПОСЛЕ НАКАЧКИ техника срабатывает**. Одномоментно можно накачивать только одну технику.

Это отличается от классического «cast time» тем, что:
1. Игрок АКТИВНО вливает Ци в технику во время накачки
2. Можно ПРЕРВАТЬ накачку (прервать = потерять уже вложенное Ци)
3. Накачку можно ПРЕКРАТИТЬ ДОПОЛДНОСТИ = техника не сработает, но Ци вернётся частично
4. Накачка ОДНОЙ техникой блокирует накачку других

---

## 📋 ПОЛНЫЙ ПЛАН ВНЕДРЕНИЯ (8 фаз)

### ═══════════════════════════════════════════
### ФАЗА 1: Система накачки техник (TechniqueChargeSystem)
### ═══════════════════════════════════════════

**Новые файлы:**
- `Assets/Scripts/Combat/TechniqueChargeSystem.cs` — ядро системы накачки
- `Assets/Scripts/Combat/ChargeState.cs` — перечисления и структуры

**Суть:**
При нажатии клавиши техники начинается фаза «накачки» (charging). Практик вливает Ци в технику. Накачка длится `chargeTime` секунд. По завершении — техника срабатывает автоматически.

**ChargeState (enum):**
```
None        — техника не накачивается
Charging    — идёт накачка (Ци вливается)
Ready       — накачка завершена, техника готова к срабатыванию
Firing      — техника срабатывает (анимация/эффект)
Interrupted — накачка прервана (урон, стан, отмена игроком)
```

**TechniqueChargeData (struct):**
```csharp
public struct TechniqueChargeData
{
    public LearnedTechnique Technique;     // Какая техника накачивается
    public ChargeState State;              // Текущее состояние
    public float ChargeProgress;           // 0.0-1.0 прогресс накачки
    public float ChargeTime;               // Полное время накачки (сек)
    public long QiCharged;                 // Сколько Ци уже вложено
    public long QiTotalRequired;           // Сколько Ци нужно всего
    public float QiChargeRate;             // Ци/сек во время накачки
    public bool CanMoveWhileCharging;      // Можно ли двигаться
    public bool CanBeInterruptedByDamage;  // Прерывается ли уроном
    public float InterruptDamageThreshold; // Мин. урон для прерывания
}
```

**Правила и ограничения накачки:**

| Правило | Описание |
|---------|----------|
| **Один слот накачки** | Одномоментно можно накачивать ТОЛЬКО одну технику |
| **Блокировка смены** | Пока идёт накачка, нажатие других клавиш техник игнорируется (или прерывает текущую) |
| **Прерывание уроном** | Если получен урон ≥ InterruptDamageThreshold → накачка прерывается |
| **Прерывание станом** | Оглушение (stun) → немедленное прерывание |
| **Отмена игроком** | Повторное нажатие той же клавиши → отмена накачки |
| **Частичный возврат Ци** | При прерывании: возвращается 50% вложенного Ци. При отмене игроком: 70%. При оглушении: 0% |
| **Движение** | melee техники: можно двигаться (0.5× скорость). ranged: нельзя двигаться |
| **Минимальная накачка** | Техника не сработает, если вложено < 30% требуемого Ци (фиаско) |
| **Перенакачка** | Если ChargeProgress ≥ 1.0 и QiCharged ≥ QiTotalRequired — автоматический запуск |

**Формула времени накачки:**
```
chargeTime = baseChargeTime × (1 / (1 + cultivationBonus)) × (1 / (1 + masteryBonus))

Где:
- baseChargeTime = TechniqueData.chargeTime (из SO, секунды)
- cultivationBonus = (cultivationLevel - 1) × 0.05
- masteryBonus = mastery / 100

Базовые времена по типу:
| Тип | baseChargeTime |
|-----|---------------|
| melee_strike | 0.5 сек |
| melee_weapon | 0.8 сек |
| ranged_projectile | 1.2 сек |
| ranged_beam | 1.5 сек |
| ranged_aoe | 2.0 сек |
| defense | 0.3 сек |
| healing | 1.0 сек |
| ultimate | 3.0 сек |
```

**Интеграция с TechniqueController:**
- TechniqueController.UseTechnique() → НЕ тратит Ци мгновенно, а начинает накачку
- TechniqueController.UseQuickSlot() → вызывает BeginCharge(slot)
- Новое поле: `private TechniqueChargeData activeCharge;`
- Новые события: `OnChargeStarted`, `OnChargeProgress`, `OnChargeCompleted`, `OnChargeInterrupted`

---

### ═══════════════════════════════════════════
### ФАЗА 2: Триггер боя (CombatTrigger)
### ═══════════════════════════════════════════

**Новые файлы:**
- `Assets/Scripts/Combat/CombatTrigger.cs` — компонент-триггер боя

**Суть:**
CombatTrigger — MonoBehaviour с Collider2D (trigger). При контакте Player/NPC с враждебным CombatTrigger — автоматически инициируется бой через CombatManager.

**Логика:**
1. На NPC/враге висит CombatTrigger с Collider2D (isTrigger=true)
2. При OnTriggerEnter2D проверяется отношение (Attitude)
3. Если Hostile → CombatManager.InitiateCombat()
4. Если Neutral/Friendly → игнорируется
5. Радиус триггера = attackRange + buffer (0.5f)

**Параметры CombatTrigger:**
```csharp
[Header("Trigger Settings")]
[SerializeField] private float triggerRadius = 3f;
[SerializeField] private bool autoEngage = true;
[SerializeField] private float aggroCooldown = 5f;
[SerializeField] private Attitude minAttitudeToEngage = Attitude.Hostile;
```

**Интеграция:**
- Вешается на префаб NPC через Phase-генератор
- Связывается с NPCController (ICombatant)
- При уничтожении NPC — триггер удаляется

---

### ═══════════════════════════════════════════
### ФАЗА 3: Исправление HitDetector для 2D
### ═══════════════════════════════════════════

**Изменяемый файл:** `Assets/Scripts/Combat/HitDetector.cs`

**Проблема:** Использует `Physics.OverlapSphere`, `Physics.Raycast` (3D)
**Решение:** Замена на `Physics2D.OverlapCircle`, `Physics2D.Raycast`

**Изменения:**
- `FindNearestTarget()` → `Physics2D.OverlapCircleAll()`
- `FindTargetsInRange()` → `Physics2D.OverlapCircleAll()`
- `HasLineOfSight()` → `Physics2D.Raycast()`
- `FindTargetsInArea()` → `Physics2D.OverlapCircleAll()`
- `FindTargetsInCone()` → `Physics2D.OverlapCircleAll()` + угол через `Vector2.Angle()`
- `Collider` → `Collider2D`
- `RaycastHit` → `RaycastHit2D`
- `Vector3` → `Vector2` (позиции/направления)

---

### ═══════════════════════════════════════════
### ФАЗА 4: CombatAI для NPC
### ═══════════════════════════════════════════

**Новые файлы:**
- `Assets/Scripts/Combat/CombatAI.cs` — ИИ принятия боевых решений
- `Assets/Scripts/Combat/AIPersonality.cs` — данные личности ИИ

**Суть:**
CombatAI — компонент на NPC, который принимает решения в бою:
- Какую технику использовать
- Когда атаковать / защищаться / отступать
- Какую цель атаковать

**AI Решения (enum AIDecision):**
```
BasicAttack         — базовая атака (без техники)
ChargeTechnique     — начать накачку техники
ContinueCharge      — продолжить накачку
UseDefensiveTech    — защитная техника
Flee                — побег
Wait                — ожидание (перегруппировка)
```

**Логика принятия решений (дерево):**
```
1. Если HP < 20% → Flee (или защитная техника, если нет пути отхода)
2. Если идёт накачка → ContinueCharge (если безопасно)
3. Если противник в ближней зоне:
   a. Если есть melee техника → ChargeTechnique (melee)
   b. Иначе → BasicAttack
4. Если противник в дальней зоне:
   a. Если есть ranged техника → ChargeTechnique (ranged)
   b. Иначе → приблизиться + BasicAttack
5. Если получил урон во время накачки:
   a. Если урон > 30% HP → прервать + защитная техника
   b. Иначе → продолжить накачку
```

**AIPersonality (ScriptableObject):**
```csharp
[CreateAssetMenu(fileName = "AIPersonality", menuName = "Cultivation/AI Personality")]
public class AIPersonality : ScriptableObject
{
    public float aggression;      // 0-1: склонность к атаке
    public float defensiveness;   // 0-1: склонность к защите
    public float retreatThreshold;// HP% при котором начинается отступление
    public float techniquePreference; // 0-1: предпочтение техник vs базовые атаки
    public float chargeRiskTolerance; // 0-1: готовность накачивать под огнём
}
```

**Интеграция с NPCController:**
- NPCController получает CombatAI компонент
- CombatAI.Init(npcController, techniqueController)
- CombatManager вызывает `combatAI.GetNextAction()` для NPC

---

### ═══════════════════════════════════════════
### ФАЗА 5: Подключение боя к Player/NPC
### ═══════════════════════════════════════════

**Изменяемые файлы:**
- `PlayerController.cs` — обработка ввода боя + накачка
- `NPCController.cs` — интеграция CombatAI + CombatTrigger
- `CombatManager.cs` — реальное время (Update-driven)

**PlayerController — новый ввод:**
```csharp
// Новые клавиши:
// 1-9: Начать накачку техники из quickslot
// Повторное нажатие: Отменить накачку
// Пробел: Базовая атака (без техники)
// Q/E: Цикл по целям

private void ProcessCombatInput()
{
    if (!CombatManager.Instance.IsInCombat) return;
    
    // Техники (1-9)
    for (int i = 0; i < 9; i++)
    {
        if (Keyboard.current.digitKey(i+1).wasPressedThisFrame)
        {
            HandleTechniqueInput(i);
        }
    }
    
    // Базовая атака (Пробел)
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        CombatManager.Instance.ExecuteBasicAttack(this, currentTarget);
    }
}

private void HandleTechniqueInput(int slot)
{
    var tech = techniqueController.GetQuickSlotTechnique(slot);
    if (tech == null) return;
    
    if (techniqueController.IsCharging)
    {
        // Уже накачиваем
        if (techniqueController.ActiveCharge.Technique == tech)
        {
            // Та же техника → отмена
            techniqueController.CancelCharge();
        }
        // Другая техника → игнорируем (правило: только одна)
        return;
    }
    
    // Начать накачку
    techniqueController.BeginCharge(tech);
}
```

**CombatManager — реальное время:**
```csharp
// Заменить синхронный ExecuteAttack на Update-driven пайплайн:
// 1. Обработка накачки техник
// 2. Когда накачка завершена → автоматический запуск
// 3. AI решения для NPC
// 4. Проверка окончания боя
```

---

### ═══════════════════════════════════════════
### ФАЗА 6: Боевой UI (обновление CombatUI)
### ═══════════════════════════════════════════

**Изменяемый файл:** `Assets/Scripts/UI/CombatUI.cs`

**Новые элементы UI:**
1. **Полоска накачки** — прогресс-бар ChargeProgress (0-100%)
2. **Индикатор Ци** — сколько Ци вложено / сколько нужно
3. **Имя накачиваемой техники** — текущая техника
4. **Иконки техник** — 9 слотов с кулдаунами и статусами
5. **Полоска вливания Ци** — визуализация QiChargeRate
6. **Индикатор прерывания** — мигает если можно прервать

**Новые методы CombatUI:**
```csharp
public void UpdateChargeProgress(float progress, long qiCharged, long qiTotal);
public void ShowChargeBar(string techniqueName);
public void HideChargeBar();
public void FlashInterruptWarning();
public void SetTechniqueSlotState(int slot, TechniqueSlotState state);
```

**TechniqueSlotState (enum):**
```
Ready       — готова к использованию
Charging    — накачивается
Cooldown    — на кулдауне
Unavailable — недостаточно Ци / уровня
```

---

### ═══════════════════════════════════════════
### ФАЗА 7: Недостающие слои пайплайна урона
### ═══════════════════════════════════════════

**Слой 1b: Урон оружия (melee_weapon)**
- Изменяемый файл: `DamageCalculator.cs`
- Для CombatSubtype.MeleeWeapon: `rawDamage += weaponBonusDamage`
- weaponBonusDamage берётся из ICombatant (новое свойство или из EquipmentController)

**Слой 3b: Бафф формаций**
- Изменяемый файл: `DamageCalculator.cs`
- Проверка активных формаций → formationBuffMultiplier
- Источник: FormationSystem (уже частично реализован через FormationArrayEffect)

**Слой 10b: Loot Generation**
- Новый файл: `Assets/Scripts/Combat/LootGenerator.cs`
- Вызывается при смерти ICombatant
- Генерирует дроп на основе уровня, стихии, вида

---

### ═══════════════════════════════════════════
### ФАЗА 8: Расхождения документации и кода
### ═══════════════════════════════════════════

**РЕШЕНИЕ: Код → Документация** (синхронизировать доки под код)

| Аспект | Код (оставить) | Документация (исправить) |
|--------|----------------|--------------------------|
| Grade множители | 1.0/1.2/1.4/1.6 | Обновить COMBAT_SYSTEM.md |
| Ultimate множитель | ×1.3 | Обновить COMBAT_SYSTEM.md |
| Poison | НЕ стихия (в enum — элемент) | Оставить как есть — Poison в Element enum для техник |
| Light элемент | Нет в коде | Добавить в Element enum + GameConstants |
| Слои 1b, 3b | Будут реализованы в Фазе 7 | Обновить после реализации |

---

## 📊 Сводная таблица файлов

### Новые файлы (5)
| Файл | Фаза | Назначение |
|------|-------|------------|
| `Scripts/Combat/TechniqueChargeSystem.cs` | 1 | Ядро системы накачки |
| `Scripts/Combat/ChargeState.cs` | 1 | Перечисления и структуры |
| `Scripts/Combat/CombatTrigger.cs` | 2 | Триггер боя (2D коллайдер) |
| `Scripts/Combat/CombatAI.cs` | 4 | ИИ принятия решений NPC |
| `Scripts/Combat/AIPersonality.cs` | 4 | ScriptableObject личности ИИ |

### Изменяемые файлы (8)
| Файл | Фаза | Изменения |
|------|-------|-----------|
| `TechniqueController.cs` | 1 | Интеграция ChargeSystem, BeginCharge/CancelCharge |
| `HitDetector.cs` | 3 | Physics → Physics2D |
| `PlayerController.cs` | 5 | Обработка боевого ввода + накачка |
| `NPCController.cs` | 5 | CombatAI + CombatTrigger интеграция |
| `CombatManager.cs` | 5 | Update-driven пайплайн, AI цикл |
| `CombatUI.cs` | 6 | Полоска накачки, слоты техник |
| `DamageCalculator.cs` | 7 | Слои 1b, 3b |
| `Combatant.cs` | 7 | ICombatant + ChargeState поддержка |

---

## 📐 Диаграмма потока боя

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ПОТОК БОЯ (реальное время)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [CombatTrigger] → InitiateCombat(attacker, defender)               │
│         ↓                                                            │
│  [CombatManager] → CombatState.Active                                │
│         ↓                                                            │
│  ┌──────────────────────────────────────────────┐                   │
│  │           Update Loop (каждый кадр)           │                   │
│  │                                                │                   │
│  │  1. Обработка накачки техник                   │                   │
│  │     ├── Игрок: из ввода (клавиши 1-9)         │                   │
│  │     └── NPC: из CombatAI                       │                   │
│  │                                                │                   │
│  │  2. Если ChargeProgress >= 1.0 →               │                   │
│  │     ExecuteTechniqueAttack()                    │                   │
│  │                                                │                   │
│  │  3. AI решения для NPC                         │                   │
│  │     └── CombatAI.GetNextAction()               │                   │
│  │                                                │                   │
│  │  4. Проверка прерываний                        │                   │
│  │     ├── Урон ≥ threshold → InterruptCharge     │                   │
│  │     ├── Стан → InterruptCharge                 │                   │
│  │     └── Отмена игроком → CancelCharge          │                   │
│  │                                                │                   │
│  │  5. Проверка окончания боя                     │                   │
│  │     └── Кто-то умер → EndCombat()              │                   │
│  └──────────────────────────────────────────────┘                   │
│                                                                      │
│  ═══════════════════════════════════════════                         │
│  ДЕТАЛЬНЫЙ ПОТОК НАКАЧКИ ТЕХНИКИ                                    │
│  ═══════════════════════════════════════════                         │
│                                                                      │
│  Нажатие клавиши (1-9)                                              │
│         ↓                                                            │
│  Проверка: можно ли использовать?                                    │
│  ├── Нет (кулдаун/мало Ци/уровень) → отказ                          │
│  └── Да → BeginCharge(technique)                                    │
│         ↓                                                            │
│  [ChargeState.Charging] — каждый кадр:                               │
│  ├── ChargeProgress += deltaTime / chargeTime                       │
│  ├── QiCharged += qiChargeRate × deltaTime                          │
│  ├── Если QiCharged ≥ QiTotalRequired → переход к Ready             │
│  ├── Если получен урон ≥ threshold → InterruptCharge()              │
│  ├── Если нажата та же клавиша → CancelCharge()                     │
│  └── Если нажата другая клавиша → игнорируется                      │
│         ↓                                                            │
│  [ChargeState.Ready] → автоматический запуск                        │
│         ↓                                                            │
│  [ChargeState.Firing] → TechniqueController.UseTechnique()          │
│  ├── Трата Ци (QiCharged)                                           │
│  ├── Установка кулдауна                                             │
│  ├── Повышение мастерства                                           │
│  └── CombatManager.ExecuteTechniqueAttack()                         │
│         ↓                                                            │
│  Пайплайн урона (11 слоёв) → Применение урона                       │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## ⚠️ Проблемы (перенесено из предыдущего чекпоинта)

1. **Grade множители** — код: 1.0/1.2/1.4/1.6, доки: 1.0/1.3/1.6/2.0 → решение: код верен, обновить доки
2. **Ultimate множитель** — код: ×1.3, доки: ×2.0 → решение: код верен, обновить доки
3. **Light элемент** — есть в ALGORITHMS.md §10, нет в Element enum → добавить
4. **Poison в Element enum** — доки говорят «не стихия», но код использует как элемент → оставить для техник

---

## Следующие шаги

1. **ФАЗА 1** — Создать TechniqueChargeSystem.cs и ChargeState.cs
2. **ФАЗА 1** — Интегрировать ChargeSystem в TechniqueController
3. **ФАЗА 2** — Создать CombatTrigger.cs
4. **ФАЗА 3** — Исправить HitDetector.cs (3D → 2D)
5. **ФАЗА 4** — Создать CombatAI.cs и AIPersonality.cs
6. **ФАЗА 5** — Подключить бой к PlayerController и NPCController
7. **ФАЗА 6** — Обновить CombatUI.cs
8. **ФАЗА 7** — Реализовать недостающие слои пайплайна (1b, 3b, 10b)
9. **ФАЗА 8** — Синхронизировать документацию с кодом
10. **Git push** после завершения

---

## Изменённые файлы

- package.json — блокировка dev сервера
- checkpoints/05_04_combat_system.md — этот файл (полный план)
- checkpoints/05_04_combat_system_code.md — аудит кода (новый)
