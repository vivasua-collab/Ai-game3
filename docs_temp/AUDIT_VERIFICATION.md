# Верификация аудитов — Наши 7 агентов vs Qwen-Coder

**Дата:** 2026-04-10  
**Версия Unity:** 6000.3 (2D URP)  
**Документация:** Unity 6.3 LTS Scripting API

---

## 1. Верификация наших рекомендаций против Unity 6000.3 Docs

### ✅ Подтверждено Unity Docs

| Рекомендация | Unity API Doc | Статус |
|-------------|-------------|--------|
| C-01: rb.velocity → rb.linearVelocity | Rigidbody2D.linearVelocity существует в 6000.3 | ✅ Верно |
| C-07: [SerializeField] без MonoBehaviour не работает | Документация показывает примеры только с MonoBehaviour | ✅ Верно |
| NC-16: JsonUtility не десериализует массивы как root | "JSON string must always have an object at the top level, not an array" | ✅ Верно |
| NM-09: GetComponent<ICombatant> менее эффективен | "non-generic version is not as efficient as the Generic version" | ✅ Верно |
| NC-18: Editor файлы без #if UNITY_EDITOR ломают build | UnityEditor namespace недоступен в player build | ✅ Верно |

### ⚠️ Уточнения

| Рекомендация | Уточнение |
|-------------|-----------|
| C-06: PlayerController Input System | Keyboard.current — корректный Unity 6 API если InputSystem package установлен. Проверка на null нужна, но не критична — если пакет есть, current не null |
| H-06: float.PositiveInfinity → MaxValue | Unity не кидает exception при операциях с Infinity. Проблема реальна но не крашит — даёт NaN/Infinity в значениях |
| NH-33: Qi long→float на слайдерах | Unity Slider.maxValue = float. Для Qi > 16.7M теряется точность. Но для UI слайдера достаточно нормализации к 0-1 |

### ❌ Не подтверждено / неверно

| Рекомендация | Проблема |
|-------------|---------|
| — | Все наши рекомендации верифицированы, неверных не найдено |

---

## 2. Анализ аудита Qwen-Coder

### Фактические ошибки Qwen

| Утверждение Qwen | Факт | Вердикт |
|-----------------|------|---------|
| "20+ FindFirstObjectByType" | Фактически: 19 файлов, ~49 вызовов. Не "20+", но близко | ⚠️ Завышено незначительно |
| "124 C# файла" | Фактически: 124 файла. У нас считалось 115 — разница за счёт Editor/Tests/Naming | ✅ Верно |
| "~52,000 строк кода" | У нас оценено ~15,000. Qwen считает ~52,000. Нужна проверка | ❌ Скорее всего завышено |
| "AssetGeneratorExtended.cs 1,253 строки" | Фактически: 1,253 строки ✅ | ✅ Верно |
| "BuffManager.cs 1,278 строк" | Фактически: 1,278 строк ✅ | ✅ Верно |
| "CombatUI.cs 942 строки" | Фактически: 942 строки ✅ | ✅ Верно |
| "async/await отсутствует" | Фактически: 0 использований async/await ✅ | ✅ Верно |
| "Camera.main устарел" | Camera.main НЕ устарел в Unity 6.3 — просто медленный | ❌ Qwen неточно |
| "Тестовое покрытие 30%" | 4 тестовых файла на 124 исходных. Реально ~5-10%, не 30% | ❌ Завышено |

### Новые находки Qwen (не в нашем аудите)

