// ============================================================================
// ChargerHeat.cs — Тепловой баланс зарядника
// Cultivation World Simulator
// Версия: 1.1 — Fix-01: [Header] убраны, [SerializeField] оставлены
// Создано: 2026-04-03 08:30:00 UTC
// Редактировано: 2026-04-10 — Fix-01: CHR-C01 fix
// ============================================================================

using System;
using UnityEngine;

namespace CultivationGame.Charger
{
    /// <summary>
    /// Состояние теплового баланса.
    /// </summary>
    public enum HeatState
    {
        Cool,       // 0-30% — Нормальная работа
        Warm,       // 31-60% — Повышенная температура
        Hot,        // 61-90% — Высокая температура
        Critical,   // 91-99% — Критическая температура
        Overheated  // 100% — Перегрев (блокировка)
    }
    
    /// <summary>
    /// Результат теплового воздействия.
    /// </summary>
    public struct HeatResult
    {
        public float heatGained;
        public float newHeatLevel;
        public HeatState previousState;
        public HeatState newState;
        public bool isOverheated;
        public float cooldownRemaining;
    }
    
    /// <summary>
    /// Тепловой баланс зарядника.
    /// Управляет нагревом, рассеиванием и перегревом.
    /// Источник: CHARGER_SYSTEM.md §4.3 "Тепловой баланс"
    /// 
    /// FIX CHR-C01: Убраны [Header] (не работают в [Serializable] non-MonoBehaviour),
    /// оставлены [SerializeField] (нужны для Unity serialization).
    /// </summary>
    [Serializable]
    public class ChargerHeat
    {
        // Heat Levels
        [SerializeField] [Range(0f, 100f)] private float currentHeat = 0f;
        [SerializeField] private float maxHeat = 100f;
        
        // Dissipation
        [SerializeField] private float baseDissipationRate = 1f;    // %/сек пассивно
        [SerializeField] private float combatDissipationRate = 0.5f; // %/сек в бою
        [SerializeField] private float postCombatDissipationRate = 2f; // %/сек после боя
        
        // Overheat
        [SerializeField] private float overheatThreshold = 100f;
        [SerializeField] private float overheatCooldown = 30f;       // секунд блокировки
        [SerializeField] private float cooldownTimer = 0f;
        
        // State
        [SerializeField] private bool isOverheated = false;
        [SerializeField] private bool isInCombat = false;
        
        // === Properties ===
        
        public float CurrentHeat => currentHeat;
        public float MaxHeat => maxHeat;
        public float HeatPercent => currentHeat / maxHeat;
        public HeatState State => GetHeatState();
        public bool IsOverheated => isOverheated;
        public bool IsCritical => currentHeat >= 90f;
        public bool IsHot => currentHeat >= 60f;
        public float CooldownRemaining => cooldownTimer;
        
        // === Events ===
        
        public event Action<float> OnHeatChanged;           // Процент тепла
        public event Action<HeatState> OnHeatStateChanged;  // Изменение состояния
        public event Action OnOverheated;                   // Перегрев
        public event Action OnCooldownComplete;             // Остывание после перегрева
        
        // === Constants ===
        
        /// <summary>
        /// Процент тепла от использованного Ци.
        /// Источник: CHARGER_SYSTEM.md §4.3
        /// "heatGain = qiUsed × 0.05" (5% от использованной Ци)
        /// </summary>
        private const float HEAT_PER_QI = 0.05f;
        
        // === Heat Management ===
        
        /// <summary>
        /// Добавить тепло от использования Ци.
        /// </summary>
        /// <param name="qiUsed">Количество использованного Ци</param>
        /// <returns>Результат теплового воздействия</returns>
        public HeatResult AddHeatFromQi(long qiUsed)
        {
            float heatGained = qiUsed * HEAT_PER_QI;
            return AddHeat(heatGained);
        }
        
