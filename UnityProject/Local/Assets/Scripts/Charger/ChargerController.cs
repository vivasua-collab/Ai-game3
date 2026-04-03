// ============================================================================
// ChargerController.cs — Главный контроллер зарядника Ци
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 08:35:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Charger
{
    /// <summary>
    /// Главный контроллер зарядника Ци.
    /// Управляет слотами камней, буфером, тепловым балансом и интеграцией с практиком.
    /// 
    /// Источник: CHARGER_SYSTEM.md
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     ПРИНЦИП РАБОТЫ ЗАРЯДНИКА                            │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │    ┌─────────────┐      ┌─────────────┐      ┌─────────────┐            │
    /// │    │  КАМЕНЬ Ци  │ ──→  │  ЗАРЯДНИК   │ ──→  │  ПРАКТИК    │            │
    /// │    │  (источник) │      │  (буфер)    │      │  (приёмник) │            │
    /// │    └─────────────┘      └─────────────┘      └─────────────┘            │
    /// │          │                    │                    │                    │
    /// │          └──── 50-200 ед/сек ─┴── 5-50 ед/сек ────┘                    │
    /// │                               ↓                                          │
    /// │                        ограничивающий фактор                             │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class ChargerController : MonoBehaviour
    {
        // === Configuration ===
        
        [Header("Charger Data")]
        [SerializeField] private ChargerData chargerData;
        
        [Header("Settings")]
        [SerializeField] private ChargerMode mode = ChargerMode.Off;
        [SerializeField] private bool autoActivateInCombat = true;
        
        // === Runtime Components ===
        
        private List<ChargerSlot> slots = new List<ChargerSlot>();
        private ChargerBuffer buffer;
        private ChargerHeat heat;
        
        // === State ===
        
        private bool isInitialized = false;
        private bool isInCombat = false;
        private QiController practitionerQi; // Ссылка на Ци практика
        
        // === Properties ===
        
        public ChargerData Data => chargerData;
        public ChargerMode Mode => mode;
        public ChargerBuffer Buffer => buffer;
        public ChargerHeat Heat => heat;
        public List<ChargerSlot> Slots => slots;
        public bool IsOperational => heat != null && heat.CanOperate() && mode == ChargerMode.On;
        public bool IsOverheated => heat != null && heat.IsOverheated;
        public bool HasStones => GetTotalStonesQi() > 0;
        public int ActiveSlotsCount => slots.FindAll(s => s.HasStone).Count;
        
        // === Events ===
        
        public event Action OnChargerActivated;
        public event Action OnChargerDeactivated;
        public event Action OnChargerOverheated;
        public event Action<int, int> OnBufferChanged; // (current, capacity)
        public event Action OnStoneInserted;
        public event Action OnStoneRemoved;
        public event Action OnStonesDepleted;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Обновляем тепло
            heat.DissipateHeat(Time.deltaTime);
            
            // Если зарядник активен — работаем
            if (mode == ChargerMode.On && IsOperational)
            {
                ProcessChargerOperation(Time.deltaTime);
            }
        }
        
        // === Initialization ===
        
        /// <summary>
        /// Инициализировать зарядник.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            // Создаём компоненты
            buffer = new ChargerBuffer();
            heat = new ChargerHeat();
            
            // Подписываемся на события
            heat.OnOverheated += HandleOverheated;
            heat.OnCooldownComplete += HandleCooldownComplete;
            buffer.OnBufferChanged += (current, capacity) => OnBufferChanged?.Invoke(current, capacity);
            
            // Загружаем данные если есть
            if (chargerData != null)
            {
                LoadFromData(chargerData);
            }
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Загрузить конфигурацию из ChargerData.
        /// </summary>
        public void LoadFromData(ChargerData data)
        {
            chargerData = data;
            
            // Настраиваем слоты
            slots.Clear();
            foreach (var slotData in data.Slots)
            {
                ChargerSlot slot = new ChargerSlot(slotData.index);
                slots.Add(slot);
            }
            
            // Настраиваем буфер
            var bufferData = data.Buffer;
            buffer.Configure(
                bufferData.capacity,
                bufferData.conductivity,
                0.1f // 10% потери
            );
            
            Debug.Log($"[Charger] Загружен: {data.ChargerName} | Слотов: {slots.Count} | Буфер: {buffer.Capacity}");
        }
        
        /// <summary>
        /// Привязать к практику.
        /// </summary>
        public void BindToPractitioner(QiController qiController)
        {
            practitionerQi = qiController;
        }
        
        // === Mode Control ===
        
        /// <summary>
        /// Активировать зарядник.
        /// </summary>
        public void Activate()
        {
            if (mode == ChargerMode.On) return;
            
            mode = ChargerMode.On;
            OnChargerActivated?.Invoke();
            
            Debug.Log("[Charger] Активирован");
        }
        
        /// <summary>
        /// Деактивировать зарядник.
        /// </summary>
        public void Deactivate()
        {
            if (mode == ChargerMode.Off) return;
            
            mode = ChargerMode.Off;
            OnChargerDeactivated?.Invoke();
            
            Debug.Log("[Charger] Деактивирован");
        }
        
        /// <summary>
        /// Переключить режим.
        /// </summary>
        public void ToggleMode()
        {
            if (mode == ChargerMode.On)
                Deactivate();
            else
                Activate();
        }
        
        // === Combat Integration ===
        
        /// <summary>
        /// Войти в боевой режим.
        /// </summary>
        public void EnterCombat()
        {
            isInCombat = true;
            heat.EnterCombat();
            
            if (autoActivateInCombat && mode == ChargerMode.Off)
            {
                Activate();
            }
        }
        
        /// <summary>
        /// Выйти из боевого режима.
        /// </summary>
        public void ExitCombat()
        {
            isInCombat = false;
            heat.ExitCombat();
        }
        
        // === Stone Management ===
        
        /// <summary>
        /// Вставить камень в слот.
        /// </summary>
        /// <param name="stone">Камень Ци</param>
        /// <param name="slotIndex">Индекс слота (-1 = первый свободный)</param>
        /// <returns>True если успешно</returns>
        public bool InsertStone(QiStone stone, int slotIndex = -1)
        {
            // Найти слот
            ChargerSlot targetSlot = null;
            
            if (slotIndex >= 0 && slotIndex < slots.Count)
            {
                targetSlot = slots[slotIndex];
            }
            else
            {
                // Первый свободный слот
                targetSlot = slots.Find(s => s.CanInsert && s.CanAcceptStone(stone));
            }
            
            if (targetSlot == null)
            {
                Debug.LogWarning("[Charger] Нет подходящего слота для камня");
                return false;
            }
            
            if (!targetSlot.CanAcceptStone(stone))
            {
                Debug.LogWarning($"[Charger] Камень не подходит для слота {targetSlot.SlotIndex}");
                return false;
            }
            
            if (targetSlot.InsertStone(stone))
            {
                OnStoneInserted?.Invoke();
                Debug.Log($"[Charger] Камень вставлен в слот {targetSlot.SlotIndex}: {stone.StoneName}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Извлечь камень из слота.
        /// </summary>
        /// <param name="slotIndex">Индекс слота</param>
        /// <returns>Извлечённый камень или null</returns>
        public QiStone RemoveStone(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return null;
            
            QiStone stone = slots[slotIndex].RemoveStone();
            
            if (stone != null)
            {
                OnStoneRemoved?.Invoke();
                Debug.Log($"[Charger] Камень извлечён из слота {slotIndex}: {stone.StoneName}");
            }
            
            return stone;
        }
        
        /// <summary>
        /// Извлечь все камни.
        /// </summary>
        /// <returns>Список извлечённых камней</returns>
        public List<QiStone> RemoveAllStones()
        {
            List<QiStone> stones = new List<QiStone>();
            
            foreach (var slot in slots)
            {
                QiStone stone = slot.RemoveStone();
                if (stone != null)
                {
                    stones.Add(stone);
                }
            }
            
            if (stones.Count > 0)
            {
                OnStoneRemoved?.Invoke();
                Debug.Log($"[Charger] Извлечено {stones.Count} камней");
            }
            
            return stones;
        }
        
        // === Qi Operations ===
        
        /// <summary>
        /// Использовать Ци для техники.
        /// Порядок: ядро практика → буфер зарядника.
        /// </summary>
        /// <param name="qiCost">Стоимость техники</param>
        /// <returns>True если Ци достаточно</returns>
        public bool UseQiForTechnique(int qiCost)
        {
            if (IsOverheated)
            {
                Debug.LogWarning("[Charger] Зарядник перегрет!");
                return false;
            }
            
            int practitionerCurrentQi = practitionerQi != null ? (int)practitionerQi.CurrentQi : 0;
            
            // Проверяем возможность
            if (!buffer.CanUseTechnique(qiCost, practitionerCurrentQi))
            {
                Debug.LogWarning($"[Charger] Недостаточно Ци: нужно {qiCost}, доступно {buffer.GetEffectiveQiAvailable(practitionerCurrentQi)}");
                return false;
            }
            
            // Используем Ци
            ChargerBufferResult result = buffer.UseQiForTechnique(qiCost, practitionerCurrentQi);
            
            // Тратим из ядра практика
            if (result.QiFromCore > 0 && practitionerQi != null)
            {
                practitionerQi.SpendQi(result.QiFromCore);
            }
            
            // Добавляем тепло от использования буфера
            if (result.QiFromBuffer > 0)
            {
                heat.AddHeatFromQi(result.QiFromBuffer);
            }
            
            Debug.Log($"[Charger] Техника: {qiCost} Ци | Ядро: {result.QiFromCore} | Буфер: {result.QiFromBuffer} | Потери: {result.QiLost}");
            
            return true;
        }
        
        /// <summary>
        /// Проверить возможность использования техники.
        /// </summary>
        public bool CanUseTechnique(int qiCost)
        {
            if (IsOverheated) return false;
            
            int practitionerCurrentQi = practitionerQi != null ? (int)practitionerQi.CurrentQi : 0;
            return buffer.CanUseTechnique(qiCost, practitionerCurrentQi);
        }
        
        /// <summary>
        /// Получить доступное Ци (ядро + буфер с учётом потерь).
        /// </summary>
        public int GetAvailableQi()
        {
            int practitionerCurrentQi = practitionerQi != null ? (int)practitionerQi.CurrentQi : 0;
            return buffer.GetEffectiveQiAvailable(practitionerCurrentQi);
        }
        
        // === Internal Processing ===
        
        /// <summary>
        /// Обработка работы зарядника (вызывается каждый кадр).
        /// </summary>
        private void ProcessChargerOperation(float deltaTime)
        {
            // 1. Извлекаем Ци из камней
            float totalStoneRate = CalculateTotalStoneRate();
            
            if (totalStoneRate > 0 && !buffer.IsFull)
            {
                // Ограничено проводимостью зарядника
                int added = buffer.AccumulateFromStones(totalStoneRate, deltaTime);
                
                if (added > 0)
                {
                    // Добавляем тепло от накопления
                    heat.AddHeat(added * 0.01f); // Меньше тепла при накоплении
                }
            }
            
            // 2. Передаём Ци практику (если не в бою)
            if (!isInCombat && practitionerQi != null && !buffer.IsEmpty)
            {
                int transferred = buffer.TransferToPractitioner(practitionerQi.Conductivity, deltaTime);
                
                if (transferred > 0)
                {
                    practitionerQi.AddQi(transferred);
                }
            }
            
            // 3. Проверяем пустые камни
            CheckDepletedStones();
        }
        
        /// <summary>
        /// Рассчитать суммарную скорость камней.
        /// </summary>
        private float CalculateTotalStoneRate()
        {
            float total = 0f;
            
            foreach (var slot in slots)
            {
                if (slot.HasStone)
                {
                    // Скорость камня, ограниченная проводимостью зарядника
                    float stoneRate = slot.InsertedStone.GetEffectiveReleaseRate(buffer.Conductivity);
                    stoneRate *= (1f + slot.AbsorptionBonus);
                    total += stoneRate;
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Получить общее количество Ци в камнях.
        /// </summary>
        public long GetTotalStonesQi()
        {
            long total = 0;
            
            foreach (var slot in slots)
            {
                if (slot.HasStone)
                {
                    total += slot.InsertedStone.CurrentQi;
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Проверить и удалить пустые камни.
        /// </summary>
        private void CheckDepletedStones()
        {
            bool anyDepleted = false;
            
            foreach (var slot in slots)
            {
                if (slot.HasStone && slot.InsertedStone.IsEmpty)
                {
                    Debug.Log($"[Charger] Камень в слоте {slot.SlotIndex} истощён");
                    slot.RemoveStone();
                    anyDepleted = true;
                }
            }
            
            if (anyDepleted)
            {
                OnStonesDepleted?.Invoke();
            }
        }
        
        // === Event Handlers ===
        
        private void HandleOverheated()
        {
            Debug.LogWarning("[Charger] ПЕРЕГРЕВ! Зарядник заблокирован на 30 секунд");
            OnChargerOverheated?.Invoke();
        }
        
        private void HandleCooldownComplete()
        {
            Debug.Log("[Charger] Остывание завершено. Зарядник разблокирован");
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить информацию о заряднике.
        /// </summary>
        public string GetChargerInfo()
        {
            string modeStr = mode == ChargerMode.On ? "Активен" : "Выключен";
            string heatStr = heat.GetHeatInfo();
            string bufferStr = buffer.GetBufferInfo();
            
            return $"[{chargerData?.ChargerName ?? "Unknown"}] {modeStr}\n" +
                   $"{bufferStr}\n{heatStr}\n" +
                   $"Камни: {ActiveSlotsCount}/{slots.Count} | Общий Ци: {GetTotalStonesQi():N0}";
        }
        
        /// <summary>
        /// Получить состояние для UI.
        /// </summary>
        public ChargerUIState GetUIState()
        {
            return new ChargerUIState
            {
                mode = mode,
                bufferQi = buffer.CurrentQi,
                bufferCapacity = buffer.Capacity,
                heatPercent = heat.HeatPercent,
                heatState = heat.State,
                isOverheated = heat.IsOverheated,
                cooldownRemaining = heat.CooldownRemaining,
                activeSlots = ActiveSlotsCount,
                totalSlots = slots.Count,
                totalStonesQi = GetTotalStonesQi()
            };
        }
        
        // === Cleanup ===
        
        private void OnDestroy()
        {
            if (heat != null)
            {
                heat.OnOverheated -= HandleOverheated;
                heat.OnCooldownComplete -= HandleCooldownComplete;
            }
        }
    }
    
    /// <summary>
    /// Состояние зарядника для UI.
    /// </summary>
    [Serializable]
    public struct ChargerUIState
    {
        public ChargerMode mode;
        public int bufferQi;
        public int bufferCapacity;
        public float heatPercent;
        public HeatState heatState;
        public bool isOverheated;
        public float cooldownRemaining;
        public int activeSlots;
        public int totalSlots;
        public long totalStonesQi;
    }
}
