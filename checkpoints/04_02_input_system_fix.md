# Чекпоинт: Исправление Input System

**Дата:** 2026-04-02 07:01:13 UTC
**Фаза:** Bugfix
**Статус:** complete

## Проблема
```
InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, 
but you have switched active Input handling to Input System package in Player Settings.
```

**Причина:** EventSystem использовал `StandaloneInputModule` (старый Input), а проект настроен на Input System Package.

## Решение

### 1. Обновлён SceneSetupTools.cs
- Добавлен `using UnityEngine.InputSystem.UI;`
- Заменён `StandaloneInputModule` на `InputSystemUIInputModule`

**Было:**
```csharp
eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
```

**Стало:**
```csharp
eventSystemGO.AddComponent<InputSystemUIInputModule>();
```

### 2. Обновлена документация
- Добавлен раздел "Ошибка Input System" в `04_BasicScene_SemiAuto.md`
- Описано решение для пользователя

## Выполненные задачи
- [x] Исправлен SceneSetupTools.cs
- [x] Обновлена документация
- [x] Добавлены комментарии даты редактирования

## Изменённые файлы
- UnityProject/Assets/Scripts/Editor/SceneSetupTools.cs
- docs/asset_setup/04_BasicScene_SemiAuto.md

## Инструкция для пользователя

1. Удали объект **EventSystem** в Hierarchy
2. Запусти **Window → Scene Setup Tools** → **Create GameUI Canvas**
3. Скрипт создаст EventSystem с правильным `InputSystemUIInputModule`

---

*Чекпоинт создан: 2026-04-02 07:01:13 UTC*
