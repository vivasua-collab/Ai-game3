# Audit: Data + Tile + UI + Generators + Editor + Charger + Tests + Character + Examples
**Date:** 2026-04-10
**Auditor:** Main Agent (direct audit)
**Status:** Complete

## Critical Issues

### DC-01: ChargerHeat, ChargerBuffer, ChargerSlot, QiStone — [SerializeField] без MonoBehaviour
**Файлы:** `Charger/ChargerHeat.cs`, `Charger/ChargerBuffer.cs`, `Charger/ChargerSlot.cs`, `Charger/QiStone` (в ChargerSlot.cs)
**Проблема:** Те же проблемы что и C-07 (StatDevelopment). Все 4 класса — обычные C# классы (не MonoBehaviour/ScriptableObject), но используют [SerializeField]. Поля НЕ будут сериализованы Unity Inspector. ChargerBuffer и ChargerHeat имеют [Header] атрибуты, которые также работают только в MonoBehaviour.
**Исправление:** Убрать [SerializeField] и [Header], использовать обычные приватные поля с публичными свойствами. Или сделать классы ScriptableObject.

### DC-02: ChargerController.UseQiForTechnique — int вместо long для Qi
**Файл:** `Charger/ChargerController.cs:326-334`
**Код:**
```csharp
public bool UseQiForTechnique(int qiCost)
{
    int practitionerCurrentQi = practitionerQi != null ? (int)practitionerQi.CurrentQi : 0;
```
**Проблема:** (int) приведение long CurrentQi к int — та же проблема что C-05. ChargerBuffer.UseQiForTechnique и CanUseTechnique тоже принимают int. Для высокоуровневых практиков Qi > int.MaxValue, зарядник будет работать некорректно.
**Исправление:** Изменить все Qi-параметры на long.

## High Issues

### DH-01: UIManager — GameState.Combat используется для экрана персонажа, Cutscene для карты
**Файл:** `UI/UIManager.cs:349,372-373`
**Код:**
```csharp
public void OpenCharacter() { SetState(GameState.Combat); } // Используем Combat как экран персонажа
public void ToggleMap() { ... SetState(GameState.Cutscene); } // Используем как карта
```
**Проблема:** GameState.Combat и GameState.Cutscene используются для других целей (Character panel и Map). Это нарушает семантику enum и вызывает путаницу. При проверке `if (currentState == GameState.Combat)` нельзя понять — это бой или экран персонажа.
**Исправление:** Добавить GameState.CharacterPanel и GameState.Map в enum.

### DH-02: CombatUI.Camera.main вызывается при каждом числе урона
**Файл:** `UI/CombatUI.cs:629,653`
**Код:** `var screenPos = Camera.main.WorldToScreenPoint(worldPosition);`
**Проблема:** Camera.main — дорогой вызов (FindWithTag), вызывается при каждом числе урона. В интенсивном бою — каждый кадр.
**Исправление:** Кэшировать ссылку на камеру.

## Medium Issues

### DM-01: ChargerController не отписывается от buffer.OnBufferChanged (lambda leak)
**Файл:** `Charger/ChargerController.cs:118`
**Код:** `buffer.OnBufferChanged += (current, capacity) => OnBufferChanged?.Invoke(current, capacity);`
**Проблема:** Lambda подписка — нельзя отписать в OnDestroy.

### DM-02: CombatUI.ProgressBar — класс наследует MonoBehaviour, но объявлен как [Serializable]
**Файл:** `UI/CombatUI.cs:798`
**Код:** `public class ProgressBar : MonoBehaviour`
**Проблема:** MonoBehaviour не должен быть [Serializable] — он уже сериализуем Unity. Компонент объявлен внутри файла CombatUI.cs, что нарушает Unity конвенцию (один класс = один файл).

### DM-03: UIManager использует старый Input API
**Файл:** `UI/UIManager.cs:259-280`
**Проблема:** Input.GetKeyDown(KeyCode.Escape/I/C/M) — старый Input API. Аналогично M-01 (GameManager).

## Low Issues

### DL-01: ChargerController.Debug.Log в production
**Файлы:** ChargerController.cs (10+ вызовов Debug.Log)
**Проблема:** Спам в консоль. Аналогично L-03.

### DL-02: CombatUI.AttackResult enum collision
**Файл:** `UI/CombatUI.cs:504`
**Проблема:** CombatUI.LogAttack использует AttackResult — может быть конфликт между struct и enum (уже описано в H-NEW-08).

### DL-03: Файлы не были глубоко проанализированы
Из-за нестабильности работы, следующие файлы не были прочитаны:
- Все 12 ScriptableObjects в Data/ScriptableObjects/
- Все 11 файлов в Generators/ (включая Naming/)
- Все 5 файлов в Editor/
- Все 4 файла в Tests/
- Остальные UI файлы (InventoryUI, CultivationProgressBar, WeaponDirectionIndicator, HUDController, MenuUI, CharacterPanelUI, DialogUI)
- Character/ файлы
- Tile/ файлы
- Examples/NPCAssemblyExample.cs
- Data/TerrainConfig.cs

## Files Reviewed

| File | Status | Notes |
|------|--------|-------|
| Charger/ChargerController.cs | ⚠️ | DC-01, DC-02, DM-01, DL-01 |
| Charger/ChargerData.cs | ✅ | Чистый ScriptableObject |
| Charger/ChargerHeat.cs | ⚠️ | DC-01 ([SerializeField] без MonoBehaviour) |
| Charger/ChargerBuffer.cs | ⚠️ | DC-01, DC-02 (int Qi) |
| Charger/ChargerSlot.cs | ⚠️ | DC-01 |
| UI/UIManager.cs | ⚠️ | DH-01, DM-03 |
| UI/CombatUI.cs | ⚠️ | DH-02, DM-02, DL-02 |
