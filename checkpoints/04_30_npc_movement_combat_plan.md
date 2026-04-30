# Чекпоинт: Планирование базовой системы движения и боевой системы NPC

**Дата:** 2026-04-30 09:18:31 UTC  
**Статус:** in_progress  
**Задача:** Аудит кода + чекпоинты внедрения базового движения NPC и боевой системы

---

## 1. АУДИТ: Текущее состояние

### 1.1 NPC Движение — КРИТИЧЕСКИЙ ПРОБЕЛ ❌

| Файл | Проблема |
|------|----------|
| `NPCController.cs` | Нет Rigidbody2D, нет кода движения. Update() — только UpdateLifespan() |
| `NPCAI.cs` | `ExecuteWandering()` — пусто, `ExecutePatrolling()` — пусто, `ExecuteFollowing()` — пусто, `ExecuteFleeing()` — только таймер (10с → Idle), `ExecuteAttacking()` — пусто |
| Phase19 | NPC спавнится без Rigidbody2D |
| Документация | NPC_AI_SYSTEM.md упоминает NavMeshAgent, но в коде его нет |

**Вывод:** NPC полностью неподвижны. AI решает состояние, но не может его выполнить.

### 1.2 NPC Боевая система — КРИТИЧЕСКИЙ ПРОБЕЛ ❌

| Проблема | Детали |
|----------|--------|
| NPCController **НЕ** реализует ICombatant | CombatManager, DamageCalculator, HitDetector — все требуют ICombatant |
| TakeDamage() — мимо пайплайна | `state.CurrentHealth -= damage` — без Qi Buffer, без брони, без BodyPart |
| HandleAttack() — хардкод 10 урона | `npcController.TakeDamage(10, "player")` — без расчёта |
| NPC не может атаковать | ExecuteAttacking() — пустая заглушка |
| Нет разрешения цели | TargetId = string, но нет поиска GameObject/ICombatant по ID |

**Вывод:** Боевая система существует только для игрока. NPC не участвует в бою.

### 1.3 Что РАБОТАЕТ ✅

| Компонент | Статус |
|-----------|--------|
| NPCGenerator | ✅ Генерация с параметрами |
| Phase19NPCPlacement | ✅ Спавн 7 NPC на тестовой поляне |
| NPCVisual | ✅ Спрайт, имя, HP-бар, Attitude-цвет |
| NPCInteractable | ⚠️ Частично — 12 взаимодействий, но Attack — хардкод |
| NPCAI.MakeDecision() | ✅ Решение принимается, PersonalityTrait-веса работают |
| NPCAI.Threat system | ✅ AddThreat / DecayThreats / GetHighestThreat |
| RelationshipController | ✅ Полная система отношений |
| CombatManager + 10-слойный пайплайн | ✅ Работает для Player↔NPC (если NPC — ICombatant) |
| PlayerController.ICombatant | ✅ Полная реализация интерфейса |

### 1.4 Связь с черновиками ИИ (docs_old/)

| Документ | Что взять |
|----------|-----------|
| NPC_AI_THEORY.md | Вариант A (State Machine) — рекомендуется для MVP. Соответствует текущей NPCAI |
| NPC_AI_NEUROTHEORY.md | Spinal AI (рефлексы) — оставить на будущее. Базовые рефлексы (dodge, flinch) можно добавить позже |
| NPC_COMBAT_INTERACTIONS.md | NPC→Player формулы урона, Level Suppression для NPC, агрессия-система |
| NPC_AI_SYSTEM.md (docs/) | 3-уровневая архитектура (BehaviourTree → Spinal → StateMachine) — фаза 2 |

---

## 2. ЧЕКПОИНТЫ ВНЕДРЕНИЯ

### ЧЕКПОИНТ 1: NPC Movement Foundation
**Приоритет:** КРИТИЧЕСКИЙ  
**Оценка:** ~3 часа  
**Зависимости:** нет

