# 📂 docs_asset_setup/ — Инструкции для Unity Editor — листинг

**Версия:** 1.0
**Дата:** 2026-04-30
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)

---

## ⚠️ Назначение папки

> **docs_asset_setup/** — инструкции по созданию .asset файлов и настройке сцены через Unity Editor.
> Каждая инструкция описывает: что скрипт делает автоматически, а что — вручную.
>
> **Полный генератор сцены:** `Tools → Full Scene Builder → Build All (One Click)` (20 фаз, 00–19).
>
> **Основная документация:** `docs/` → [!LISTING.md](../docs/!LISTING.md).
> **Черновики, аудиты:** `docs_temp/` → [!listing.md](../docs_temp/!listing.md).
> **Архив (Phaser-эра):** `docs_old/` → [Listing.md](../docs_old/Listing.md).

### 📐 Оценка токенов

> **Метод:** `chars ÷ 3`. **Легенда стоимости:** 🔥 >15K | ⚠️ 5K–15K | ✅ <5K

---

## 📊 Сводка

| Показатель | Значение |
|---|---|
| Файлов | 34 |
| Объём | 375 KB |
| Токенов ≈ | 125K |

---

## Оркестратор сцен

| Файл | Описание | Размер | Токены ≈ | |
|---|---|---|---|---|
| [SCENE_BUILDER_ARCHITECTURE.md](./SCENE_BUILDER_ARCHITECTURE.md) | Архитектура FullSceneBuilder (заморожен) + 20 фаз (00–19) | 19 KB | 6.4K | ⚠️ |
| [README.md](./README.md) | Обзор папки, порядок выполнения инструкций | 12 KB | 3.9K | ✅ |

---

## Базовые данные

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [01_CultivationLevelData.md](./01_CultivationLevelData.md) | 10 уровней культивации | Автоматически | 15 KB | 5.0K | ⚠️ |
| [02_MortalStageData.md](./02_MortalStageData.md) | 6 этапов смертного | Автоматически | 13 KB | 4.3K | ✅ |
| [03_ElementData.md](./03_ElementData.md) | 7 элементов (стихий) | Автоматически | 13 KB | 4.5K | ✅ |

---

## Сцены и игрок

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [04_BasicScene.md](./04_BasicScene.md) | Базовая сцена | Вручную | 9 KB | 3.1K | ✅ |
| [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md) | Базовая сцена (авто) | Полуавтомат | 9 KB | 2.9K | ✅ |
| [05_PlayerSetup.md](./05_PlayerSetup.md) | Настройка игрока | Вручную | 13 KB | 4.3K | ✅ |
| [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md) | Настройка игрока (авто) | Полуавтомат | 14 KB | 4.7K | ✅ |
| [05_PlayerSetup_Animation.md](./05_PlayerSetup_Animation.md) | Анимации игрока | Вручную | 19 KB | 6.4K | ⚠️ |

---

## Игровой контент

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [06_TechniqueData.md](./06_TechniqueData.md) | 34 техники | Вручную | 14 KB | 4.8K | ✅ |
| [06_TechniqueData_SemiAuto.md](./06_TechniqueData_SemiAuto.md) | 34 техники (авто) | Полуавтомат | 6 KB | 1.8K | ✅ |
| [07_NPCPresetData.md](./07_NPCPresetData.md) | 15 пресетов NPC | Вручную | 12 KB | 4.1K | ✅ |
| [07_NPCPresetData_SemiAuto.md](./07_NPCPresetData_SemiAuto.md) | 15 пресетов NPC (авто) | Полуавтомат | 4 KB | 1.2K | ✅ |
| [08_EquipmentData.md](./08_EquipmentData.md) | 39 единиц экипировки | Вручную | 15 KB | 4.8K | ✅ |
| [08_EquipmentData_SemiAuto.md](./08_EquipmentData_SemiAuto.md) | 39 единиц экипировки (авто) | Полуавтомат | 4 KB | 1.3K | ✅ |
| [09_EnemySetup.md](./09_EnemySetup.md) | 27 типов врагов | Вручную | 13 KB | 4.2K | ✅ |
| [09_EnemySetup_SemiAuto.md](./09_EnemySetup_SemiAuto.md) | 27 типов врагов (авто) | Полуавтомат | 3 KB | 1.0K | ✅ |
| [10_QuestSetup.md](./10_QuestSetup.md) | 15 квестов | Вручную | 14 KB | 4.6K | ✅ |
| [10_QuestSetup_SemiAuto.md](./10_QuestSetup_SemiAuto.md) | 15 квестов (авто) | Полуавтомат | 4 KB | 1.2K | ✅ |
| [11_ItemData.md](./11_ItemData.md) | 8 расходников | Вручную | 12 KB | 4.2K | ✅ |
| [11_ItemData_SemiAuto.md](./11_ItemData_SemiAuto.md) | 8 расходников (авто) | Полуавтомат | 4 KB | 1.3K | ✅ |
| [12_MaterialData.md](./12_MaterialData.md) | 17 материалов | Вручную | 18 KB | 5.9K | ⚠️ |
| [12_MaterialData_SemiAuto.md](./12_MaterialData_SemiAuto.md) | 17 материалов (авто) | Полуавтомат | 4 KB | 1.5K | ✅ |

---

## Формации и ядра

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [14_FormationData.md](./14_FormationData.md) | 24+ формаций | Вручную | 8 KB | 2.6K | ✅ |
| [15_FormationCoreData.md](./15_FormationCoreData.md) | 30+ ядер формаций | Вручную | 7 KB | 2.3K | ✅ |

---

## Графика

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [13_SpriteSetup.md](./13_SpriteSetup.md) | 57 спрайтов | Вручную | 18 KB | 6.1K | ⚠️ |
| [13_SpriteSetup_QuickStart.md](./13_SpriteSetup_QuickStart.md) | 57 спрайтов (быстрый старт) | Полуавтомат | 3 KB | 0.9K | ✅ |

---

## Тайловая система

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [16_TileSystem_SemiAuto.md](./16_TileSystem_SemiAuto.md) | Тайлы + тестовая локация | Полуавтомат | 20 KB | 6.7K | ⚠️ |

---

## Инвентарь

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [17_InventoryData.md](./17_InventoryData.md) | 5 рюкзаков + 4 кольца + volume/allowNesting | Вручную | 10 KB | 3.4K | ✅ |
| [17_InventoryData_SemiAuto.md](./17_InventoryData_SemiAuto.md) | Данные инвентаря (авто) | Полуавтомат | 6 KB | 2.0K | ✅ |
| [18_InventoryUI.md](./18_InventoryUI.md) | InventoryScreen + панели (вручную wiring) | Вручную | 14 KB | 4.6K | ✅ |
| [18_InventoryUI_SemiAuto.md](./18_InventoryUI_SemiAuto.md) | InventoryScreen + ~150 wiring автоматически | Полуавтомат | 10 KB | 3.4K | ✅ |

---

## NPC

| Файл | Описание | Способ | Размер | Токены ≈ | |
|---|---|---|---|---|---|
| [19_NPCPlacement.md](./19_NPCPlacement.md) | 7 NPC на тестовой поляне + hotkey-спавн | Полуавтомат | 16 KB | 5.4K | ⚠️ |

---

## 💰 Топ-3 самых дорогих файлов

| Файл | Токены ≈ | |
|---|---|---|
| 16_TileSystem_SemiAuto.md | 6.7K | ⚠️ |
| 05_PlayerSetup_Animation.md | 6.4K | ⚠️ |
| SCENE_BUILDER_ARCHITECTURE.md | 6.4K | ⚠️ |

> ⚡ Нет файлов >15K токенов. Папка оптимальна по стоимости.

---

## 🔗 Соседние папки

| Папка | Листинг | Описание |
|---|---|---|
| `docs/` | [!LISTING.md](../docs/!LISTING.md) | Основная документация (42 файла) |
| `docs_temp/` | [!listing.md](../docs_temp/!listing.md) | Черновики, аудиты (57 файлов) |
| `docs_old/` | [Listing.md](../docs_old/Listing.md) | Архив Phaser-эры (69 файлов) |

---

*Создано: 2026-04-30 08:09:40 UTC*
