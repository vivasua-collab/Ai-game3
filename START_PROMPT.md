# START_PROMPT.md — Холодный старт ИИ агента

**Проект:** Cultivation World Simulator (Unity 6.3)
**Обращение:** "Мой Господин"
**Режим:** lite (без filler, предложения полные)

---

## СТРУКТУРА ПАПОК

```
/home/z/my-project/
├── UnityProject/              # Unity проект
│   ├── Assets/Scripts/        # C# скрипты
│   ├── Assets/Sprites/        # Спрайты
│   ├── Assets/Scenes/         # Сцены
│   ├── Assets/Data/           # ScriptableObjects, JSON
│   └── Local/                 # ТОЛЬКО ручной перенос (ИИ НЕ ПИСАТЬ)
├── checkpoints/               # Чекпоинты (ММ_ДД_цель.md)
├── docs/                      # Документация
│   ├── asset_setup/           # Инструкции Unity Editor
│   ├── examples/              # Примеры реализаций
│   ├── temp_docs/             # Черновики, временные
│   ├── !LISTING.md            # Список документации
│   ├── !Ai_Skills.md          # AI Skills
│   └── ARCHITECTURE.md        # Архитектура
├── docs_old/                  # Архив
├── Caveman.md                 # Режим коммуникации
└── START_PROMPT.md            # Этот файл
```

---

## ПРАВИЛА РАБОТЫ

### Дата и время
```bash
date '+%Y-%m-%d %H:%M:%S %Z'
```

### Комментарии
```csharp
// Создано: YYYY-MM-DD HH:MM:SS UTC
// Редактировано: YYYY-MM-DD HH:MM:SS UTC
```

### Чекпоинты
**Папка:** `checkpoints/`
**Формат:** `ММ_ДД_цель.md`

### Git push
После длинных задач:
```bash
git add -A && git commit -m "описание" && git push
```

---

## КЛЮЧЕВЫЕ ФАЙЛЫ

| Файл | Назначение |
|------|------------|
| `docs/!LISTING.md` | Список документации |
| `docs/!Ai_Skills.md` | AI Skills |
| `docs/ARCHITECTURE.md` | Архитектура |
| `checkpoints/README.md` | Инструкция чекпоинтов |
| `Caveman.md` | Режим коммуникации |

---

## ЗАПРЕЩЕНО

- Писать в `UnityProject/Local/` — только ручной перенос
- Создавать Assets/ в корне проекта
- .md файлы в UnityProject (кроме README.md)
- Чекпоинты вне `checkpoints/`
- Временная документация вне `docs/temp_docs/`

---

## РЕЖИМЫ КОММУНИКАЦИИ (Caveman.md)

| Уровень | Описание |
|---------|----------|
| lite | Без filler, предложения полные |
| full | Без артиклей, фрагменты OK |
| ultra | Аббревиатуры, стрелки, минимум |

Триггеры: "caveman mode", "less tokens", "be brief"

---

*Создано: 2026-04-02 06:37:09 UTC*
*Редактировано: 2026-04-09 14:30:00 UTC*