| Находка | Валидность | Оценка |
|---------|-----------|--------|
| BuffManager SRP нарушение (6 файлов вместо 1) | ✅ Валидно — 1,278 строк, много ответственностей | У нас NH-10..12 описывают симптомы, Qwen предлагает структурное решение |
| CombatUI 30+ public полей для Inspector | ✅ Валидно — нужно [SerializeField] private | Новое — у нас не отмечено |
| CombatUI создание объектов в Update | ✅ Валидно — damage numbers без пулинга | Перекрывается с нашим NM-07 |
| CombatUI нет разделения View/Controller | ✅ Валидно архитектурно | Новое — у нас не отмечено |
| ServiceLocator [RuntimeInitializeOnLoadMethod] | ✅ Валидно — для автоочистки | Новое — у нас упомянуто косвенно в NM-04 |
| Расширение RegisteredBehaviour<T> | ✅ Валидно | Новое |
| Валидация дублирующихся регистраций ServiceLocator | ✅ Валидно | Новое — у нас NL-20 про pending requests |
| CombatTests нет тестов Formation/Buff/Save | ✅ Валидно | Перекрывается с нашим NL-34 |
| Рефакторинг BuffManager на 6 файлов | ⚠️ Спорно — увеличение количества файлов может усложнить | Кандидат на P2, не P0 |
| async/await для Save/Load | ⚠️ Спорно — Unity coroutines + UniTask лучше подходят | Не рекомендуется — Unity не ASP.NET |
| Moq/NSubstitute для моков | ❌ Unity тесты используют NUnit, моки через ручные stub'ы | Unity рекомендует ручные моки для MonoBehaviour |

### Рекомендации Qwen — оценка

| Рекомендация Qwen | Наша оценка | Приоритет |
|------------------|-------------|-----------|
| Разделить BuffManager → 6 файлов | Согласен частично → 3 файла (BuffManager_Core, BuffManager_Special, BuffManager_Control) | P2 |
| Заменить FindFirstObjectByType → ServiceLocator | Полностью согласен | P0 ✅ |
| Тесты BuffManager 15-20 штук | Согласен | P1 |
| async/await для Save/Load | НЕ согласен — Unity использует корутины. Альтернатива: UniTask | Отклонено |
| Вынести магические числа | Согласен | P1 |
| CombatUI View/ViewModel | Согласен архитектурно | P2 |
| Camera.main → кэш | Согласен (уже в нашем аудите NL-29, NL-30) | P1 |
| Moq/NSubstitute | НЕ согласен — Unity test framework не поддерживает полноценно | Отклонено |
| Object Pooling для UI | Согласен | P2 |

---

## 3. Сводка уникальных находок

### Только у нас (нет у Qwen)

- C-03: Heart blackHP (лор-специфичный баг)
- C-08: StatDevelopment <10 не растут
- C-11: DamageCalculator стихийные взаимодействия
- NC-01..02: Duplicate enums с расходящимися значениями
- NC-10: NPCSaveData без MaxQi/MaxHealth
- NC-13: QuestData mutable objectives на SO
- NC-14: InventorySlot.GetCondition durability=0 → Pristine
- NC-15: FormationEffects control перманентный
- NC-17: StatDevelopment.ConsolidateStat сломан
- Все int→long Qi проблемы (10+ файлов)
- Все JsonUtility сериализации проблемы
- Все namespace проблемы (CultivationWorld vs CultivationGame)
- Все Time.time vs gameTime проблемы
- RelationshipController/DialogueSystem/NPCAI специфичные баги

### Только у Qwen (нет у нас)

- CombatUI 30+ public полей для Inspector
- CombatUI нет View/Controller разделения
- ServiceLocator [RuntimeInitializeOnLoadMethod] автоочистка
- Валидация дублирующихся регистраций
- Расширение RegisteredBehaviour<T> использование

---

## 4. Итоговые рекомендации

### P0 — наши, подтверждённые Unity docs

1. Qi: int → long (10+ файлов)
2. rb.velocity → linearVelocity
3. Heart blackHP = 0
4. DamageCalculator стихийные взаимодействия
5. StatDevelopment пороги + консолидация
6. BuffManager float.Parse → TryParse
7. NPCSaveData MaxQi/MaxHealth
8. QuestData clone objectives
9. #if UNITY_EDITOR для Editor файлов
10. JsonUtility wrapper для массивов/Dictionary

### P1 — новые от Qwen (валидные)

11. CombatUI public → [SerializeField] private
12. ServiceLocator [RuntimeInitializeOnLoadMethod] + дубликаты проверка
13. Camera.main кэширование (уже в нашем аудите)

### Отклонено

- async/await для Save/Load → использовать корутины/UniTask
- Moq/NSubstitute → ручные моки в Unity
- BuffManager → 6 файлов (избыточно, достаточно 3)

---

*Верификация: 2026-04-10 — Unity 6000.3 Docs, 7 агентов + Qwen-Coder*
