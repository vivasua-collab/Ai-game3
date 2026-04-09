# Зеркалирование спрайтов персонажей

**Создано:** 2026-04-03
**Версия:** 1.0

---

## Обзор

В нашей игре персонажи имеют только 2 направления взгляда — вправо и влево. Вместо создания отдельных спрайтов для каждого направления, мы используем зеркалирование одного спрайта.

---

## Принцип

```
Один спрайт (направо) → Зеркалирование scaleX = -1 → Направо/Налево
```

**Преимущества:**
- Экономия места (1 спрайт вместо 2+)
- Простота анимации
- Согласованность визуала

---

## Базовая реализация

### CharacterSpriteController.cs

```csharp
// Создано: 2026-04-03
using UnityEngine;

/// <summary>
/// Контроллер спрайта персонажа.
/// Управляет направлением взгляда через зеркалирование.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterSpriteController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Settings")]
    [SerializeField] private bool faceRightByDefault = true;

    /// <summary>
    /// Текущее направление: 1 = вправо, -1 = влево
    /// </summary>
    public int FacingDirection { get; private set; }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Инициализация направления
        FacingDirection = faceRightByDefault ? 1 : -1;
        ApplyFacingDirection();
    }

    /// <summary>
    /// Устанавливает направление взгляда.
    /// </summary>
    public void SetFacingDirection(int direction)
    {
        if (direction == 0) return;

        FacingDirection = direction > 0 ? 1 : -1;
        ApplyFacingDirection();
    }

    /// <summary>
    /// Поворачивает персонажа к указанной точке.
    /// </summary>
    public void FaceTowards(Vector2 target)
    {
        Vector2 position = transform.position;
        float direction = target.x - position.x;

        if (Mathf.Abs(direction) > 0.1f)
        {
            SetFacingDirection(direction > 0 ? 1 : -1);
        }
    }

    /// <summary>
    /// Поворачивает персонажа в сторону движения.
    /// </summary>
    public void FaceMovementDirection(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) > 0.1f)
        {
            SetFacingDirection(velocity.x > 0 ? 1 : -1);
        }
    }

    private void ApplyFacingDirection()
    {
        // Зеркалируем спрайт через scale
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    /// <summary>
    /// Быстро проверить, смотрит ли персонаж вправо.
    /// </summary>
    public bool IsFacingRight => FacingDirection > 0;
}
```

---

## Интеграция с PlayerController

### PlayerController.cs (обновлённый)

```csharp
// Редактировано: 2026-04-03
using UnityEngine;

[RequireComponent(typeof(CharacterSpriteController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Components")]
    [SerializeField] private CharacterSpriteController spriteController;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private void Awake()
    {
        if (spriteController == null)
            spriteController = GetComponent<CharacterSpriteController>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Получаем ввод
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Нормализуем диагональное движение
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // Обновляем направление взгляда
        UpdateFacingDirection();

        // Обновляем анимацию
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Плавное перемещение
        Vector2 targetVelocity = moveInput * moveSpeed;
        currentVelocity = Vector2.Lerp(
            currentVelocity,
            targetVelocity,
            (moveInput.sqrMagnitude > 0.1f ? acceleration : deceleration) * Time.fixedDeltaTime
        );

        rb.velocity = currentVelocity;
    }

    private void UpdateFacingDirection()
    {
        // Способ 1: По вводу движения
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            spriteController.SetFacingDirection(moveInput.x > 0 ? 1 : -1);
        }

        // Способ 2: По позиции мыши (раскомментируйте если нужно)
        /*
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        spriteController.FaceTowards(mousePos);
        */
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", currentVelocity.sqrMagnitude);
        animator.SetBool("IsMoving", currentVelocity.sqrMagnitude > 0.1f);
    }
}
```

---

## Нюанс с дочерними объектами

### Проблема

При зеркалировании через `transform.localScale.x = -1`, все дочерние объекты тоже зеркалируются. Это может создать проблемы:
- Оружие на орбите тоже зеркалится
- Эффекты частиц отражаются
- UI элементы поверх персонажа отражаются

### Решение 1: Разделение слоёв

```csharp
// Создано: 2026-04-03

/// <summary>
/// Контроллер для объектов, которые НЕ должны зеркалиться вместе с персонажем.
/// </summary>
public class IndependentScale : MonoBehaviour
{
    private Transform parentTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        // Компенсируем зеркалирование родителя
        if (parentTransform != null)
        {
            float parentScaleX = parentTransform.localScale.x;
            if (parentScaleX < 0)
            {
                // Родитель зеркален — компенсируем
                Vector3 scale = originalScale;
                scale.x *= -1; // Зеркалируем обратно
                transform.localScale = scale;
            }
            else
            {
                transform.localScale = originalScale;
            }
        }
    }
}
```

