# Настройка анимации Player (Детальная инструкция)

**Создано:** 2026-04-03
**Редактировано:** 2026-04-03

**Инструмент:** Animator Window

---

## Подготовка

### Шаг 0: Открой Animator окно

1. Выдели `Player` в Hierarchy
2. Окно **Animator** откроется автоматически (или **Window → Animation → Animator**)
3. Убедись, что в заголовке окна выбран `PlayerAnimator`

---

## Создание состояний (States)

### Шаг 1: Создание состояния Idle

1. **Правый клик** в пустом месте Animator окна
2. Выбери **Create State → Empty**
3. В Inspector (справа) назови его `Idle`
4. Это будет состояние по умолчанию (оранжевый цвет)

```
┌─────────────────────────────────────────────────────┐
│  Animator Window                                    │
│                                                     │
│     ┌──────┐                                        │
│     │ Idle │  ← оранжевый (Default State)          │
│     └──────┘                                        │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Если Idle не оранжевый:**
- Правый клик на `Idle` → **Set as Layer Default State**

---

### Шаг 2: Создание состояния Walk

1. **Правый клик** в пустом месте
2. **Create State → Empty**
3. Назови `Walk`

```
┌─────────────────────────────────────────────────────┐
│  Animator Window                                    │
│                                                     │
│     ┌──────┐       ┌──────┐                        │
│     │ Idle │       │ Walk │                        │
│     └──────┘       └──────┘                        │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

### Шаг 3: Создание состояния Attack

1. **Правый клик** → **Create State → Empty**
2. Назови `Attack`

---

### Шаг 4: Создание состояния Hurt

1. **Правый клик** → **Create State → Empty**
2. Назови `Hurt`

---

### Шаг 5: Создание состояния Death

1. **Правый клик** → **Create State → Empty**
2. Назови `Death`

---

## Итоговая структура состояний

```
┌─────────────────────────────────────────────────────────────┐
│  Animator Window                                            │
│                                                             │
│     ┌──────┐       ┌──────┐       ┌────────┐              │
│     │ Idle │ ←───→ │ Walk │       │ Attack │              │
│     └──────┘       └──────┘       └────────┘              │
│         ↑              ↑              │                    │
│         │              │              │                    │
│         └──────────────┴──────────────┘                    │
│                                                             │
│     ┌──────┐       ┌──────┐                                │
│     │ Hurt │       │Death │                                │
│     └──────┘       └──────┘                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Создание переходов (Transitions)

### Переход Idle → Walk

1. **Правый клик на `Idle`**
2. Выбери **Make Transition**
3. **Кликни на `Walk`**
4. Появится стрелка от Idle к Walk

### Переход Walk → Idle

1. **Правый клик на `Walk`**
2. **Make Transition**
3. **Кликни на `Idle`**

### Переход Idle → Attack

1. **Правый клик на `Idle`**
2. **Make Transition**
3. **Кликни на `Attack`**

### Переход Attack → Idle

1. **Правый клик на `Attack`**
2. **Make Transition**
3. **Кликни на `Idle`**

---

### Переходы из Any State (особые состояния)

**Any State → Hurt:**

1. Найди оранжевый прямоугольник **Any State** (слева вверху)
2. **Правый клик на Any State**
3. **Make Transition**
4. **Кликни на `Hurt`**

**Any State → Death:**

1. **Правый клик на Any State**
2. **Make Transition**
3. **Кликни на `Death`**

**Any State → Attack (опционально):**

1. **Правый клик на Any State**
2. **Make Transition**
3. **Кликни на `Attack`**

---

### Переход Hurt → Idle

1. **Правый клик на `Hurt`**
2. **Make Transition**
3. **Кликни на `Idle`**

---

## Добавление параметров

### Шаг 1: Открой вкладку Parameters

В Animator окне найди вкладку **Parameters** (слева вверху)

### Шаг 2: Создай параметры

Нажми **+** и выбери тип:

| Имя параметра | Тип | Описание |
|---------------|-----|----------|
| `speed` | Float | Скорость движения (0 = стоит, >0 = идет) |
| `isMoving` | Bool | true = движется, false = стоит |
| `attack` | Trigger | Нажал кнопку атаки |
| `hurt` | Trigger | Получил урон |
| `death` | Trigger | Умер |
| `moveX` | Float | Направление по X (-1, 0, 1) |
| `moveY` | Float | Направление по Y (-1, 0, 1) |

```
┌─────────────────────────────────────────────────────────────┐
│  Parameters                                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ⊕  speed          Float    0                        │  │
│  │  ⊕  isMoving       Bool     □                        │  │
│  │  ⊕  attack         Trigger                           │  │
│  │  ⊕  hurt           Trigger                           │  │
│  │  ⊕  death          Trigger                           │  │
│  │  ⊕  moveX          Float    0                        │  │
│  │  ⊕  moveY          Float    0                        │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Настройка условий переходов

