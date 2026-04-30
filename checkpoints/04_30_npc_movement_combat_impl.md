# Чекпоинт: Детальное внедрение NPC Movement + Combat

**Дата:** 2026-04-30 09:36:12 UTC  
**Статус:** complete  
**Источник:** checkpoints/04_30_npc_movement_combat_plan.md  
**Задача:** Реализация базового движения NPC, интеграция ICombatant, боевая система и ответ на атаки

---

## 1. АРХИТЕКТУРНЫЕ РЕШЕНИЯ

### 1.1 NPCController + ICombatant → ЯВНАЯ реализация интерфейса
**Решение:** NPCController реализует ICombatant напрямую (как PlayerController), а НЕ через CombatantBase.

**Причины:**
- NPCController уже имеет [SerializeField] ссылки на bodyController, qiController, techniqueController
- CombatantBase имеет свои Awake/OnDestroy, конфликтующие с NPCController
- PlayerController использует тот же подход — explicit interface implementation
- Меньше риск сломать существующий код

**Паттерн:**
```csharp
string ICombatant.Name => NpcName;
int ICombatant.CultivationLevel => (int)(state?.CultivationLevel ?? Core.CultivationLevel.None);
void ICombatant.TakeDamage(BodyPartType part, float damage) { ... }
```

### 1.2 NPCMovement — отдельный компонент (НЕ MonoBehaviour с Update)
**Решение:** NPCMovement — это компонент с Update(), вызываемый NPCAI.

**Методы:**
- `MoveTo(Vector3 target, float speed)` — движение к точке
- `WanderAround(Vector3 center, float radius, float speed)` — случайное блуждание
- `FleeFrom(Vector3 source, float speed)` — бегство от точки
- `FollowTarget(Transform target, float stopDistance, float speed)` — следование
- `Stop()` — остановка

**Движение:** через `Rigidbody2D.linearVelocity` (как PlayerController)
**Разворот:** flip SpriteRenderer по X (как PlayerController)

### 1.3 NPCAI.ExecuteXxx() → через NPCMovement + CombatManager
- `ExecuteWandering()` → NPCMovement.WanderAround() + Flip
- `ExecutePatrolling()` → NPCMovement.MoveTo(patrolPoint[currentIndex])
- `ExecuteFollowing()` → NPCMovement.FollowTarget()
- `ExecuteFleeing()` → NPCMovement.FleeFrom() + 10с таймер → Idle
- `ExecuteAttacking()` → проверка дальности → CombatManager.ExecuteBasicAttack()

### 1.4 TakeDamage пайплайн
**Старый путь (убрать):** `state.CurrentHealth -= damage`
**Новый путь:** `CombatManager.ExecuteAttack()` → `DamageCalculator` → `ICombatant.TakeDamage(part, damage)` → `BodyController.TakeDamage(part, damage)`

---

## 2. ФАЙЛЫ ДЛЯ СОЗДАНИЯ/РЕДАКТИРОВАНИЯ

### Новый файл:
| Файл | Назначение |
|------|------------|
| `Scripts/NPC/NPCMovement.cs` | Компонент движения NPC через Rigidbody2D |

### Редактируемые файлы:
| Файл | Изменения |
|------|-----------|
| `Scripts/NPC/NPCController.cs` | +ICombatant, +OnDeath/OnDamageTaken/OnQiChanged events, TakeDamage через пайплайн |
| `Scripts/NPC/NPCAI.cs` | +NPCMovement ref, +ExecuteXxx() реализация, +FindTarget(), +AggroRadius, +attackCooldown |
| `Scripts/NPC/NPCInteractable.cs` | HandleAttack → CombatManager.ExecuteBasicAttack() |
| `Scripts/Editor/NPCSceneSpawner.cs` | +NPCMovement при спавне |

---

## 3. ДЕТАЛЬНЫЙ ПЛАН ПО ФАЙЛАМ

### 3.1 NPCMovement.cs (НОВЫЙ)