        /// <summary>
        /// Добавить тепло.
        /// </summary>
        /// <param name="amount">Количество тепла</param>
        /// <returns>Результат теплового воздействия</returns>
        public HeatResult AddHeat(float amount)
        {
            HeatResult result = new HeatResult
            {
                previousState = GetHeatState(),
                heatGained = amount,
                isOverheated = false,
                cooldownRemaining = 0f
            };
            
            // Если уже перегрет — не добавляем
            if (isOverheated)
            {
                result.newHeatLevel = currentHeat;
                result.newState = HeatState.Overheated;
                result.isOverheated = true;
                result.cooldownRemaining = cooldownTimer;
                return result;
            }
            
            // Добавляем тепло
            currentHeat = Mathf.Min(maxHeat, currentHeat + amount);
            result.newHeatLevel = currentHeat;
            result.newState = GetHeatState();
            result.isOverheated = currentHeat >= overheatThreshold;
            
            // Проверяем перегрев
            if (result.isOverheated && !isOverheated)
            {
                isOverheated = true;
                cooldownTimer = overheatCooldown;
                OnOverheated?.Invoke();
            }
            
            OnHeatChanged?.Invoke(HeatPercent);
            
            if (result.newState != result.previousState)
            {
                OnHeatStateChanged?.Invoke(result.newState);
            }
            
            return result;
        }
        
        /// <summary>
        /// Рассеять тепло (вызывать каждый кадр).
        /// </summary>
        /// <param name="deltaTime">Время кадра</param>
        public void DissipateHeat(float deltaTime)
        {
            // Если перегрет — уменьшаем таймер кулдауна
            if (isOverheated)
            {
                cooldownTimer -= deltaTime;
                
                if (cooldownTimer <= 0f)
                {
                    // Кулдаун завершён — сбрасываем тепло
                    currentHeat = 0f;
                    isOverheated = false;
                    cooldownTimer = 0f;
                    
                    OnHeatChanged?.Invoke(0f);
                    OnHeatStateChanged?.Invoke(HeatState.Cool);
                    OnCooldownComplete?.Invoke();
                }
                
                return;
            }
            
            // Нормальное рассеивание
            float rate = isInCombat ? combatDissipationRate : baseDissipationRate;
            
            if (currentHeat > 0f)
            {
                float previousHeat = currentHeat;
                HeatState previousState = GetHeatState();
                
                currentHeat = Mathf.Max(0f, currentHeat - rate * deltaTime);
                
                OnHeatChanged?.Invoke(HeatPercent);
                
                HeatState newState = GetHeatState();
                if (newState != previousState)
                {
                    OnHeatStateChanged?.Invoke(newState);
                }
            }
        }
        
        /// <summary>
        /// Принудительное охлаждение (после боя).
        /// </summary>
        /// <param name="deltaTime">Время кадра</param>
        public void ForceCoolDown(float deltaTime)
        {
            if (isOverheated) return;
            
            float previousHeat = currentHeat;
            HeatState previousState = GetHeatState();
            
            currentHeat = Mathf.Max(0f, currentHeat - postCombatDissipationRate * deltaTime);
            
            OnHeatChanged?.Invoke(HeatPercent);
            
            HeatState newState = GetHeatState();
            if (newState != previousState)
            {
                OnHeatStateChanged?.Invoke(newState);
            }
        }
        
        // === Combat Mode ===
        
        /// <summary>
        /// Войти в боевой режим (медленное рассеивание).
        /// </summary>
        public void EnterCombat()
        {
            isInCombat = true;
        }
        
        /// <summary>
        /// Выйти из боевого режима (быстрое рассеивание).
        /// </summary>
        public void ExitCombat()
        {
            isInCombat = false;
        }
        
        // === State Query ===
        
        /// <summary>
        /// Получить текущее состояние тепла.
        /// </summary>
        public HeatState GetHeatState()
        {
            if (isOverheated) return HeatState.Overheated;
            if (currentHeat >= 90f) return HeatState.Critical;
            if (currentHeat >= 60f) return HeatState.Hot;
            if (currentHeat >= 30f) return HeatState.Warm;
            return HeatState.Cool;
        }
        
