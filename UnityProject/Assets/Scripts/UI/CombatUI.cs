// ============================================================================
// CombatUI.cs — Боевой интерфейс
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: Camera.main null guards, Input note
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    /// <summary>
    /// Боевой интерфейс.
    /// Отображает HP, Ци, техники, лог боя.
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        #region UI References

        [Header("Player Stats")]
        [Tooltip("Полоска HP игрока")]
        public ProgressBar playerHealthBar;

        [Tooltip("Полоска Ци игрока")]
        public ProgressBar playerQiBar;

        [Tooltip("Текст уровня культивации")]
        public TMP_Text playerLevelText;

        [Tooltip("Имя игрока")]
        public TMP_Text playerNameText;

        [Header("Enemy Stats")]
        [Tooltip("Полоска HP врага")]
        public ProgressBar enemyHealthBar;

        [Tooltip("Полоска Ци врага")]
        public ProgressBar enemyQiBar;

        [Tooltip("Имя врага")]
        public TMP_Text enemyNameText;

        [Tooltip("Контейнер для врагов (для множества)")]
        public Transform enemiesContainer;

        [Tooltip("Префаб полоски врага")]
        public GameObject enemyBarPrefab;

        [Header("Techniques Panel")]
        [Tooltip("Контейнер техник")]
        public Transform techniquesContainer;

        [Tooltip("Префаб кнопки техники")]
        public GameObject techniqueButtonPrefab;

        [Tooltip("Панель техник")]
        public GameObject techniquesPanel;

        [Header("Combat Log")]
        [Tooltip("Контейнер лога")]
        public Transform logContainer;

        [Tooltip("Префаб сообщения лога")]
        public GameObject logEntryPrefab;

        [Tooltip("ScrollRect лога")]
        public ScrollRect logScrollRect;

        [Tooltip("Максимум сообщений в логе")]
        public int maxLogEntries = 50;

        [Header("Combat State")]
        [Tooltip("Панель состояния боя")]
        public GameObject combatStatePanel;

        [Tooltip("Текст состояния")]
        public TMP_Text stateText;

        [Tooltip("Текст хода")]
        public TMP_Text turnText;

        [Header("Action Buttons")]
        [Tooltip("Кнопка атаки")]
        public Button attackButton;

        [Tooltip("Кнопка защиты")]
        public Button defendButton;

        [Tooltip("Кнопка техник")]
        public Button techniquesButton;

        [Tooltip("Кнопка побега")]
        public Button fleeButton;

        [Header("Turn Order")]
        [Tooltip("Контейнер порядка ходов")]
        public Transform turnOrderContainer;

        [Tooltip("Префаб иконки порядка")]
        public GameObject turnIconPrefab;

        [Header("Damage Numbers")]
        [Tooltip("Контейнер чисел урона")]
        public Transform damageNumbersContainer;

        [Tooltip("Префаб числа урона")]
        public GameObject damageNumberPrefab;

        #endregion

        #region Runtime Data

        // Текущие данные игрока
        private CombatantData playerData;
        private CombatantData enemyData;

        // Список врагов
        private List<EnemyUIEntry> enemies = new List<EnemyUIEntry>();

        // Кнопки техник
        private List<TechniqueButtonEntry> techniqueButtons = new List<TechniqueButtonEntry>();

        // Лог боя
        private Queue<LogEntry> combatLog = new Queue<LogEntry>();

        // Состояние
        private CombatStage currentStage = CombatStage.None;
        private int currentTurn = 0;
        private bool isPlayerTurn = false;

        #endregion

        #region Events

        public event Action OnAttackClicked;
        public event Action OnDefendClicked;
        public event Action<int> OnTechniqueClicked;
        public event Action OnFleeClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
            SetupButtonListeners();
        }

        private void Start()
        {
            HideCombatUI();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Скрываем всё
            if (playerHealthBar != null) playerHealthBar.gameObject.SetActive(false);
            if (playerQiBar != null) playerQiBar.gameObject.SetActive(false);
            if (enemyHealthBar != null) enemyHealthBar.gameObject.SetActive(false);
            if (techniquesPanel != null) techniquesPanel.SetActive(false);
            if (combatStatePanel != null) combatStatePanel.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (attackButton != null)
                attackButton.onClick.AddListener(() => OnAttackClicked?.Invoke());

            if (defendButton != null)
                defendButton.onClick.AddListener(() => OnDefendClicked?.Invoke());

            if (techniquesButton != null)
                techniquesButton.onClick.AddListener(ToggleTechniquesPanel);

            if (fleeButton != null)
                fleeButton.onClick.AddListener(() => OnFleeClicked?.Invoke());
        }

        #endregion

        #region Show/Hide

        /// <summary>
        /// Показывает боевой интерфейс
        /// </summary>
        public void ShowCombatUI()
        {
            gameObject.SetActive(true);

            if (playerHealthBar != null) playerHealthBar.gameObject.SetActive(true);
            if (playerQiBar != null) playerQiBar.gameObject.SetActive(true);
            if (enemyHealthBar != null) enemyHealthBar.gameObject.SetActive(true);
            if (combatStatePanel != null) combatStatePanel.SetActive(true);

            SetInteractable(true);
        }

        /// <summary>
        /// Скрывает боевой интерфейс
        /// </summary>
        public void HideCombatUI()
        {
            gameObject.SetActive(false);
            ClearEnemies();
            ClearTechniques();
            ClearLog();
        }

        /// <summary>
        /// Устанавливает интерактивность кнопок
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (attackButton != null) attackButton.interactable = interactable;
            if (defendButton != null) defendButton.interactable = interactable;
            if (techniquesButton != null) techniquesButton.interactable = interactable;
            if (fleeButton != null) fleeButton.interactable = interactable;
        }

        #endregion

        #region Player Update

        /// <summary>
        /// Обновляет данные игрока
        /// </summary>
        public void UpdatePlayerData(CombatantData data)
        {
            playerData = data;

            if (playerHealthBar != null)
            {
                playerHealthBar.SetValues(data.currentHealth, data.maxHealth);
                playerHealthBar.SetColor(GetHealthColor(data.currentHealth, data.maxHealth));
            }

            if (playerQiBar != null)
            {
                playerQiBar.SetValues(data.currentQi, data.maxQi);
            }

            if (playerNameText != null)
            {
                playerNameText.text = data.name;
            }

            if (playerLevelText != null)
            {
                playerLevelText.text = $"Lv.{(int)data.cultivationLevel}";
            }
        }

        /// <summary>
        /// Обновляет HP игрока с анимацией
        /// </summary>
        public void UpdatePlayerHealth(int current, int max, bool animate = true)
        {
            if (playerData != null)
            {
                playerData.currentHealth = current;
                playerData.maxHealth = max;
            }

            if (playerHealthBar != null)
            {
                if (animate)
                    playerHealthBar.AnimateTo(current, max);
                else
                    playerHealthBar.SetValues(current, max);
            }
        }

        /// <summary>
        /// Обновляет Ци игрока
        /// </summary>
        public void UpdatePlayerQi(float current, float max)
        {
            if (playerData != null)
            {
                playerData.currentQi = current;
                playerData.maxQi = max;
            }

            if (playerQiBar != null)
            {
                playerQiBar.SetValues(current, max);
            }
        }

        #endregion

        #region Enemy Update

        /// <summary>
        /// Устанавливает данные врага
        /// </summary>
        public void SetEnemyData(CombatantData data)
        {
            enemyData = data;

            if (enemyHealthBar != null)
            {
                enemyHealthBar.gameObject.SetActive(true);
                enemyHealthBar.SetValues(data.currentHealth, data.maxHealth);
            }

            if (enemyNameText != null)
            {
                enemyNameText.text = data.name;
            }
        }

        /// <summary>
        /// Добавляет врага (для группового боя)
        /// </summary>
        public void AddEnemy(CombatantData data, int index)
        {
            if (enemiesContainer == null || enemyBarPrefab == null)
                return;

            var entry = Instantiate(enemyBarPrefab, enemiesContainer).GetComponent<EnemyUIEntry>();
            if (entry != null)
            {
                entry.Initialize(data, index);
                entry.OnSelected += (i) => SelectEnemy(i);
                enemies.Add(entry);
            }
        }

        /// <summary>
        /// Обновляет HP врага
        /// </summary>
        public void UpdateEnemyHealth(int enemyIndex, int current, int max)
        {
            if (enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                // Одиночный враг
                if (enemyHealthBar != null)
                    enemyHealthBar.AnimateTo(current, max);
                return;
            }

            enemies[enemyIndex].UpdateHealth(current, max);
        }

        /// <summary>
        /// Удаляет врага
        /// </summary>
        public void RemoveEnemy(int enemyIndex)
        {
            if (enemyIndex >= 0 && enemyIndex < enemies.Count)
            {
                var entry = enemies[enemyIndex];
                enemies.RemoveAt(enemyIndex);
                Destroy(entry.gameObject);
            }
        }

        /// <summary>
        /// Очищает список врагов
        /// </summary>
        public void ClearEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }
            enemies.Clear();
        }

        private void SelectEnemy(int index)
        {
            foreach (var enemy in enemies)
            {
                enemy.SetSelected(enemy.EnemyIndex == index);
            }
        }

        #endregion

        #region Techniques

        /// <summary>
        /// Устанавливает список техник
        /// </summary>
        public void SetTechniques(List<TechniqueUIData> techniques)
        {
            ClearTechniques();

            if (techniquesContainer == null || techniqueButtonPrefab == null)
                return;

            for (int i = 0; i < techniques.Count; i++)
            {
                var technique = techniques[i];
                var button = Instantiate(techniqueButtonPrefab, techniquesContainer);

                var entry = button.GetComponent<TechniqueButtonEntry>();
                if (entry != null)
                {
                    entry.Initialize(technique, i);
                    int index = i;
                    entry.OnClicked += () => OnTechniqueClicked?.Invoke(index);
                    techniqueButtons.Add(entry);
                }
            }
        }

        /// <summary>
        /// Обновляет доступность техники
        /// </summary>
        public void UpdateTechniqueAvailability(int index, bool canUse, string reason = "")
        {
            if (index >= 0 && index < techniqueButtons.Count)
            {
                techniqueButtons[index].SetAvailable(canUse, reason);
            }
        }

        /// <summary>
        /// Показывает/скрывает панель техник
        /// </summary>
        public void ToggleTechniquesPanel()
        {
            if (techniquesPanel != null)
            {
                techniquesPanel.SetActive(!techniquesPanel.activeSelf);
            }
        }

        private void ClearTechniques()
        {
            foreach (var button in techniqueButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            techniqueButtons.Clear();
        }

        #endregion

        #region Combat Log

        /// <summary>
        /// Добавляет сообщение в лог
        /// </summary>
        public void AddLogEntry(string message, LogType logType = LogType.Normal)
        {
            var entry = new LogEntry
            {
                message = message,
                logType = logType,
                timestamp = DateTime.Now
            };

            combatLog.Enqueue(entry);

            // Удаляем старые записи
            while (combatLog.Count > maxLogEntries)
            {
                combatLog.Dequeue();

                // Удаляем старый UI элемент
                if (logContainer != null && logContainer.childCount > maxLogEntries)
                {
                    Destroy(logContainer.GetChild(0).gameObject);
                }
            }

            // Создаём UI элемент
            if (logContainer != null && logEntryPrefab != null)
            {
                var logGO = Instantiate(logEntryPrefab, logContainer);
                var text = logGO.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = FormatLogMessage(entry);
                    text.color = GetLogColor(logType);
                }

                // Прокрутка вниз
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        /// <summary>
        /// Добавляет сообщение об атаке
        /// </summary>
        // FIX CORE-M03: AttackResult→CombatAttackResult (коллизия с CombatManager.AttackResult struct)
        public void LogAttack(string attacker, string target, int damage, CombatAttackResult result)
        {
            string resultText = result switch
            {
                CombatAttackResult.Miss => $"{attacker} промахивается по {target}!",
                CombatAttackResult.Dodge => $"{target} уклоняется от атаки {attacker}!",
                CombatAttackResult.Parry => $"{target} парирует атаку {attacker}!",
                CombatAttackResult.Block => $"{attacker} бьёт {target} (блок) — {damage} урона",
                CombatAttackResult.Hit => $"{attacker} попадает по {target} — {damage} урона",
                CombatAttackResult.CriticalHit => $"{attacker} критически попадает по {target} — {damage} урона!",
                CombatAttackResult.Kill => $"{attacker} побеждает {target}!",
                _ => $"{attacker} атакует {target}"
            };

            LogType type = result == CombatAttackResult.CriticalHit || result == CombatAttackResult.Kill
                ? LogType.Important
                : LogType.Normal;

            AddLogEntry(resultText, type);
        }

        /// <summary>
        /// Добавляет сообщение о технике
        /// </summary>
        public void LogTechnique(string user, string technique, string target, int damage)
        {
            AddLogEntry($"{user} использует {technique} на {target} — {damage} урона!", LogType.Technique);
        }

        /// <summary>
        /// Очищает лог
        /// </summary>
        public void ClearLog()
        {
            combatLog.Clear();

            if (logContainer != null)
            {
                for (int i = logContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(logContainer.GetChild(i).gameObject);
                }
            }
        }

        private string FormatLogMessage(LogEntry entry)
        {
            return $"[{entry.timestamp:HH:mm}] {entry.message}";
        }

        private Color GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Important => Color.yellow,
                LogType.Technique => Color.cyan,
                LogType.Damage => Color.red,
                LogType.Heal => Color.green,
                LogType.System => Color.gray,
                _ => Color.white
            };
        }

        #endregion

        #region Combat State

        /// <summary>
        /// Устанавливает стадию боя
        /// </summary>
        public void SetCombatStage(CombatStage stage)
        {
            currentStage = stage;

            if (stateText != null)
            {
                stateText.text = stage switch
                {
                    CombatStage.None => "",
                    CombatStage.Initiative => "Определение инициативы...",
                    CombatStage.PlayerTurn => "Ваш ход",
                    CombatStage.EnemyTurn => "Ход противника",
                    CombatStage.Resolution => "Разрешение...",
                    CombatStage.Victory => "Победа!",
                    CombatStage.Defeat => "Поражение",
                    _ => ""
                };
            }

            // Управляем интерактивностью
            SetInteractable(stage == CombatStage.PlayerTurn);

            // Показываем/скрываем панель техник
            if (stage != CombatStage.PlayerTurn && techniquesPanel != null)
            {
                techniquesPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Устанавливает номер хода
        /// </summary>
        public void SetTurn(int turn, bool isPlayerTurn)
        {
            currentTurn = turn;
            this.isPlayerTurn = isPlayerTurn;

            if (turnText != null)
            {
                turnText.text = $"Ход {turn}";
            }
        }

        #endregion

        #region Damage Numbers

        /// <summary>
        /// Показывает число урона
        /// </summary>
        public void ShowDamageNumber(Vector3 worldPosition, int damage, DamageType damageType = DamageType.Physical)
        {
            if (damageNumbersContainer == null || damageNumberPrefab == null)
                return;

            // FIX UI-L02: Camera.main null guard (2026-04-12)
            var cam = Camera.main;
            if (cam == null) return;

            var screenPos = cam.WorldToScreenPoint(worldPosition);
            var damageGO = Instantiate(damageNumberPrefab, damageNumbersContainer);
            damageGO.transform.position = screenPos;

            var text = damageGO.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = damage > 0 ? $"-{damage}" : $"+{Mathf.Abs(damage)}";
                text.color = GetDamageColor(damageType);
            }

            // Автоудаление через анимацию
            Destroy(damageGO, 1.5f);
        }

        /// <summary>
        /// Показывает число исцеления
        /// </summary>
        public void ShowHealNumber(Vector3 worldPosition, int amount)
        {
            if (damageNumbersContainer == null || damageNumberPrefab == null)
                return;

            // FIX UI-L02: Camera.main null guard (2026-04-12)
            var cam = Camera.main;
            if (cam == null) return;

            var screenPos = cam.WorldToScreenPoint(worldPosition);
            var healGO = Instantiate(damageNumberPrefab, damageNumbersContainer);
            healGO.transform.position = screenPos;

            var text = healGO.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = $"+{amount}";
                text.color = Color.green;
            }

            Destroy(healGO, 1.5f);
        }

        private Color GetDamageColor(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => Color.red,
                DamageType.Qi => new Color(0.5f, 0.8f, 1f), // Голубой
                DamageType.Elemental => Color.magenta,
                DamageType.Pure => Color.white,
                DamageType.Void => new Color(0.5f, 0f, 0.8f), // Фиолетовый
                _ => Color.red
            };
        }

        #endregion

        #region Turn Order

        /// <summary>
        /// Устанавливает порядок ходов
        /// </summary>
        public void SetTurnOrder(List<TurnOrderEntry> order)
        {
            if (turnOrderContainer == null || turnIconPrefab == null)
                return;

            // Очищаем старые
            for (int i = turnOrderContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(turnOrderContainer.GetChild(i).gameObject);
            }

            // Создаём новые
            for (int i = 0; i < order.Count; i++)
            {
                var entry = order[i];
                var icon = Instantiate(turnIconPrefab, turnOrderContainer);

                var image = icon.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = entry.icon;
                    image.color = entry.isPlayer ? Color.green : Color.red;
                }

                // Подсветка текущего
                if (i == 0)
                {
                    icon.transform.localScale = Vector3.one * 1.2f;
                }
            }
        }

        #endregion

        #region Helpers

        private Color GetHealthColor(float current, float max)
        {
            float percent = current / max;

            if (percent > 0.6f) return Color.green;
            if (percent > 0.3f) return Color.yellow;
            return Color.red;
        }

        #endregion
    }

    // ============================================================================
    // Data Classes
    // ============================================================================

    [Serializable]
    public class CombatantData
    {
        public string name;
        public int currentHealth;
        public int maxHealth;
        public float currentQi;
        public float maxQi;
        public CultivationLevel cultivationLevel;
        public Sprite portrait;
    }

    [Serializable]
    public class TechniqueUIData
    {
        public string techniqueId;
        public string name;
        public string description;
        public int qiCost;
        public Sprite icon;
        public bool isAvailable;
        public string unavailableReason;
    }

    [Serializable]
    public class TurnOrderEntry
    {
        public string id;
        public string name;
        public Sprite icon;
        public bool isPlayer;
        public int initiative;
    }

    [Serializable]
    public class LogEntry
    {
        public string message;
        public LogType logType;
        public DateTime timestamp;
    }

    public enum LogType
    {
        Normal,
        Important,
        Technique,
        Damage,
        Heal,
        System
    }

    // ============================================================================
    // UI Component Helpers
    // ============================================================================

    /// <summary>
    /// Прогресс-бар для HP/Ци
    /// </summary>
    [Serializable]
    public class ProgressBar : MonoBehaviour
    {
        public Image fillImage;
        public TMP_Text valueText;
        public float animationSpeed = 2f;

        private float targetValue;
        private float targetMax;
        private Coroutine animationCoroutine;

        public void SetValues(float current, float max)
        {
            if (fillImage != null)
                fillImage.fillAmount = max > 0 ? current / max : 0;

            if (valueText != null)
                valueText.text = $"{Mathf.Ceil(current)}/{max}";
        }

        public void AnimateTo(float current, float max)
        {
            targetValue = current;
            targetMax = max;

            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);

            animationCoroutine = StartCoroutine(AnimateFill());
        }

        private System.Collections.IEnumerator AnimateFill()
        {
            float startFill = fillImage.fillAmount;
            float targetFill = targetMax > 0 ? targetValue / targetMax : 0;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * animationSpeed;
                fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed);

                if (valueText != null)
                {
                    float displayValue = Mathf.Lerp(startFill * targetMax, targetValue, elapsed);
                    valueText.text = $"{Mathf.Ceil(displayValue)}/{targetMax}";
                }

                yield return null;
            }

            SetValues(targetValue, targetMax);
        }

        public void SetColor(Color color)
        {
            if (fillImage != null)
                fillImage.color = color;
        }
    }

    /// <summary>
    /// Запись врага в UI
    /// </summary>
    public class EnemyUIEntry : MonoBehaviour
    {
        public Image portraitImage;
        public ProgressBar healthBar;
        public TMP_Text nameText;
        public GameObject selectionBorder;

        public int EnemyIndex { get; private set; }
        public event Action<int> OnSelected;

        private Button button;

        public void Initialize(CombatantData data, int index)
        {
            EnemyIndex = index;

            if (nameText != null) nameText.text = data.name;
            if (portraitImage != null) portraitImage.sprite = data.portrait;
            if (healthBar != null) healthBar.SetValues(data.currentHealth, data.maxHealth);

            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => OnSelected?.Invoke(EnemyIndex));
        }

        public void UpdateHealth(int current, int max)
        {
            if (healthBar != null)
                healthBar.AnimateTo(current, max);
        }

        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
                selectionBorder.SetActive(selected);
        }
    }

    /// <summary>
    /// Кнопка техники
    /// </summary>
    public class TechniqueButtonEntry : MonoBehaviour
    {
        public Image iconImage;
        public TMP_Text nameText;
        public TMP_Text costText;
        public GameObject unavailableOverlay;
        public TMP_Text unavailableReasonText;

        public int TechniqueIndex { get; private set; }
        public event Action OnClicked;

        private Button button;

        public void Initialize(TechniqueUIData data, int index)
        {
            TechniqueIndex = index;

            if (nameText != null) nameText.text = data.name;
            if (iconImage != null) iconImage.sprite = data.icon;
            if (costText != null) costText.text = $"Ци: {data.qiCost}";

            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => OnClicked?.Invoke());

            SetAvailable(data.isAvailable, data.unavailableReason);
        }

        public void SetAvailable(bool available, string reason = "")
        {
            if (button != null)
                button.interactable = available;

            if (unavailableOverlay != null)
                unavailableOverlay.SetActive(!available);

            if (unavailableReasonText != null && !available)
                unavailableReasonText.text = reason;
        }
    }
}
