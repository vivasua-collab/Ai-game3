# КОНСОЛИДИРОВАННЫЙ АУДИТ — Cultivation World Simulator

**Дата консолидации:** 2026-04-10  
**Проект:** Cultivation World Simulator v0.1.0-alpha  
**Unity:** 6000.3 (2D URP)  
**Файлов просканировано:** 115 C# файлов, 21+ система  
**Аудиторов:** 7 параллельных агентов + ручная верификация  

---

## 📊 Сводная статистика (дедуплицировано)

| Категория | Уникальных проблем |
|-----------|-------------------|
| 🔴 CRITICAL | 32 |
| 🟠 HIGH | 49 |
| 🟡 MEDIUM | 63 |
| 🟢 LOW | 47 |
| **Итого** | **191** |

> **Примечание:** Дедупликация выполнена путём объединения пересекающихся проблем из разных файлов. Маппинг оригинальных ID → консолидированных ID приведён в Приложении A.

---

## 📁 Индекс источников аудита

| # | Файл | Охват | Проблем |
|---|------|-------|---------|
| [S1](./AUDIT_2026-04-10.md) | Основной аудит (первый проход) | 12 ключевых файлов | 58 |
| [S2](./audit_core_combat.md) | Core + Combat | 25 файлов | 38 |
| [S3](./audit_body_qi_player.md) | Body + Qi + Player + Managers + Buff | 11 файлов | 56 |
| [S4](./audit_world_npc_formation.md) | World + NPC + Formation + Save + Inv + Quest | 28 файлов | 60 |
| [S5](./audit_data_tile_ui_gen.md) | Data + Tile + UI + Charger (частично) | 9 файлов | 7 |
| [S6](./audit_batch3_supplement.md) | Batch 3: Data/SO + Gen/Editor + UI/Char/Tile/Tests | 52 файла | 57 |
| [S7](./AUDIT_VERIFICATION.md) | Верификация (Unity docs + Qwen) | — | — |
| [S8](./CODE_AUDIT_Unity_6.3.md) | Unity 6.3 совместимость (ранний аудит) | — | 5+8 |

**Связанные аналитические файлы:**
- [ANALYSIS_REPORT.md](./ANALYSIS_REPORT.md) — Анализ документации
- [!CONTRADICTIONS_REPORT_v2.md](./!CONTRADICTIONS_REPORT_v2.md) — Противоречия в документации
- [!DUPLICATION_REPORT.md](./!DUPLICATION_REPORT.md) — Дублирование в документации

---

## Содержание

