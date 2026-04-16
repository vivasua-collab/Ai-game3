# Harvest System — Прогресс выполнения
**Дата начала:** 2026-04-16
**Чекпоинт:** 04_15_harvest_system_plan.md v3

## Подготовительная проверка

- [x] Прочитан чекпоинт 04_15_harvest_system_plan.md
- [x] Прочитан docs/UNITY_DOCS_LINKS.md
- [x] Проанализированы 7 ключевых файлов проекта (PlayerController, TileMapController, DestructibleObjectController, ResourcePickup, HarvestFeedbackUI, TestLocationGameController, ResourceSpawner)
- [x] Проверка совместимости с Unity 6.3
- [x] Фиксация результатов проверки в чекпоинте (§12 добавлен)
- [x] Начало выполнения шагов 1-8

## Результаты проверки совместимости (детали в логе ниже)

### Unity 6.3 — КРИТИЧЕСКИЕ проблемы
1. **"LegacyRuntime.ttf"** — шрифт НЕ существует в Unity 6.3 → HarvestFeedbackUI упадёт
2. **UnityEngine.UI.Text** — legacy, нужно TMPro

### Unity 6.3 — СРЕДНИЕ проблемы
3. **Material leak** — ResourceSpawner создаёт new Material() на каждый ресурс
4. **HarvestTarget GO leak** — PlayerController создаёт временный GO без Destroy
5. **ScriptableObject.CreateInstance leak** — ResourcePickup создаёт ItemData каждый раз

### Unity 6.3 — Проверенные API (OK)
- Physics2D.OverlapCircleAll ✅
- Physics2D.OverlapCircle ✅
- BoxCollider2D / CircleCollider2D ✅
- Rigidbody2D (Static) ✅
- SpriteRenderer ✅
- ScriptableObject ✅
- LayerMask ✅
- FindObjectsByType ✅ (уже мигрировано)
- linearVelocity ✅ (уже мигрировано)

## Шаги реализации (из чекпоинта §9)

- [x] Шаг 0: Документация (docs_temp/tool_system_draft.md — УЖЕ СОЗДАН)
- [x] Шаг 1: Создать Harvestable.cs
- [x] Шаг 2: Создать HarvestableSpawner.cs
- [x] Шаг 3: Модифицировать PlayerController.AttemptHarvest()
- [x] Шаг 4: Модифицировать TileMapController.RenderMap()
- [x] Шаг 5: Модифицировать ResourceSpawner.SetDefaultResourceTypes()
- [x] Шаг 6: UI подсказки (HarvestFeedbackUI) — TMP миграция + ShowHarvestPrompt
- [x] Шаг 7: Physics-слой "Harvestable" — Editor скрипт + инструкции
- [x] Шаг 8: Интеграция в TestLocationGameController

## Дополнительные исправления (найдены при проверке)

- [ ] FIX-H01: LegacyRuntime.ttf → Arial.ttf или TMPro в HarvestFeedbackUI
- [ ] FIX-H02: Material leak в ResourceSpawner
- [ ] FIX-H03: HarvestTarget GO leak в PlayerController