### Настрой Idle → Walk

1. **Кликни на стрелку** (переход Idle → Walk)
2. В Inspector найди **Conditions**
3. Нажми **+**
4. Выбери условие:

```
Conditions:
├── isMoving == true
```

Или альтернативно:

```
Conditions:
├── speed > 0.1
```

### Настрой Walk → Idle

1. **Кликни на стрелку** (Walk → Idle)
2. Добавь условие:

```
Conditions:
├── isMoving == false
```

Или:

```
Conditions:
├── speed < 0.1
```

### Настрой Idle → Attack

1. **Кликни на стрелку** (Idle → Attack)
2. Добавь условие:

```
Conditions:
├── attack (Trigger)
```

### Настрой Attack → Idle

1. **Кликни на стрелку** (Attack → Idle)
2. **Условие не нужно!**
3. Включи **Has Exit Time** = true
4. Установи **Exit Time** = 0.9 (90% анимации)

```
Settings:
├── Has Exit Time: ☑
├── Exit Time: 0.9
└── Transition Duration: 0.1
```

### Настрой Any State → Hurt

1. **Кликни на стрелку** (Any State → Hurt)
2. Добавь условие:

```
Conditions:
├── hurt (Trigger)
```

3. **Settings:**
```
├── Can Transition To Self: ☐ (отключи!)
├── Has Exit Time: ☐
└── Transition Duration: 0.1
```

### Настрой Hurt → Idle

1. **Кликни на стрелку** (Hurt → Idle)
2. **Settings:**
```
├── Has Exit Time: ☑
├── Exit Time: 0.8
└── Transition Duration: 0.1
```

### Настрой Any State → Death

1. **Кликни на стрелку** (Any State → Death)
2. Добавь условие:

```
Conditions:
├── death (Trigger)
```

---

## Настройка времени переходов

Для каждого перехода (стрелки) настрой:

| Переход | Has Exit Time | Exit Time | Duration |
|---------|---------------|-----------|----------|
| Idle → Walk | ☐ | — | 0.1 |
| Walk → Idle | ☐ | — | 0.1 |
| Idle → Attack | ☐ | — | 0.05 |
| Attack → Idle | ☑ | 0.9 | 0.1 |
| Any State → Hurt | ☐ | — | 0.05 |
| Hurt → Idle | ☑ | 0.8 | 0.1 |
| Any State → Death | ☐ | — | 0.1 |

---

## Подключение анимаций к состояниям

### Если есть анимационные клипы (.anim):

1. **Кликни на состояние** (например, Idle)
2. В Inspector найди поле **Motion**
3. Перетащи туда анимационный клип `PlayerIdle`

```
State: Idle
├── Motion: PlayerIdle
├── Speed: 1
└── Mirror: ☐
```

### Если нет анимаций (временно):

Можно использовать пустые состояния для тестирования логики.

---

## Итоговая диаграмма переходов

```
                        ┌─────────────┐
                        │  Any State  │
                        └──────┬──────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           ▼                   ▼                   ▼
      ┌────────┐          ┌──────┐           ┌──────┐
      │  Hurt  │          │Attack│           │Death │
      └───┬────┘          └───┬──┘           └──────┘
          │                   │
          │     ┌─────────────┘
          │     │
          ▼     ▼
       ┌──────────┐      ┌──────────┐
       │   Idle   │ ←──→ │   Walk   │
       └──────────┘      └──────────┘
           ▲
           └─────────────────────────┐
                                     │
                              (Entry → Idle)
```

---

## Таблица всех переходов и условий

| Из | В | Условие | Exit Time |
|----|---|---------|-----------|
| Entry | Idle | — | — |
| Idle | Walk | `isMoving == true` | — |
| Walk | Idle | `isMoving == false` | — |
| Idle | Attack | `attack` (Trigger) | — |
| Attack | Idle | (auto) | 0.9 |
| Any State | Hurt | `hurt` (Trigger) | — |
| Hurt | Idle | (auto) | 0.8 |
| Any State | Death | `death` (Trigger) | — |

---

