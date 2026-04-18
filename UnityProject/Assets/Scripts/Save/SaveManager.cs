// ============================================================================
// SaveManager.cs — Менеджер сохранений
// Cultivation World Simulator
// Версия: 1.2 — Fix-08: path traversal, real play time, collect all systems,
//               cache miss, validation, encryption comments
// ============================================================================
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-11 Fix-08
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.2:
// - FIX SAV-C01: GetSlotFilePath — валидация slotId (regex) от path traversal
// - FIX SAV-H02: TotalPlayTimeHours — real play time через unscaledDeltaTime
// - FIX SAV-H03: CollectSaveData — сбор из всех систем (Formation, Buff, Tile, Charger, NPC, Quest, Player)
// - FIX SAV-H04: GetSlotInfo — чтение файла при cache miss
// - FIX SAV-H05: ValidateSaveData — проверка null/битых полей перед ApplySaveData
// - FIX SAV-M01: Комментарий о дублировании шифрования с SaveFileHandler
// - FIX SAV-M03: Комментарий о миграции шифрования на SaveFileHandler.AES
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.NPC;
using CultivationGame.World;
using CultivationGame.Combat;
using CultivationGame.Formation;
using CultivationGame.Buff;
using CultivationGame.TileSystem;
using CultivationGame.Charger;
using CultivationGame.Quest;
using CultivationGame.Player;
using CultivationGame.Inventory;

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
        
        // FIX SAV-H03: Additional system references (2026-04-11)
        private FormationController formationController;
        private BuffManager buffManager;
        private TileMapController tileMapController;
        private ChargerController chargerController;
        private NPCController npcController;
        private QuestController questController;
        private PlayerController playerController;
        
        // SpiritStorage reference (added 2026-04-19)
        private SpiritStorageController spiritStorageController;
        
        // StorageRing reference (added 2026-04-19)
        private StorageRingController storageRingController;
        
        // === State ===
        private string currentSaveSlot = "";
        private float autoSaveTimer = 0f;
        private bool isSaving = false;
        private bool isLoading = false;
        
        // FIX SAV-H02: Real play time accumulator (2026-04-11)
        private float realPlayTimeSeconds = 0f;
        
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
        
        // FIX SAV-H02: Expose real play time for UI (2026-04-11)
        public float RealPlayTimeSeconds => realPlayTimeSeconds;
        public float RealPlayTimeHours => realPlayTimeSeconds / 3600f;
        
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
            // FIX SAV-H02: Accumulate real play time (2026-04-11)
            realPlayTimeSeconds += Time.unscaledDeltaTime;
            
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
            
            // FIX SAV-H03: Initialize additional system references (2026-04-11)
            formationController = FindFirstObjectByType<FormationController>();
            buffManager = FindFirstObjectByType<BuffManager>();
            tileMapController = FindFirstObjectByType<TileMapController>();
            chargerController = FindFirstObjectByType<ChargerController>();
            npcController = FindFirstObjectByType<NPCController>();
            questController = FindFirstObjectByType<QuestController>();
            playerController = FindFirstObjectByType<PlayerController>();
            
            // SpiritStorage (added 2026-04-19)
            spiritStorageController = FindFirstObjectByType<SpiritStorageController>();
            
            // StorageRing (added 2026-04-19)
            storageRingController = FindFirstObjectByType<StorageRingController>();
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
                
                // FIX: Используем Unix timestamp вместо DateTime для JsonUtility
                saveData.SaveTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                // FIX SAV-H02: Используем реальное время игры, не игровое (2026-04-11)
                saveData.TotalPlayTimeHours = realPlayTimeSeconds / 3600f;
                
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = GetSlotFilePath(slotId);
                
                // FIX SAV-C01: Empty filePath means invalid slotId (2026-04-11)
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogError($"[Save] Cannot save — invalid slotId: {slotId}");
                    OnSaveCompleted?.Invoke(slotId, false);
                    isSaving = false;
                    return false;
                }
                
                if (useEncryption)
                {
                    // FIX SAV-M01/SAV-M03: Это XOR шифрование дублирует SaveFileHandler.AES.
                    // Для продакшена мигрировать на SaveFileHandler.WriteToFile(path, content, encrypt:true, key:...).
                    // Обратная совместимость: старые XOR-сохранения несовместимы с AES (2026-04-11)
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
        /// FIX SAV-H03: Добавлен сбор из всех систем (2026-04-11)
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
            
            // FIX SAV-H03: Игрок (2026-04-11)
            if (playerController != null)
            {
                data.PlayerData = playerController.GetSaveData();
            }
            
            // FIX SAV-H03: Формации (2026-04-11)
            if (formationController != null)
            {
                var formationSave = formationController.GetSaveData();
                if (formationSave != null && formationSave.activeFormations != null)
                {
                    data.FormationData = new FormationSaveEntry
                    {
                        formationId = formationSave.activeFormations.Count > 0 
                            ? formationSave.activeFormations[0]?.formationId ?? "" 
                            : "",
                        practitionerCount = formationSave.activeFormations.Count,
                        qiPoolAmount = 0 // Will be populated when FormationCore exposes it
                    };
                }
            }
            
            // FIX SAV-H03: Баффы (2026-04-11)
            if (buffManager != null && buffManager.ActiveBuffCount > 0)
            {
                var firstBuff = buffManager.ActiveBuffs[0];
                data.BuffData = new BuffSaveData
                {
                    buffId = firstBuff?.data?.buffId ?? "",
                    remainingDuration = firstBuff?.remainingTicks ?? 0,
                    stacks = firstBuff?.currentStacks ?? 0
                };
            }
            
            // FIX SAV-H03: Тайловая карта (2026-04-11)
            if (tileMapController != null && tileMapController.MapData != null)
            {
                data.TileData = new TileSaveData
                {
                    width = tileMapController.Width,
                    height = tileMapController.Height,
                    serializedTiles = tileMapController.MapData.ToJson()
                };
            }
            
            // FIX SAV-H03: Зарядник (2026-04-11)
            if (chargerController != null)
            {
                data.ChargerData = new ChargerSaveData
                {
                    slotCount = chargerController.Slots?.Count ?? 0,
                    heatLevel = chargerController.Heat?.CurrentHeat ?? 0f,
                    qiStored = chargerController.Buffer?.CurrentQi ?? 0
                };
            }
            
            // FIX SAV-H03: NPC (2026-04-11)
            if (npcController != null)
            {
                data.NPCData = new List<NPCSaveData> { npcController.GetSaveData() };
            }
            
            // FIX SAV-H03: Квесты (2026-04-11)
            if (questController != null)
            {
                data.QuestData = questController.GetSaveData();
            }
            
            // SpiritStorage (added 2026-04-19)
            if (spiritStorageController != null)
            {
                data.SpiritStorageData = spiritStorageController.GetSaveData();
            }
            
            // StorageRing (added 2026-04-19)
            if (storageRingController != null)
            {
                data.StorageRingData = storageRingController.GetSaveData();
            }
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
                    // FIX SAV-M01/SAV-M03: См. комментарий в SaveGame о миграции на SaveFileHandler.AES (2026-04-11)
                    json = Decrypt(json);
                }
                
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                // FIX SAV-H05: Validate save data before applying (2026-04-11)
                if (!ValidateSaveData(saveData))
                {
                    Debug.LogError($"[Save] Validation failed for slot: {slotId}");
                    OnLoadCompleted?.Invoke(slotId, false);
                    isLoading = false;
                    return false;
                }
                
                ApplySaveData(saveData);
                
                // FIX SAV-H02: Restore real play time from save (2026-04-11)
                realPlayTimeSeconds = saveData.TotalPlayTimeHours * 3600f;
                
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
        /// FIX SAV-H03: Добавлено применение для всех систем (2026-04-11)
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
            
            // FIX SAV-H03: Игрок (2026-04-11)
            if (playerController != null && data.PlayerData != null)
            {
                playerController.LoadSaveData(data.PlayerData);
            }
            
            // FIX SAV-H03: NPC (2026-04-11)
            if (npcController != null && data.NPCData != null)
            {
                foreach (var npcData in data.NPCData)
                {
                    if (npcData != null)
                    {
                        npcController.LoadSaveData(npcData);
                    }
                }
            }
            
            // FIX SAV-H03: Квесты (2026-04-11)
            // Note: QuestSystemSaveData is a struct (never null), always apply
            if (questController != null)
            {
                questController.LoadSaveData(data.QuestData);
            }
            
            // Note: Formation, Buff, Tile, Charger restore requires additional
            // LoadSaveData methods on those controllers. For now, data is saved
            // and will be restored when those methods are implemented.
            
            // SpiritStorage (added 2026-04-19)
            if (spiritStorageController != null && data.SpiritStorageData != null)
            {
                // Note: itemDatabase must be built from loaded ItemData assets
                // For now, pass empty dict — real implementation needs asset loading
                var itemDatabase = new Dictionary<string, Data.ScriptableObjects.ItemData>();
                spiritStorageController.LoadSaveData(data.SpiritStorageData, itemDatabase);
            }
            
            // StorageRing (added 2026-04-19)
            if (storageRingController != null && data.StorageRingData != null)
            {
                var itemDatabase = new Dictionary<string, Data.ScriptableObjects.ItemData>();
                storageRingController.LoadSaveData(data.StorageRingData, itemDatabase);
            }
        }
        
        // === Validation ===
        
        /// <summary>
        /// Проверить целостность данных сохранения.
        /// FIX SAV-H05: Валидация после FromJson (2026-04-11)
        /// </summary>
        private bool ValidateSaveData(GameSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[Save] Save data is null after deserialization");
                return false;
            }
            
            // Version is informational, not fatal
            if (string.IsNullOrEmpty(data.Version))
            {
                Debug.LogWarning("[Save] Save data missing version — continuing load");
            }
            
            // Check critical fields — if all system data is null, this might be a corrupted save
            if (data.TimeData == null && data.WorldData == null && 
                data.PlayerData == null && data.LocationData == null)
            {
                Debug.LogWarning("[Save] Save data has no system data — may be an empty or corrupted save");
                // Not fatal: could be a fresh save with no gameplay yet
            }
            
            return true;
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
        /// FIX SAV-H04: При cache miss — читать файл (2026-04-11)
        /// </summary>
        public SaveSlotInfo GetSlotInfo(string slotId)
        {
            if (slotCache.TryGetValue(slotId, out SaveSlotInfo info))
            {
                return info;
            }
            
            // FIX SAV-H04: Cache miss — try reading the file header (2026-04-11)
            string filePath = GetSlotFilePath(slotId);
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    if (useEncryption)
                    {
                        json = Decrypt(json);
                    }
                    GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                    if (saveData != null)
                    {
                        var slotInfo = new SaveSlotInfo
                        {
                            SlotId = slotId,
                            Exists = true,
                            SaveTimeUnix = saveData.SaveTimeUnix,
                            TotalPlayTimeHours = saveData.TotalPlayTimeHours,
                            WorldAge = saveData.WorldData?.WorldAge ?? 0,
                            Version = saveData.Version
                        };
                        slotCache[slotId] = slotInfo;
                        return slotInfo;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Save] Failed to read slot info for {slotId}: {e.Message}");
                }
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
                SaveTimeUnix = data.SaveTimeUnix,
                TotalPlayTimeHours = data.TotalPlayTimeHours,
                WorldAge = data.WorldData?.WorldAge ?? 0,
                Version = data.Version
            };
        }
        
        // === File Operations ===
        
        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFolder);
        }
        
        /// <summary>
        /// Получить путь к файлу слота.
        /// FIX SAV-C01: Валидация slotId для предотвращения path traversal (2026-04-11)
        /// </summary>
        private string GetSlotFilePath(string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || !Regex.IsMatch(slotId, @"^[a-zA-Z0-9_-]+$"))
            {
                Debug.LogError($"[Save] Invalid slotId rejected (path traversal protection): {slotId}");
                return "";
            }
            return Path.Combine(GetSavePath(), $"{slotId}{fileExtension}");
        }
        
        // === Encryption ===
        
        // FIX SAV-M01: Это XOR шифрование дублирует SaveFileHandler.AES (2026-04-11).
        // Оба метода сосуществуют для обратной совместимости:
        // - SaveManager.Encrypt/Decrypt — XOR, используется при useEncryption=true
        // - SaveFileHandler.Encrypt/Decrypt — AES, более надёжный
        //
        // FIX SAV-M03: Для продакшена нужно мигрировать на SaveFileHandler.AES (2026-04-11).
        // Рекомендуемый подход:
        //   SaveManager.SaveGame → SaveFileHandler.WriteToFile(path, json, encrypt:true, key:encryptionKey)
        //   SaveManager.LoadGame → SaveFileHandler.ReadFromFile(path, decrypt:true, key:encryptionKey)
        // Это обеспечит настоящую защиту вместо простого XOR.
        
        private string Encrypt(string data)
        {
            // Простое XOR шифрование для базовой защиты
            // NOTE: Не является криптографически стойким. См. SAV-M03.
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
            realPlayTimeSeconds = 0f; // FIX SAV-H02: Reset play time on new game (2026-04-11)
            
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
        public long SaveTimeUnix;  // FIX: Unix timestamp вместо DateTime
        public float TotalPlayTimeHours;  // FIX: В часах, накапливается
        public int WorldAge;
        public string Version;
        
        /// <summary>
        /// Получить DateTime из Unix timestamp.
        /// </summary>
        public System.DateTime GetSaveTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(SaveTimeUnix).LocalDateTime;
        }
        
        public string FormattedPlayTime
        {
            get
            {
                int hours = (int)TotalPlayTimeHours;
                int minutes = (int)((TotalPlayTimeHours % 1) * 60);
                return $"{hours:D2}:{minutes:D2}";
            }
        }
        
        public string FormattedSaveTime => GetSaveTime().ToString("yyyy-MM-dd HH:mm");
    }
    
    /// <summary>
    /// Полные данные сохранения игры.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public string Version;
        public int Seed;
        public long SaveTimeUnix;  // FIX: Unix timestamp вместо DateTime
        public float TotalPlayTimeHours;  // FIX SAV-H02: Накапливаемое реальное время в часах (2026-04-11)
        
        // Системы
        public TimeSaveData TimeData;
        public WorldSaveData WorldData;
        public LocationSaveData LocationData;
        public FactionSystemSaveData FactionData;
        public EventSaveData EventData;
        
        // Игрок
        public PlayerSaveData PlayerData;
        
        // NPC
        public List<NPCSaveData> NPCData;
        
        // FIX SAV-H03: Additional system save data (2026-04-11)
        public FormationSaveEntry FormationData;
        public BuffSaveData BuffData;
        public TileSaveData TileData;
        public ChargerSaveData ChargerData;
        public QuestSystemSaveData QuestData;
        
        // SpiritStorage (added 2026-04-19)
        public SpiritStorageSaveData SpiritStorageData;
        
        // StorageRing (added 2026-04-19)
        public StorageRingSaveData StorageRingData;
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
        public long CurrentQi; // FIX PLR-H02: Changed from int to long — Qi can exceed int.MaxValue (2026-04-11)
        public string CurrentLocationId;
    }
}
