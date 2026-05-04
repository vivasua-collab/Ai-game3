# 📦 Кодовая база: Система боя — аудит

👉 Основной план: [05_04_combat_system.md](05_04_combat_system.md)
**Дата:** 2026-05-04 04:10 UTC
**Статус:** in_progress

---

## Аудит существующего кода

### Структура Combat/ (17 файлов)

```
Assets/Scripts/Combat/
├── CombatManager.cs           v1.3  ✅ — Центральный менеджер боя
├── DamageCalculator.cs        v1.1  ✅ — Калькулятор урона (10 слоёв)
├── DefenseProcessor.cs        v1.1  ✅ — Обработка защит
├── QiBuffer.cs                v1.1  ✅ — Буфер Ци (int→long)
├── LevelSuppression.cs        v1.0  ✅ — Подавление уровнем
├── TechniqueCapacity.cs       v1.0  ✅ — Ёмкость техник
├── TechniqueController.cs     v1.0  ✅ — Управление техниками
├── HitDetector.cs             v1.0  ⚠️ — Определение попаданий (3D→2D)
├── Combatant.cs               v1.0  ✅ — ICombatant + CombatantBase
├── CombatEvents.cs            v1.0  ✅ — Система событий
├── OrbitalSystem/
│   ├── OrbitalWeapon.cs       —     ✅ — Орбитальное оружие
│   └── OrbitalWeaponController.cs — ✅ — Контроллер орбит. оружия
└── Effects/
    ├── TechniqueEffect.cs     —     ✅ — Базовый класс эффектов
    ├── DirectionalEffect.cs   —     ✅ — Направленный эффект
    ├── ExpandingEffect.cs     —     ✅ — Расширяющийся эффект
    ├── FormationArrayEffect.cs—     ✅ — Эффект формации
    └── TechniqueEffectFactory.cs—   ✅ — Фабрика эффектов
```

### Связанные файлы

```
Assets/Scripts/
├── Player/PlayerController.cs  v1.2  — Реализует ICombatant (явно)
├── NPC/NPCController.cs        —     — Реализует ICombatant (явно)
├── NPC/NPCAI.cs                —     — AI контроллер (без боевых решений)
└── UI/CombatUI.cs              —     — UI боя (скелет, не подключён)
```

---

## Ключевые проблемы в существующем коде

### 1. HitDetector — 3D вместо 2D

**Файл:** `HitDetector.cs`
**Проблема:** Все методы используют `Physics.OverlapSphere`, `Physics.Raycast`
**Строки:**
- L102: `Physics.OverlapSphere` → нужен `Physics2D.OverlapCircleAll`
- L151: `Physics.OverlapSphere` → нужен `Physics2D.OverlapCircleAll`
- L235: `Physics.Raycast` → нужен `Physics2D.Raycast`
- L258: `Physics.Raycast` → нужен `Physics2D.Raycast`
- L334: `Physics.OverlapSphere` → нужен `Physics2D.OverlapCircleAll`
- L407: `GetComponent<Collider>()` → нужен `Collider2D`

### 2. TechniqueController — мгновенное использование

**Файл:** `TechniqueController.cs`
**Проблема:** UseTechnique() мгновенно тратит Ци и устанавливает кулдаун. Нет фазы накачки.
**Строки:**
- L237-282: `UseTechnique()` — Ци тратится сразу, кулдаун ставится сразу
- Нет понятия "charging" — техника срабатывает мгновенно

**Необходимые изменения:**
- Добавить `TechniqueChargeData activeCharge`
- Заменить `UseTechnique()` на `BeginCharge()` / `CompleteCharge()` / `CancelCharge()`
- В `Update()` — обрабатывать прогресс накачки
- Новые события: `OnChargeStarted`, `OnChargeProgress`, `OnChargeCompleted`, `OnChargeInterrupted`

### 3. CombatManager — синхронный бой

**Файл:** `CombatManager.cs`
**Проблема:** Бой не подключён к gameplay. InitiateCombat() нигде не вызывается.
**Строки:**
- L175: `InitiateCombat()` — нет вызова из Player/NPC
- L159-166: `Update()` — только проверка конца боя и таймаута
- Нет цикла AI решений для NPC

**Необходимые изменения:**
- Подключить CombatTrigger для автоматической инициации
- Добавить Update-driven цикл: обработка накачки, AI решения, прерывания
- Вызывать CombatAI.GetNextAction() для NPC

### 4. NPCController — нет боевого AI

**Файл:** `NPCController.cs`
**Проблема:** NPC реализует ICombatant, но не имеет боевых решений
**Строки:**
- L37: `class NPCController : MonoBehaviour, ICombatant` — интерфейс реализован
- L46: `NPCAI aiController` — AI есть, но без боевых решений

### 5. PlayerController — нет боевого ввода

**Файл:** `PlayerController.cs`
**Проблема:** Нет обработки ввода для боя (клавиши 1-9, пробел)
**Строки:**
- L34: `class PlayerController : MonoBehaviour, ICombatant` — интерфейс реализован
- L298-328: `ProcessInput()` — только движение, медитация, добыча
- Нет проверки CombatManager.IsInCombat
- Нет обработки клавиш техник (1-9)
- Нет обработки базовой атаки (пробел)

