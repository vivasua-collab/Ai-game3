# Настройка NPCPresetData (Полуавтомат)

**Инструмент:** `Tools → Generate Assets → NPC Presets (15)`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папки NPCPresets/ | ✅ Автоматически |
| Чтение JSON данных | ✅ Автоматически |
| Создание NPCPresetData assets | ✅ Автоматически |
| Заполнение базовых полей | ✅ Автоматически |
| Заполнение характеристик | ✅ Автоматически |
| Добавление иконок | ❌ Руками |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Generate Assets → NPC Presets (15)`

**Результат в консоли:**
```
[AssetGeneratorExtended] Generated 15 NPCPresetData assets
```

**Созданные файлы:**
```
Assets/Data/NPCPresets/
├── NPC_Селянин.asset
├── NPC_Стражник.asset
├── NPC_Практик.asset
├── NPC_Разбойник.asset
├── NPC_Волк.asset
├── NPC_Духовный_тигр.asset
├── NPC_Торговец.asset
├── NPC_Старейшина.asset
├── NPC_Ученик.asset
├── NPC_Трактирщик.asset
├── NPC_Кузнец.asset
├── NPC_Алхимик.asset
├── NPC_Отшельник.asset
├── NPC_Странствующий_монах.asset
└── NPC_Укротитель.asset
```

---

## Шаг 2: Проверка созданного NPC (ВРУКАМИ)

**Выдели NPC_Стражник:**

```
NPCPresetData:
├── presetId: npc_guard_01
├── nameTemplate: Стражник
├── title: Охранник
├── cultivationLevel: 2
├── cultivationSubLevel: 3
├── coreCapacity: 1500
├── strength: 12
├── agility: 10
├── intelligence: 7
├── vitality: 12
└── conductivity: 0.8
```

---

## Шаг 3: Категории NPC

| Категория | Описание | Кол-во |
|-----------|----------|--------|
| Temp | Временные NPC | 4 |
| Plot | Сюжетные NPC | 4 |
| Unique | Уникальные NPC | 7 |

---

## Шаг 4: Использование NPCPresets

**В коде:**
```csharp
// Загрузка пресета
var preset = Resources.Load<NPCPresetData>("Data/NPCPresets/NPC_Стражник");

// Применение к NPC
npcController.ApplyPreset(preset);
```

---

## Сводная таблица NPC (15 пресетов)

| ID | Название | Уровень | Тип |
|----|----------|---------|-----|
| npc_villager_01 | Селянин | 1 | Temp |
| npc_guard_01 | Стражник | 2 | Plot |
| npc_cultivator_01 | Практик | 2 | Plot |
| npc_bandit_01 | Разбойник | 1 | Temp |
| npc_wolf_01 | Волк | 1 | Temp |
| npc_spirit_tiger_01 | Духовный тигр | 3 | Temp |
| npc_merchant_01 | Торговец | 1 | Unique |
| npc_sect_elder_01 | Старейшина | 5 | Unique |
| npc_disciple_01 | Ученик | 2 | Plot |
| npc_innkeeper_01 | Трактирщик | 1 | Unique |
| npc_blacksmith_01 | Кузнец | 2 | Unique |
| npc_alchemist_01 | Алхимик | 3 | Unique |
| npc_hermit_01 | Отшельник | 4 | Unique |
| npc_traveling_monk_01 | Странствующий монах | 2 | Temp |
| npc_beast_tamer_01 | Укротитель | 3 | Plot |

---

*Документ создан: 2026-04-02*
