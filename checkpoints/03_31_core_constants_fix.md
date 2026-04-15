# Чекпоинт: Core Constants Fix

**Дата:** 2026-03-31 09:54:21 UTC
**Фаза:** Code Review
**Статус:** complete

## Выполненные задачи
- [x] Получена системная дата: 2026-03-31 09:54:21 UTC
- [x] Constants.cs — исправлены множители Grade техник (1.2/1.4/1.6 вместо 1.3/1.6/2.0)
- [x] Constants.cs — исправлен Ultimate множитель (1.3 вместо 2.0)
- [x] Constants.cs — добавлены timestamps и документация источников
- [x] Enums.cs — добавлены timestamps
- [x] Enums.cs — добавлен SoulType.Artifact (отсутствовал)
- [x] Enums.cs — добавлены hybrid_* морфологии (HybridCentaur, HybridMermaid, HybridHarpy, HybridLamia)
- [x] Enums.cs — добавлен BodyMaterial.Construct (отсутствовал)
- [x] GameSettings.cs — добавлены timestamps
- [x] Camera2DSetup.cs — добавлены timestamps
- [x] TechniqueCapacity.cs — обновлены комментарии (расхождения устранены)
- [x] Обновлён checkpoints/README.md

## Проблемы
- **ДОКУМЕНТАЦИЯ ПРИОРИТЕТНА!** Код был исправлен по документации, а не наоборот.

## Ключевые исправления (ДОКА ПРИОРИТЕТ!)

### Constants.cs — Множители Grade техник

**Было (код):**
| Grade | Множитель |
|-------|-----------|
| Refined | 1.3 |
| Perfect | 1.6 |
| Transcendent | 2.0 |
| Ultimate | 2.0 |

**Стало (по TECHNIQUE_SYSTEM.md):**
| Grade | Множитель |
|-------|-----------|
| Refined | 1.2 |
| Perfect | 1.4 |
| Transcendent | 1.6 |
| Ultimate | 1.3 |

### Enums.cs — Недостающие значения

**SoulType:** Добавлен `Artifact` (Разумные предметы)
**Morphology:** Добавлены `HybridCentaur`, `HybridMermaid`, `HybridHarpy`, `HybridLamia`
**BodyMaterial:** Добавлен `Construct`

## Следующие шаги
- Продолжить проверку остальных папок Scripts
- Проверить папку Qi
- Проверить папку Data

## Изменённые файлы
- UnityProject/Assets/Scripts/Core/Constants.cs
- UnityProject/Assets/Scripts/Core/Enums.cs
- UnityProject/Assets/Scripts/Core/GameSettings.cs
- UnityProject/Assets/Scripts/Core/Camera2DSetup.cs
- UnityProject/Assets/Scripts/Combat/TechniqueCapacity.cs
- checkpoints/README.md