1. [Core System](#1-core-system)
2. [Combat System](#2-combat-system)
3. [Qi System](#3-qi-system)
4. [Body System](#4-body-system)
5. [Player System](#5-player-system)
6. [Buff System](#6-buff-system)
7. [NPC System](#7-npc-system)
8. [World System](#8-world-system)
9. [Formation System](#9-formation-system)
10. [Save System](#10-save-system)
11. [Inventory/Equipment/Crafting System](#11-inventoryequipmentcrafting-system)
12. [Quest System](#12-quest-system)
13. [Interaction/Dialogue System](#13-interactiondialogue-system)
14. [Charger System](#14-charger-system)
15. [Data/ScriptableObjects](#15-datascriptableobjects)
16. [Generators/Editor](#16-generatorseditor)
17. [UI System](#17-ui-system)
18. [Tile System](#18-tile-system)
19. [Character System](#19-character-system)
20. [Managers](#20-managers)
21. [Tests](#21-tests)
22. [Кросс-системные проблемы](#22-кросс-системные-проблемы)
23. [Приоритетный план исправлений](#23-приоритетный-план-исправлений)
24. [Приложение A: Маппинг ID](#приложение-a-маппинг-id)

---

## 1. Core System

> **Файлы:** Constants.cs, Enums.cs, StatDevelopment.cs, GameEvents.cs, ServiceLocator.cs, VFXPool.cs, Camera2DSetup.cs, GameSettings.cs  
> **Источник:** S1, S2, S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CORE-C01 | Heart не должен иметь blackHP, но имеет | BodyPart.cs:93-95 | S1(C-03), S3 |
| CORE-C02 | StatDevelopment: характеристики <10 не могут расти (threshold=0) | StatDevelopment.cs:322-333 | S1(C-08), S2(C-NEW-02) |
| CORE-C03 | StatDevelopment.ConsolidateStat — availableConsolidation вычислен но не использован; консолидация не работает для stat≥10 | StatDevelopment.cs:415 | S2(C-NEW-02) |
| CORE-C04 | [SerializeField] на не-MonoBehaviour (StatDevelopment) | StatDevelopment.cs:86-88 | S1(C-07), S2(L-NEW-01) |
| CORE-C05 | BodyMaterialReduction/Hardness не содержит Construct — KeyNotFoundException | Constants.cs:381-403 | S2(C-NEW-01), S1(M-02) |
| CORE-C06 | RegenerationMultipliers[9] = float.PositiveInfinity | Constants.cs:217 | S1(H-06→CRIT) |
| CORE-C07 | GameConstants.MAX_STAT_VALUE=100 конфликтует с StatDevelopment.MAX_STAT_VALUE=1000 | Constants.cs:48 vs StatDevelopment.cs:119 | S2(H-NEW-02) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CORE-H01 | OppositeElements не содержит Lightning/Void/Poison/Neutral | Constants.cs:591-597 | S2(H-NEW-01), S1(M-03) |
| CORE-H02 | GameEvents — статические события без автоотписки при смене сцены | GameEvents.cs | S1(H-05), S2(L-NEW-02) |
| CORE-H03 | VFXPool — глобальный лимит вместо per-prefab | VFXPool.cs:37,113 | S1(H-03), S2(H-NEW-06) |
| CORE-H04 | AwakeningChance — единицы измерения неоднозначны (% vs вероятность) | Constants.cs:105-114 | S2(M-NEW-12) |
| CORE-H05 | Element.Count в enum — итерация даёт фейковый элемент | Enums.cs:103 | S2(M-NEW-04), S6(DA-M-03) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CORE-M01 | Disposition смешивает отношение и характер в одном enum | Enums.cs:452-461 | S1(M-16), S2(M-NEW-05) |
| CORE-M02 | EquipmentSlot не соответствует документации | Enums.cs:322-334 | S1(M-12) |
| CORE-M03 | AttackResult — коллизия имён struct (Combat) и enum (Core) | Enums.cs:576 vs CombatManager.cs:52 | S2(H-NEW-08) |
| CORE-M04 | ServiceLocator.pendingRequests — нет таймаута/очистки | ServiceLocator.cs:44 | S2(L-NEW-03) |
| CORE-M05 | VFXPool.Awake не вызывает DontDestroyOnLoad (несогласованность со Spawn) | VFXPool.cs:64-72 | S2(M-NEW-01) |
| CORE-M06 | VFXPool.prewarmOnStart объявлен но не реализован | VFXPool.cs:38 | S2(M-NEW-02) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CORE-L01 | Camera2DSetup.OnValidate перезаписывает ручные настройки | Camera2DSetup.cs:77-83 | S2(L-NEW-04) |
| CORE-L02 | Magic numbers (VFXPool: maxPoolSize=50, initialPoolSize=10) | VFXPool.cs | S1(L-04) |
| CORE-L03 | Debug.Log в production коде | Множество файлов | S1(L-03) |
| CORE-L04 | Отсутствие #region в больших файлах | NPCAI.cs, FormationController.cs | S1(L-01) |

---

## 2. Combat System

> **Файлы:** DamageCalculator.cs, DefenseProcessor.cs, Combatant.cs, CombatManager.cs, TechniqueController.cs, CombatEvents.cs, QiBuffer.cs, HitDetector.cs, LevelSuppression.cs, TechniqueCapacity.cs, Effects/*, OrbitalSystem/*  
> **Источник:** S1, S2, S4, S5, S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CMB-C01 | DamageCalculator игнорирует стихийные взаимодействия — CalculateElementalInteraction существует но НЕ вызывается | DamageCalculator.cs:307-314 | S1(C-11), S2(C-NEW-03) |
| CMB-C02 | AttackType всегда Technique для обычных атак (нет Normal) | DamageCalculator.cs:128 | S1(H-14), S2(C-NEW-04) |
| CMB-C03 | ParryBonus/BlockBonus двойной расчёт (финальный шанс передаётся как бонус) | Combatant.cs:282-284, DamageCalculator.cs:192-194 | S1(C-09), S2(H-NEW-03) |
| CMB-C04 | SpendQi(int) но QiController использует long — переполнение на L5+ | Combatant.cs:62, QiController.cs:162 | S1(C-04), S3 |
| CMB-C05 | DefenderParams.CurrentQi — int вместо long, усечение Qi>2.1B | Combatant.cs:275 | S1(C-05), S2(C-NEW-05) |
| CMB-C06 | DefenseProcessor использует System.Random вместо UnityEngine.Random | DefenseProcessor.cs:77,232,271 | S1(C-02), S2(H-NEW-07) |
| CMB-C07 | CombatLog подписывается в статическом конструкторе — может не выполниться | CombatEvents.cs:274-277 | S1(C-10), S2(M-NEW-06) |
| CMB-C08 | CombatantBase не отписывается от QiController.OnQiChanged (lambda leak) | Combatant.cs:195 | S1(M-09→CRIT), S2(C-NEW-06) |
| CMB-C09 | TechniqueEffectFactory.DetermineEffectType: Poison→FireSlash (загрязнение пула) | TechniqueEffectFactory.cs:254-263 | S2(H-NEW-04) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CMB-H01 | TechniqueController.IncreaseMastery игнорирует формулу из документации | TechniqueController.cs:344-349 | S1(H-12), S2(M-NEW-08) |
| CMB-H02 | TechniqueController cooldown ×60 — единицы не документированы | TechniqueController.cs:262 | S1(H-13), S2(M-NEW-09) |
| CMB-H03 | CombatManager.totalDamageDealt == totalDamageTaken | CombatManager.cs:269-270 | S1(H-15), S2(M-NEW-07) |
| CMB-H04 | CombatManager не отписывается от OnDeath корректно (null risk) | CombatManager.cs:379-383 | S1(H-11) |
| CMB-H05 | DefenderParams не содержит defenderElement — стихийная защита невозможна | DamageCalculator.cs:68-85 | S2(M-NEW-10) |
| CMB-H06 | HitDetector.GetComponent\<ICombatant\>() — дорого и ненадёжно | HitDetector.cs:109 | S2(M-NEW-11) |
| CMB-H07 | OrbitalWeaponController — race condition на _currentAngle (Update vs Coroutine) | OrbitalWeaponController.cs:98-121 | S2(M-NEW-14) |
| CMB-H08 | TechniqueEffectFactory double-dequeue из пула | TechniqueEffectFactory.cs:147-151 | S2(M-NEW-13) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CMB-M01 | QiBuffer.ProcessDamage — misleading alias для ProcessQiTechniqueDamage | QiBuffer.cs:96-102 | S2(L-NEW-08) |
| CMB-M02 | DamageResult.IsFatal — хардкод порога 50 damage | DamageCalculator.cs:297-298 | S2(L-NEW-09) |
| CMB-M03 | LevelSuppression attackIndex cast предполагает совпадение enum→array | LevelSuppression.cs:84 | S2(L-NEW-10) |
| CMB-M04 | DirectionalEffect/OrbitalWeapon — Instantiate/Destroy вместо пулинга | DirectionalEffect.cs:193-194 | S2(M-NEW-03) |
| CMB-M05 | FormationArrayEffect/ExpandingEffect — разная квалификация ICombatTarget | FormationArrayEffect.cs:112 vs ExpandingEffect.cs:126 | S2(H-NEW-05) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CMB-L01 | TechniqueEffect Pause/Stop асимметрия (Pause не деактивирует) | TechniqueEffect.cs:136,160 | S2(L-NEW-07) |
| CMB-L02 | Мёртвые буферы OverlapCircleNonAlloc (_affectedBuffer, _hitBuffer, _hitResults) | ExpandingEffect, DirectionalEffect, FormationArrayEffect, OrbitalWeapon | S2(L-NEW-05, L-NEW-06) |
| CMB-L03 | CombatManager.CheckCombatEnd вызывается каждый Update | CombatManager.cs:163 | S1(L-14) |
| CMB-L04 | Нет [RequireComponent] на CombatantBase | Combatant.cs | S1(L-15) |

---

## 3. Qi System

> **Файлы:** QiController.cs  
> **Источник:** S1, S2, S3

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QI-C01 | QiController: long→int cast теряет Qi>2.1B в бою | QiController.cs:357,364,365,371 | S3 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QI-H01 | Медитация не продвигает игровое время | QiController.cs:248-260 | S1(H-08), S3 |
| QI-H02 | coreCapacity = maxQiCapacity после прорыва использует СТАРОЕ значение | QiController.cs:308 | S3 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QI-M01 | Meditate float overflow risk при больших durationTicks | QiController.cs:251 | S3 |
| QI-M02 | baseConductivity float precision loss для больших capacities | QiController.cs:100 | S3 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QI-L01 | SpendQi(0) вызывает события и возвращает true | QiController.cs:162 | S3 |

---

## 4. Body System

> **Файлы:** BodyPart.cs, BodyController.cs, BodyDamage.cs  
> **Источник:** S1, S3

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BOD-C01 | CreateHumanoidBody HP значения не соответствуют BODY_SYSTEM.md | BodyDamage.cs:99-149 | S3 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BOD-M01 | BodyDamage.ApplyDamage и BodyPart.ApplyDamage — двойной 70/30 split | BodyDamage.cs:62 | S3 |
| BOD-M02 | BodyController.FullRestore устанавливает Heart blackHP≠0 (каскад от CORE-C01) | BodyController.cs:251-258 | S3 |
| BOD-M03 | BodyController.BodyParts — мутабельный внутренний список | BodyController.cs:54 | S3 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BOD-L01 | BodyController.InitializeBody() можно вызвать многократно, потеря damage state | BodyController.cs:84-93 | S3 |
| BOD-L02 | BodyController.Heal(int) — деление на ноль при пустом bodyParts | BodyController.cs:283 | S3 |
| BOD-L03 | BodyDamage: Quadruped Torso isVital=true вопреки документации | BodyDamage.cs:176 | S3 |

---

## 5. Player System

> **Файлы:** PlayerController.cs, PlayerVisual.cs, SleepSystem.cs  
> **Источник:** S1, S3

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| PLR-C01 | rb.velocity устарел в Unity 6 — нужно linearVelocity | PlayerController.cs:223 | S1(C-01), S3 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| PLR-H01 | PlayerController не реализует ICombatant | PlayerController.cs | S1(H-09), S3 |
| PLR-H02 | Duplicate PlayerSaveData (SaveManager vs PlayerController) | SaveManager.cs:532, PlayerController.cs:496 | S1(H-01) |
| PLR-H03 | Revive() игнорирует healthPercent (всегда FullRestore) | PlayerController.cs:351 | S3 |
| PLR-H04 | PlayerState: long→int truncation для Qi | PlayerController.cs:231-232 | S3 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| PLR-M01 | SleepSystem: HP recovery формула неверна (100% за 8ч для всех частей) | SleepSystem.cs:288-289 | S3 |
| PLR-M02 | SleepSystem.ProcessFinalHPRecovery — FullRestore при любом sleep duration | SleepSystem.cs:302-306 | S3 |
| PLR-M03 | SleepSystem: FallingAsleep/WakingUp states никогда не активны | SleepSystem.cs:169-174 | S3 |
| PLR-M04 | SleepSystem.QuickSleep bypasses state management и events | SleepSystem.cs:246-251 | S3 |
| PLR-M05 | SleepSystem: auto-sleep capped at optimalSleepHours (8h) не maxSleepHours (12h) | SleepSystem.cs:346 | S3 |
| PLR-M06 | SleepSystem: long→int truncation в ProcessFinalQiRecovery | SleepSystem.cs:317 | S3 |
| PLR-M07 | PlayerVisual: неверный URP shader name | PlayerVisual.cs:70 | S3 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| PLR-L01 | PlayerVisual: Material и Texture2D утечки памяти | PlayerVisual.cs:70,103 | S3 |
| PLR-L02 | PlayerVisual: Flash race condition при множественных вызовах | PlayerVisual.cs:166-172 | S3 |
| PLR-L03 | PlayerVisual: Camera.main медленный/устаревший | PlayerVisual.cs:190 | S3 |
| PLR-L04 | SleepSystem.sleepStartTime не используется | SleepSystem.cs:98 | S3 |
| PLR-L05 | PlayerController.OnDestroy отписывает только body/qi events | PlayerController.cs:105-118 | S3 |
| PLR-L06 | Keyboard.current null check не полный | PlayerController.cs:23 | S1(C-06) |

---

## 6. Buff System

> **Файлы:** Buff/BuffManager.cs, Formation/FormationEffects.cs (заглушка BuffManager)  
> **Источник:** S3, S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BUF-C01 | float.Parse на special.parameters — crash при невалидном вводе (7 call sites) | BuffManager.cs:788,792,796,800,930,934,938 | S3 |
| BUF-C02 | HandleExistingBuff: Independent возвращает Applied но НИЧЕГО не добавляет | BuffManager.cs:1000-1004 | S3 |
| BUF-C03 | ConductivityModifier отслеживается но никогда не применяется к QiController — мёртвый код | BuffManager.cs:126-439 | S3 |
| BUF-C04 | FormationEffects.ApplyControlFallback — перманентная модификация Rigidbody2D (Freeze/Slow/Root не откатываются) | FormationEffects.cs:340-368 | S4 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BUF-H01 | ScriptableObject.CreateInstance утечка памяти | BuffManager.cs:1156,1202 | S3 |
| BUF-H02 | Slow effect hardcoded 50%, не использует buff data | BuffManager.cs:772,905 | S3 |
| BUF-H03 | RemoveControl не проверяет другие активные баффы (два Stun: снятие одного = сброс флага) | BuffManager.cs:1232-1257 | S3 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BUF-M01 | RemoveBuff(FormationBuffType) удаляет слишком широко (все баффы модифицирующие stat) | BuffManager.cs:308-320 | S3 |
| BUF-M02 | CreateTempBuffData: секунды→тики без учёта tickInterval | BuffManager.cs:1173 | S3 |
| BUF-M03 | ApplySpecialEffect: triggerChance проверяется при применении, не при тике | BuffManager.cs:763 | S3 |
| BUF-M04 | CurrentControl теряет информацию при нескольких контроль-эффектах | BuffManager.cs:1260 | S3 |
| BUF-M05 | BuffManager заглушка в FormationEffects.cs + полноценный Buff/BuffManager.cs | FormationEffects.cs | S1(M-04) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| BUF-L01 | BuffResult.Replaced enum value никогда не возвращается | BuffManager.cs:68 | S3 |
| BUF-L02 | IsPercentageModifier хардкодит stat names | BuffManager.cs:1100-1107 | S3 |
| BUF-L03 | GetStatFromController обрабатывает только 3 из многих статов | BuffManager.cs:1109-1129 | S3 |

---

## 7. NPC System

> **Файлы:** NPCController.cs, NPCAI.cs, NPCData.cs, RelationshipController.cs  
> **Источник:** S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| NPC-C01 | NPCSaveData не содержит MaxQi/MaxHealth/MaxStamina/MaxLifespan → загруженный NPC мёртв | NPCController.cs | S4 |
| NPC-C02 | NPCState.SkillLevels — Dictionary\<string,float\> не сериализуем Unity | NPCData.cs:60 | S4 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| NPC-H01 | NPCAI.DecideNormalBehavior — нелогичное распределение вероятностей | NPCAI.cs:146-175 | S4 |
| NPC-H02 | UpdateLifespan() пустой — смерть от возраста невозможна | NPCController.cs:201-205 | S4 |
| NPC-H03 | TakeDamage не уведомляет AI атакера | NPCController.cs:241-253 | S4 |
| NPC-H04 | NPCController: long→int cast для maxQi | NPCController.cs:360-361 | S4 |
| NPC-H05 | RelationshipController: ProcessDecay смешивает Time.time с game time | RelationshipController.cs:327-358 | S4 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| NPC-M01 | NPCAI: Threat levels никогда не затухают | NPCAI.cs:308-323 | S4 |
| NPC-M02 | ExecuteCultivating восстанавливает только 10 Qi | NPCAI.cs:265-273 | S4 |
| NPC-M03 | RelationshipController.ownerId не валидируется | RelationshipController.cs:107-109 | S4 |
| NPC-M04 | GetTargetsByType использует устаревший record.Type | RelationshipController.cs:269-282 | S4 |
| NPC-M05 | InitializeFromGenerated игнорирует generated.age | NPCController.cs:332 | S4 |
| NPC-M06 | CalculateRelationshipType не возвращает Stranger/SwornSibling | RelationshipController.cs | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| NPC-L01 | NPCInteractionResult и DialogueOption определены но не используются | NPCData.cs:119-135 | S4 |

---

## 8. World System

> **Файлы:** TimeController.cs, WorldController.cs, LocationController.cs, FactionController.cs, EventController.cs  
> **Источник:** S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| WLD-C01 | LocationController.CompleteTravel обнуляет travelDestinationId до ShouldTriggerTravelEvent | LocationController.cs:330-335 | S4 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| WLD-H01 | TimeController.AdvanceHour fires OnHourPassed с пост-инкремент значением | TimeController.cs:195-207 | S4 |
| WLD-H02 | EventController: Cooldown использует Time.time (real) не game time | EventController.cs:201-207 | S4 |
| WLD-H03 | WorldController: WorldEvent.EventData — Dictionary\<string,object\> не сериализуем | WorldController.cs | S4 |
| WLD-H04 | FactionController.LoadSaveData не восстанавливает playerMemberships | FactionController.cs:467-477 | S4 |
| WLD-H05 | FactionData.FactionRelations — Dictionary\<string,int\> не сериализуем Unity | FactionData.cs | S4 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| WLD-M01 | TimeController.SetTime() не вызывает transition events | TimeController.cs:299-303 | S4 |
| WLD-M02 | TimeController.AdvanceHours/Days — cascading event storms | TimeController.cs:318-324 | S4 |
| WLD-M03 | LocationController: BuildingType? nullable enum не сериализуем Unity | LocationController.cs:70 | S4 |
| WLD-M04 | EventController: eventCheckInterval в real seconds, не game time | EventController.cs:134-146 | S4 |
| WLD-M05 | FindFirstObjectByType вместо ServiceLocator (Location, Event, Save, Formation) | Множество файлов | S1(H-02), S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| WLD-L01 | CalculateTimeOfDay: hour 0 = Midnight, 21-23 = Night (нет Late Night) | TimeController.cs:341 | S4 |
| WLD-L02 | GetNPCsInLocation — новый List каждый вызов | WorldController.cs:259-272 | S4 |
| WLD-L03 | FactionData.MaxMembers defaults to 0 (confusing) | FactionController.cs:93-98 | S4 |

---

## 9. Formation System

> **Файлы:** FormationController.cs, FormationCore.cs, FormationQiPool.cs, FormationEffects.cs, FormationData.cs, FormationUI.cs  
> **Источник:** S4

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| FRM-H01 | FormationCore.ApplyEffects — OverlapCircleAll без layer mask | FormationCore.cs:454 | S4 |
| FRM-H02 | FormationEffects.IsAlly() — faction detection никогда не работает | FormationEffects.cs:69-101 | S4 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| FRM-M01 | FormationCore.ContributeQi — нет ограничения maxCapacity | FormationCore.cs | S1(M-05) |
| FRM-M02 | FormationCore.practitionerId = GetInstanceID() — не персистентно | FormationCore.cs:34 | S4 |
| FRM-M03 | FormationQiPool.AcceptQi — int вместо long для accepted amount | FormationQiPool.cs:326-330 | S4 |
| FRM-M04 | FormationEffects.ApplyHeal всегда лечит и Qi и body | FormationEffects.cs:291-310 | S4 |
| FRM-M05 | FormationController.ContributeQi — long→int cast | FormationController.cs:507 | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| FRM-L01 | FormationUI.Instance в Awake может быть null | FormationUI.cs:131 | S4 |
| FRM-L02 | FormationUI: Camera.main каждый кадр в placement mode | FormationUI.cs:484 | S4 |
| FRM-L03 | FormationUI.CreateDefaultPreview — Texture2D утечка | FormationUI.cs:526-559 | S4 |

---

## 10. Save System

> **Файлы:** SaveManager.cs, SaveFileHandler.cs, SaveDataTypes.cs  
> **Источник:** S1, S4

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| SAV-H01 | SaveDataTypes: Dictionary fields не JsonUtility-совместимы (KeyBindings, Objectives, customBonuses, skills) | SaveDataTypes.cs:130,209 | S4 |
| SAV-H02 | SaveManager.TotalPlayTimeHours использует game-time вместо real play time | SaveManager.cs:143 | S4 |
| SAV-H03 | SaveManager.CollectSaveData не сохраняет Player/NPC/Formation/Quest/Inventory/Equipment | SaveManager.cs:232-234 | S4 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| SAV-M01 | SaveFileHandler не используется (мёртвый код) | SaveFileHandler.cs | S1(M-06) |
| SAV-M02 | SaveDataTypes.cs — раздутый файл | SaveDataTypes.cs | S1(M-15) |
| SAV-M03 | SaveManager: XOR encryption тривиально ломается, SaveFileHandler AES не используется | SaveManager.cs:434-442 | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| SAV-L01 | SaveFileHandler AES: zero IV + no salt в key derivation | SaveFileHandler.cs:336 | S4 |
| SAV-L02 | SaveManager: Random seed saved as hash, не restorable | SaveManager.cs:199 | S4 |

---

## 11. Inventory/Equipment/Crafting System

> **Файлы:** InventoryController.cs, EquipmentController.cs, CraftingController.cs, MaterialSystem.cs  
> **Источник:** S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| INV-C01 | InventorySlot.GetCondition: durability=0 → Pristine (должно быть Broken) | InventorySlot.cs:674 | S4 |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| INV-H01 | CraftingController.Craft() не применяет рассчитанный grade | CraftingController.cs:194-197 | S4 |
| INV-H02 | CraftingController.AddSkillExperience: random 20% level-up вместо XP | CraftingController.cs:426-438 | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| INV-L01 | EquipmentController.CanEquip всегда true | EquipmentController.cs:239-248 | S4 |
| INV-L02 | CraftingController.CraftCustom всегда успешен | CraftingController.cs:218-269 | S4 |

---

## 12. Quest System

> **Файлы:** QuestController.cs, QuestData.cs, QuestObjective.cs  
> **Источник:** S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QST-C01 | QuestData objectives на shared ScriptableObject — state corruption | QuestData.cs + QuestObjective.cs | S4 |
| QST-C02 | QuestController.LoadSaveData не восстанавливает активные квесты | QuestController.cs:641-650 | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| QST-L01 | GrantRewards — TODO stub | QuestController.cs:283 | S4 |

---

## 13. Interaction/Dialogue System

> **Файлы:** DialogueSystem.cs, InteractionController.cs  
> **Источник:** S4

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DLG-C01 | LoadDialogueFromJson: JsonUtility.FromJson\<DialogueNode[]\> — массив как root ВСЕГДА fails | DialogueSystem.cs:474 | S4 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DLG-M01 | InteractionController.ScanForInteractables — OverlapCircleAll каждый кадр | InteractionController.cs:108-138 | S4 |
| DLG-M02 | InteractionResult.CustomData — Dictionary\<string,object\> не сериализуем | InteractionController.cs:58 | S4 |
| DLG-M03 | DialogueSystem: нет save/load для dialogue state | DialogueSystem.cs | S4 |
| DLG-M04 | DialogueSystem.LoadDialogueNode — всегда возвращает null (stub) | DialogueSystem.cs:460-465 | S4 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DLG-L01 | Typing effect may skip characters at low FPS | DialogueSystem.cs:351-358 | S4 |

---

## 14. Charger System

> **Файлы:** ChargerController.cs, ChargerData.cs, ChargerHeat.cs, ChargerBuffer.cs, ChargerSlot.cs  
> **Источник:** S5

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CHR-C01 | [SerializeField] без MonoBehaviour (ChargerHeat, ChargerBuffer, ChargerSlot, QiStone) | 4 файла | S5(DC-01) |
| CHR-C02 | ChargerController.UseQiForTechnique — int вместо long для Qi | ChargerController.cs:326-334 | S5(DC-02) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CHR-M01 | ChargerController lambda leak на buffer.OnBufferChanged | ChargerController.cs:118 | S5(DM-01) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CHR-L01 | Debug.Log в production (10+ вызовов) | ChargerController.cs | S5(DL-01) |

---

## 15. Data/ScriptableObjects

> **Файлы:** SpeciesData.cs, ElementData.cs, TechniqueData.cs, MortalStageData.cs, ItemData.cs, NPCPresetData.cs, BuffData.cs, CultivationLevelData.cs, FactionData.cs, LocationData.cs, FormationCoreData.cs, TerrainConfig.cs  
> **Источник:** S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DAT-C01 | Duplicate FactionRelationType enum (Core/Enums:4 знач vs FactionData:6 знач) | Enums.cs:466 vs FactionData.cs:37 | S6(DA-C-01) |
| DAT-C02 | Duplicate LocationType enum (Core:4 vs LocationData:6) | Enums.cs:481 vs LocationData.cs:24 | S6(DA-C-02) |
| DAT-C03 | MortalStageData.GetCoreFormationForAge — деление на ноль при minAge==maxAge | MortalStageData.cs:230 | S6(DA-H-05→CRIT) |
| DAT-C04 | TerrainConfig Lava Range(-50,50) обрезает temperatureModifier=1000f до 50f | TerrainConfig.cs:47,126 | S6(DA-H-04→CRIT) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DAT-H01 | TechniqueData.baseQiCost int вместо long | TechniqueData.cs:70 | S6(DA-H-01) |
| DAT-H02 | SpeciesData.coreCapacityBase float вместо long | SpeciesData.cs:74 | S6(DA-H-02) |
| DAT-H03 | Duplicate LocationData class name (SO vs plain class) | LocationData.cs vs LocationController.cs:20 | S6(DA-H-03) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DAT-M01 | FactionData.GetRankByReputation без сортировки | FactionData.cs | S6(DA-M-01) |
| DAT-M02 | LocationData/BuffData циклические SO ссылки | LocationData.cs, BuffData.cs | S6(DA-M-02) |
| DAT-M03 | TerrainConfig singleton не сбрасывается при domain reload | TerrainConfig.cs | S6(DA-M-04) |
| DAT-M04 | SpeciesData weaknesses/resistances пересечение | SpeciesData.cs | S6(DA-M-05) |
| DAT-M05 | MortalStageData Qi int вместо long | MortalStageData.cs | S6(DA-M-06) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| DAT-L01 | MinMaxRange.GetRandom() без min<=max проверки | S6(DA-L-01) |
| DAT-L02 | NPCPresetData.PersonalityTrait.intensity без Range | S6(DA-L-02) |
| DAT-L03 | ItemData.ItemEffect.effectType string вместо enum | S6(DA-L-03) |
| DAT-L04 | SpeciesData.innateTechniques List\<string\> без валидации | S6(DA-L-04) |
| DAT-L05 | FactionData.JoinRequirement Quest/Item всегда true | S6(DA-L-05) |

---

## 16. Generators/Editor

> **Файлы:** GeneratorRegistry.cs, SeededRandom.cs, ArmorGenerator.cs, NPCGenerator.cs, TechniqueGenerator.cs, ConsumableGenerator.cs, WeaponGenerator.cs, Naming/*, Editor/*  
> **Источник:** S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| GEN-C01 | SceneSetupTools без #if UNITY_EDITOR → BUILD BREAK | Editor/SceneSetupTools.cs | S6(GE-C-01) |
| GEN-C02 | FormationUIPrefabsGenerator — Texture2D/Sprite не сохранены как ассеты | Editor/FormationUIPrefabsGenerator.cs:381-405 | S6(GE-C-02) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| GEN-H01 | GeneratorRegistry seed truncation long→int с sign issues | GeneratorRegistry.cs:100 | S6(GE-H-01) |
| GEN-H02 | ArmorGenerator cross-dependency на WeaponGenerator | ArmorGenerator.cs:372 | S6(GE-H-02) |
| GEN-H03 | NamingDatabase/NameBuilder — мёртвый код (генераторы используют хардкод) | Naming/ | S6(GE-H-03) |
| GEN-H04 | ConsumableGenerator Qi float вместо long | ConsumableGenerator.cs:394-399 | S6(GE-H-04) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| GEN-M01 | AdjectiveForms автодеривация неверна для русских прилагательных | AdjectiveForms.cs | S6(GE-M-01) |
| GEN-M02 | TechniqueGenerator Combat names дубли ("Удар" ×3) | TechniqueGenerator.cs | S6(GE-M-02) |
| GEN-M03 | TechniqueGenerator Healing генерирует damage потом очищает | TechniqueGenerator.cs | S6(GE-M-03) |
| GEN-M04 | WeaponGenerator duplicate bonus statNames | WeaponGenerator.cs | S6(GE-M-04) |
| GEN-M05 | AssetGeneratorExtended Gloves → LeftHand slot | AssetGeneratorExtended.cs | S6(GE-M-05) |
| GEN-M06 | GeneratorRegistry unbounded cache | GeneratorRegistry.cs | S6(GE-M-06) |
| GEN-M07 | Weapon/Armor nameEn = nameRu | WeaponGenerator.cs, ArmorGenerator.cs | S6(GE-M-07) |
| GEN-M08 | SeededRandom NextGaussian log(0) → -Infinity | SeededRandom.cs | S6(GE-M-08) |
| GEN-M09 | SceneSetupTools reflection wrong namespace | SceneSetupTools.cs | S6(GE-M-09) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| GEN-L01 | SeededRandom dead code | SeededRandom.cs | S6(GE-L-01..02) |
| GEN-L02 | ID collision risk (5 генераторов) | Генераторы | S6(GE-L-09) |
| GEN-L03 | GeneratorRegistry без DontDestroyOnLoad | GeneratorRegistry.cs | S6(GE-L-10) |

---

## 17. UI System

> **Файлы:** UIManager.cs, CombatUI.cs, InventoryUI.cs, CultivationProgressBar.cs, HUDController.cs, MenuUI.cs, CharacterPanelUI.cs, DialogUI.cs, WeaponDirectionIndicator.cs  
> **Источник:** S5, S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| UI-C01 | DialogUI.HideDialog деактивирует панель до завершения анимации | DialogUI.cs:196-204 | S6(UT-C-05) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| UI-H01 | UIManager: GameState.Combat для character panel, Cutscene для map | UIManager.cs:349,372-373 | S5(DH-01) |
| UI-H02 | CombatUI.Camera.main при каждом числе урона | CombatUI.cs:629,653 | S5(DH-02) |
| UI-H03 | 9 UI файлов FindFirstObjectByType вместо ServiceLocator | InventoryUI, CultivationProgressBar, HUDController, MenuUI, CharacterPanelUI, DialogUI, DestructibleObjectController | S6(UT-H-01) |
| UI-H04 | Qi long→float потеря точности на слайдерах (L7+) | CultivationProgressBar.cs:228, HUDController.cs:157, CharacterPanelUI.cs:256 | S6(UT-H-02) |
| UI-H05 | CharacterPanelUI GetComponentsInChildren каждый кадр | CharacterPanelUI.cs:507 | S6(UT-H-03) |
| UI-H06 | 4 UI файла старый Input System | InventoryUI, CultivationProgressBar, DialogUI | S6(UT-H-04) |
| UI-H07 | DestructibleObjectController утечка Texture2D/Sprite | DestructibleObjectController.cs:316-342 | S6(UT-H-05) |
| UI-H08 | WeaponDirectionIndicator неправильный namespace | WeaponDirectionIndicator.cs:8 | S6(UT-H-06) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| UI-M01 | UIManager: старый Input API | UIManager.cs:259-280 | S5(DM-03) |
| UI-M02 | CombatUI.ProgressBar — [Serializable] MonoBehaviour, вложенный класс | CombatUI.cs:798 | S5(DM-02) |
| UI-M03 | HUDController divide by zero в cooldown | HUDController.cs | S6(UT-M-03) |
| UI-M04 | CultivationProgressBar оба бара одинаковое значение | CultivationProgressBar.cs | S6(UT-M-06) |
| UI-M05 | CharacterPanelUI CultivationLevel как int вместо имени | CharacterPanelUI.cs | S6(UT-M-09) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| UI-L01 | CombatUI.AttackResult enum collision | CombatUI.cs:504 | S5(DL-02) |
| UI-L02 | Camera.main без null check в UI | Множество | S6(UT-L-02) |
| UI-L03 | Duplicate helpers в 4+ UI файлов | Множество | S6(UT-L-04) |

---

## 18. Tile System

> **Файлы:** TileMapController.cs, GameTile.cs, ResourcePickup.cs, TileData.cs, TileMapData.cs, DestructibleObjectController.cs, DestructibleSystem.cs  
> **Источник:** S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TIL-C01 | TileData temperature accumulation bug (+= вместо =) | TileData.cs:81 | S6(UT-C-01) |
| TIL-C02 | TileMapData DateTime не сериализуется JsonUtility | TileMapData.cs:35 | S6(UT-C-02) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TIL-M01 | ResourcePickup Time.time вместо unscaledTime | ResourcePickup.cs | S6(UT-M-01) |
| TIL-M02 | DestructibleSystem weak attacks → 0 damage | DestructibleSystem.cs | S6(UT-M-02) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TIL-L01 | ResourcePickup TryAddToInventory always true | ResourcePickup.cs | S6(UT-L-01) |
| TIL-L02 | TestLocationGameController.CreateTempSprite — Texture2D утечка | TestLocationGameController.cs:258-293 | S4, S1(L-07) |

---

## 19. Character System

> **Файлы:** IndependentScale.cs, CharacterSpriteController.cs  
> **Источник:** S6

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CHR-H01 | IndependentScale + CharacterSpriteController — неправильный namespace (CultivationWorld вместо CultivationGame) | IndependentScale.cs:6, CharacterSpriteController.cs:6 | S6(UT-H-07) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| CHR-L01 | IndependentScale — утилита без документации | IndependentScale.cs | S1(L-09) |

---

## 20. Managers

> **Файлы:** GameManager.cs, GameInitializer.cs, SceneLoader.cs  
> **Источник:** S1, S3

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| MGR-H01 | GameManager.FindReferences использует FindFirstObjectByType | GameManager.cs:155-165 | S1(H-10), S3 |
| MGR-H02 | GameInitializer.SubscribeToEvents() никогда не вызывается | GameInitializer.cs:356-364 | S1(H-04), S3 |
| MGR-H03 | SceneLoader: loading scene unloaded after Single-mode load destroys it | SceneLoader.cs:226-277 | S3 |
| MGR-H04 | SceneLoader: unconditional timeScale=1f после loading (unpauses previously paused) | SceneLoader.cs:286-289 | S3 |
| MGR-H05 | GameInitializer: individual subscribe methods don't check isSubscribed | GameInitializer.cs:394-413 | S3 |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| MGR-M01 | GameManager.ProcessInput использует старый Input API | GameManager.cs:407-422 | S1(M-01) |
| MGR-M02 | GameManager.Time property shadows UnityEngine.Time | GameManager.cs:91 | S3 |
| MGR-M03 | GameManager: singleton с DontDestroyOnLoad хранит stale scene references | GameManager.cs:99-109 | S3 |
| MGR-M04 | GameManager.StartNewGame: Loading→Playing мгновенно | GameManager.cs:314-331 | S3 |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| MGR-L01 | QuickSave/QuickLoad заглушки | GameManager.cs:429-437 | S1(L-13) |
| MGR-L02 | GameManager.LoadGame — stub | GameManager.cs:337-344 | S3 |
| MGR-L03 | GameInitializer.HandleBodyDamageTaken: float→int cast теряет precision | GameInitializer.cs:446 | S3 |
| MGR-L04 | SceneLoader.ReloadCurrentScene: stale currentScene variable | SceneLoader.cs:169 | S3 |

---

## 21. Tests

> **Файлы:** IntegrationTests.cs, IntegrationTestScenarios.cs, BalanceVerification.cs, CombatTests.cs, NPCAssemblyExample.cs  
> **Источник:** S6

### 🔴 CRITICAL

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TST-C01 | IntegrationTestScenarios PlayerSaveData Qi — float вместо long | IntegrationTestScenarios.cs:655-656 | S6(UT-C-03) |
| TST-C02 | IntegrationTestScenarios Dictionary не сериализуем JsonUtility | IntegrationTestScenarios.cs:551 | S6(UT-C-04) |

### 🟠 HIGH

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TST-H01 | IntegrationTests MockCombatant.SpendQi int вместо long | IntegrationTests.cs:859 | S6(UT-H-08) |
| TST-H02 | IntegrationTests ConductivityPayback dead loop | IntegrationTests.cs:339-347 | S6(UT-H-09) |

### 🟡 MEDIUM

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TST-M01 | IntegrationTests FormationStage duplicate enum | IntegrationTests.cs | S6(UT-M-04) |
| TST-M02 | IntegrationTests test name contradicts assertion | IntegrationTests.cs | S6(UT-M-05) |
| TST-M03 | NPCAssemblyExample qiDensity underflow при level=0 | NPCAssemblyExample.cs | S6(UT-M-07) |
| TST-M04 | BalanceVerification non-deterministic random | BalanceVerification.cs | S6(UT-M-08) |

### 🟢 LOW

| ID | Проблема | Файл:строка | Источник |
|----|----------|-------------|----------|
| TST-L01 | NPCAssemblyExample — пример в основной папке | Examples/ | S1(L-08) |
| TST-L02 | Нет Editor-only Assembly Definition | Editor/ | S1(L-10) |
| TST-L03 | Нет asmdef для основных скриптов (кроме TileSystem) | S1(M-08) |

---

## 22. Кросс-системные проблемы

### 🔴 Qi: int vs long (10+ файлов)

| ID | Файл | Описание |
|----|------|----------|
| CMB-C04 | Combatant.cs:62 | ICombatant.SpendQi(int) |
| CMB-C05 | Combatant.cs:275 | DefenderParams.CurrentQi (int) |
| QI-C01 | QiController.cs:357,364,365,371 | long→int cast в бою |
| PLR-H04 | PlayerController.cs:231-232 | PlayerState Qi (int) |
| CHR-C02 | ChargerController.cs:326-334 | UseQiForTechnique(int) |
| NPC-H04 | NPCController.cs:360-361 | (int)generated.maxQi |
| FRM-M05 | FormationController.cs:507 | (int)playerQi.CurrentQi |
| FRM-M03 | FormationQiPool.cs:326-330 | AcceptQi int |
| DAT-H01 | TechniqueData.cs:70 | baseQiCost int |
| DAT-H02 | SpeciesData.cs:74 | coreCapacityBase float |
| GEN-H04 | ConsumableGenerator.cs:394-399 | Qi as float |
| TST-C01 | IntegrationTestScenarios.cs:655 | Qi as float |
| TST-H01 | IntegrationTests.cs:859 | SpendQi(int) |

### 🔴 JsonUtility сериализация (Dictionary/nullable/array root)

| ID | Файл | Описание |
|----|------|----------|
| NPC-C02 | NPCData.cs:60 | Dictionary\<string,float\> |
| WLD-H03 | WorldController.cs | Dictionary\<string,object\> |
| WLD-H05 | FactionData.cs | Dictionary\<string,int\> |
| SAV-H01 | SaveDataTypes.cs | Multiple Dictionary/nullable |
| DLG-C01 | DialogueSystem.cs:474 | Array root JSON |
| DLG-M02 | InteractionController.cs:58 | Dictionary\<string,object\> |
| TIL-C02 | TileMapData.cs:35 | DateTime |
| WLD-M03 | LocationController.cs:70 | BuildingType? nullable |

### 🟠 FindFirstObjectByType вместо ServiceLocator (19 файлов, ~49 вызовов)

| ID | Файл |
|----|------|
| MGR-H01 | GameManager.cs:155-165 |
| WLD-M05 | LocationController.cs:112 |
| WLD-M05 | EventController.cs:129-131 |
| SAV-M01 | SaveManager.cs:104-108 |
| FRM-M05 | FormationController.cs:215 |
| UI-H03 | 9 UI файлов |
| PLR-M07 | SleepSystem.cs:139 |
| GEN-H01 | GeneratorRegistry.cs |

### 🟠 Input System: смешивание старого и нового

| ID | Файл |
|----|------|
| MGR-M01 | GameManager.cs:407-422 (старый) |
| UI-H06 | 4 UI файла (старый) |
| UI-M01 | UIManager.cs:259-280 (старый) |
| PLR-C01 | PlayerController.cs (новый, но без null check) |

### 🟡 Namespace несогласованность

| ID | Файл | Проблема |
|----|------|----------|
| CHR-H01 | IndependentScale.cs, CharacterSpriteController.cs | CultivationWorld.Character вместо CultivationGame |
| UI-H08 | WeaponDirectionIndicator.cs | CultivationWorld.UI вместо CultivationGame |
| CMB-M05 | FormationArrayEffect.cs vs ExpandingEffect.cs | Разная квалификация ICombatTarget |

### 🟡 Duplicate enums/classes

| ID | Файлы | Проблема |
|----|-------|----------|
| DAT-C01 | Enums.cs vs FactionData.cs | FactionRelationType (4 vs 6 знач) |
| DAT-C02 | Enums.cs vs LocationData.cs | LocationType (4 vs 6 знач) |
| DAT-H03 | LocationData.cs vs LocationController.cs | LocationData class name collision |
| CORE-M03 | Enums.cs vs CombatManager.cs | AttackResult enum vs struct |
| PLR-H02 | SaveManager.cs vs PlayerController.cs | PlayerSaveData duplicate class |

---

## 23. Приоритетный план исправлений

### Phase 0: Критические баги (блокируют gameplay)

| # | Задача | ID | Файлы |
|---|--------|-----|-------|
| 1 | Qi: int→long (все 13 файлов) | CMB-C04/C05, QI-C01, PLR-H04, CHR-C02, NPC-H04, FRM-M05, FRM-M03, DAT-H01, DAT-H02, GEN-H04, TST-C01, TST-H01 | 13 файлов |
| 2 | rb.velocity→linearVelocity | PLR-C01 | PlayerController.cs |
| 3 | Heart blackHP=0 | CORE-C01 | BodyPart.cs |
| 4 | DamageCalculator стихийные взаимодействия | CMB-C01 | DamageCalculator.cs |
| 5 | StatDevelopment: пороги + консолидация | CORE-C02, CORE-C03 | StatDevelopment.cs |
| 6 | ParryBonus/BlockBonus двойной расчёт | CMB-C03 | Combatant.cs, DamageCalculator.cs |
| 7 | AttackType Normal для обычных атак | CMB-C02 | DamageCalculator.cs |
| 8 | DefenseProcessor System.Random→UnityEngine.Random | CMB-C06 | DefenseProcessor.cs |
| 9 | [SerializeField] без MonoBehaviour | CORE-C04, CHR-C01 | StatDevelopment.cs, Charger/*.cs |
| 10 | CombatLog надёжная инициализация | CMB-C07 | CombatEvents.cs |
| 11 | BodyDamage HP значения по документации | BOD-C01 | BodyDamage.cs |
| 12 | SceneSetupTools #if UNITY_EDITOR | GEN-C01 | Editor/SceneSetupTools.cs |
| 13 | InventorySlot.GetCondition durability=0→Broken | INV-C01 | InventorySlot.cs |
| 14 | FormationEffects control восстановление | BUF-C04 | FormationEffects.cs |
| 15 | BuffManager float.Parse→TryParse | BUF-C01 | BuffManager.cs |
| 16 | BuffManager Independent stacking | BUF-C02 | BuffManager.cs |
| 17 | NPCSaveData MaxQi/MaxHealth | NPC-C01 | NPCController.cs |
| 18 | QuestData clone objectives | QST-C01 | QuestData.cs |
| 19 | DialogueSystem JsonUtility array root | DLG-C01 | DialogueSystem.cs |
| 20 | TileData temperature accumulation | TIL-C01 | TileData.cs |

### Phase 1: Высокие проблемы (влияют на баланс/корректность)

| # | Задача | ID |
|---|--------|-----|
| 21 | Заменить FindFirstObjectByType на ServiceLocator (19 файлов) | WLD-M05 |
| 22 | Реализовать формулу мастерства из документации | CMB-H01 |
| 23 | Уточнить единицу cooldown | CMB-H02 |
| 24 | Разделить totalDamageDealt/Taken | CMB-H03 |
| 25 | Реализовать ICombatant на PlayerController | PLR-H01 |
| 26 | Объединить PlayerSaveData | PLR-H02 |
| 27 | Добавить defenderElement в DefenderParams | CMB-H05 |
| 28 | Fix QiController.coreCapacity stale after breakthrough | QI-H02 |
| 29 | Fix ConductivityModifier dead code | BUF-C03 |
| 30 | Добавить очистку GameEvents при смене сцены | CORE-H02 |
| 31 | Fix Duplicate enums (FactionRelationType, LocationType) | DAT-C01, DAT-C02 |
| 32 | JsonUtility Dictionary serialization wrapper | SAV-H01 и др. |
| 33 | Fix LocationController.CompleteTravel | WLD-C01 |
| 34 | Fix NPCAI/TimeController/EventController game time | WLD-H01, WLD-H02, WLD-H04, NPC-H05 |
| 35 | Fix Charger Qi int→long | CHR-C02 |

### Phase 2: Средние проблемы (качество/оптимизация)

| # | Задача | ID |
|---|--------|-----|
| 36 | VFXPool per-prefab limits + prewarm | CORE-H03, CORE-M05, CORE-M06 |
| 37 | Разделить Disposition на Attitude + PersonalityTrait | CORE-M01 |
| 38 | Fix EquipmentSlot под документацию | CORE-M02 |
| 39 | NPCAI threat decay + cultivating Qi | NPC-M01, NPC-M02 |
| 40 | SleepSystem: fix recovery formulas + dead states | PLR-M01..M06 |
| 41 | Save system: полная интеграция всех подсистем | SAV-H03 |
| 42 | Formation: fix IsAlly + layer mask + persistent IDs | FRM-H01, FRM-H02, FRM-M02 |
| 43 | Interaction/Dialogue: save/load + scanning interval | DLG-M01..M04 |
| 44 | UI: GameState enum + Qi sliders + old Input | UI-H01, UI-H04, UI-H06 |
| 45 | Remove dead code (SaveFileHandler, Naming, buffers) | SAV-M01, GEN-H03, CMB-L02 |

---

## Приложение A: Маппинг ID

### Маппинг S1 (основной аудит) → Консолидированный

| S1 ID | Консолидированный ID | Примечание |
|-------|---------------------|------------|
| C-01 | PLR-C01 | rb.velocity |
| C-02 | CMB-C06 | System.Random |
| C-03 | CORE-C01 | Heart blackHP |
| C-04 | CMB-C04 | SpendQi int |
| C-05 | CMB-C05 | CurrentQi int |
| C-06 | PLR-L06 | Input System null check |
| C-07 | CORE-C04 | [SerializeField] |
| C-08 | CORE-C02 | StatDev <10 |
| C-09 | CMB-C03 | ParryBonus double |
| C-10 | CMB-C07 | CombatLog static |
| C-11 | CMB-C01 | Elements ignored |
| H-01 | PLR-H02 | PlayerSaveData dup |
| H-02 | WLD-M05 | FindFirstObjectByType |
| H-03 | CORE-H03 | VFXPool global |
| H-04 | MGR-H02 | SubscribeToEvents |
| H-05 | CORE-H02 | GameEvents leak |
| H-06 | CORE-C06 | Infinity regen |
| H-07 | CORE-H05 | Element.Count |
| H-08 | QI-H01 | Meditate time |
| H-09 | PLR-H01 | ICombatant |
| H-10 | MGR-H01 | FindReferences |
| H-11 | CMB-H04 | OnDeath unsub |
| H-12 | CMB-H01 | Mastery formula |
| H-13 | CMB-H02 | Cooldown ×60 |
| H-14 | CMB-C02 | AttackType Technique |
| H-15 | CMB-H03 | totalDamageDealt==Taken |
| M-01 | MGR-M01 | Old Input API |
| M-02 | CORE-C05 | Construct missing |
| M-03 | CORE-H01 | OppositeElements |
| M-04 | BUF-M05 | BuffManager stub |
| M-05 | FRM-M01 | ContributeQi no limit |
| M-06 | SAV-M01 | SaveFileHandler unused |
| M-07 | — | BodyDamage not read (now BOD-C01) |
| M-08 | TST-L03 | No asmdef |
| M-09 | CMB-C08 | Lambda leak |
| M-10 | CORE-M05 | VFXPool DontDestroyOnLoad |
| M-11 | WLD-M01 | TimeController events |
| M-12 | CORE-M02 | EquipmentSlot |
| M-13 | CHR-M01 | ChargerController interface |
| M-14 | GEN-H01 | GeneratorRegistry |
| M-15 | SAV-M02 | SaveDataTypes bloated |
| M-16 | CORE-M01 | Disposition mixed |
| M-17 | — | (дубликат H-15) |
| L-01 | CORE-L04 | No #region |
| L-02 | — | Redundant XML-doc |
| L-03 | CORE-L03 | Debug.Log production |
| L-04 | CORE-L02 | Magic numbers |
| L-05 | — | Camera2DSetup limited |
| L-06 | — | Settings SO no consumers |
| L-07 | TIL-L02 | TestLocationGameController |
| L-08 | TST-L01 | NPCAssemblyExample |
| L-09 | CHR-L01 | IndependentScale |
| L-10 | TST-L02 | Editor-only asmdef |
| L-11 | — | TODO comments |
| L-12 | — | LoadSaveData stub |
| L-13 | MGR-L01 | QuickSave/QuickLoad |
| L-14 | CMB-L03 | CheckCombatEnd every Update |
| L-15 | CMB-L04 | RequireComponent |

---

*Консолидация выполнена: 2026-04-10*  
*Источники: 7 параллельных агентов + ручная верификация*  
*Всего уникальных проблем: 191 (32 CRITICAL, 49 HIGH, 63 MEDIUM, 47 LOW)*
