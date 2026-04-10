// ============================================================================
// DialogueSystem.cs — Система диалогов
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-11 00:00:00 UTC — Fix-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.NPC;

namespace CultivationGame.Interaction
{
    /// <summary>
    /// Узел диалога.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public string NodeId;
        
        [TextArea(3, 10)]
        public string Text;
        
        public string SpeakerName;
        public Sprite SpeakerPortrait;
        
        public List<DialogueChoice> Choices;
        public List<DialogueAction> Actions;
        
        public string NextNodeId;       // Автопереход (если нет выборов)
        public bool IsEndNode;
        
        public DialogueNode()
        {
            NodeId = Guid.NewGuid().ToString();
            Choices = new List<DialogueChoice>();
            Actions = new List<DialogueAction>();
        }
    }
    
    // FIX DLG-C01: Wrapper class for JsonUtility array root deserialization (2026-04-11)
    // JsonUtility.FromJson cannot deserialize JSON arrays at root level.
    // This wrapper allows: {"nodes": [...]} instead of [...]
    [Serializable]
    public class DialogueNodeArrayWrapper
    {
        public DialogueNode[] nodes;
    }
    
    /// <summary>
    /// Выбор в диалоге.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string ChoiceId;
        
        [TextArea(1, 3)]
        public string Text;
        
        public string NextNodeId;
        
        // Условия доступности
        public DialogueCondition[] Conditions;
        
        // Эффекты выбора
        public int RelationshipChange;
        public string[] SetFlags;
        public string[] RequiredFlags;
        
        public DialogueChoice()
        {
            ChoiceId = Guid.NewGuid().ToString();
        }
        
        public bool IsAvailable(Dictionary<string, bool> flags)
        {
            if (RequiredFlags == null || RequiredFlags.Length == 0)
                return true;
            
            foreach (string flag in RequiredFlags)
            {
                if (!flags.TryGetValue(flag, out bool value) || !value)
                    return false;
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Действие в диалоге.
    /// </summary>
    [Serializable]
    public class DialogueAction
    {
        public DialogueActionType Type;
        public string StringValue;
        public int IntValue;
        public float FloatValue;
    }
    
    /// <summary>
    /// Типы действий в диалоге.
    /// </summary>
    public enum DialogueActionType
    {
        SetFlag,
        ClearFlag,
        ModifyRelationship,
        ModifyQi,
        ModifyHealth,
        TeachTechnique,
        GiveItem,
        TriggerEvent,
        PlaySound,
        PlayAnimation
    }
    
    /// <summary>
    /// Условие диалога.
    /// </summary>
    [Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType Type;
        public string StringValue;
        public int IntValue;
        public CompareOperator Operator;
    }
    
    public enum DialogueConditionType
    {
        HasFlag,
        CultivationLevel,
        Relationship,
        HasItem,
        HasTechnique,
        QuestComplete
    }
    
    public enum CompareOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }
    
    /// <summary>
    /// Система диалогов — управление диалогами между игроком и NPC.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private string dialogueResourcePath = "Dialogues"; // Path in Resources
        
        // === State ===
        private bool isInDialogue = false;
        private NPCController currentNPC;
        private DialogueNode currentNode;
        private Dictionary<string, DialogueNode> dialogueNodes = new Dictionary<string, DialogueNode>();
        private Dictionary<string, bool> dialogueFlags = new Dictionary<string, bool>();
        private Queue<char> typingQueue = new Queue<char>();
        private bool isTyping = false;
        private string currentText = "";
        
        // === Events ===
        public event Action<bool> OnDialogueStateChanged;
        public event Action<DialogueNode> OnNodeChanged;
        public event Action<string> OnTextUpdated;
        public event Action<List<DialogueChoice>> OnChoicesUpdated;
        public event Action<DialogueAction> OnActionTriggered;
        public event Action OnDialogueEnded;
        
        // === Properties ===
        public bool IsInDialogue => isInDialogue;
        public NPCController CurrentNPC => currentNPC;
        public DialogueNode CurrentNode => currentNode;
        
        // === Unity Lifecycle ===
        
        private void Update()
        {
            if (isTyping)
            {
                ProcessTyping();
            }
        }
        
        // === Dialogue Control ===
        
        /// <summary>
        /// Начать диалог с NPC.
        /// </summary>
        public bool StartDialogue(NPCController npc, string startNodeId)
        {
            if (isInDialogue || npc == null)
                return false;
            
            currentNPC = npc;
            isInDialogue = true;
            
            OnDialogueStateChanged?.Invoke(true);
            
            // Загружаем начальный узел
            return GoToNode(startNodeId);
        }
        
        /// <summary>
        /// Завершить текущий диалог.
        /// </summary>
        public void EndDialogue()
        {
            if (!isInDialogue) return;
            
            isInDialogue = false;
            currentNPC = null;
            currentNode = null;
            
            OnDialogueStateChanged?.Invoke(false);
            OnDialogueEnded?.Invoke();
        }
        
        /// <summary>
        /// Перейти к узлу диалога.
        /// </summary>
        public bool GoToNode(string nodeId)
        {
            if (!isInDialogue || string.IsNullOrEmpty(nodeId))
                return false;
            
            // Ищем узел
            if (!dialogueNodes.TryGetValue(nodeId, out DialogueNode node))
            {
                // Пробуем загрузить из ресурсов NPC
                node = LoadDialogueNode(nodeId);
                if (node == null)
                {
                    Debug.LogWarning($"Dialogue node not found: {nodeId}");
                    return false;
                }
                dialogueNodes[nodeId] = node;
            }
            
            currentNode = node;
            
            // Выполняем действия узла
            ExecuteActions(node.Actions);
            
            // Уведомляем о смене узла
            OnNodeChanged?.Invoke(node);
            
            // Начинаем печать текста
            StartTyping(node.Text);
            
            // Если есть выборы — показываем их
            if (node.Choices != null && node.Choices.Count > 0)
            {
                UpdateAvailableChoices();
            }
            
            // Проверяем на конечный узел
            if (node.IsEndNode)
            {
                EndDialogue();
            }
            
            return true;
        }
        
        /// <summary>
        /// Выбрать вариант ответа.
        /// </summary>
        public bool SelectChoice(int choiceIndex)
        {
            if (!isInDialogue || currentNode == null)
                return false;
            
            if (choiceIndex < 0 || choiceIndex >= currentNode.Choices.Count)
                return false;
            
            DialogueChoice choice = currentNode.Choices[choiceIndex];
            
            // Применяем эффекты выбора
            ApplyChoiceEffects(choice);
            
            // Переходим к следующему узлу
            if (!string.IsNullOrEmpty(choice.NextNodeId))
            {
                return GoToNode(choice.NextNodeId);
            }
            else
            {
                EndDialogue();
                return true;
            }
        }
        
        /// <summary>
        /// Продолжить диалог (без выбора).
        /// </summary>
        public bool Continue()
        {
            if (!isInDialogue || currentNode == null)
                return false;
            
            // Если текст печатается — завершаем печать
            if (isTyping)
            {
                CompleteTyping();
                return true;
            }
            
            // Если есть автопереход
            if (!string.IsNullOrEmpty(currentNode.NextNodeId))
            {
                return GoToNode(currentNode.NextNodeId);
            }
            
            // Если нет выборов и нет автоперехода — завершаем
            if (currentNode.Choices == null || currentNode.Choices.Count == 0)
            {
                EndDialogue();
            }
            
            return true;
        }
        
        // === Typing Effect ===
        
        private void StartTyping(string text)
        {
            currentText = "";
            typingQueue.Clear();
            
            foreach (char c in text)
            {
                typingQueue.Enqueue(c);
            }
            
            isTyping = true;
        }
        
        private void ProcessTyping()
        {
            if (typingQueue.Count == 0)
            {
                isTyping = false;
                return;
            }
            
            // Добавляем символы с задержкой
            if (Time.deltaTime >= typingSpeed)
            {
                int charsToAdd = Mathf.CeilToInt(Time.deltaTime / typingSpeed);
                for (int i = 0; i < charsToAdd && typingQueue.Count > 0; i++)
                {
                    currentText += typingQueue.Dequeue();
                }
                OnTextUpdated?.Invoke(currentText);
            }
        }
        
        private void CompleteTyping()
        {
            while (typingQueue.Count > 0)
            {
                currentText += typingQueue.Dequeue();
            }
            isTyping = false;
            OnTextUpdated?.Invoke(currentText);
        }
        
        // === Choices ===
        
        private void UpdateAvailableChoices()
        {
            if (currentNode == null || currentNode.Choices == null) return;
            
            List<DialogueChoice> available = new List<DialogueChoice>();
            
            foreach (var choice in currentNode.Choices)
            {
                if (choice.IsAvailable(dialogueFlags))
                {
                    available.Add(choice);
                }
            }
            
            OnChoicesUpdated?.Invoke(available);
        }
        
        // === Effects ===
        
        private void ApplyChoiceEffects(DialogueChoice choice)
        {
            // Изменение отношений
            if (choice.RelationshipChange != 0 && currentNPC != null)
            {
                // Получаем ID игрока (реализуется при интеграции с PlayerSystem)
                string playerId = "player";
                currentNPC.ModifyRelationship(playerId, choice.RelationshipChange);
            }
            
            // Установка флагов
            if (choice.SetFlags != null)
            {
                foreach (string flag in choice.SetFlags)
                {
                    dialogueFlags[flag] = true;
                }
            }
        }
        
        // === Actions ===
        
        private void ExecuteActions(List<DialogueAction> actions)
        {
            if (actions == null) return;
            
            foreach (var action in actions)
            {
                ExecuteAction(action);
            }
        }
        
        private void ExecuteAction(DialogueAction action)
        {
            switch (action.Type)
            {
                case DialogueActionType.SetFlag:
                    dialogueFlags[action.StringValue] = true;
                    break;
                    
                case DialogueActionType.ClearFlag:
                    dialogueFlags.Remove(action.StringValue);
                    break;
                    
                case DialogueActionType.ModifyRelationship:
                    if (currentNPC != null)
                    {
                        currentNPC.ModifyRelationship("player", action.IntValue);
                    }
                    break;
                    
                case DialogueActionType.ModifyQi:
                    // Интеграция с QiController
                    break;
                    
                case DialogueActionType.ModifyHealth:
                    // Интеграция с BodyController
                    break;
                    
                default:
                    OnActionTriggered?.Invoke(action);
                    break;
            }
        }
        
        // === Loading ===
        
        // FIX DLG-M04: Implement LoadDialogueNode instead of stub (2026-04-11)
        /// <summary>
        /// Load a dialogue node by ID from Resources or cached data.
        /// Tries to load from: 1) already loaded nodes, 2) Resources folder, 3) NPC dialogue assets.
        /// </summary>
        private DialogueNode LoadDialogueNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            
            // Already loaded?
            if (dialogueNodes.TryGetValue(nodeId, out DialogueNode existing))
            {
                return existing;
            }
            
            // Try loading from Resources/Dialogues/{nodeId}
            // Dialogue assets can be stored as JSON files in Resources
            TextAsset jsonAsset = Resources.Load<TextAsset>($"{dialogueResourcePath}/{nodeId}");
            if (jsonAsset != null)
            {
                try
                {
                    // Try wrapper format first
                    var wrapper = JsonUtility.FromJson<DialogueNodeArrayWrapper>(jsonAsset.text);
                    if (wrapper?.nodes != null && wrapper.nodes.Length > 0)
                    {
                        // Register all nodes from the file
                        foreach (var node in wrapper.nodes)
                        {
                            if (node != null && !string.IsNullOrEmpty(node.NodeId))
                            {
                                dialogueNodes[node.NodeId] = node;
                            }
                        }
                        // Return the requested node
                        if (dialogueNodes.TryGetValue(nodeId, out DialogueNode foundNode))
                        {
                            return foundNode;
                        }
                        // If not found by ID, return first node
                        return wrapper.nodes[0];
                    }
                    
                    // Try single node format
                    DialogueNode singleNode = JsonUtility.FromJson<DialogueNode>(jsonAsset.text);
                    if (singleNode != null)
                    {
                        dialogueNodes[singleNode.NodeId] = singleNode;
                        return singleNode;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Dialogue] Failed to parse dialogue JSON for {nodeId}: {e.Message}");
                }
            }
            
            return null;
        }
        
        // FIX DLG-C01: LoadDialogueFromJson — wrap array in wrapper object for JsonUtility (2026-04-11)
        /// <summary>
        /// Загрузить диалог из JSON.
        /// JsonUtility.FromJson cannot deserialize arrays at root level.
        /// Wraps the JSON array in a wrapper object: {"nodes": [...]}.
        /// Also supports JSON that is already wrapped.
        /// </summary>
        public void LoadDialogueFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            
            try
            {
                string trimmedJson = json.Trim();
                
                // Check if JSON starts with '[' — it's a raw array, needs wrapping
                if (trimmedJson.StartsWith("["))
                {
                    // Wrap in object for JsonUtility: {"nodes": [...]}
                    string wrappedJson = "{\"nodes\":" + trimmedJson + "}";
                    DialogueNodeArrayWrapper wrapper = JsonUtility.FromJson<DialogueNodeArrayWrapper>(wrappedJson);
                    
                    if (wrapper?.nodes != null)
                    {
                        foreach (var node in wrapper.nodes)
                        {
                            if (node != null && !string.IsNullOrEmpty(node.NodeId))
                            {
                                dialogueNodes[node.NodeId] = node;
                            }
                        }
                        Debug.Log($"[Dialogue] Loaded {wrapper.nodes.Length} nodes from JSON array");
                    }
                }
                else
                {
                    // JSON is already an object — try wrapper format first
                    DialogueNodeArrayWrapper wrapper = JsonUtility.FromJson<DialogueNodeArrayWrapper>(trimmedJson);
                    if (wrapper?.nodes != null && wrapper.nodes.Length > 0)
                    {
                        foreach (var node in wrapper.nodes)
                        {
                            if (node != null && !string.IsNullOrEmpty(node.NodeId))
                            {
                                dialogueNodes[node.NodeId] = node;
                            }
                        }
                        Debug.Log($"[Dialogue] Loaded {wrapper.nodes.Length} nodes from wrapped JSON");
                    }
                    else
                    {
                        // Try single node format
                        DialogueNode singleNode = JsonUtility.FromJson<DialogueNode>(trimmedJson);
                        if (singleNode != null && !string.IsNullOrEmpty(singleNode.NodeId))
                        {
                            dialogueNodes[singleNode.NodeId] = singleNode;
                            Debug.Log($"[Dialogue] Loaded single node: {singleNode.NodeId}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load dialogue: {e.Message}");
            }
        }
        
        // === Flags ===
        
        public void SetFlag(string flag, bool value)
        {
            dialogueFlags[flag] = value;
        }
        
        public bool HasFlag(string flag)
        {
            return dialogueFlags.TryGetValue(flag, out bool value) && value;
        }
        
        public void ClearFlag(string flag)
        {
            dialogueFlags.Remove(flag);
        }
    }
}
