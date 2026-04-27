# 🛠️ Unity Setup Guide — Cultivation World Simulator

**Версия:** 1.1
**Дата:** 2026-04-27 (обновлено)
**Unity:** 6000.3 (6000.3.0f1)

---

## 📋 Чек-лист перед началом

- [ ] Unity Hub установлен
- [ ] Unity 6000.3 установлен (2D Core + URP)
- [ ] Git установлен
- [ ] Репозиторий клонирован

---

## 🚀 Шаг 1: Создание проекта

### 1.1 Открыть Unity Hub

1. Запустите Unity Hub
2. Нажмите **"New Project"**

### 1.2 Выбрать шаблон

1. Выберите категорию **"Core"**
2. Выберите шаблон **"2D Core"** или **"2D (URP)"**
3. Если нет URP шаблона — выберите "2D Core", URP добавим позже

### 1.3 Настроить проект

| Параметр | Значение |
|----------|----------|
| Project Name | `CultivationWorldSimulator` |
| Location | Папка с репозиторием |

**⚠️ Важно:** НЕ создавайте проект внутри папки `UnityProject/`. Создайте рядом и скопируйте содержимое.

### 1.4 Альтернатива: Клонирование структуры

1. Создайте пустой проект Unity
2. Закройте Unity
3. Скопируйте содержимое `UnityProject/Assets/` в ваш проект
4. Откройте проект в Unity

---

## 📁 Шаг 2: Импорт структуры

### 2.1 Копировать скрипты

```
Из: UnityProject/Assets/Scripts/
В:  YourProject/Assets/Scripts/
```

### 2.2 Копировать данные

```
Из: UnityProject/Assets/Data/
В:  YourProject/Assets/Data/
```

### 2.3 Проверить компиляцию

1. Откройте Unity
2. Дождитесь окончания компиляции
3. Проверьте Console на ошибки

**Если есть ошибки:**
- Проверьте версию Unity (должна быть 6000.3)
- Убедитесь, что все файлы скопированы

---

## ⚙️ Шаг 3: Настройка Project Settings

### 3.1 Player Settings

Откройте: `Edit → Project Settings → Player`

| Настройка | Значение |
|-----------|----------|
| Company Name | `YourStudio` |
| Product Name | `Cultivation World Simulator` |
| Version | `0.1.0` |
| Default Screen Width | `1920` |
| Default Screen Height | `1080` |
| Fullscreen Mode | `Windowed` |
| Run In Background | `✓` |

### 3.2 Input System (опционально)

Если используете новый Input System:

1. `Window → Package Manager`
2. Найдите "Input System"
3. Установите
4. `Edit → Project Settings → Player → Active Input Handling`
5. Выберите "Both" или "Input System Package (New)"

### 3.3 URP (если не был выбран)

1. `Window → Package Manager`
2. Найдите "Universal RP"
3. Установите
4. `Assets → Create → Rendering → URP Asset`
5. Назовите его "URPSettings"
6. `Edit → Project Settings → Graphics`
7. Назначьте URPSettings как Scriptable Render Pipeline Settings

---

## 📦 Шаг 4: Создание ScriptableObjects

### 4.1 Cultivation Levels

1. `Assets → Create → Cultivation → Cultivation Level Data`
2. Назовите `CultivationLevel_01`
3. Заполните по таблице:

| Поле | Значение |
|------|----------|
| Level | 1 |
| Name Ru | Пробуждённое Ядро |
| Name En | Awakened Core |
| Qi Density | 1 |
| Aging Multiplier | 1.0 |
| Regeneration Multiplier | 1.1 |
| ~~Conductivity Multiplier~~ | ❌ Удалено — проводимость = `coreCapacity / 360` (производная) |

4. Повторите для всех 9 уровней (10 = Вознесение, конец игры)

### 4.2 Elements

1. `Assets → Create → Cultivation → Element Data`
2. Создайте 8 элементов:
   - Fire (Огонь)
   - Water (Вода)
   - Earth (Земля)
   - Air (Воздух)
   - Lightning (Молния)
   - Void (Пустота)
   - Light (Свет)
   - Neutral (Нейтральный)

### 4.3 Technique Types

1. `Assets → Create → Cultivation → Technique Type Data`
2. Создайте типы:
   - Combat
   - Cultivation
   - Defense
   - Support
   - Healing
   - Movement
   - Sensory
   - Curse
   - Poison
   - ~~Formation~~ ❌ Формации — отдельная система, см. [FORMATION_SYSTEM.md](./FORMATION_SYSTEM.md)

### 4.4 Materials

1. `Assets → Create → Cultivation → Material Data`
2. Создайте материалы по тиру:

**Tier 1:**
- Iron (Железо)
- Leather (Кожа)
- Cloth (Ткань)
- Wood (Дерево)
- Bone (Кость)

**Tier 2:**
- Steel (Сталь)
- Silk (Шёлк)
- Silver (Серебро)

