# Чекпоинт: Исправление GameTile CS0115 — подтверждено по документации Unity 6000.3

**Дата:** 2026-04-11 14:12:33 UTC
**Фаза:** 7 — Интеграция
**Статус:** complete

## Контекст
Пользователь указал на необходимость проверки исправления по документации Unity 6000.3.
Предыдущее исправление (ITilemap→Tilemap) было ОШИБОЧНЫМ и не соответствовало официальному API.

## Проверка по официальной документации Unity 6000.3

### Источник
https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.TileBase.GetTileData.html

### Официальная сигнатура метода
```csharp
public void GetTileData(Vector3Int position, Tilemaps.ITilemap tilemap, ref Tilemaps.TileData tileData);
```

### Пример из документации
```csharp
public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
{
    bool evenCell = Mathf.Abs(position.y + position.x) % 2 > 0;
    tileData.sprite = evenCell ? spriteA : spriteB;
}
```

### Ключевые выводы
1. **ITilemap** — правильный тип 2-го параметра (НЕ Tilemap!)
2. **TileData** — `UnityEngine.Tilemaps.TileData` (struct)
3. **Tilemap** может быть неявно преобразован в **ITilemap** (есть оператор)

## КОРНЕВАЯ ПРИЧИНА CS0115

**Конфликт имён TileData**, а НЕ ITilemap vs Tilemap!

Внутри `namespace CultivationGame.TileSystem`:
- `TileData` резолвится в `CultivationGame.TileSystem.TileData` (наш класс данных тайла)
- Вместо `UnityEngine.Tilemaps.TileData` (Unity struct для рендеринга)
- Сигнатура override не совпадает → CS0115 → весь Assembly-CSharp не компилируется
- → Каскад 7 ошибок CS0234/CS0246

## Правильное исправление

```csharp
public override void GetTileData(
    Vector3Int position,
    ITilemap tilemap,                           // ← ITilemap (по документации Unity 6000.3)
    ref UnityEngine.Tilemaps.TileData tileData)  // ← Полная квалификация (конфликт имён)
{
    tileData.sprite = sprite;
    tileData.color = color;
    tileData.flags = UnityEngine.Tilemaps.TileFlags.None;  // ← Тоже полная квалификация
}
```

## Предыдущие ошибочные диагнозы

1. ❌ «ITilemap→Tilemap решает CS0115» — Tilemap НЕ совпадает с базовым методом
2. ❌ «ITilemap вызывает CS0115» — ITilemap — правильный тип для Unity 6000.3
3. ✅ Реальная причина: `TileData` без квалификации резолвится в наш класс, а не в Unity struct

## Аналогичные уже решённые конфликты имён

| Конфликт | Решение |
|----------|---------|
| TileData (наш vs Unity) | Полная квалификация `UnityEngine.Tilemaps.TileData` |
| TileFlags (наш vs Unity) | Переименован в `GameTileFlags` |
| DamageType (Core vs TileSystem) | Переименован в `TileDamageType` |
| TerrainType (Core vs TileSystem) | Переименован в `BiomeType` |

## Изменённые файлы
- Assets/Scripts/Tile/GameTile.cs — ITilemap + UnityEngine.Tilemaps.TileData

## Коммит
- `9eb0999` — FIX CS0115: Восстановлен ITilemap + полная квалификация TileData