```
Поля:
- Rigidbody2D rb
- SpriteRenderer spriteRenderer
- float baseSpeed = 3f
- float wanderRadius = 5f
- Vector3 wanderCenter
- Vector3 currentTarget
- float wanderTimer
- float wanderInterval = 3f
- bool isMoving

Методы:
- void Awake() — кэшировать rb, spriteRenderer
- void MoveTo(Vector3 target, float speed) — установить velocity к target
- void WanderAround(Vector3 center, float radius, float speed)
  - Выбрать случайную точку в radius от center
  - Двигаться к ней; при достижении — ждать, выбрать новую
- void FleeFrom(Vector3 source, float speed) — двигаться от source
- void FollowTarget(Transform target, float stopDistance, float speed)
- void Stop() — velocity = 0
- void UpdateFacing(Vector2 direction) — flip spriteRenderer.flipX
- float DistanceTo(Vector3 target) — utility
- bool HasReachedTarget(Vector3 target, float threshold = 0.1f) — utility
- Vector3 GetRandomPointAround(Vector3 center, float radius) — utility
```

### 3.2 NPCController.cs (РЕДАКТИРОВАНИЕ)

**Добавить:**
```
+ using CultivationGame.Combat;
+ event Action OnDeath (ICombatant)
+ event Action<float> OnDamageTaken (ICombatant)
+ event Action<long, long> OnQiChanged (ICombatant)
+ Явная реализация ICombatant:
  - string ICombatant.Name => NpcName
  - GameObject ICombatant.GameObject => gameObject
  - int ICombatant.CultivationLevel => (int)(state?.CultivationLevel ?? Core.CultivationLevel.None)
  - int ICombatant.CultivationSubLevel => state?.SubLevel ?? 0
  - int ICombatant.Strength => (int)(state?.BodyStrength ?? 10)
  - int ICombatant.Agility => (int)(state?.BodyStrength ?? 10) // TODO: dedicated field
  - int ICombatant.Intelligence => (int)(state?.Intelligence ?? 10)
  - int ICombatant.Vitality => (int)(state?.Constitution ?? 10)
  - long ICombatant.CurrentQi => state?.CurrentQi ?? 0
  - long ICombatant.MaxQi => state?.MaxQi ?? 0
  - float ICombatant.QiDensity => qiController?.QiDensity ?? 1f
  - QiDefenseType ICombatant.QiDefense => qiController?.QiDefense ?? QiDefenseType.RawQi
  - bool ICombatant.HasShieldTechnique => false // TODO
  - BodyMaterial ICombatant.BodyMaterial => bodyController?.BodyMaterial ?? BodyMaterial.Organic
  - float ICombatant.HealthPercent => (float)(state?.CurrentHealth ?? 0) / Mathf.Max(1, state?.MaxHealth ?? 1)
  - bool ICombatant.IsAlive => IsAlive
  - int ICombatant.Penetration => 0
  - float ICombatant.DodgeChance => DefenseProcessor.CalculateDodgeChance((int)(state?.BodyStrength ?? 10), 0f)
  - float ICombatant.ParryChance => DefenseProcessor.CalculateParryChance((int)(state?.BodyStrength ?? 10), 0f)
  - float ICombatant.BlockChance => DefenseProcessor.CalculateBlockChance((int)(state?.BodyStrength ?? 10), 0f)
  - float ICombatant.ArmorCoverage => 0f
  - float ICombatant.DamageReduction => 0f
  - int ICombatant.ArmorValue => 0
  - void ICombatant.TakeDamage(BodyPartType part, float damage)
      → bodyController.TakeDamage(part, damage)
      → OnDamageTaken?.Invoke(damage)
      → aiController.AddThreat() (сохранить текущую логику)
      → Проверить смерть через bodyController.IsAlive
  - void ICombatant.TakeDamageRandom(float damage)
      → bodyController.TakeDamageRandom(damage)
      → OnDamageTaken?.Invoke(damage)
  - bool ICombatant.SpendQi(long amount) => qiController?.SpendQi(amount) ?? false
  - void ICombatant.AddQi(long amount) => qiController?.AddQi(amount)
  - AttackerParams ICombatant.GetAttackerParams(Element element)
  - DefenderParams ICombatant.GetDefenderParams()
```

**Изменить:**
- Старый `TakeDamage(int damage, string attackerId)` → пометить [Obsolete], новый через ICombatant
- Die() → дополнительно вызывать OnDeath?.Invoke()
- Подписка на bodyController.OnDeath для автосмерти

### 3.3 NPCAI.cs (РЕДАКТИРОВАНИЕ)

**Добавить:**
```
+ NPCMovement movement
+ float attackCooldown = 1.5f
+ float lastAttackTime
+ float attackRange = 1.5f
+ Transform cachedTargetTransform
+ float aggroCheckInterval = 0.5f
+ float aggroCheckTimer
```