        /// <summary>
        /// Проверить, можно ли использовать зарядник.
        /// </summary>
        public bool CanOperate()
        {
            return !isOverheated;
        }
        
        /// <summary>
        /// Получить эффективность зарядника (% от нормы).
        /// Снижается при высокой температуре.
        /// </summary>
        public float GetEfficiency()
        {
            if (isOverheated) return 0f;
            
            // 100% при 0-30%, линейно снижается до 50% при 90%
            if (currentHeat <= 30f) return 1.0f;
            if (currentHeat >= 90f) return 0.5f;
            
            return 1.0f - (currentHeat - 30f) / 120f;
        }
        
        /// <summary>
        /// Получить множитель риска перегрева.
        /// Для UI предупреждения.
        /// </summary>
        public float GetOverheatRisk()
        {
            if (isOverheated) return 1f;
            if (currentHeat < 60f) return 0f;
            
            return (currentHeat - 60f) / 40f; // 60-100%
        }
        
        // === Utility ===
        
        /// <summary>
        /// Сбросить тепло (для тестов или особых эффектов).
        /// </summary>
        public void ResetHeat()
        {
            currentHeat = 0f;
            isOverheated = false;
            cooldownTimer = 0f;
            
            OnHeatChanged?.Invoke(0f);
            OnHeatStateChanged?.Invoke(HeatState.Cool);
        }
        
        /// <summary>
        /// Получить информацию о тепле.
        /// </summary>
        public string GetHeatInfo()
        {
            if (isOverheated)
            {
                return $"ПЕРЕГРЕВ! Кулдаун: {cooldownTimer:F1} сек";
            }
            
            return $"Тепло: {currentHeat:F1}% ({State}) | Эффективность: {GetEfficiency():P0}";
        }
        
        /// <summary>
        /// Получить предупреждение о состоянии.
        /// </summary>
        public string GetHeatWarning()
        {
            if (isOverheated) return "Зарядник заблокирован из-за перегрева!";
            if (currentHeat >= 90f) return "КРИТИЧЕСКАЯ температура! Риск перегрева!";
            if (currentHeat >= 60f) return "Высокая температура. Осторожно!";
            return null;
        }
        
        /// <summary>
        /// Получить цвет для UI индикатора.
        /// </summary>
        public UnityEngine.Color GetHeatColor()
        {
            return State switch
            {
                HeatState.Cool => UnityEngine.Color.green,
                HeatState.Warm => UnityEngine.Color.yellow,
                HeatState.Hot => new UnityEngine.Color(1f, 0.5f, 0f), // Оранжевый
                HeatState.Critical => UnityEngine.Color.red,
                HeatState.Overheated => new UnityEngine.Color(0.5f, 0f, 0f), // Тёмно-красный
                _ => UnityEngine.Color.green
            };
        }
        
        // === Serialization ===
        
        /// <summary>
        /// Сохранить состояние тепла.
        /// </summary>
        public HeatSaveData GetSaveData()
        {
            return new HeatSaveData
            {
                currentHeat = currentHeat,
                isOverheated = isOverheated,
                cooldownTimer = cooldownTimer,
                isInCombat = isInCombat
            };
        }
        
        /// <summary>
        /// Загрузить состояние тепла.
        /// </summary>
        public void LoadSaveData(HeatSaveData data)
        {
            currentHeat = data.currentHeat;
            isOverheated = data.isOverheated;
            cooldownTimer = data.cooldownTimer;
            isInCombat = data.isInCombat;
        }
    }
    
    /// <summary>
    /// Данные сохранения тепла.
    /// </summary>
    [Serializable]
    public struct HeatSaveData
    {
        public float currentHeat;
        public bool isOverheated;
        public float cooldownTimer;
        public bool isInCombat;
    }
}
