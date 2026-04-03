# Чекпоинт: Исправление HUD текста - TextMeshPro

**Дата:** 2026-04-02 09:54:23 UTC
**Фаза:** Bugfix
**Статус:** complete

## Проблема
HUD текст не отображается в GameUI — виден только полупрозрачный фон панели.

**Причина:**
1. TextMeshPro не установлен в Packages/manifest.json
2. Шрифт TMP не импортирован (TMP Essentials)

## Решение

### 1. Добавлен TextMeshPro в зависимости
```json
"com.unity.textmeshpro": "4.0.0"
```

### 2. Обновлена документация
- Добавлена инструкция "Если HUD текст НЕ отображается"
- Описаны шаги для импорта TMP Essentials
- Добавлена информация о структуре HUD

### 3. Инструкция для пользователя
1. **Window → TextMeshPro → Import TMP Essentials**
2. Удалить HUD и пересоздать через Scene Setup Tools
3. Проверить что Font Asset назначен в Inspector

## Выполненные задачи
- [x] Добавлен TextMeshPro в manifest.json
- [x] Обновлена документация с инструкцией TMP
- [x] Добавлено пояснение о структуре HUD

## Изменённые файлы
- Packages/manifest.json
- docs/asset_setup/04_BasicScene_SemiAuto.md

---

*Чекпоинт создан: 2026-04-02 09:54:23 UTC*
