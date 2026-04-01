# 🎮 Cultivation World Simulator

**Версия:** 0.1.0-alpha  
**Unity:** 6000.3 (2D Core с URP)  
**Платформа:** PC (Windows/Mac/Linux)  
**Репозиторий:** [GitHub](https://github.com/vivasua-collab/Ai-game3)

---

## 📖 Описание

Однопользовательская игра-симулятор культивации в жанре Xianxia. Игрок развивает своего персонажа от смертного до могущественного бессмертного.

### Ключевые особенности:
- 🌱 **Развитие смертного** → Пробуждение → Культивация
- 🔥 **10 уровней культивации** с уникальными способностями
- ⚔️ **Боевая система** с многослойным пайплайном урона
- 🧘 **Система Ци** с ядром культивации
- 🦴 **Система тела** Kenshi-style (двойная HP: красная + чёрная)
- 🤖 **NPC AI** с Behaviour Trees
- 🌍 **Живой мир** с течением времени

---

## 📁 Структура проекта

```
CultivationWorldSimulator/
├── Assets/                    # Unity ассеты (корневой проект)
│   ├── Scenes/               # Сцены Unity
│   ├── Settings/             # URP настройки
│   └── ...
├── ProjectSettings/          # Настройки Unity проекта
├── Packages/                 # Unity пакеты
├── UnityProject/             # Основной код игры
│   ├── Assets/
│   │   ├── Scripts/          # C# скрипты
│   │   │   ├── Core/         # Ядро (Constants, Enums, Settings)
│   │   │   ├── Body/         # Система тела
│   │   │   ├── Combat/       # Боевая система
│   │   │   ├── Qi/           # Система Ци
│   │   │   ├── NPC/          # NPC и AI
│   │   │   ├── Player/       # Игрок
│   │   │   ├── Inventory/    # Инвентарь
│   │   │   ├── World/        # Мир и локации
│   │   │   ├── Save/         # Сохранение
│   │   │   ├── UI/           # Интерфейс
│   │   │   ├── Interaction/  # Взаимодействие
│   │   │   ├── Generators/   # Генераторы данных
│   │   │   └── Data/ScriptableObjects/  # SO классы
│   │   └── Data/JSON/        # JSON конфигурации
│   ├── README.md
│   └── SETUP_GUIDE.md
├── docs/                     # Документация
│   ├── ARCHITECTURE.md       # Архитектура проекта
│   ├── BODY_SYSTEM.md        # Система тела
│   ├── COMBAT_SYSTEM.md      # Боевая система
│   ├── QI_SYSTEM.md          # Система Ци
│   ├── implementation_plans/ # Планы внедрения
│   └── ...
├── checkpoints/              # Контрольные точки разработки
├── upload/                   # Временные файлы (архивы)
└── worklog.md               # Лог работы
```

---

## 🚀 Быстрый старт

### 1. Клонировать репозиторий
```bash
git clone https://github.com/vivasua-collab/Ai-game3.git
```

### 2. Открыть в Unity Hub
- Add → выберите папку проекта
- Unity Version: **6000.3** (2D Core с URP)

### 3. Дождаться импорта

---

## ⚠️ Git Workflow (Два ПК)

> **Проект разрабатывается на двух ПК одновременно!**

### Перед началом работы:
```bash
git pull origin main
```

### После работы:
```bash
git add -A
git commit -m "описание изменений"
git pull --rebase
git push
```

📚 **Подробно:** [GIT_WORKFLOW_TWO_PC.md](docs/GIT_WORKFLOW_TWO_PC.md)

---

## 📚 Документация

| Документ | Описание |
|----------|----------|
| [UnityProject/README.md](UnityProject/README.md) | Описание Unity проекта |
| [UnityProject/SETUP_GUIDE.md](UnityProject/SETUP_GUIDE.md) | Настройка проекта |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Архитектура систем |
| [docs/BODY_SYSTEM.md](docs/BODY_SYSTEM.md) | Система тела (Kenshi-style) |
| [docs/COMBAT_SYSTEM.md](docs/COMBAT_SYSTEM.md) | Боевая система |
| [docs/QI_SYSTEM.md](docs/QI_SYSTEM.md) | Система Ци |
| [docs/MORTAL_DEVELOPMENT.md](docs/MORTAL_DEVELOPMENT.md) | Развитие смертных |
| [docs/DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md) | 🎯 План развития проекта |
| [docs/implementation_plans/](docs/implementation_plans/) | Планы внедрения |

---

## 🎯 Этапы разработки

### ✅ Фаза 1: Foundation
- [x] Структура проекта
- [x] Базовые константы и Enums
- [x] ScriptableObject модели
- [x] JSON конфигурации

### ✅ Фаза 2: Combat Core
- [x] Body System (Kenshi-style HP)
- [x] Damage Pipeline (10 слоёв)
- [x] Qi Buffer
- [x] Technique System
- [x] Level Suppression

### ✅ Фаза 3: NPC & Interaction
- [x] NPC AI (Behaviour Tree, Spinal AI)
- [x] Dialogue System
- [x] Relationship System
- [x] Faction System

### ✅ Фаза 4: World & Time
- [x] Time System
- [x] Location System
- [x] Event System
- [x] Generator Registry

### ✅ Фаза 5: UI Enhancement
- [x] Inventory UI (Diablo-style)
- [x] Character Panel
- [x] Combat UI
- [x] HUD

### ✅ Фаза 6: Testing & Balance
- [x] Unit Tests (Combat)
- [x] Integration Tests
- [x] Balance Verification

### ⏳ Фаза 7: Интеграция (Текущая)
- [ ] Создание .asset файлов (Unity Editor)
- [ ] Создание сцен (MainMenu, GameWorld)
- [ ] GameManager интеграция
- [ ] Первичный геймплей

### ⬜ Фаза 8: Контент
- [ ] Техники (30+)
- [ ] Предметы (50+)
- [ ] Враги (10+)
- [ ] Локации

---

## 🔧 Технологии

- **Engine:** Unity 6000.3 (2D URP)
- **Language:** C# (.NET Standard 2.1)
- **Data:** ScriptableObjects + JSON
- **Architecture:** Singleton Managers + Events
- **Save:** JSON сериализация

---

## 👥 Команда

- **AI Agent** — код, архитектура, документация
- **User (ПК 1)** — Unity Editor, ассеты, тестирование
- **User (ПК 2)** — Unity Editor, ассеты, тестирование

---

*Последнее обновление: 2025-04-01*  
*Git branch: main*
