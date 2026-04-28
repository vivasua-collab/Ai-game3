# Настройка ItemData (Полуавтомат)

**Инструмент:** `Tools → Generate Assets → Items (8)`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папки Items/ | ✅ Автоматически |
| Чтение JSON данных | ✅ Автоматически |
| Создание ItemData assets | ✅ Автоматически |
| Заполнение всех полей | ✅ Автоматически |
| Парсинг эффектов | ✅ Автоматически |
| Добавление иконок | ❌ Руками |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Generate Assets → Items (8)`

**Результат:**
```
Assets/Data/Items/
├── Item_Healing_Pill.asset
├── Item_Qi_Pill.asset
├── Item_Stamina_Pill.asset
├── Item_Breakthrough_Pill.asset
├── Item_Bread.asset
├── Item_Meat.asset
├── Item_Antidote.asset
└── Scroll_Technique.asset
```

---

## Сводная таблица расходников (7)

| ID | Название | Тип | Эффект | Значение | Стак | Цена |
|----|----------|-----|--------|----------|------|------|
| item_healing_pill | Лечебная пилюля | pill | heal | 20 HP | 99 | 50 |
| item_qi_pill | Пилюля Ци | pill | restore_qi | 100 Ци | 99 | 80 |
| item_stamina_pill | Пилюля выносливости | pill | reduce_fatigue | 30% | 99 | 40 |
| item_breakthrough_pill | Пилюля прорыва | pill | breakthrough_bonus | +20% | 20 | 500 |
| item_food_bread | Хлеб | food | reduce_hunger | 30 | 20 | 5 |
| item_food_meat | Мясо | food | reduce_hunger | 60 | 20 | 15 |
| item_antidote | Противоядие | medicine | remove_poison | 1 | 30 | 100 |

---

## Свитки (1)

| ID | Название | Объём (л) | Цена |
|----|----------|-----------|------|
| scroll_technique_common | Свиток техники | 0.5 | 200 |

---

## Типы эффектов

| effectType | Описание | duration |
|------------|----------|----------|
| heal | Восстановление HP | 0 |
| restore_qi | Восстановление Ци | 0 |
| reduce_fatigue | Снижение усталости | 0 |
| reduce_hunger | Снижение голода | 0 |
| remove_poison | Снятие отравления | 0 |
| breakthrough_bonus | Бонус к прорыву | >0 |
| learn_technique | Изучение техники | 0 |

---

## Поля ItemData

| Поле | Тип | Описание |
|------|-----|----------|
| itemId | string | Уникальный идентификатор предмета |
| itemName | string | Отображаемое название |
| itemType | enum | Тип предмета (pill, food, medicine, scroll и т.д.) |
| effectType | enum | Тип эффекта при использовании |
| effectValue | float | Числовое значение эффекта |
| duration | float | Длительность эффекта (0 — мгновенный) |
| maxStack | int | Максимальный размер стака |
| price | int | Базовая цена |
| weight | float | Вес предмета (кг) |
| volume | float | Объём предмета (литры) — используется для линейного инвентаря |
| allowNesting | bool | Разрешить вложение предметов внутрь (контейнеры) |
| icon | Sprite | Иконка предмета |

---

## Использование в коде

```csharp
// Загрузка ItemData
var item = Resources.Load<ItemData>("Data/Items/Item_Healing_Pill");

// Использование
inventory.UseItem(item, targetEntity);
```

---

*Документ обновлён: 2025-07-09 • Версия 2.0 (миграция на линейный инвентарь)*
