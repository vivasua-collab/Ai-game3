// ============================================================================
// MenuUI.cs — Меню игры
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Save;

namespace CultivationGame.UI
{
    /// <summary>
    /// Контроллер меню — главное меню, пауза, настройки.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SaveManager saveManager;
        
        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text versionText;
        
        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Load Menu")]
        [SerializeField] private GameObject loadMenuPanel;
        [SerializeField] private Transform saveSlotsContainer;
        [SerializeField] private GameObject saveSlotPrefab;
        [SerializeField] private Button loadBackButton;
        
        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Button settingsBackButton;
        
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        // === State ===
        private SettingsSaveData currentSettings;
        private Action pendingConfirmation;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (uiManager == null)
                uiManager = FindFirstObjectByType<UIManager>();
            if (saveManager == null)
                saveManager = FindFirstObjectByType<SaveManager>();
        }
        
        private void Start()
        {
            SetupButtons();
            LoadSettings();
            UpdateVersionText();
            CheckContinueAvailability();
        }
        
        // === Setup ===
        
        private void SetupButtons()
        {
            // Main Menu
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinue);
            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGame);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);
            
            // Pause Menu
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSave);
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadGame);
            if (pauseSettingsButton != null)
                pauseSettingsButton.onClick.AddListener(OnSettings);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);
            
            // Settings
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(OnSettingsBack);
            
            // Load Menu
            if (loadBackButton != null)
                loadBackButton.onClick.AddListener(OnLoadBack);
            
            // Confirmation
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
        
        private void LoadSettings()
        {
            currentSettings = new SettingsSaveData();
            
            // Применяем к UI
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = currentSettings.MasterVolume;
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = currentSettings.MusicVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = currentSettings.SfxVolume;
            if (qualityDropdown != null)
                qualityDropdown.value = currentSettings.QualityLevel;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = currentSettings.Fullscreen;
        }
        
        private void UpdateVersionText()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }
        
        private void CheckContinueAvailability()
        {
            if (continueButton != null)
            {
                continueButton.interactable = saveManager != null && saveManager.HasCurrentSave;
            }
        }
        
        // === Main Menu Actions ===
        
        private void OnNewGame()
        {
            if (uiManager != null)
            {
                uiManager.StartNewGame();
            }
        }
        
        private void OnContinue()
        {
            if (saveManager != null && saveManager.HasCurrentSave)
            {
                saveManager.LoadGame(saveManager.CurrentSlot);
            }
        }
        
        private void OnLoadGame()
        {
            ShowLoadMenu();
        }
        
        private void OnSettings()
        {
            ShowSettings();
        }
        
        private void OnQuit()
        {
            ShowConfirmation("Вы уверены, что хотите выйти?", () =>
            {
                if (uiManager != null)
                {
                    uiManager.QuitGame();
                }
            });
        }
        
        // === Pause Menu Actions ===
        
        private void OnResume()
        {
            if (uiManager != null)
            {
                uiManager.TogglePause();
            }
        }
        
        private void OnSave()
        {
            if (saveManager != null)
            {
                // Показать меню сохранения или сохранить в текущий слот
                if (saveManager.HasCurrentSave)
                {
                    saveManager.SaveGame(saveManager.CurrentSlot);
                }
                else
                {
                    // Показать выбор слота
                    ShowLoadMenu(); // Reuse for save
                }
            }
        }
        
        private void OnMainMenu()
        {
            ShowConfirmation("Несохранённый прогресс будет потерян. Продолжить?", () =>
            {
                if (uiManager != null)
                {
                    uiManager.QuitToMainMenu();
                }
            });
        }
        
        // === Settings Actions ===
        
        private void OnMasterVolumeChanged(float value)
        {
            currentSettings.MasterVolume = value;
            ApplyAudioSettings();
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            currentSettings.MusicVolume = value;
            ApplyAudioSettings();
        }
        
        private void OnSfxVolumeChanged(float value)
        {
            currentSettings.SfxVolume = value;
            ApplyAudioSettings();
        }
        
        private void OnQualityChanged(int index)
        {
            currentSettings.QualityLevel = index;
            QualitySettings.SetQualityLevel(index);
        }
        
        private void OnFullscreenChanged(bool isFullscreen)
        {
            currentSettings.Fullscreen = isFullscreen;
            Screen.fullScreen = isFullscreen;
        }
        
        private void OnLanguageChanged(int index)
        {
            // Реализуется при интеграции с локализацией
        }
        
        private void OnSettingsBack()
        {
            SaveSettings();
            HideSettings();
        }
        
        private void ApplyAudioSettings()
        {
            // Реализуется при интеграции с AudioManager
            AudioListener.volume = currentSettings.MasterVolume;
        }
        
        private void SaveSettings()
        {
            // Сохраняем через PlayerPrefs или SaveManager
            PlayerPrefs.SetFloat("MasterVolume", currentSettings.MasterVolume);
            PlayerPrefs.SetFloat("MusicVolume", currentSettings.MusicVolume);
            PlayerPrefs.SetFloat("SfxVolume", currentSettings.SfxVolume);
            PlayerPrefs.SetInt("QualityLevel", currentSettings.QualityLevel);
            PlayerPrefs.SetInt("Fullscreen", currentSettings.Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        // === Load Menu ===
        
        private void ShowLoadMenu()
        {
            if (loadMenuPanel != null)
            {
                loadMenuPanel.SetActive(true);
            }
            
            PopulateSaveSlots();
        }
        
        private void OnLoadBack()
        {
            if (loadMenuPanel != null)
            {
                loadMenuPanel.SetActive(false);
            }
        }
        
        private void PopulateSaveSlots()
        {
            if (saveSlotsContainer == null || saveSlotPrefab == null || saveManager == null) return;
            
            // Очищаем контейнер
            foreach (Transform child in saveSlotsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Создаём слоты
            SaveSlotInfo[] slots = saveManager.GetSlotInfos();
            foreach (var slot in slots)
            {
                CreateSaveSlot(slot);
            }
        }
        
        private void CreateSaveSlot(SaveSlotInfo slot)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotsContainer);
            
            TMP_Text slotNameText = slotObj.transform.Find("SlotName")?.GetComponent<TMP_Text>();
            TMP_Text slotInfoText = slotObj.transform.Find("SlotInfo")?.GetComponent<TMP_Text>();
            Button loadButton = slotObj.transform.Find("LoadButton")?.GetComponent<Button>();
            Button deleteButton = slotObj.transform.Find("DeleteButton")?.GetComponent<Button>();
            
            if (slotNameText != null)
            {
                slotNameText.text = GetSlotDisplayName(slot.SlotId);
            }
            
            if (slotInfoText != null)
            {
                if (slot.Exists)
                {
                    slotInfoText.text = $"{slot.FormattedSaveTime}\nВремя игры: {slot.FormattedPlayTime}";
                }
                else
                {
                    slotInfoText.text = "Пусто";
                }
            }
            
            if (loadButton != null)
            {
                loadButton.interactable = slot.Exists;
                loadButton.onClick.AddListener(() =>
                {
                    if (saveManager != null)
                    {
                        saveManager.LoadGame(slot.SlotId);
                        OnLoadBack();
                    }
                });
            }
            
            if (deleteButton != null)
            {
                deleteButton.interactable = slot.Exists;
                deleteButton.onClick.AddListener(() =>
                {
                    ShowConfirmation($"Удалить сохранение {GetSlotDisplayName(slot.SlotId)}?", () =>
                    {
                        if (saveManager != null)
                        {
                            saveManager.DeleteSlot(slot.SlotId);
                            PopulateSaveSlots();
                        }
                    });
                });
            }
        }
        
        private string GetSlotDisplayName(string slotId)
        {
            return slotId switch
            {
                "autosave" => "Автосохранение",
                "quicksave" => "Быстрое сохранение",
                _ => slotId.Replace("slot", "Слот ")
            };
        }
        
        // === Panels ===
        
        private void ShowSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }
        
        private void HideSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
        
        // === Confirmation ===
        
        private void ShowConfirmation(string message, Action onConfirm)
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);
            }
            
            if (confirmationText != null)
            {
                confirmationText.text = message;
            }
            
            pendingConfirmation = onConfirm;
        }
        
        private void OnConfirmYes()
        {
            pendingConfirmation?.Invoke();
            pendingConfirmation = null;
            
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }
        }
        
        private void OnConfirmNo()
        {
            pendingConfirmation = null;
            
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }
        }
    }
}