**Tier 3:**
- Spirit Iron (Духовное железо)
- Jade (Нефрит)
- Cold Iron (Холодное железо)

**Tier 4:**
- Star Metal (Звёздный металл)
- Dragon Bone (Кость дракона)
- Elemental Core (Элементальное ядро)

**Tier 5:**
- Void Matter (Материя пустоты)
- Chaos Matter (Материя хаоса)
- Primordial Essence (Первородная эссенция)

---

## 🎨 Шаг 5: Создание базовых префабов

### 5.1 Player Prefab

1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите "Player"
3. Добавьте компоненты:
   - `Sprite Renderer`
   - `Rigidbody2D`
   - `Box Collider2D`
   - Скрипты:
     - `BodyController`
     - `QiController`
     - `EquipmentController`
4. Перетащите в `Assets/Prefabs/Player/`

### 5.2 NPC Prefab

1. Создайте пустой GameObject
2. Назовите "NPC_Base"
3. Добавьте компоненты:
   - `Sprite Renderer`
   - `Rigidbody2D`
   - `Capsule Collider2D`
   - `NavMeshAgent` (если используете NavMesh)
   - Скрипты:
     - `NPCController`
     - `NPCAI`
     - `BodyController`
4. Перетащите в `Assets/Prefabs/NPC/`

---

## 🖼️ Шаг 6: Импорт графики

### 6.1 Спрайты персонажей

1. Скопируйте спрайты в `Assets/Art/Sprites/Characters/`
2. Выберите все спрайты
3. В Inspector настройте:
   - Texture Type: `Sprite (2D and UI)`
   - Pixels Per Unit: `32` или `64` (зависит от стиля)
   - Filter Mode: `Point (no filter)`
   - Compression: `None`

### 6.2 UI элементы

1. Скопируйте UI графику в `Assets/Art/UI/`
2. Настройте как спрайты выше

---

## 🔊 Шаг 7: Импорт аудио

### 7.1 Музыка

1. Скопируйте в `Assets/Audio/Music/`
2. В Inspector настройте:
   - Load Type: `Compressed In Memory`
   - Compression Format: `Vorbis`
   - Quality: `70-100%`

### 7.2 Звуковые эффекты

1. Скопируйте в `Assets/Audio/SFX/`
2. В Inspector настройте:
   - Load Type: `Decompress On Load` (для коротких)
   - Compression Format: `ADPCM` или `Vorbis`

---

## 🧪 Шаг 8: Первая сцена

### 8.1 Создать сцену MainMenu

1. `File → New Scene`
2. Сохраните как `Assets/Scenes/MainMenu.unity`
3. Добавьте UI:
   - Canvas
   - Panel с кнопками:
     - "New Game"
     - "Continue"
     - "Settings"
     - "Quit"

### 8.2 Создать сцену GameWorld

1. `File → New Scene`
2. Сохраните как `Assets/Scenes/GameWorld.unity`
3. Добавьте:
   - GameObject "GameManager" со скриптом `GameManager`
   - GameObject "UIManager" со скриптом `UIManager`
   - GameObject "Player" (из префаба)

---

## ✅ Шаг 9: Проверка

### 9.1 Компиляция

1. Откройте `Console` (`Window → General → Console`)
2. Нажмите `Clear`
3. `Assets → Refresh`
4. Проверьте на ошибки

### 9.2 Play Mode

1. Откройте сцену `MainMenu`
2. Нажмите `Play`
3. Проверьте:
   - Сцена загружается
   - UI отображается корректно
   - Нет ошибок в Console

---

## 🐛 Устранение проблем

### Ошибка: "The type or namespace name 'UnityEngine' could not be found"

**Решение:**
- Убедитесь, что открываете проект через Unity Hub
- Файлы .cs должны быть внутри Assets/ папки

### Ошибка: "Script class does not match file name"

**Решение:**
- Имя файла .cs должно совпадать с именем класса
- Пример: `BodyController.cs` → `public class BodyController`

### Ошибка: "Prefab missing script"

**Решение:**
- Скрипт должен скомпилироваться до создания префаба
- Перетащите скрипт на префаб заново

### Предупреждение: "ScriptableObject not found"

**Решение:**
- Создайте .asset файл через меню Create
- Убедитесь, что скрипт SO класса компилируется

---

## 📚 Следующие шаги

После завершения настройки:

1. ✅ Проект создан и настроен
2. ✅ Скрипты компилируются
3. ✅ ScriptableObjects созданы
4. ✅ Базовые префабы готовы
5. ✅ Сцены созданы

**Готовы к Фазе 2: Combat Core!**

---

## 📞 Поддержка

При возникновении проблем:
1. Проверьте Console на ошибки
2. Убедитесь, что версия Unity соответствует
3. Обратитесь к AI-ассистенту с описанием проблемы

---

*Документ создан: 2026-03-30*
*Для проекта: Cultivation World Simulator*
