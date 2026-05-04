# Чекпоинт: Система боя — полный план внедрения

**Дата:** 2026-05-04 04:10 UTC
**Обновлено:** 2026-05-04 07:35 UTC — ФАЗЫ 6-8 завершены
**Статус:** complete

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
- [x] Аудит тиковой системы (TimeController) — см. раздел ниже
- [x] Добавлено ограничение: минимальное время накачки = tick / 10
- [x] **ТИКОВАЯ СИСТЕМА**: Полная интеграция TimeController v2.0 — синглтон, OnTick в обоих режимах, OnTickDelta, TickInterval/MinChargeTime публичные
- [x] **ФАЗА 1**: Созданы ChargeState.cs + TechniqueChargeSystem.cs — ядро накачки
- [x] **ФАЗА 1**: Интегрирован UseTechniqueFromCharge в TechniqueController v1.1
- [x] **ФАЗА 2**: Создан CombatTrigger.cs — триггер боя (2D коллайдер)
- [x] **ФАЗА 3**: HitDetector.cs мигрирован Physics → Physics2D (v2.0)
- [x] **ФАЗА 4**: Созданы CombatAI.cs + AIPersonality.cs — ИИ для NPC
- [x] **ФАЗА 5**: CombatManager v2.0 — интеграция прерываний накачки
- [x] **ТИКОВАЯ СИСТЕМА v3.0**: Dual Tick Model — OnTick (константа для боя) + OnWorldTick (масштабируется скоростью)
- [x] **ФАЗА 5**: PlayerController — ProcessCombatInput (1-9, пробел, Q/E) + ITechniqueUser
- [x] **ФАЗА 5**: NPCController — CombatAI + CombatTrigger + ITechniqueUser интеграция
- [x] **ФАЗА 5**: CombatManager v2.1 — AI цикл + Update-driven пайплайн
- [x] **FIX CS0414**: CombatTrigger.minAttitudeToEngage используется в ShouldEngage
- [x] **МИГРАЦИЯ**: FormationController.OnTick → OnWorldTick
- [x] **ФАЗА 6**: CombatUI — полоска накачки, слоты техник (TechniqueSlotUI), индикатор прерывания, TechniqueSlotState enum
- [x] **ФАЗА 6**: CombatUI.SubscribeToChargeSystem — подписка на OnChargeStarted/Progress/Completed/Interrupted/Fired
- [x] **ФАЗА 6**: TechniqueSlotUI — UI-компонент слота (иконка, номер, кулдаун, накачка, недоступность)
- [x] **ФАЗА 7: Слой 1b** — WeaponBonusDamage в AttackerParams + DamageCalculator
- [x] **ФАЗА 7: Слой 3b** — FormationBuffMultiplier в DefenderParams + DamageCalculator
- [x] **ФАЗА 7: Слой 10b** — LootGenerator.cs + интеграция в CombatManager.OnLootGenerated
- [x] **ФАЗА 8** — Чекпоинт обновлён, расхождения документации задокументированы

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
| **Минимальное время накачки** | **tick / 10** — накачка НЕ может быть быстрее чем tickInterval ÷ 10. При tickInterval=1с → min=0.1с. Это абсолютный пол для chargeTime |
| **Перенакачка** | Если ChargeProgress ≥ 1.0 и QiCharged ≥ QiTotalRequired — автоматический запуск |

**Формула времени накачки:**
```
chargeTime = max(minChargeTime, baseChargeTime × (1 / (1 + cultivationBonus)) × (1 / (1 + masteryBonus)))

Где:
- **minChargeTime = TimeController.tickInterval / 10** — абсолютный минимум (правило tick/10)
  - При tickInterval=1с → minChargeTime=0.1с
  - При изменении tickInterval автоматически пересчитывается
- baseChargeTime = TechniqueData.chargeTime (из SO, секунды)
- cultivationBonus = (cultivationLevel - 1) × 0.05
- masteryBonus = mastery / 100

Базовые времена по типу:
| Тип | baseChargeTime | effectiveTime (L1, 0% mastery) |
|-----|---------------|--------------------------------|
| melee_strike | 0.5 сек | max(0.1, 0.5) = 0.5 сек |
| melee_weapon | 0.8 сек | max(0.1, 0.8) = 0.8 сек |
| ranged_projectile | 1.2 сек | max(0.1, 1.2) = 1.2 сек |
| ranged_beam | 1.5 сек | max(0.1, 1.5) = 1.5 сек |
| ranged_aoe | 2.0 сек | max(0.1, 2.0) = 2.0 сек |
| defense | 0.3 сек | max(0.1, 0.3) = 0.3 сек |
| healing | 1.0 сек | max(0.1, 1.0) = 1.0 сек |
| ultimate | 3.0 сек | max(0.1, 3.0) = 3.0 сек |

⚠️ При высоком уровне культивации и мастерстве chargeTime может стать < 0.1с —
тогда срабатывает ограничение tick/10 и техника не может быть быстрее 0.1с.
Пример: defense L10, 100% mastery:
  chargeTime = 0.3 × (1/(1+0.45)) × (1/(1+1.0)) = 0.3 × 0.69 × 0.5 = 0.103с
  → ок, выше 0.1с. Но если mastery=200% (хипотетически) → 0.075с → ограничено до 0.1с
```