**Изменить ExecuteWandering():**
```csharp
movement.WanderAround(homePosition, wanderRadius, speed);
```

**Изменить ExecutePatrolling():**
```csharp
movement.MoveTo(patrolPoints[currentPatrolIndex], speed);
if (movement.HasReachedTarget(patrolPoints[currentPatrolIndex]))
    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
```

**Изменить ExecuteFollowing():**
```csharp
var target = FindTargetTransform(state.TargetId);
if (target != null)
    movement.FollowTarget(target, stopDistance: 1.5f, speed);
```

**Изменить ExecuteFleeing():**
```csharp
var threat = FindTargetTransform(GetHighestThreat());
if (threat != null)
    movement.FleeFrom(threat.position, speed * 1.3f);
// Существующий таймер 10с → Idle
```

**Изменить ExecuteAttacking():**
```csharp
var target = FindTargetTransform(state.TargetId);
if (target == null) { ChangeState(NPCAIState.Idle); return; }

float dist = Vector3.Distance(transform.position, target.position);
if (dist > attackRange)
{
    // Подходим к цели
    movement.MoveTo(target.position, speed);
}
else
{
    // В зоне атаки
    movement.Stop();
    if (Time.time - lastAttackTime >= attackCooldown)
    {
        lastAttackTime = Time.time;
        // Получить ICombatant для NPC и цели
        var npcCombatant = npcController as ICombatant;
        var targetCombatant = target.GetComponent<ICombatant>();
        if (npcCombatant != null && targetCombatant != null)
        {
            CombatManager.Instance?.ExecuteBasicAttack(npcCombatant, targetCombatant);
        }
    }
}
```

**Добавить AggroRadius():**
```csharp
// В Update, каждый aggroCheckInterval
if (state.Attitude == Attitude.Hostile || state.Attitude == Attitude.Hatred)
{
    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aggroRange, playerLayerMask);
    // Если Player найден → SetTarget("player"), ChangeState(Attacking)
}
```

**Добавить FindTargetTransform():**
```csharp
private Transform FindTargetTransform(string targetId)
{
    // Поиск через GameObject.Find (для "player")
    // Или через кэшированный список NPC
}
```

### 3.4 NPCInteractable.cs (РЕДАКТИРОВАНИЕ)

**Изменить HandleAttack():**
```csharp
private InteractionResult HandleAttack(InteractionController player)
{
    var result = new InteractionResult
    {
        Type = InteractionType.Attack,
        Success = true,
        Message = $"Вы атакуете {npcController.NpcName}!",
        RelationshipChange = -50
    };

    // Получаем ICombatant для NPC и Player
    var npcCombatant = npcController as ICombatant;
    var playerCombatant = player?.GetComponent<PlayerController>() as ICombatant;
    
    if (npcCombatant != null && playerCombatant != null && CombatManager.Instance != null)
    {
        CombatManager.Instance.InitiateCombat(playerCombatant, npcCombatant);
        CombatManager.Instance.ExecuteBasicAttack(playerCombatant, npcCombatant);
    }
    else
    {
        // Fallback: старый метод без пайплайна
        npcController.TakeDamage(10, "player");
    }

    npcController.ModifyRelationship("player", result.RelationshipChange);
    return result;
}
```

### 3.5 NPCSceneSpawner.cs (РЕДАКТИРОВАНИЕ)

**Добавить после строки `var interactable = go.AddComponent<NPCInteractable>();`:**
```csharp
var movement = go.AddComponent<NPCMovement>();
```

---

## 4. КРИТИЧЕСКИЕ ЗАВИСИМОСТИ

1. **Rigidbody2D** — NPCSceneSpawner уже добавляет его (Dynamic, gravityScale=0)
2. **BodyController** — NPCSceneSpawner уже добавляет его
3. **QiController** — NPCSceneSpawner уже добавляет его
4. **CombatManager.Instance** — singleton на сцене; нужен для NPC→Player атаки
5. **ICombatant на PlayerController** — уже реализован

---

## 5. ПОРЯДОК КОДИРОВАНИЯ

1. NPCMovement.cs (новый, независимый)
2. NPCController.cs (+ICombatant)
3. NPCAI.cs (+NPCMovement, ExecuteXxx, aggro)
4. NPCInteractable.cs (HandleAttack → CombatManager)
5. NPCSceneSpawner.cs (+NPCMovement)

---

*Создано: 2026-04-30 09:36:12 UTC*