**Задачи:**
1. Добавить `Rigidbody2D` в NPCController (или гарантировать через Phase19/NPCSceneSpawner)
2. Создать `NPCMovement.cs` — компонент движения NPC
   - `MoveTo(Vector3 target, float speed)` — движение к точке
   - `WanderAround(Vector3 center, float radius)` — случайное блуждание
   - `FleeFrom(Vector3 source, float speed)` — бегство от точки
   - `FollowTarget(Transform target, float stopDistance)` — следование
   - `Stop()` — остановка
3. Реализовать `NPCAI.ExecuteXxx()` через NPCMovement:
   - `ExecuteWandering()` → `NPCMovement.WanderAround()`
   - `ExecutePatrolling()` → `NPCMovement.MoveTo(patrolPoint)`
   - `ExecuteFollowing()` → `NPCMovement.FollowTarget()`
   - `ExecuteFleeing()` → `NPCMovement.FleeFrom(threatPosition)`
   - `ExecuteAttacking()` → подход к цели + атака
4. Настройка скорости по роли (Monster быстрее, Elder медленнее)
5. Flip спрайта по направлению движения

**Критерии приёмки:**
- NPC блуждает, патрулирует, следует, убегает
- Движение плавное, через Rigidbody2D.linearVelocity
- Спрайт разворачивается по направлению движения
- NPCVisual корректно обновляется при движении

---

### ЧЕКПОИНТ 2: NPC ICombatant Integration
**Приоритет:** КРИТИЧЕСКИЙ  
**Оценка:** ~4 часа  
**Зависимости:** Чекпоинт 1 (нужен подход к цели)

**Задачи:**
1. `NPCController : ICombatant` — реализовать интерфейс
   - Свойства: Name, CultivationLevel, Strength, Agility, CurrentQi, MaxQi, etc.
   - Делегирование к QiController, BodyController
   - GetAttackerParams() / GetDefenderParams()
   - TakeDamage(BodyPartType, float) — через BodyController (10-слойный пайплайн!)
   - SpendQi / AddQi — через QiController
2. Заменить `NPCController.TakeDamage(int damage, string attackerId)` на пайплайн:
   - Урон → CombatManager.ExecuteAttack() → DamageCalculator → BodyController
   - Сохранить AddThreat() при получении урона
3. Обновить `NPCInteractable.HandleAttack()`:
   - Вместо `TakeDamage(10)` → `CombatManager.InitiateCombat(player, npc)`
   - Или `CombatManager.ExecuteBasicAttack(player, npc)`
4. NPCSceneSpawner — добавить BodyController + QiController при спавне

**Критерии приёмки:**
- NPCController реализует ICombatant полностью
- Player атака по NPC проходит через 10-слойный пайплайн
- Qi Buffer работает для NPC
- Level Suppression работает для NPC
- Body damage распределяется по частям тела
- NPC умирает корректно (Die() + OnNPCDeath)

---

### ЧЕКПОИНТ 3: NPC Attack System
**Приоритет:** ВЫСОКИЙ  
**Оценка:** ~3 часа  
**Зависимости:** Чекпоинт 1 + 2

**Задачи:**
1. Реализовать `NPCAI.ExecuteAttacking()`:
   - Проверка дальности (attackRange)
   - Проверка кулдауна атаки (attackCooldown)
   - Если в зоне досягаемости → CombatManager.ExecuteBasicAttack(npc, target)
   - Если далеко → NPCMovement.MoveTo(target.position)
2. Разрешение цели (Target Resolution):
   - `ICombatant FindTargetById(string targetId)` — поиск в сцене
   - Кэширование Transform для производительности
   - При потере цели → поиск новой или возврат Idle
3. NPC использование техник:
   - `NPCAI.SelectTechnique()` — выбор техники на основе ситуации
   - `CombatManager.ExecuteTechniqueAttack(npc, target, technique)`
4. Кулдаун атак:
   - `attackCooldown` — базовый 1.5с, модификатор от Agility
   - `techniqueCooldown` — из TechniqueData

