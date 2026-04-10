# Чекпоинт: Fix-05 — Body System + Heart blackHP

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0

---

## Описание

Сердце не должно иметь blackHP, HP значения не соответствуют документации, двойной 70/30 split, мутабельный список, деление на ноль.

---

## Файлы (3 файла, ~931 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Body/BodyPart.cs` | 304 | Heart: maxBlackHP=0, currentBlackHP=0 |
| 2 | `Body/BodyController.cs` | 380 | FullRestore Heart fix, BodyParts readonly, InitializeBody guard, Heal /0 |
| 3 | `Body/BodyDamage.cs` | 247 | HP значения по документации, двойной 70/30 split, Quadruped Torso |

---

## Задачи

### CRITICAL
- [ ] CORE-C01: BodyPart.cs:93-95 — если partType == Heart: maxBlackHP = 0, currentBlackHP = 0
- [ ] BOD-C01: BodyDamage.CreateHumanoidBody — HP значения по BODY_SYSTEM.md

### MEDIUM
- [ ] BOD-M01: BodyDamage.ApplyDamage — убрать двойной 70/30 split (один в BodyDamage, другой в BodyPart). Оставить split в BodyPart.ApplyDamage, убрать из BodyDamage
- [ ] BOD-M02: BodyController.FullRestore — после Fix CORE-C01, Heart blackHP будет 0 корректно
- [ ] BOD-M03: BodyController.BodyParts — вернуть IReadOnlyList или копию вместо мутабельного внутреннего списка

### LOW
- [ ] BOD-L01: BodyController.InitializeBody — добавить guard `if (_isInitialized) return;`
- [ ] BOD-L02: BodyController.Heal — проверить `bodyParts.Count > 0` перед делением
- [ ] BOD-L03: BodyDamage Quadruped Torso — isVital=false по документации

---

## Порядок выполнения

1. BodyPart.cs — Heart blackHP fix
2. BodyDamage.cs — HP значения + двойной split + Quadruped
3. BodyController.cs — FullRestore + BodyParts + InitializeBody + Heal

---

## Зависимости

- **Предшествующие:** Fix-04 (CORE-C01 подтверждён как часть Core)

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*