## Код для управления аниматором

Добавь в `PlayerController.cs`:

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.5f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        // Или animator = GetComponentInChildren<Animator>(); 
        // если Animator на дочернем объекте Visual
    }
    
    void Update()
    {
        // Чтение ввода
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // Обновление параметров аниматора
        if (animator != null)
        {
            bool isMoving = movement.magnitude > 0.1f;
            animator.SetBool("isMoving", isMoving);
            animator.SetFloat("speed", movement.magnitude);
            animator.SetFloat("moveX", movement.x);
            animator.SetFloat("moveY", movement.y);
        }
        
        // Атака (пример)
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("attack");
        }
    }
    
    void FixedUpdate()
    {
        // Применение движения
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
    
    // Вызывается при получении урона
    public void TakeDamage()
    {
        animator.SetTrigger("hurt");
    }
    
    // Вызывается при смерти
    public void Die()
    {
        animator.SetTrigger("death");
    }
}
```

---

## Создание анимаций (Animation Clips)

### Создание анимации Idle

1. Выдели `Player` в Hierarchy
2. Открой **Window → Animation → Animation** (или Ctrl+6)
3. Нажми **Create**
4. Сохрани как `Assets/Animations/Player/PlayerIdle.anim`
5. Добавь ключи:
   - Нажми красную кнопку записи
   - Измени Sprite Renderer → Sprite на первый кадр
   - Перемести ползунок времени
   - Измени на следующий кадр
   - Повтори для всех кадров
6. Нажми кнопку записи снова (остановить)

### Создание анимации Walk

То же самое, но с кадрами ходьбы. Сохрани как `PlayerWalk.anim`.

### Создание анимации Attack

То же самое, но с кадрами атаки. Сохрани как `PlayerAttack.anim`.

---

## Рекомендуемая структура папок

```
Assets/
├── Animations/
│   └── Player/
│       ├── PlayerIdle.anim
│       ├── PlayerWalk.anim
│       ├── PlayerAttack.anim
│       ├── PlayerHurt.anim
│       ├── PlayerDeath.anim
│       └── PlayerAnimator.controller
├── Prefabs/
│   └── Player/
│       └── Player.prefab
└── Sprites/
    └── Characters/
        └── Player/
            ├── Idle/
            │   ├── idle_0.png
            │   ├── idle_1.png
            │   ├── idle_2.png
            │   └── idle_3.png
            ├── Walk/
            │   ├── walk_0.png
            │   ├── walk_1.png
            │   ├── walk_2.png
            │   └── walk_3.png
            └── Attack/
                ├── attack_0.png
                ├── attack_1.png
                └── attack_2.png
```

---

## Добавление спрайта игроку

### Вариант 1: Одиночный спрайт

1. Выдели `Player` в Hierarchy
2. **Add Component → Sprite Renderer**
3. Перетащи спрайт в поле **Sprite**

### Вариант 2: Дочерний объект Visual (рекомендуется)

1. Правый клик на `Player` → **Create Empty**
2. Назови `Visual`
3. Добавь **Sprite Renderer** на `Visual`
4. Добавь **Animator** на `Visual`
5. Перетащи спрайт и animator controller

**Структура:**
```
Player
├── Visual              ← Sprite Renderer, Animator
└── InteractionRange    ← Circle Collider 2D (Trigger)
```

---

## Быстрая проверка

| Этап | Статус |
|------|--------|
| Animator Controller создан | ☐ |
| Состояния созданы (Idle, Walk, Attack, Hurt, Death) | ☐ |
| Переходы созданы | ☐ |
| Параметры добавлены | ☐ |
| Условия переходов настроены | ☐ |
| Анимационные клипы подключены | ☐ |
| Код управления аниматором добавлен | ☐ |
| Sprite Renderer добавлен | ☐ |
| Спрайт выбран | ☐ |

---

## Частые ошибки

### 1. Анимация не проигрывается
- Проверь что Animator Controller подключен к компоненту Animator
- Проверь что есть анимационный клип в состоянии (Motion)

### 2. Переходы не работают
- Проверь условия переходов
- Проверь что параметры правильно обновляются в коде

### 3. Зацикливание анимации
- В анимационном клипе включи/выключи **Loop Time**
- Attack, Hurt, Death — обычно без цикла
- Idle, Walk — обычно с циклом

### 4. Анимация мигает
- Увеличь **Transition Duration**
- Проверь что нет конфликтующих переходов

---

*Документ создан: 2026-04-03*
