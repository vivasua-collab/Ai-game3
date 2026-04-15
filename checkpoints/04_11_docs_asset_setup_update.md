# Чекпоинт: Обновление docs_asset_setup/ актуальными данными

**Дата:** 2026-04-11 16:10:57 UTC
**Статус:** complete

## Контекст
После Fix-01..Fix-13 (Qi int→long миграция, Disposition→Attitude, и др.) документация в docs_asset_setup/ частично устарела. Задача: прочитать все файлы папки и обновить при необходимости.

## Выполненные задачи

### 01_CultivationLevelData.md
- [x] Удалено поле `regenerationMultiplier` — отсутствует в CultivationLevelData.cs
- [x] Обновлены все 10 блоков Body Effects (убраны строки regenerationMultiplier)
- [x] Обновлена сводная таблица (убран столбец regen)
- [x] Добавлено предупреждение о причине удаления

### 02_MortalStageData.md
- [x] `minQiCapacity: int` → `long` (DAT-M05)
- [x] `maxQiCapacity: int` → `long` (DAT-M05)
- [x] `GetRandomQiCapacity()` → тип возврата `long`
- [x] `GetCoreFormationForAge()` → добавлен guard `maxAge <= minAge` (DAT-C03)

### 06_TechniqueData.md
- [x] `baseQiCost: int` → `long` (DAT-H01)

### 14_FormationData.md
- [x] `contourQiOverride` → тип `long` (вместо int)
- [x] `tickInterval` → `float` (вместо int)
- [x] Добавлено `contourQiOverride: 0 (авто)` в примеры формаций
- [x] Версия обновлена до 1.1

### README.md
- [x] Обновлена дата

### Остальные файлы — без изменений
- 03_ElementData.md — актуален (Poison добавлен ранее)
- 07_NPCPresetData.md — актуален (Attitude+PersonalityTrait добавлены ранее)
- 08_EquipmentData.md — актуален
- 09_EnemySetup.md — актуален
- 10_QuestSetup.md — актуален
- 11_ItemData.md — актуален
- 12_MaterialData.md — актуален
- 13_SpriteSetup.md — актуален
- 15_FormationCoreData.md — актуален
- 16_TileSystem_SemiAuto.md — актуален
- Все SemiAuto файлы — актуальны

## Изменённые файлы
- docs_asset_setup/01_CultivationLevelData.md
- docs_asset_setup/02_MortalStageData.md
- docs_asset_setup/06_TechniqueData.md
- docs_asset_setup/14_FormationData.md
- docs_asset_setup/README.md

## Коммит
- Pending
