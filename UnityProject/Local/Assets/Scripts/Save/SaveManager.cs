// ============================================================================
// SaveManager.cs — Менеджер сохранений
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.NPC;
using CultivationGame.World;
using CultivationGame.Combat;

namespace CultivationGame.Save
{
    /// <summary>
    /// Менеджер сохранений — управляет сохранением и загрузкой игры.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string saveFolder = "Saves";
        [SerializeField] private string fileExtension = ".sav";
        [SerializeField] private bool useEncryption = false;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 минут
        
        [Header("Slots")]
        [SerializeField] private int maxSlots = 5;
        
        // === References ===
        private WorldController worldController;
        private TimeController timeController;
        private LocationController locationController;
        private FactionController factionController;
        private EventController eventController;
        
        // === State ===
        private string currentSaveSlot = "";
        private float autoSaveTimer = 0f;
        private bool isSaving = false;
        private bool isLoading = false;
        
        // === Cache ===
        private Dictionary<string, SaveSlotInfo> slotCache = new Dictionary<string, SaveSlotInfo>();
        
        // === Events ===
        public event Action<string> OnSaveStarted;
        public event Action<string, bool> OnSaveCompleted;  // slotId, success
        public event Action<string> OnLoadStarted;
        public event Action<string, bool> OnLoadCompleted;  // slotId, success
        public event Action OnAutoSaveTriggered;
        public event Action<SaveSlotInfo[]> OnSlotListUpdated;
        