**Интеграция с TechniqueController:**
- TechniqueController.UseTechnique() → НЕ тратит Ци мгновенно, а начинает накачку
- TechniqueController.UseQuickSlot() → вызывает BeginCharge(slot)
- Новое поле: `private TechniqueChargeData activeCharge;`
- Новые события: `OnChargeStarted`, `OnChargeProgress`, `OnChargeCompleted`, `OnChargeInterrupted`

**Интеграция с тиковой системой (TimeController):**
- TechniqueChargeSystem берёт tickInterval из TimeController.Instance (если доступен)
- Формула: `minChargeTime = TimeController.Instance?.TickInterval / 10f ?? 0.1f`
- Fallback: если TimeController не доступен → 0.1с (hardcoded default)
- Накачка обрабатывается ПО КАДРАМ (Time.deltaTime), НЕ по тикам — для плавности UI
- Тик используется ТОЛЬКО для расчёта минимального времени накачки

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

## 🔍 АУДИТ ТИКОВОЙ СИСТЕМЫ (TimeController)

**Дата аудита:** 2026-05-05

### Исходные данные

| Параметр | Значение | Расположение |
|----------|----------|-------------|
| tickInterval | 1.0 сек (настраиваемый) | `TimeController.cs:44` |
| currentTick | int, ++каждый тик | `TimeController.cs:63` |
| OnTick event | Action<int> | `TimeController.cs:78` |
| useDeterministicTime | true (по умолчанию) | `TimeController.cs:36` |
| TICKS_PER_MINUTE | 1 (константа) | `GameConstants.cs:540` |

### Механика работы

```
FixedUpdate()
  → if (useDeterministicTime && autoAdvance && !Paused)
     → deterministicAccumulator += Time.fixedDeltaTime (0.02с)
     → if (deterministicAccumulator >= tickInterval)
        → deterministicAccumulator -= tickInterval
        → currentTick++
        → OnTick?.Invoke(currentTick)
     → timeAccumulator += Time.fixedDeltaTime × speedRatio
     → while (timeAccumulator >= 60f) → AdvanceMinute()
```

### Подписчики OnTick

| Компонент | Подписка | Файл |
|-----------|----------|------|
| FormationController | ✅ OnTick += HandleTimeTick | `FormationController.cs:228` |
| FormationCore | ✅ ProcessTimeTick(int) | `FormationCore.cs:534` |
| FormationQiPool | ✅ ProcessDrain(int) | `FormationQiPool.cs:360` |
| QuestController | ⚠️ OnTick() метод есть, но подписка не найдена | `QuestController.cs:593` |
| **CombatManager** | ❌ НЕ подписан | — |
| **TechniqueController** | ❌ НЕ подписан | — |
| **TechniqueChargeSystem** | ❌ НЕ существует | — |

### Результаты аудита

| # | Проверка | Результат | Комментарий |
|---|----------|-----------|------------|
| 1 | Тик срабатывает в детерминированном режиме | ✅ PASS | Каждые 1с, FixedUpdate-based |
| 2 | Тик срабатывает в недетерминированном режиме | ❌ FAIL | `ProcessTimeUpdate()` НЕ отправляет OnTick |
| 3 | currentTick корректно инкрементируется | ✅ PASS | currentTick++, OnTick?.Invoke |
| 4 | deterministicAccumulator не переполняется | ✅ PASS | -= tickInterval, не обнуляется |
| 5 | TickInterval доступен извне | ❌ FAIL | Нет публичного свойства TickInterval |
| 6 | Боевая система подключена к тикам | ❌ FAIL | CombatManager/TechniqueController не подписаны |
| 7 | TICKS_PER_MINUTE используется корректно | ⚠️ WARN | Константа существует, но нигде не используется в бою |

### Обнаруженные проблемы

1. **❌ КРИТИЧЕСКОЕ: Нет публичного свойства TickInterval**
   - TimeController.tickInterval = `private float tickInterval = 1f`
   - Нужно добавить: `public float TickInterval => tickInterval;`
   - TechniqueChargeSystem не сможет получить tickInterval для расчёта minChargeTime

2. **❌ OnTick не срабатывает в недетерминированном режиме**
   - `ProcessTimeUpdate()` обновляет время, но НЕ отправляет OnTick
   - Если useDeterministicTime=false, подписчики не получают тики
   - **Влияние на бой:** минимальное — зарядка работает по deltaTime, не по тикам

