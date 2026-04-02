# Настройка базовой сцены (Полуавтомат)

**Инструмент:** `Window → Scene Setup Tools`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папок | ❌ Руками |
| Создание файла сцены (.unity) | ❌ Руками |
| Создание GameObject GameManager | ✅ Автоматически |
| Добавление компонента GameManager | ✅ Автоматически |
| Создание дочернего объекта Systems | ✅ Автоматически |
| Добавление всех контроллеров | ✅ Автоматически |
| Настройка TimeController | ✅ Автоматически |
| Настройка SaveManager | ✅ Автоматически |
| Создание Canvas (GameUI) | ✅ Автоматически |
| Создание EventSystem | ✅ Автоматически |
| Создание HUD Panel | ✅ Автоматически |
| Создание текстовых элементов | ✅ Автоматически |

---

## Шаг 1: Создание папок (ВРУКАМИ)

Перед началом создай структуру папок в Project окне:

```
Assets/
├── Scenes/
├── Prefabs/
│   └── Player/
└── Data/
    ├── JSON/
    ├── CultivationLevels/
    ├── Elements/
    └── MortalStages/
```

**Действия:**
1. Правый клик на `Assets` в Project окне
2. **Create → Folder** → назови папку
3. Повтори для каждой папки

---

## Шаг 2: Создание сцены (ВРУКАМИ)

**Файл сцены (.unity) нельзя создать скриптом!**

**Действия:**
1. В Project найди папку `Assets/Scenes/`
2. Правый клик → **Create → Scene**
3. Назови файл `Main`
4. Двойной клик — открой сцену

---

## Шаг 3: Запуск Scene Setup Tools (АВТОМАТИЧЕСКИ)

**Действия:**
1. Открой меню: **Window → Scene Setup Tools**
2. Нажми кнопку **"SETUP ALL (Full Scene)"**
3. Дождись сообщений в Console

**Результат в Hierarchy:**
```
Main (сцена)
├── Main Camera          ← был изначально
├── Directional Light    ← был изначально
├── GameManager          ← создан скриптом
│   └── Systems          ← ОДИН объект, все контроллеры как КОМПОНЕНТЫ
├── Player               ← создан скриптом
├── EventSystem          ← создан скриптом
└── GameUI (Canvas)      ← создан скриптом
    └── HUD
        ├── TimeText
        ├── HPText
        └── QiText
```

> ⚠️ **Важно:** Объект `Systems` — это **один GameObject**, а контроллеры (WorldController, TimeController и др.) добавлены как **компоненты** в Inspector, а не как дочерние объекты!
> 
> **Выбери `Systems` в Hierarchy → смотри Inspector → увидишь все контроллеры как компоненты:**
> - WorldController (Script)
> - TimeController (Script)
> - LocationController (Script)
> - EventController (Script)
> - FactionController (Script)
> - GeneratorRegistry (Script)
> - SaveManager (Script)

---

## Шаг 4: Project Settings (ВРУКАМИ)

### 4.1 Теги:

**Edit → Project Settings → Tags and Layers → Tags**

Добавь теги:
```
Player
NPC
Interactable
Item
Enemy
```

### 4.2 Слои:

**Edit → Project Settings → Tags and Layers → Layers**

Добавь слои (начиная с User Layer 6):
```
Layer 6: Player
Layer 7: NPC
Layer 8: Interactable
Layer 9: Item
Layer 10: Enemy
Layer 11: UI
Layer 12: Background
```

### 4.3 Input System (если используется):

**Edit → Project Settings → Input System**

Или используй старый Input Manager:
**Edit → Project Settings → Input Manager**

Добавь оси:
- Horizontal (A/D, Left/Right arrows)
- Vertical (W/S, Up/Down arrows)
- Submit (Enter, Space)
- Cancel (Escape)

---

## Шаг 5: Настройка Camera (ВРУКАМИ)

Выдели `Main Camera` в Hierarchy:

```
Camera:
├── Clear Flags: Solid Color
├── Background: Чёрный (R=0, G=0, B=0)
├── Projection: Orthographic
├── Size: 5
└── Z Position: -10
```

---

## Шаг 6: Настройка Lighting (ВРУКАМИ)

Выдели `Directional Light`:

```
Light:
├── Intensity: 1
├── Color: Белый
└── Rotation: X=50, Y=-30, Z=0
```

---

## Шаг 7: Сохранение сцены (ВРУКАМИ)

1. **Ctrl + S** или **File → Save**
2. Сцена сохранится в `Assets/Scenes/Main.unity`

---

## Шаг 8: Проверка

### Нажми Play:

**Console должна показать:**
```
[GameManager] Initializing game...
World initialized: Cultivation World (Seed: 12345)
Player initialized: Игрок
```

**HUD должен отображать:**
- Время: "День 1 - 06:00"
- HP: "HP: 100%"
- Ци: "Ци: 0/100"

### Типичные ошибки:

| Ошибка | Причина | Решение |
|--------|---------|---------|
| "Can't add component" | Скрипт не скомпилировался | Проверь Console на ошибки C# |
| "Missing component" | Тип компонента не найден | Проверь что скрипт существует |
| Canvas не виден | Нет EventSystem | Скрипт создаст автоматически |
| Текст не отображается | Нет TextMeshPro | Import TextMeshPro package |
| **InvalidOperationException: Input** | EventSystem использует старый Input | Удали EventSystem и пересоздай через Setup Tools |

### ⚠️ Ошибка Input System (Input System Package):

Если видишь ошибку:
```
InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, 
but you have switched active Input handling to Input System package in Player Settings.
```

**Решение:**
1. Удали объект **EventSystem** в Hierarchy
2. Запусти **Window → Scene Setup Tools** → **Create GameUI Canvas**
3. Скрипт создаст EventSystem с правильным `InputSystemUIInputModule`

Или вручную:
1. Выбери EventSystem в Hierarchy
2. Удали компонент `StandaloneInputModule`
3. Добавь компонент `Input System UI Input Module`

---

## Шпаргалка: Что руками, что скриптом

| Задача | Способ |
|--------|--------|
| Создать папки | ✋ Руками |
| Создать сцену | ✋ Руками |
| Настроить Project Settings | ✋ Руками |
| Настроить Camera | ✋ Руками |
| Настроить Lighting | ✋ Руками |
| Создать GameManager | 🤖 Скрипт |
| Создать Systems | 🤖 Скрипт |
| Добавить контроллеры | 🤖 Скрипт |
| Создать Canvas | 🤖 Скрипт |
| Создать HUD | 🤖 Скрипт |

---

*Документ создан: 2025-04-01*  
*Редактировано: 2026-04-02 09:46:31 UTC*  
*Исправлено: Input System UI Module + уточнена структура Systems (компоненты, не дочерние объекты)*
