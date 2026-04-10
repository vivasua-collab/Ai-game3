# Чекпоинт: Fix-07 — NPC + Quest + Dialogue

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

NPC: SaveData неполная, SkillLevels не сериализуем, AI поведение сломано, возраст не работает, Disposition → Attitude+PersonalityTrait. Quest: GrantRewards заглушка, LoadSaveData не восстанавливает, objectives на shared SO. Dialogue: JsonUtility array root всегда fails.

---

## Файлы (7 файлов, ~2620 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `NPC/NPCController.cs` | 483 | NPCSaveData, maxQi long, UpdateLifespan, TakeDamage notify, disposition→attitude+personality |
| 2 | `NPC/NPCAI.cs` | 408 | DecideNormalBehavior вероятности, Threat decay, Cultivating Qi, PersonalityTrait weights |
| 3 | `NPC/NPCData.cs` | ~200 | SkillLevels Dictionary→сериализуемая структура |
| 4 | `NPC/RelationshipController.cs` | 425 | ProcessDecay game time, ownerId validation, CalculateRelationshipType, Attitude integration |
| 5 | `Quest/QuestController.cs` | 677 | GrantRewards реализация, LoadSaveData активные квесты |
| 6 | `Quest/QuestData.cs` | 277 | Clone objectives вместо shared SO |
| 7 | `Interaction/DialogueSystem.cs` | 503 | JsonUtility array root fix, LoadDialogueNode stub |

---

## Задачи

### CRITICAL
- [ ] NPC-C01: NPCSaveData — добавить MaxQi (long), MaxHealth, MaxStamina, MaxLifespan
- [ ] NPC-C02: NPCData.SkillLevels — Dictionary→сериализуемый wrapper (KeyValuePair[] или JsonArray)
- [ ] QST-C01: QuestData — клонировать objectives для каждого экземпляра квеста
- [ ] QST-C02: QuestController.LoadSaveData — восстановить активные квесты из save data
- [ ] QST-C03: QuestController.GrantRewards — реализовать: XP→StatDevelopment, Items→InventoryController, Gold→PlayerState
- [ ] DLG-C01: DialogueSystem.LoadDialogueFromJson — обернуть массив в wrapper object для JsonUtility

### Disposition → Attitude + PersonalityTrait (HIGH, cascade from Fix-04)
- [ ] NPC-ATT-01: NPCController — заменить `Disposition disposition` на `Attitude attitude` + `PersonalityTrait personality`
- [ ] NPC-ATT-02: NPCAI.DecideNormalBehavior — PersonalityTrait flags для весов:
  - Aggressive: +50% атака, −30% защита
  - Cautious: +50% защита, −30% атака
  - Ambitious: +30% атака, +30% лидерство
  - Treacherous: при Attitude < Neutral → шанс предательства
  - Loyal: никогда не предаёт
  - Pacifist: −50% атака, +30% побег
- [ ] NPC-ATT-03: RelationshipController — CalculateAttitude() вместо GetDisposition(). Использовать числовое отношение → Attitude enum
- [ ] NPC-ATT-04: NPCSaveData — добавить поля attitude (int) + personality (int/flags)

### HIGH
- [ ] NPC-H01: NPCAI.DecideNormalBehavior — пересмотреть вероятности (логичные веса)
- [ ] NPC-H02: NPCController.UpdateLifespan — реализовать уменьшение remainingLifespan
- [ ] NPC-H03: NPCController.TakeDamage — уведомить AI атакера
- [ ] NPC-H05: RelationshipController.ProcessDecay — использовать game time (TimeController) вместо Time.time

### MEDIUM
- [ ] NPC-M01: NPCAI Threat levels — добавить затухание со временем
- [ ] NPC-M02: NPCAI.ExecuteCultivating — увеличить Qi восстановление (не 10, пропорционально conductivity)
- [ ] NPC-M03: RelationshipController.ownerId — валидация
- [ ] NPC-M05: NPCController.InitializeFromGenerated — использовать generated.age
- [ ] NPC-M06: RelationshipController.CalculateRelationshipType — добавить Stranger/SwornAlly (соответствует новому Attitude enum)
- [ ] DLG-M04: DialogueSystem.LoadDialogueNode — реализовать вместо stub

---

## Порядок выполнения

1. NPCData.cs — SkillLevels сериализация
2. NPCController.cs — SaveData + UpdateLifespan + TakeDamage + Attitude/PersonalityTrait поля
3. NPCAI.cs — вероятности + Threat decay + Cultivating + PersonalityTrait weights
4. RelationshipController.cs — game time + validation + Attitude integration
5. QuestData.cs — clone objectives
6. QuestController.cs — GrantRewards + LoadSaveData
7. DialogueSystem.cs — JsonUtility fix + LoadDialogueNode

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-04 (Attitude + PersonalityTrait enum созданы)
- **Последующие:** Fix-08 (Save — NPC/Quest save data)

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*
