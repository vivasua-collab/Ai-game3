# Настройка TechniqueData (Полуавтомат)

**Инструмент:** `Tools → Generate Assets → Techniques (34)`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папки Techniques/ | ✅ Автоматически |
| Чтение JSON данных | ✅ Автоматически |
| Создание TechniqueData assets | ✅ Автоматически |
| Заполнение всех полей | ✅ Автоматически |
| Парсинг элементов | ✅ Автоматически |
| Парсинг типов техник | ✅ Автоматически |
| Парсинг эффектов | ✅ Автоматически |
| Добавление иконок | ❌ Руками |

---

## Шаг 1: Проверка JSON файла

**JSON файл уже существует:**
```
Assets/Data/JSON/techniques.json
```

Файл содержит 34 техники:
- Combat (14) — боевые техники
- Defense (6) — защитные техники
- Curse (3) — проклятия
- Cultivation (2) — культивация
- Formation (2) — формации
- Healing (2) — исцеление
- Movement (2) — перемещение
- Poison (1) — яд
- Sensory (1) — чувствительность
- Support (1) — поддержка

---

## Шаг 2: Запуск генерации (АВТОМАТИЧЕСКИ)

**Действия:**
1. Открой меню: **Tools → Generate Assets → Techniques (34)**
2. Дождись сообщения в Console:
   ```
   [AssetGeneratorExtended] Generated 34 TechniqueData assets
   ```

**Результат в Project:**
```
Assets/Data/Techniques/
├── Tech_Fire_Fist.asset
├── Tech_Water_Shield.asset
├── Tech_Lightning_Step.asset
├── Tech_Earth_Palm.asset
├── Tech_Void_Pierce.asset
├── Tech_Healing_Light.asset
├── Tech_Wind_Blade.asset
├── Tech_Poison_Mist.asset
├── ... (34 файла)
```

---

## Шаг 3: Проверка созданных assets (ВРУКАМИ)

**Выдели любой asset и проверь:**

```
TechniqueData:
├── techniqueId: tech_fire_fist_01
├── nameRu: Огненный кулак
├── nameEn: Fire Fist
├── description: Базовая боевая техника огненной стихии...
├── techniqueType: Combat
├── element: Fire
├── grade: Common
├── techniqueLevel: 1
├── baseQiCost: 10
├── cooldown: 3
├── baseCapacity: 64
├── minCultivationLevel: 1
├── effects:
│   └── [0] Damage, value: 64, duration: 0, chance: 100
│   └── [1] Elemental, value: 10, duration: 60, chance: 20
└── learnableFromScroll: true
```

---

## Шаг 4: Добавление иконок (ВРУКАМИ)

Иконки не генерируются автоматически. Добавь вручную:

1. Выдели asset в Project
2. В Inspector найди поле **Icon**
3. Перетащи спрайт иконки

**Рекомендуемый размер:** 64x64 пикселей

---

## Шаг 5: Использование в игре

### Добавление техники игроку:

```csharp
// Через TechniqueController
var techController = player.GetComponent<TechniqueController>();
techController.LearnTechnique(techniqueData);

// Установка в quick slot
techController.SetQuickSlot(0, learnedTechnique);
```

### Загрузка из Resources:

```csharp
TechniqueData technique = Resources.Load<TechniqueData>("Data/Techniques/Tech_Fire_Fist");
```

---

## Генерация всех assets сразу

Если нужно сгенерировать все типы данных:

**Tools → Generate Assets → All Extended Assets (122)**

Это создаст:
- 34 техники
- 15 NPC пресетов
- 39 предметов экипировки
- 8 расходников
- 17 материалов

---

## Очистка сгенерированных assets

**Tools → Generate Assets → Clear Extended Assets**

Удалит все сгенерированные assets из папок:
- Techniques/
- NPCPresets/
- Equipment/
- Items/
- Materials/

---

## Шпаргалка: Что руками, что скриптом

| Задача | Способ |
|--------|--------|
| Создать папку | 🤖 Скрипт |
| Создать assets | 🤖 Скрипт |
| Заполнить поля | 🤖 Скрипт |
| Парсинг JSON | 🤖 Скрипт |
| Добавить иконки | ✋ Руками |
| Настроить визуал | ✋ Руками |

---

## Типы техник (TechniqueType)

| Тип | Описание |
|-----|----------|
| Combat | Боевые техники |
| Defense | Защитные техники |
| Healing | Исцеление |
| Movement | Перемещение |
| Curse | Проклятия |
| Cultivation | Культивация |
| Support | Поддержка |
| Sensory | Чувствительность |
| Poison | Яд |

---

## Грейды техник (TechniqueGrade)

| Grade | Множитель | Цвет |
|-------|-----------|------|
| Common | ×1.0 | Белый |
| Refined | ×1.4 | Зелёный |
| Perfect | ×2.0 | Синий |
| Transcendent | ×3.0 | Золотой |

---

*Документ создан: 2026-04-02*
