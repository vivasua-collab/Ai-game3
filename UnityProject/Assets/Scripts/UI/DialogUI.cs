// ============================================================================
// DialogUI.cs — Интерфейс диалогов
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: ServiceLocator, Input note
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.NPC;
using CultivationGame.Interaction;

namespace CultivationGame.UI
{
    /// <summary>
    /// Интерфейс диалогов — отображает диалоги и варианты ответов.
    /// </summary>
    public class DialogUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueSystem dialogueSystem;
        [SerializeField] private UIManager uiManager;
        
        [Header("Dialog Display")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogText;
        [SerializeField] private Image speakerPortrait;
        
        [Header("Choices")]
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private int maxVisibleChoices = 4;
        
        [Header("Continue")]
        [SerializeField] private GameObject continueButton;
        [SerializeField] private TMP_Text continueText;
        
        [Header("Typing Effect")]
        [SerializeField] private float typingSpeed = 0.03f;
        [SerializeField] private bool useTypingEffect = true;
        
        [Header("Animation")]
        [SerializeField] private Animator panelAnimator;
        [SerializeField] private string showTrigger = "Show";
        [SerializeField] private string hideTrigger = "Hide";
        
        // === Runtime ===
        private List<GameObject> choiceButtons = new List<GameObject>();
        private bool isTyping = false;
        private string currentText = "";
        private int currentChoiceIndex = 0;
        
        // === Events ===
        public event Action OnDialogStarted;
        public event Action OnDialogEnded;
        public event Action<int> OnChoiceSelected;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (dialogueSystem == null)
                dialogueSystem = ServiceLocator.GetOrFind<DialogueSystem>(); // FIX UI-H03 (2026-04-12)
            if (uiManager == null)
                uiManager = ServiceLocator.GetOrFind<UIManager>(); // FIX UI-H03 (2026-04-12)
        }
        
        private void Start()
        {
            SubscribeToEvents();
            HideDialog();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        // === Initialization ===
        
        private void SubscribeToEvents()
        {
            if (dialogueSystem != null)
            {
                dialogueSystem.OnDialogueStateChanged += OnDialogueStateChanged;
                dialogueSystem.OnNodeChanged += OnNodeChanged;
                dialogueSystem.OnTextUpdated += OnTextUpdated;
                dialogueSystem.OnChoicesUpdated += OnChoicesUpdated;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (dialogueSystem != null)
            {
                dialogueSystem.OnDialogueStateChanged -= OnDialogueStateChanged;
                dialogueSystem.OnNodeChanged -= OnNodeChanged;
                dialogueSystem.OnTextUpdated -= OnTextUpdated;
                dialogueSystem.OnChoicesUpdated -= OnChoicesUpdated;
            }
        }
        
        // === Dialog State ===
        
        private void OnDialogueStateChanged(bool isInDialogue)
        {
            if (isInDialogue)
            {
                ShowDialog();
                OnDialogStarted?.Invoke();
            }
            else
            {
                HideDialog();
                OnDialogEnded?.Invoke();
            }
        }
        
        private void OnNodeChanged(DialogueNode node)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = node.SpeakerName;
            }
            
            if (speakerPortrait != null && node.SpeakerPortrait != null)
            {
                speakerPortrait.sprite = node.SpeakerPortrait;
            }
            
            // Показываем кнопку продолжения если нет выборов
            if (continueButton != null)
            {
                continueButton.SetActive(node.Choices == null || node.Choices.Count == 0);
            }
        }
        
        private void OnTextUpdated(string text)
        {
            if (dialogText != null)
            {
                if (useTypingEffect)
                {
                    currentText = text;
                    isTyping = true;
                }
                else
                {
                    dialogText.text = text;
                    isTyping = false;
                }
            }
        }
        
        private void OnChoicesUpdated(List<DialogueChoice> choices)
        {
            UpdateChoices(choices);
            
            if (continueButton != null)
            {
                continueButton.SetActive(choices.Count == 0);
            }
        }
        
        // === Display ===
        
        private void ShowDialog()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }
            
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(showTrigger);
            }
            
            if (uiManager != null)
            {
                uiManager.StartDialog();
            }
        }
        
        private void HideDialog()
        {
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(hideTrigger);
            }
            
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
            
            ClearChoices();
            
            if (uiManager != null)
            {
                uiManager.EndDialog();
            }
        }
        
        // === Choices ===
        
        private void UpdateChoices(List<DialogueChoice> choices)
        {
            ClearChoices();
            
            if (choicesContainer == null || choiceButtonPrefab == null) return;
            
            for (int i = 0; i < choices.Count && i < maxVisibleChoices; i++)
            {
                CreateChoiceButton(choices[i], i);
            }
            
            currentChoiceIndex = 0;
            HighlightChoice(0);
        }
        
        private void CreateChoiceButton(DialogueChoice choice, int index)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
            
            TMP_Text textComponent = buttonObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = choice.Text;
            }
            
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                int capturedIndex = index;
                button.onClick.AddListener(() => SelectChoice(capturedIndex));
            }
            
            choiceButtons.Add(buttonObj);
        }
        
        private void ClearChoices()
        {
            foreach (var button in choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            choiceButtons.Clear();
        }
        
        private void HighlightChoice(int index)
        {
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                Button button = choiceButtons[i].GetComponent<Button>();
                if (button != null)
                {
                    // Визуальное выделение
                    ColorBlock colors = button.colors;
                    colors.normalColor = (i == index) ? Color.white : Color.gray;
                    button.colors = colors;
                }
            }
        }
        
        // === Selection ===
        
        public void SelectChoice(int index)
        {
            if (dialogueSystem != null)
            {
                dialogueSystem.SelectChoice(index);
                OnChoiceSelected?.Invoke(index);
            }
        }
        
        public void Continue()
        {
            if (dialogueSystem != null)
            {
                if (isTyping)
                {
                    // Завершить печать текста
                    if (dialogText != null)
                    {
                        dialogText.text = currentText;
                    }
                    isTyping = false;
                }
                else
                {
                    dialogueSystem.Continue();
                }
            }
        }
        
        // === Input ===
        
        private void HandleInput()
        {
            // NOTE UI-H06: Старый Input System — будущий переход на новый Input System (2026-04-12)
            if (dialogueSystem == null || !dialogueSystem.IsInDialogue) return;
            
            // Навигация по выборам
            if (choiceButtons.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    currentChoiceIndex = (currentChoiceIndex - 1 + choiceButtons.Count) % choiceButtons.Count;
                    HighlightChoice(currentChoiceIndex);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    currentChoiceIndex = (currentChoiceIndex + 1) % choiceButtons.Count;
                    HighlightChoice(currentChoiceIndex);
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    SelectChoice(currentChoiceIndex);
                }
            }
            else
            {
                // Продолжить диалог
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    Continue();
                }
            }
        }
        
        // === Public API ===
        
        /// <summary>
        /// Начать диалог с NPC.
        /// </summary>
        public void StartDialogWith(NPCController npc, string startNodeId)
        {
            if (dialogueSystem != null && npc != null)
            {
                dialogueSystem.StartDialogue(npc, startNodeId);
            }
        }
        
        /// <summary>
        /// Завершить текущий диалог.
        /// </summary>
        public void EndCurrentDialog()
        {
            if (dialogueSystem != null)
            {
                dialogueSystem.EndDialogue();
            }
        }
        
        /// <summary>
        /// Проверить, активен ли диалог.
        /// </summary>
        public bool IsInDialog()
        {
            return dialogueSystem != null && dialogueSystem.IsInDialogue;
        }
    }
}