**Критерии приёмки:**
- NPC атакует игрока при Attitude.Hostile/Hatred
- NPC атакует при получении урона (контратака)
- Атака идёт через CombatManager и полный пайплайн
- NPC использует техники (если есть TechniqueController)
- Кулдауны работают

---

### ЧЕКПОИНТ 4: Combat Response System
**Приоритет:** ВЫСОКИЙ  
**Оценка:** ~2 часа  
**Зависимости:** Чекпоинт 2 + 3

**Задачи:**
1. Автоответ на атаку:
   - `NPCController.TakeDamage()` → добавить Threat + сменить состояние на Attacking
   - Проверка: `ShouldFlee()` → если HP < 20% и cautious > aggressive → Fleeing
   - Иначе → SetTarget(attacker) → Attacking
2. Aggro Radius:
   - `NPCAI.Update()` → проверка `Physics2D.OverlapCircle()` для обнаружения игрока
   - При обнаружении + Attitude.Hostile → немедленная атака
   - При Attitude.Neutral → добавить Threat (наблюдение)
   - При Attitude.Friendly → игнорировать
3. Flee-to-safety:
   - Бегство в направлении от угрозы
   - После 10с без урона → попытка спрятаться (Idle)
   - Если HP < 10% → Speed boost (adrenaline)
4. Смерть в бою:
   - `NPCController.Die("combat")` → CombatManager.EndCombat()
   - Отписка от событий
   - Визуальный эффект (tint красным, затухание)

**Критерии приёмки:**
- NPC реагирует на атаку: контратака или бегство
- Монстры агрятся при входе в aggroRange
- Guard/Elder атакуют только при провокации
- Fleeing NPC убегает корректно
- Смерть NPC завершает бой корректно

---

## 3. ПОРЯДОК ВНЕДРЕНИЯ

```
ЧЕКПОИНТ 1 (Movement)
    ↓
ЧЕКПОИНТ 2 (ICombatant)  ← можно начать параллельно с 1
    ↓
ЧЕКПОИНТ 3 (Attack)      ← зависит от 1+2
    ↓
ЧЕКПОИНТ 4 (Response)    ← зависит от 2+3
```

**Рекомендация:** Чекпоинты 1 и 2 можно делать параллельно — Movement не зависит от ICombatant, а ICombatant не зависит от Movement. Чекпоинт 3 требует оба.

---

## 4. ФАЙЛЫ ДЛЯ СОЗДАНИЯ/РЕДАКТИРОВАНИЯ

### Новые файлы:
| Файл | Назначение |
|------|------------|
| `Scripts/NPC/NPCMovement.cs` | Компонент движения NPC |

### Редактируемые файлы:
| Файл | Изменения |
|------|-----------|
| `Scripts/NPC/NPCController.cs` | +ICombatant, Rigidbody2D, TakeDamage через пайплайн |
| `Scripts/NPC/NPCAI.cs` | +ExecuteXxx() реализация, +FindTarget(), +AggroRadius |
| `Scripts/NPC/NPCInteractable.cs` | HandleAttack → CombatManager |
| `Scripts/Editor/NPCSceneSpawner.cs` | +Rigidbody2D, +BodyController при спавне |

---

## 5. РИСКИ И ОГРАНИЧЕНИЯ

1. **NavMesh vs Rigidbody2D:** Документация упоминает NavMeshAgent, но для 2D URP проще и правильнее использовать Rigidbody2D + простое движение к точке. NavMesh в 2D — избыточен.
2. **BodyController на NPC:** Текущий BodyController требует настройки. Если BodyController отсутствует — fallback на простой HP-pool.
3. **QiController на NPC:** Аналогично — если нет QiController, Qi Buffer не работает. Нужен fallback.
4. **Производительность:** 7 NPC с Update() — нормально. 50+ — нужен LoD (docs_old/NPC_AI_THEORY.md §"LoD AI system").

---

*Создано: 2026-04-30 09:18:31 UTC*