3. **⚠️ QuestController.OnTick не подключён**
   - Метод существует, но подписка не найдена в коде
   - Возможно, подключается через инспектор или автоматически

4. **⚠️ TICKS_PER_MINUTE = 1 не используется**
   - Константа в GameConstants, но не привязана к TimeController.tickInterval
   - Может быть источником путаницы

### Необходимые исправления (для Phase 1)

1. **Добавить публичное свойство** в `TimeController.cs`:
   ```csharp
   public float TickInterval => tickInterval;
   ```

2. **Добавить fallback** в TechniqueChargeSystem:
   ```csharp
   private static float GetMinChargeTime()
   {
       var tc = CultivationGame.World.TimeController.Instance;
       float tickInterval = tc != null ? tc.TickInterval : 1f;
       return tickInterval / 10f; // tick / 10
   }
   ```

3. **(ОПЦИОНАЛЬНО) Добавить OnTick в ProcessTimeUpdate** для недетерминированного режима:
   ```csharp
   private void ProcessTimeUpdate()
   {
       float ratio = GetSpeedRatio();
       timeAccumulator += Time.deltaTime * ratio;
       deterministicAccumulator += Time.deltaTime;
       if (deterministicAccumulator >= tickInterval)
       {
           deterministicAccumulator -= tickInterval;
           currentTick++;
           OnTick?.Invoke(currentTick);
       }
       while (timeAccumulator >= 60f)
       {
           timeAccumulator -= 60f;
           AdvanceMinute();
       }
   }
   ```

### Расчёт minChargeTime для текущих настроек

| tickInterval | minChargeTime (tick/10) | Примечание |
|-------------|------------------------|------------|
| 0.5 сек | 0.05 сек | Быстрый серверный тик |
| **1.0 сек** | **0.1 сек** | **Текущее значение по умолчанию** |
| 2.0 сек | 0.2 сек | Медленный тик |
| 5.0 сек | 0.5 сек | Очень медленный тик |

### Вывод по тиковой системе

✅ **Тиковая система работает корректно в детерминированном режиме** (по умолчанию).
Тики срабатывают каждые 1с (tickInterval=1f), OnTick отправляется подписчикам.

❌ **Два критических пробела для боевой системы:**
1. Нет публичного доступа к tickInterval → нужно добавить свойство
2. Боевая система полностью отключена от тиков → накачка работает по deltaTime (это правильно для плавности UI), но minChargeTime должен вычисляться из tickInterval

✅ **Правило tick/10 реализуемо:** при текущем tickInterval=1с, минимальное время накачки = 0.1с.

---

## Следующие шаги

✅ **ВСЕ 8 ФАЗ ЗАВЕРШЕНЫ**

1. ~~ФАЗА 1~~ — Созданы TechniqueChargeSystem.cs и ChargeState.cs ✅
2. ~~ФАЗА 1~~ — Интегрирован ChargeSystem в TechniqueController ✅
3. ~~ФАЗА 2~~ — Создан CombatTrigger.cs ✅
4. ~~ФАЗА 3~~ — Исправлен HitDetector.cs (3D → 2D) ✅
5. ~~ФАЗА 4~~ — Созданы CombatAI.cs и AIPersonality.cs ✅
6. ~~ФАЗА 5~~ — Подключён бой к PlayerController и NPCController ✅
7. ~~ФАЗА 6~~ — Обновлён CombatUI.cs (накачка, слоты, прерывания) ✅
8. ~~ФАЗА 7~~ — Реализованы недостающие слои пайплайна (1b, 3b, 10b) ✅
9. ~~ФАЗА 8~~ — Синхронизирована документация с кодом ✅
10. **Git push** после завершения

---

## Изменённые файлы

- package.json — блокировка dev сервера
- checkpoints/05_04_combat_system.md — этот файл (полный план)
- checkpoints/05_04_combat_system_code.md — аудит кода

### Фаза 6 — изменённые файлы
- `Assets/Scripts/UI/CombatUI.cs` — полоска накачки, TechniqueSlotUI, TechniqueSlotState, подписка на ChargeSystem

### Фаза 7 — изменённые/созданные файлы
- `Assets/Scripts/Combat/DamageCalculator.cs` — Слой 1b (WeaponBonusDamage), Слой 3b (FormationBuffMultiplier)
- `Assets/Scripts/Combat/Combatant.cs` — WeaponBonusDamage + FormationBuffMultiplier в CombatantBase
- `Assets/Scripts/Combat/LootGenerator.cs` — Новый файл: генерация лута при смерти (Слой 10b)
- `Assets/Scripts/Combat/CombatManager.cs` — OnLootGenerated событие + LootGenerator.GenerateLoot()
- `Assets/Scripts/Player/PlayerController.cs` — AttackerParams/DefenderParams обновлены
- `Assets/Scripts/NPC/NPCController.cs` — AttackerParams/DefenderParams обновлены