### 6. CombatUI — не подключён

**Файл:** `CombatUI.cs`
**Проблема:** UI создан как скелет, но не получает данные из CombatManager
- Нет подписки на CombatEvents
- Нет обновления от CombatManager
- Нет полоски накачки

### 7. DamageCalculator — отсутствующие слои

**Файл:** `DamageCalculator.cs`
**Проблема:** Слои 1b (урон оружия) и 3b (бафф формаций) не реализованы
**Строки:**
- L119-128: Слой 1 — есть
- Нет: Слой 1b (weapon bonus для melee_weapon)
- L136-143: Слой 2 — есть
- L163: Слой 3 — есть
- Нет: Слой 3b (formationBuffMultiplier)

---

## ICombatant — текущий интерфейс

```csharp
public interface ICombatant
{
    string Name { get; }
    GameObject GameObject { get; }
    int CultivationLevel { get; }
    int CultivationSubLevel { get; }
    int Strength { get; }
    int Agility { get; }
    int Intelligence { get; }
    int Vitality { get; }
    long CurrentQi { get; }
    long MaxQi { get; }
    float QiDensity { get; }
    QiDefenseType QiDefense { get; }
    bool HasShieldTechnique { get; }
    BodyMaterial BodyMaterial { get; }
    float HealthPercent { get; }
    bool IsAlive { get; }
    int Penetration { get; }
    float DodgeChance { get; }
    float ParryChance { get; }
    float BlockChance { get; }
    float ArmorCoverage { get; }
    float DamageReduction { get; }
    int ArmorValue { get; }
    void TakeDamage(BodyPartType part, float damage);
    void TakeDamageRandom(float damage);
    bool SpendQi(long amount);
    void AddQi(long amount);
    AttackerParams GetAttackerParams(Element attackElement = Element.Neutral);
    DefenderParams GetDefenderParams();
    event Action OnDeath;
    event Action<float> OnDamageTaken;
    event Action<long, long> OnQiChanged;
}
```

**Необходимые расширения для Charge System:**
- `bool IsCharging { get; }` — накачивает ли сейчас технику
- `TechniqueChargeData ActiveCharge { get; }` — текущая накачка
- `void InterruptCharge(ChargeInterruptReason reason)` — прервать накачку

---

## TechniqueController — текущая структура

```csharp
public class TechniqueController : MonoBehaviour
{
    // Поля
    private QiController qiController;
    private List<LearnedTechnique> learnedTechniques;
    private LearnedTechnique[] quickSlots;
    
    // Ключевые методы:
    public bool LearnTechnique(TechniqueData technique, float initialMastery)
    public bool CanUseTechnique(LearnedTechnique technique)
    public TechniqueUseResult UseTechnique(LearnedTechnique technique)  // ← МГНОВЕННЫЙ
    public TechniqueUseResult UseQuickSlot(int slot)                    // ← МГНОВЕННЫЙ
    
    // Расчёты:
    private long CalculateQiCost(LearnedTechnique technique)
    private int CalculateCapacity(LearnedTechnique technique)
    private int CalculateDamage(LearnedTechnique technique)
    private float CalculateCastTime(LearnedTechnique technique)
    
    // Кулдауны:
    private void ProcessCooldowns()
}
```

**Необходимые добавления для Charge System:**
```csharp
// Новые поля:
private TechniqueChargeData activeCharge;
private bool isCharging = false;

// Новые методы:
public bool BeginCharge(LearnedTechnique technique)     // Начать накачку
public void CancelCharge()                               // Отменить (игрок)
public void InterruptCharge(ChargeInterruptReason reason)// Прервать (урон/стан)
private void ProcessCharge()                             // Обработка в Update
private TechniqueUseResult CompleteCharge()              // Завершить накачку → срабатывание

// Новые свойства:
public bool IsCharging => isCharging;
public TechniqueChargeData ActiveCharge => activeCharge;

// Новые события:
public event Action<LearnedTechnique> OnChargeStarted;
public event Action<LearnedTechnique, float> OnChargeProgress;
public event Action<LearnedTechnique, TechniqueUseResult> OnChargeCompleted;
public event Action<LearnedTechnique, ChargeInterruptReason> OnChargeInterrupted;
```

---

## Техника использования — поток ДО и ПОСЛЕ

### ДО (текущий):
```
Нажатие клавиши → UseTechnique() → мгновенная трата Ци → мгновенный кулдаун → мгновенный результат
```

### ПОСЛЕ (с накачкой):
```
Нажатие клавиши → BeginCharge() → [накачка 0.5-3 сек] → CompleteCharge() → трата Ци → кулдаун → результат
                                                        ↘ InterruptCharge() → частичный возврат Ци
                                                        ↘ CancelCharge() → возврат 70% Ци
```

---

*Создано: 2026-05-04 04:10 UTC*
