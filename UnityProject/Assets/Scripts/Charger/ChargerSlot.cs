// ============================================================================
// ChargerSlot.cs — Слот для камня Ци в заряднике
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 08:20:00 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Charger
{
    /// <summary>
    /// Качество камня Ци.
    /// Источник: CHARGER_SYSTEM.md (косвенно)
    /// </summary>
    public enum QiStoneQuality
    {
        Raw,        // Сырой (низшее качество)
        Refined,    // Очищенный
        Perfect,    // Совершенный
        Transcendent // Трансцендентный
    }
    
    /// <summary>
    /// Размер камня Ци.
    /// </summary>
    public enum QiStoneSize
    {
        Tiny,       // Крошечный (100 Ци)
        Small,      // Малый (500 Ци)
        Medium,     // Средний (2000 Ци)
        Large,      // Большой (10000 Ци)
        Huge        // Огромный (50000 Ци)
    }
    
    /// <summary>
    /// Камень Ци — источник энергии для зарядника.
    /// </summary>
    [Serializable]
    public class QiStone
    {
        [SerializeField] private string stoneId;
        [SerializeField] private string stoneName;
        [SerializeField] private QiStoneQuality quality = QiStoneQuality.Raw;
        [SerializeField] private QiStoneSize size = QiStoneSize.Small;
        [SerializeField] private Element element = Element.Neutral;
        [SerializeField] private long currentQi;
        [SerializeField] private long maxQi;
        [SerializeField] private float releaseRate; // Скорость высвобождения Ци/сек
        
        // === Properties ===
        
        public string StoneId => stoneId;
        public string StoneName => stoneName;
        public QiStoneQuality Quality => quality;
        public QiStoneSize Size => size;
        public Element Element => element;
        public long CurrentQi => currentQi;
        public long MaxQi => maxQi;
        public float ReleaseRate => releaseRate;
        public bool IsEmpty => currentQi <= 0;
        public bool IsDepleted => currentQi <= maxQi * 0.1f; // Менее 10%
        public float QiPercent => maxQi > 0 ? (float)currentQi / maxQi : 0f;
        
        // === Constructor ===
        
        public QiStone()
        {
            stoneId = Guid.NewGuid().ToString().Substring(0, 8);
            InitializeStats();
        }
        
        public QiStone(QiStoneQuality quality, QiStoneSize size, Element element = Element.Neutral)
        {
            stoneId = Guid.NewGuid().ToString().Substring(0, 8);
            this.quality = quality;
            this.size = size;
            this.element = element;
            InitializeStats();
        }
        
        /// <summary>
        /// Инициализировать характеристики камня.
        /// </summary>
        private void InitializeStats()
        {
            // Базовое количество Ци по размеру
            maxQi = size switch
            {
                QiStoneSize.Tiny => 100,
                QiStoneSize.Small => 500,
                QiStoneSize.Medium => 2000,
                QiStoneSize.Large => 10000,
                QiStoneSize.Huge => 50000,
                _ => 500
            };
            
            // Множитель качества
            float qualityMult = quality switch
            {
                QiStoneQuality.Raw => 1.0f,
                QiStoneQuality.Refined => 1.5f,
                QiStoneQuality.Perfect => 2.5f,
                QiStoneQuality.Transcendent => 4.0f,
                _ => 1.0f
            };
            
            maxQi = (long)(maxQi * qualityMult);
            currentQi = maxQi;
            
            // Скорость высвобождения (50-200 Ци/сек)
            releaseRate = size switch
            {
                QiStoneSize.Tiny => 50f,
                QiStoneSize.Small => 80f,
                QiStoneSize.Medium => 120f,
                QiStoneSize.Large => 160f,
                QiStoneSize.Huge => 200f,
                _ => 80f
            };
            
            // Множитель качества для скорости
            releaseRate *= qualityMult;
            
            // Имя
            stoneName = $"{quality} {size} {element} Stone";
        }
        
        // === Qi Management ===
        
        /// <summary>
        /// Извлечь Ци из камня.
        /// </summary>
        /// <param name="amount">Запрашиваемое количество</param>
        /// <returns>Фактически извлечённое количество</returns>
        public long ExtractQi(long amount)
        {
            if (currentQi <= 0) return 0;
            
            long extracted = Math.Min(currentQi, amount);
            currentQi -= extracted;
            
            return extracted;
        }
        
        /// <summary>
        /// Рассчитать максимальную скорость высвобождения.
        /// </summary>
        public float GetEffectiveReleaseRate(float conductivity)
        {
            // Ограничено проводимостью зарядника и скоростью камня
            return Math.Min(releaseRate, conductivity);
        }
        
        /// <summary>
        /// Перезарядить камень (для тестов).
        /// </summary>
        public void Recharge()
        {
            currentQi = maxQi;
        }
        
        /// <summary>
        /// Перезарядить камень частично.
        /// </summary>
        public void Recharge(long amount)
        {
            currentQi = Math.Min(maxQi, currentQi + amount);
        }
    }
    
    /// <summary>
    /// Слот для камня Ци в заряднике.
    /// </summary>
    [Serializable]
    public class ChargerSlot
    {
        // FIX CHR-C01: [SerializeField] оставлен для Unity serialization в [Serializable] классе
        [SerializeField] private int slotIndex;
        [SerializeField] private QiStone insertedStone;
        [SerializeField] private bool isActive = true;
        [SerializeField] private bool isSealed = false;
        [SerializeField] private QiStoneQuality minQualityRequired = QiStoneQuality.Raw;
        [SerializeField] private QiStoneSize maxSizeAllowed = QiStoneSize.Huge;
        [SerializeField] private float absorptionBonus = 0f;
        [SerializeField] private float qiRetention = 0.95f;
        
        // === Properties ===
        
        public int SlotIndex => slotIndex;
        public QiStone InsertedStone => insertedStone;
        public bool IsActive => isActive;
        public bool IsSealed => isSealed;
        public bool HasStone => insertedStone != null && !insertedStone.IsEmpty;
        public bool CanInsert => isActive && !isSealed && insertedStone == null;
        public float AbsorptionBonus => absorptionBonus;
        public float QiRetention => qiRetention;
        
        // === Constructor ===
        
        public ChargerSlot(int index)
        {
            slotIndex = index;
        }
        
        public ChargerSlot(int index, QiStoneQuality minQuality, QiStoneSize maxSize)
        {
            slotIndex = index;
            minQualityRequired = minQuality;
            maxSizeAllowed = maxSize;
        }
        
        // === Stone Management ===
        
        /// <summary>
        /// Вставить камень в слот.
        /// </summary>
        /// <returns>True если успешно</returns>
        public bool InsertStone(QiStone stone)
        {
            if (!CanInsert) return false;
            if (!CanAcceptStone(stone)) return false;
            
            insertedStone = stone;
            return true;
        }
        
        /// <summary>
        /// Извлечь камень из слота.
        /// </summary>
        /// <returns>Извлечённый камень или null</returns>
        public QiStone RemoveStone()
        {
            if (insertedStone == null) return null;
            
            QiStone stone = insertedStone;
            insertedStone = null;
            return stone;
        }
        
        /// <summary>
        /// Проверить, можно ли принять камень.
        /// </summary>
        public bool CanAcceptStone(QiStone stone)
        {
            if (!isActive || isSealed) return false;
            if ((int)stone.Quality < (int)minQualityRequired) return false;
            if ((int)stone.Size > (int)maxSizeAllowed) return false;
            
            return true;
        }
        
        // === Qi Extraction ===
        
        /// <summary>
        /// Извлечь Ци из камня в слоте.
        /// </summary>
        /// <param name="maxAmount">Максимальное количество</param>
        /// <param name="chargerConductivity">Проводимость зарядника</param>
        /// <returns>Извлечённое Ци</returns>
        public long ExtractQi(long maxAmount, float chargerConductivity)
        {
            if (!HasStone || !isActive) return 0;
            
            // Ограничено: запрашиваемое количество, скорость камня, проводимость зарядника
            float effectiveRate = insertedStone.GetEffectiveReleaseRate(chargerConductivity);
            long effectiveMax = Math.Min(maxAmount, (long)effectiveRate);
            
            // Применяем бонус поглощения
            effectiveMax = (long)(effectiveMax * (1f + absorptionBonus));
            
            // Применяем потерю на сохранение (обратная величина)
            long extracted = insertedStone.ExtractQi(effectiveMax);
            long retained = (long)(extracted * qiRetention);
            
            return retained;
        }
        
        // === State Management ===
        
        /// <summary>
        /// Активировать слот.
        /// </summary>
        public void Activate()
        {
            if (!isSealed) isActive = true;
        }
        
        /// <summary>
        /// Деактивировать слот.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }
        
        /// <summary>
        /// Запечатать слот (навсегда или до особых условий).
        /// </summary>
        public void Seal()
        {
            isSealed = true;
            isActive = false;
        }
        
        /// <summary>
        /// Установить бонус поглощения.
        /// </summary>
        public void SetAbsorptionBonus(float bonus)
        {
            absorptionBonus = Mathf.Clamp(bonus, 0f, 1f);
        }
        
        /// <summary>
        /// Установить коэффициент сохранения Ци.
        /// </summary>
        public void SetQiRetention(float retention)
        {
            qiRetention = Mathf.Clamp(retention, 0f, 1f);
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить информацию о слоте.
        /// </summary>
        public string GetSlotInfo()
        {
            if (!isActive) return $"Слот {slotIndex}: Неактивен";
            if (isSealed) return $"Слот {slotIndex}: Запечатан";
            if (!HasStone) return $"Слот {slotIndex}: Пуст";
            
            return $"Слот {slotIndex}: {insertedStone.StoneName} ({insertedStone.CurrentQi:N0}/{insertedStone.MaxQi:N0} Ци)";
        }
    }
}
