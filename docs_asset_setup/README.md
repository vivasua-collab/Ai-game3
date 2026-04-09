# 📁 Asset Setup — Инструкции по настройке Unity Editor

Навигатор по инструкциям создания .asset файлов через Unity Editor.

---

## 📋 Перечень инструкций

### Базовые данные

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [01_CultivationLevelData.md](./01_CultivationLevelData.md) | 10 уровней культивации | Автоматически |
| [02_MortalStageData.md](./02_MortalStageData.md) | 6 этапов смертного | Автоматически |
| [03_ElementData.md](./03_ElementData.md) | 7 элементов (стихий) | Автоматически |

### Игровой контент

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [06_TechniqueData.md](./06_TechniqueData.md) | 34 техники | Автоматически |
| [07_NPCPresetData.md](./07_NPCPresetData.md) | 15 пресетов NPC | Автоматически |
| [08_EquipmentData.md](./08_EquipmentData.md) | 39 единиц экипировки | Автоматически |
| [09_EnemySetup.md](./09_EnemySetup.md) | 27 типов врагов | Автоматически |
| [10_QuestSetup.md](./10_QuestSetup.md) | 15 квестов | Автоматически |
| [11_ItemData.md](./11_ItemData.md) | 8 расходников | Автоматически |
| [12_MaterialData.md](./12_MaterialData.md) | 17 материалов | Автоматически |

### Формации и ядра

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [14_FormationData.md](./14_FormationData.md) | 24+ формаций | Автоматически |
| [15_FormationCoreData.md](./15_FormationCoreData.md) | 30+ ядер формаций | Автоматически |

### Сцены и игрок

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [04_BasicScene.md](./04_BasicScene.md) | Базовая сцена | Вручную |
| [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md) | Базовая сцена | Полуавтомат |
| [05_PlayerSetup.md](./05_PlayerSetup.md) | Player prefab | Вручную |
| [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md) | Player prefab | Полуавтомат |
| [05_PlayerSetup_Animation.md](./05_PlayerSetup_Animation.md) | Анимации игрока | Вручную |

### Графика

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [13_SpriteSetup.md](./13_SpriteSetup.md) | 57 спрайтов | Вручную |
| [13_SpriteSetup_QuickStart.md](./13_SpriteSetup_QuickStart.md) | 57 спрайтов | Полуавтомат |

### Тайловая система

| Файл | Что создаёт | Способ |
|------|-------------|--------|
| [16_TileSystem_SemiAuto.md](./16_TileSystem_SemiAuto.md) | Тайлы + тестовая локация | Полуавтомат |

---

## 🤖 Инструменты автоматизации

### Меню Unity Editor

| Меню | Скрипт | Назначение |
|------|--------|------------|
| `Window → Asset Generator` | `Editor/AssetGenerator.cs` | Базовые данные |
| `Window → Asset Generator Extended` | `Editor/AssetGeneratorExtended.cs` | Контент |
| `Tools → Generate Assets → Formation Assets` | `Editor/FormationAssetGenerator.cs` | Формации |
| `Window → Scene Setup Tools` | `Editor/SceneSetupTools.cs` | Сцена + Player |
| `Tools → Generate Tile Sprites` | `Tile/Editor/TileSpriteGenerator.cs` | Спрайты тайлов |
| `Tools → Setup Test Location Scene` | `Tile/Editor/TestLocationSetup.cs` | Тестовая локация |

---

## 🚀 Порядок настройки (быстрый старт)

1. **Базовые данные** — `Window → Asset Generator` → Generate All
2. **Контент** — `Window → Asset Generator Extended` → All Extended Assets
3. **Формации** — `Tools → Generate Assets → Formation Assets`
4. **Сцена** — см. [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md)
5. **Player** — см. [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md)

---

## 📚 Где искать информацию

| Тема | Документ |
|------|----------|
| Архитектура систем | `docs/ARCHITECTURE.md` |
| Формулы расчёта | `docs/ALGORITHMS.md` |
| Система Ци | `docs/QI_SYSTEM.md` |
| Боевая система | `docs/COMBAT_SYSTEM.md` |
| Enums проекта | `UnityProject/Assets/Scripts/Core/Enums.cs` |
| Примеры кода | `docs_examples/` |

---

## ⚠️ Важно

- **SemiAuto** = автоматизация + ручные шаги после
- Все числовые значения вводить без запятых
- Checkbox (✓/✗) = true/false

---

*Обновлено: 2026-04-09*
