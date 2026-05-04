# Чекпоинт: Система боя — аудит и разработка

**Дата:** 2026-05-04 03:59 UTC
**Статус:** in_progress

## Выполненные задачи

- [x] Остановлен и заблокирован DEV сервер (package.json: `echo ⛔ && exit 1`)
- [x] Очищено окружение от Next.js файлов (src/, public/, next.config.ts, prisma/, db/ и т.д.)
- [x] Код сверен с GitHub — откачено до `d536592` (13 коммитов отстало было)
- [x] Прочитана документация боя: COMBAT_SYSTEM.md, ALGORITHMS.md, TECHNIQUE_SYSTEM.md
- [x] Прочитан весь существующий код боя (7 файлов Combat/)

## Текущий аудит боевой системы

### Существующие файлы (Assets/Scripts/Combat/)

| Файл | Версия | Назначение | Статус |
|------|--------|------------|--------|
| CombatManager.cs | 1.3 | Центральный менеджер боя | ✅ Работает |
| DamageCalculator.cs | 1.1 | Калькулятор урона (10 слоёв) | ✅ Работает |
| DefenseProcessor.cs | 1.1 | Обработка защит | ✅ Работает |
| QiBuffer.cs | 1.1 | Буфер Ци | ✅ Работает |
| LevelSuppression.cs | 1.0 | Подавление уровнем | ✅ Работает |
| TechniqueCapacity.cs | 1.0 | Ёмкость техник | ✅ Работает |
| TechniqueController.cs | 1.0 | Управление техниками | ✅ Работает |
| HitDetector.cs | 1.0 | Определение попаданий | ⚠️ Частично |
| Combatant.cs | 1.0 | ICombatant + CombatantBase | ✅ Работает |
| CombatEvents.cs | 1.0 | Система событий | ✅ Работает |
| Effects/TechniqueEffect.cs | — | Базовый класс эффектов | ✅ Работает |
| Effects/DirectionalEffect.cs | — | Направленный эффект | ✅ Работает |
| Effects/ExpandingEffect.cs | — | Расширяющийся эффект | ✅ Работает |
| Effects/FormationArrayEffect.cs | — | Эффект формации | ✅ Работает |
| Effects/TechniqueEffectFactory.cs | — | Фабрика эффектов | ✅ Работает |
| OrbitalWeapon.cs | — | Орбитальное оружие | ✅ Работает |
| OrbitalWeaponController.cs | — | Контроллер орбит. оружия | ✅ Работает |

### Проблемы и пробелы

1. **Бой не подключён к gameplay** — CombatManager.InitiateCombat() нигде не вызывается из PlayerController/NPCController
2. **Нет CombatUI** — CombatUI.cs существует, но не реализует реальный UI боя
3. **Нет триггера боя** — нет коллайдера/зоны, инициирующей бой при контакте с врагом
4. **HitDetector использует 3D Physics** — проект URP 2D, нужен Physics2D
5. **Слой 1b (урон оружия) не реализован** — в DamageCalculator отсутствует бонус оружия для melee_weapon
6. **Слой 3b (бафф формаций) не реализован** — formationBuffMultiplier отсутствует
7. **Слой 10b (Loot Generation) не реализован** — отмечен как ⏳
8. **Нет реального Combat AI** — NPC не принимает боевые решения
9. **Faction/Attitude не влияет на бой** — IsValidAlly/IsValidNeutral возвращают false

### Расхождение документации и кода

| Аспект | Документация (COMBAT_SYSTEM.md) | Код |
|--------|--------------------------------|-----|
| Слоёв пайплайна | 11 (1, 1b, 2, 3, 3b, 4, 5, 6, 7, 8, 9, 10) | 10 (без 1b, 3b) |
| Grade множитель техник | Common:1.0, Refined:1.3, Perfect:1.6, Transcendent:2.0 | Common:1.0, Refined:1.2, Perfect:1.4, Transcendent:1.6 |
| Ultimate множитель | ×2.0 | ×1.3 |
| Light элемент | Есть в ALGORITHMS.md §10 | Нет в Element enum |
| Poison стихия | НЕ стихия по докам | Есть в Element enum |

## Текущие задачи

- [ ] Определить приоритеты доработки боевой системы
- [ ] Исправить расхождения документации и кода
- [ ] Подключить бой к Player/NPC
- [ ] Реализовать CombatAI для NPC
- [ ] Адаптировать HitDetector под 2D
- [ ] Реализовать Слой 1b (урон оружия)

## Проблемы

- Grade множители техник в коде (1.0/1.2/1.4/1.6) расходятся с COMBAT_SYSTEM.md (1.0/1.3/1.6/2.0)
- Ultimate множитель в коде (×1.3) расходится с COMBAT_SYSTEM.md (×2.0)
- ALGORITHMS.md §10 упоминает Light элемент, но его нет в Element enum
- HitDetector использует Physics (3D), проект 2D — нужен Physics2D

## Следующие шаги

1. Уточнить у пользователя расхождения в множителях Grade/Ultimate
2. Синхронизировать документацию и код
3. Реализовать подключение боя к Player/NPC
4. Адаптировать HitDetector под 2D физику

## Изменённые файлы

- package.json — блокировка dev сервера