### Решение 2: SpriteRenderer.flip

Альтернативный подход — не трогать scale, а использовать `SpriteRenderer.flipX`:

```csharp
// Создано: 2026-04-03

/// <summary>
/// Альтернативный контроллер через flipX.
/// Не затрагивает дочерние объекты.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterFlipController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int FacingDirection { get; private set; } = 1;

    public void SetFacingDirection(int direction)
    {
        FacingDirection = direction > 0 ? 1 : -1;
        spriteRenderer.flipX = FacingDirection < 0;
    }
}
```

**Плюсы flipX:**
- Дочерние объекты не затрагиваются
- Проще для простых персонажей

**Минусы flipX:**
- Работает только для SpriteRenderer
- Сложнее для сложной иерархии (несколько спрайтов)

---

## Структура префаба персонажа

```
Player (GameObject)
├── CharacterController2D       # Управление
├── CharacterSpriteController   # Зеркалирование
├── Rigidbody2D
├── Collider2D
│
├── Visuals (GameObject)        # Визуальная часть
│   ├── SpriteRenderer          # Спрайт персонажа
│   └── Animator                # Анимации
│
├── OrbitalWeapons (GameObject) # Орбитальное оружие
│   └── OrbitalWeaponController
│       ├── Weapon1 (IndependentScale)
│       └── Weapon2 (IndependentScale)
│
└── Effects (GameObject)        # Эффекты
    └── ParticleSystems (IndependentScale)
```

---

## Доступные спрайты персонажей

### Игрок (ГГ) — 8 вариантов

| Файл | Описание | Стиль |
|------|----------|-------|
| player_variant1_cultivator.png | Мужчина, бело-синие одежды | Культиватор |
| player_variant2_cultivator.png | Женщина, белые одежды | Культиватор |
| player_variant3_warrior.png | Мужчина, тёмно-зелёные одежды | Воин |
| player_variant4_warrior.png | Женщина, красно-золотые одежды | Воин |
| player_variant5_advanced.png | Мужчина, золотая аура | Продвинутый |
| player_variant6_immortal.png | Бессмертный, радужная аура | Трансцендентный |
| player_variant7_mysterious.png | Капюшон, фиолетовые одежды | Таинственный |
| player_variant8_monk.png | Монах, оранжевые одежды | Монах |

### NPC — 12 вариантов

| Файл | Описание |
|------|----------|
| npc_elder_master.png | Старейшина секты |
| npc_disciple_male.png | Ученик муж. |
| npc_disciple_female.png | Ученик жен. |
| npc_merchant.png | Торговец |
| npc_enemy_demonic.png | Демонический культиватор |
| npc_rival.png | Соперник |
| npc_village_elder.png | Старейшина деревни |
| npc_villager_male.png | Селянин |
| npc_beast_cultivator.png | Зверо-культиватор |
| npc_rogue.png | Бродяга |
| npc_fairy.png | Небесная фея |
| npc_guard.png | Стражник |

---

## Настройка в Unity Editor

### Быстрая настройка

1. **Импортируйте спрайт:**
   - Поместите PNG в `Assets/Sprites/Characters/Player/` или `NPC/`
   - Настройте Texture Type = Sprite (2D and UI)
   - Pixels Per Unit = 100 (или по необходимости)

2. **Создайте GameObject:**
   - Создайте пустой GameObject
   - Добавьте SpriteRenderer и выберите спрайт
   - Добавьте необходимые скрипты

3. **Настройте направление:**
   - Убедитесь, что спрайт изначально смотрит вправо
   - Скрипт автоматически настроит зеркалирование

### Проверка направления

```csharp
// В редакторе — кнопка для тестирования
[ContextMenu("Test Face Left")]
private void TestFaceLeft() => SetFacingDirection(-1);

[ContextMenu("Test Face Right")]
private void TestFaceRight() => SetFacingDirection(1);
```

---

## Советы

1. **Спрайт должен смотреть вправо** — это стандарт для 2D игр
2. **Используйте flipX для простых объектов** — меньше проблем с иерархией
3. **Используйте scale для сложных объектов** — когда нужно зеркалировать всю группу
4. **Добавляйте IndependentScale для независимых элементов** — оружие, эффекты

---

*Документ создан: 2026-04-03*
