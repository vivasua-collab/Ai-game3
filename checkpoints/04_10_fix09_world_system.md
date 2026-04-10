# Чекпоинт: Fix-09 — World System (Time + Location + Faction + Event)

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

TimeController: пост-инкремент в событиях, нет валидации при загрузке, cascading event storms. Location: travel event order. Faction: save data неполная. Event: real time вместо game time.

---

## Файлы (5 файлов, ~2694 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `World/TimeController.cs` | 429 | AdvanceHour post-increment, LoadSaveData validation, event storms |
| 2 | `World/LocationController.cs` | 491 | CompleteTravel event order |
| 3 | `World/FactionController.cs` | 501 | LoadSaveData playerMemberships |
| 4 | `World/EventController.cs` | 425 | Cooldown game time, check interval game time |
| 5 | `World/WorldController.cs` | 416 | WorldEvent.EventData сериализация |

---

## Задачи

### CRITICAL
- [ ] WLD-C01: LocationController.CompleteTravel — сохранить travelDestinationId ДО ShouldTriggerTravelEvent

### HIGH
- [ ] WLD-H01: TimeController.AdvanceHour — fire OnHourPassed с ПРЕ-инкремент значением (сохранить old, инкрементировать, fire с new)
- [ ] WLD-H02: EventController.Cooldown — использовать game time (TimeController) вместо Time.time
- [ ] WLD-H03: WorldController.WorldEvent.EventData — заменить Dictionary на [Serializable] структуру
- [ ] WLD-H04: FactionController.LoadSaveData — восстановить playerMemberships
- [ ] WLD-H06: TimeController.LoadSaveData — добавить Clamp для hours/minutes/days/months

### MEDIUM
- [ ] WLD-M01: TimeController.SetTime — вызвать transition events
- [ ] WLD-M02: TimeController.AdvanceHours/Days — ограничить cascade (batch events, не вызывать по одному)
- [ ] WLD-M04: EventController.eventCheckInterval — перевести в game time
- [ ] WLD-M06: TimeController.GetTotalDays — уточнить семантику (включить ли текущий день)

---

## Порядок выполнения

1. TimeController.cs — все фиксы (ядро мировой системы)
2. LocationController.cs — CompleteTravel
3. FactionController.cs — LoadSaveData
4. EventController.cs — game time
5. WorldController.cs — EventData сериализация

---

## Зависимости

- **Предшествующие:** Fix-04 (GameEvents cleanup), Fix-01 (Qi types)
- **Последующие:** Fix-08 (Save — WorldSaveData)

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*