        // === Properties ===
        public string CurrentSlot => currentSaveSlot;
        public bool IsSaving => isSaving;
        public bool IsLoading => isLoading;
        public bool HasCurrentSave => !string.IsNullOrEmpty(currentSaveSlot);
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            InitializeReferences();
            EnsureSaveDirectory();
            RefreshSlotCache();
        }
        
        private void Start()
        {
            if (autoSave)
            {
                autoSaveTimer = autoSaveInterval;
            }
        }
        
        private void Update()
        {
            if (autoSave && !string.IsNullOrEmpty(currentSaveSlot))
            {
                autoSaveTimer -= Time.deltaTime;
                if (autoSaveTimer <= 0f)
                {
                    autoSaveTimer = autoSaveInterval;
                    TriggerAutoSave();
                }
            }
        }
        
        // === Initialization ===
        
        private void InitializeReferences()
        {
            worldController = FindFirstObjectByType<WorldController>();
            timeController = FindFirstObjectByType<TimeController>();
            locationController = FindFirstObjectByType<LocationController>();
            factionController = FindFirstObjectByType<FactionController>();
            eventController = FindFirstObjectByType<EventController>();
        }
        
        private void EnsureSaveDirectory()
        {
            string path = GetSavePath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        // === Save Operations ===
        
        /// <summary>
        /// Сохранить игру в указанный слот.
        /// </summary>
        public bool SaveGame(string slotId)
        {
            if (isSaving || isLoading) return false;
            
            isSaving = true;
            OnSaveStarted?.Invoke(slotId);
            
            try
            {
                GameSaveData saveData = CollectSaveData();
                saveData.SaveTime = DateTime.Now;
                saveData.PlayTimeSeconds = Time.realtimeSinceStartup;
                
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = GetSlotFilePath(slotId);
                
                if (useEncryption)
                {
                    json = Encrypt(json);
                }
                
                File.WriteAllText(filePath, json);
                
                currentSaveSlot = slotId;
                UpdateSlotCache(slotId, saveData);
                
                OnSaveCompleted?.Invoke(slotId, true);
                isSaving = false;
                
                Debug.Log($"Game saved to slot: {slotId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
                OnSaveCompleted?.Invoke(slotId, false);
                isSaving = false;
                return false;
            }
        }
        
        /// <summary>
        /// Быстрое сохранение.
        /// </summary>
        public bool QuickSave()
        {
            return SaveGame("quicksave");
        }
        
        /// <summary>
        /// Автосохранение.
        /// </summary>
        private void TriggerAutoSave()
        {
            OnAutoSaveTriggered?.Invoke();
            SaveGame("autosave");
        }
        
        /// <summary>
        /// Собрать данные для сохранения.
        /// </summary>
        private GameSaveData CollectSaveData()
        {
            GameSaveData data = new GameSaveData
            {
                Version = Application.version,
                Seed = UnityEngine.Random.state.GetHashCode()
            };
            
            // Время
            if (timeController != null)
            {
                data.TimeData = timeController.GetSaveData();
            }
            
            // Мир
            if (worldController != null)
            {
                data.WorldData = worldController.GetSaveData();
            }
            
            // Локации
            if (locationController != null)
            {
                data.LocationData = locationController.GetSaveData();
            }
            
            // Фракции
            if (factionController != null)
            {
                data.FactionData = factionController.GetSaveData();
            }
            
            // События
            if (eventController != null)
            {
                data.EventData = eventController.GetSaveData();
            }
            
            // TODO: Добавить игрока и NPC при интеграции
            
            return data;
        }
        
        // === Load Operations ===
        
        /// <summary>
        /// Загрузить игру из слота.
        /// </summary>
        public bool LoadGame(string slotId)
        {
            if (isSaving || isLoading) return false;
            if (!SlotExists(slotId)) return false;
            
            isLoading = true;
            OnLoadStarted?.Invoke(slotId);
            
            try
            {
                string filePath = GetSlotFilePath(slotId);
                string json = File.ReadAllText(filePath);
                
                if (useEncryption)
                {
                    json = Decrypt(json);
                }
                
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                ApplySaveData(saveData);
                
                currentSaveSlot = slotId;
                
                OnLoadCompleted?.Invoke(slotId, true);
                isLoading = false;
                
                Debug.Log($"Game loaded from slot: {slotId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                OnLoadCompleted?.Invoke(slotId, false);
                isLoading = false;
                return false;
            }
        }
        
        /// <summary>
        /// Быстрая загрузка.
        /// </summary>
        public bool QuickLoad()
        {
            return LoadGame("quicksave");
        }
        
        /// <summary>
        /// Применить загруженные данные.
        /// </summary>
        private void ApplySaveData(GameSaveData data)
        {
            // Время
            if (timeController != null && data.TimeData != null)
            {
                timeController.LoadSaveData(data.TimeData);
            }
            
            // Мир
            if (worldController != null && data.WorldData != null)
            {
                worldController.LoadSaveData(data.WorldData);
            }
            
            // Локации
            if (locationController != null && data.LocationData != null)
            {
                locationController.LoadSaveData(data.LocationData);
            }
            
            // Фракции
            if (factionController != null && data.FactionData != null)
            {
                factionController.LoadSaveData(data.FactionData);
            }
            
            // События
            if (eventController != null && data.EventData != null)
            {
                eventController.LoadSaveData(data.EventData);
            }
        }
        
        // === Slot Management ===
        
        /// <summary>
        /// Получить информацию о слотах.
        /// </summary>
        public SaveSlotInfo[] GetSlotInfos()
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
            
            // Стандартные слоты
            for (int i = 1; i <= maxSlots; i++)
            {
                string slotId = $"slot{i}";
                slots.Add(GetSlotInfo(slotId));
            }
            
            // Автосохранение
            slots.Add(GetSlotInfo("autosave"));
            
            // Быстрое сохранение
            slots.Add(GetSlotInfo("quicksave"));
            
            OnSlotListUpdated?.Invoke(slots.ToArray());
            return slots.ToArray();
        }
        
        /// <summary>
        /// Получить информацию о слоте.
        /// </summary>
        public SaveSlotInfo GetSlotInfo(string slotId)
        {
            if (slotCache.TryGetValue(slotId, out SaveSlotInfo info))
            {
                return info;
            }
            
            return new SaveSlotInfo { SlotId = slotId, Exists = false };
        }
        
        /// <summary>
        /// Проверить существование слота.
        /// </summary>
        public bool SlotExists(string slotId)
        {
            return File.Exists(GetSlotFilePath(slotId));
        }
        
        /// <summary>
        /// Удалить слот.
        /// </summary>
        public bool DeleteSlot(string slotId)
        {
            try
            {
                string filePath = GetSlotFilePath(slotId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                slotCache.Remove(slotId);
                
                if (currentSaveSlot == slotId)
                {
                    currentSaveSlot = "";
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete slot: {e.Message}");
                return false;
            }
        }
        
        private void RefreshSlotCache()
        {
            slotCache.Clear();
            GetSlotInfos();
        }
        
        private void UpdateSlotCache(string slotId, GameSaveData data)
        {
            slotCache[slotId] = new SaveSlotInfo
            {
                SlotId = slotId,
                Exists = true,
                SaveTime = data.SaveTime,
                PlayTimeSeconds = data.PlayTimeSeconds,
                WorldAge = data.WorldData?.WorldAge ?? 0,
                Version = data.Version
            };
        }
        
        // === File Operations ===
        
        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFolder);
        }
        
        private string GetSlotFilePath(string slotId)
        {
            return Path.Combine(GetSavePath(), $"{slotId}{fileExtension}");
        }
        
        // === Encryption ===
        
        private string Encrypt(string data)
        {
            // Простое XOR шифрование для базовой защиты
            char[] chars = data.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)(chars[i] ^ 0x5A);
            }
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(chars));
        }
        
        private string Decrypt(string data)
        {
            char[] chars = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data)).ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)(chars[i] ^ 0x5A);
            }
            return new string(chars);
        }
        
        // === New Game ===
        
        /// <summary>
        /// Начать новую игру.
        /// </summary>
        public void NewGame(string slotId)
        {
            currentSaveSlot = slotId;
            
            // Сброс всех систем к начальному состоянию
            // Реализуется при интеграции с остальными системами
        }
    }
    
    /// <summary>
    /// Информация о слоте сохранения.
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public string SlotId;
        public bool Exists;
        public DateTime SaveTime;
        public float PlayTimeSeconds;
        public int WorldAge;
        public string Version;
        
        public string FormattedPlayTime
        {
            get
            {
                int hours = (int)(PlayTimeSeconds / 3600);
                int minutes = (int)((PlayTimeSeconds % 3600) / 60);
                return $"{hours:D2}:{minutes:D2}";
            }
        }
        
        public string FormattedSaveTime => SaveTime.ToString("yyyy-MM-dd HH:mm");
    }
    
    /// <summary>
    /// Полные данные сохранения игры.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public string Version;
        public int Seed;
        public DateTime SaveTime;
        public float PlayTimeSeconds;
        
        // Системы
        public TimeSaveData TimeData;
        public WorldSaveData WorldData;
        public LocationSaveData LocationData;
        public FactionSystemSaveData FactionData;
        public EventSaveData EventData;
        
        // Игрок (добавляется при интеграции)
        public PlayerSaveData PlayerData;
        
        // NPC (добавляется при интеграции)
        public List<NPCSaveData> NPCData;
    }
    
    /// <summary>
    /// Данные игрока для сохранения (заглушка).
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerId;
        public string Name;
        public int CultivationLevel;
        public int CurrentQi;
        public string CurrentLocationId;
    }
}
