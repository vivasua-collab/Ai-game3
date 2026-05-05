// ============================================================================
// CombatUI.cs — Боевой интерфейс
// Cultivation World Simulator
// Создано: 2026-04-03
// Редактировано: 2026-04-11 06:38:02 UTC — UI-L02: Camera.main null guards, CORE-M03: AttackResult→CombatAttackResult
// Редактировано: 2026-05-04 07:20:00 UTC — ФАЗА 6: Полоска накачки, слоты техник, индикатор прерывания
// Редактировано: 2026-05-07 11:00:00 UTC — ФАЗА 4: Оружие в боевом интерфейсе

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Combat;

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

        // === ФАЗА 6: Система накачки техник ===

        [Header("Накачка техник (ФАЗА 6)")]
        [Tooltip("Панель накачки (показывается при зарядке)")]
        public GameObject chargePanel;

        [Tooltip("Полоска прогресса накачки")]
        public ProgressBar chargeProgressBar;

        [Tooltip("Имя накачиваемой техники")]
        public TMP_Text chargeTechniqueNameText;

        [Tooltip("Текст вложенного/требуемого Ци")]
        public TMP_Text chargeQiText;

        [Tooltip("Индикатор прерывания (мигает при опасности)")]
        public GameObject interruptWarningIndicator;

        [Tooltip("Скорость вливания Ци (Ци/сек)")]
        public TMP_Text chargeRateText;

        [Header("Слоты техник (ФАЗА 6)")]
        [Tooltip("Массив иконок слотов техник (1-9)")]
        public TechniqueSlotUI[] techniqueSlots = new TechniqueSlotUI[9];

        // ФАЗА 4: Отображение оружия в бою
        [Header("Оружие (ФАЗА 4)")]
        [Tooltip("Иконка оружия в основной руке")]
        public Image weaponMainIcon;

        [Tooltip("Иконка оружия/щита во второй руке")]
        public Image weaponOffIcon;

        [Tooltip("Текст названия + урон оружия")]
        public TMP_Text weaponMainText;

        [Tooltip("Текст названия + защита оружия")]
        public TMP_Text weaponOffText;

        [Tooltip("Панель оружия (показывается в бою)")]
        public GameObject weaponPanel;

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

        // ФАЗА 6: Состояние накачки
        private bool isChargeActive = false;
        private float interruptFlashTimer = 0f;
        private const float INTERRUPT_FLASH_INTERVAL = 0.3f;

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
            HideChargeBar();
        }

        private void Update()
        {
            // ФАЗА 6: Мигание индикатора прерывания
            if (isChargeActive && interruptWarningIndicator != null)
            {
                interruptFlashTimer += Time.deltaTime;
                if (interruptFlashTimer >= INTERRUPT_FLASH_INTERVAL)
                {
                    interruptFlashTimer = 0f;
                    interruptWarningIndicator.SetActive(!interruptWarningIndicator.activeSelf);
                }
            }
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

            // ФАЗА 4: Показать панель оружия
            if (weaponPanel != null) weaponPanel.SetActive(true);

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

            // ФАЗА 4: Скрыть панель оружия
            if (weaponPanel != null) weaponPanel.SetActive(false);
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

        #region ФАЗА 6: Накачка техник

        /// <summary>
        /// Обновить прогресс накачки техники.
        /// Вызывается каждый кадр во время зарядки.
        /// </summary>
        /// <param name="progress">Прогресс 0.0-1.0</param>
        /// <param name="qiCharged">Вложенное Ци</param>
        /// <param name="qiTotal">Требуемое Ци</param>
        public void UpdateChargeProgress(float progress, long qiCharged, long qiTotal)
        {
            if (chargeProgressBar != null)
            {
                chargeProgressBar.SetValues(progress * 100f, 100f);

                // Цвет: жёлтый → оранжевый → зелёный по мере зарядки
                if (progress < 0.3f)
                    chargeProgressBar.SetColor(Color.yellow);
                else if (progress < 0.7f)
                    chargeProgressBar.SetColor(new Color(1f, 0.6f, 0f)); // Оранжевый
                else
                    chargeProgressBar.SetColor(Color.green);
            }

            if (chargeQiText != null)
            {
                chargeQiText.text = $"{qiCharged}/{qiTotal} Ци";
            }
        }

        /// <summary>
        /// Показать полоску накачки.
        /// </summary>
        /// <param name="techniqueName">Имя техники</param>
        public void ShowChargeBar(string techniqueName)
        {
            isChargeActive = true;
            interruptFlashTimer = 0f;

            if (chargePanel != null)
                chargePanel.SetActive(true);

            if (chargeTechniqueNameText != null)
                chargeTechniqueNameText.text = techniqueName;

            if (chargeProgressBar != null)
            {
                chargeProgressBar.SetValues(0f, 100f);
                chargeProgressBar.SetColor(Color.yellow);
            }

            if (interruptWarningIndicator != null)
                interruptWarningIndicator.SetActive(false);

            // Добавляем в лог
            AddLogEntry($"Накачка: {techniqueName}...", LogType.Technique);
        }

        /// <summary>
        /// Скрыть полоску накачки.
        /// </summary>
        public void HideChargeBar()
        {
            isChargeActive = false;

            if (chargePanel != null)
                chargePanel.SetActive(false);

            if (interruptWarningIndicator != null)
                interruptWarningIndicator.SetActive(false);
        }

        /// <summary>
        /// Мигание индикатора прерывания — техника может быть прервана!
        /// </summary>
        public void FlashInterruptWarning()
        {
            if (interruptWarningIndicator != null)
            {
                interruptWarningIndicator.SetActive(true);
                interruptFlashTimer = 0f;
            }
        }

        /// <summary>
        /// Установить состояние слота техники.
        /// </summary>
        /// <param name="slot">Номер слота 0-8</param>
        /// <param name="state">Состояние слота</param>
        public void SetTechniqueSlotState(int slot, TechniqueSlotState state)
        {
            if (slot < 0 || slot >= techniqueSlots.Length) return;
            if (techniqueSlots[slot] == null) return;

            techniqueSlots[slot].SetState(state);
        }

        /// <summary>
        /// Обновить кулдаун слота техники.
        /// </summary>
        /// <param name="slot">Номер слота 0-8</param>
        /// <param name="remaining">Оставшееся время (сек)</param>
        /// <param name="total">Общее время кулдауна (сек)</param>
        public void UpdateTechniqueSlotCooldown(int slot, float remaining, float total)
        {
            if (slot < 0 || slot >= techniqueSlots.Length) return;
            if (techniqueSlots[slot] == null) return;

            techniqueSlots[slot].UpdateCooldown(remaining, total);
        }

        /// <summary>
        /// Инициализировать слоты техник из TechniqueController.
        /// </summary>
        public void InitializeTechniqueSlots(TechniqueController techController)
        {
            if (techController == null) return;

            for (int i = 0; i < 9 && i < techniqueSlots.Length; i++)
            {
                if (techniqueSlots[i] == null) continue;

                var tech = techController.GetQuickSlotTechnique(i);
                if (tech != null && tech.Data != null)
                {
                    techniqueSlots[i].Initialize(
                        tech.Data.nameRu,
                        tech.Data.baseQiCost,
                        i + 1 // Номер клавиши (1-9)
                    );
                }
                else
                {
                    techniqueSlots[i].Clear();
                }
            }
        }

        // === ФАЗА 4: Отображение оружия ===

        /// <summary>
        /// Обновить отображение оружия в бою из EquipmentController.
        /// Вызывается при ShowCombatUI() и при смене экипировки.
        /// </summary>
        public void UpdateWeaponDisplay(CultivationGame.Inventory.EquipmentController eqCtrl)
        {
            if (eqCtrl == null) return;

            // Основная рука
            var mainWeapon = eqCtrl.GetMainWeapon();
            if (weaponMainIcon != null)
            {
                weaponMainIcon.gameObject.SetActive(mainWeapon != null);
                if (mainWeapon != null && mainWeapon.equipmentData.icon != null)
                {
                    weaponMainIcon.sprite = mainWeapon.equipmentData.icon;
                }
            }
            if (weaponMainText != null)
            {
                if (mainWeapon != null)
                {
                    weaponMainText.text = $"{mainWeapon.Name}\nУрон: {mainWeapon.equipmentData.damage}";
                }
                else
                {
                    weaponMainText.text = "Без оружия";
                }
            }

            // Вторичная рука
            var offWeapon = eqCtrl.GetOffWeapon();
            if (weaponOffIcon != null)
            {
                weaponOffIcon.gameObject.SetActive(offWeapon != null);
                if (offWeapon != null && offWeapon.equipmentData.icon != null)
                {
                    weaponOffIcon.sprite = offWeapon.equipmentData.icon;
                }
            }
            if (weaponOffText != null)
            {
                if (offWeapon != null)
                {
                    weaponOffText.text = $"{offWeapon.Name}\nЗащита: {offWeapon.equipmentData.defense}";
                }
                else
                {
                    weaponOffText.text = "";
                }
            }
        }

        /// <summary>
        /// Обновить все слоты техник (кулдауны, доступность).
        /// Вызывать каждый кадр или по событию.
        /// </summary>
        public void RefreshTechniqueSlots(TechniqueController techController)
        {
            if (techController == null) return;

            for (int i = 0; i < 9 && i < techniqueSlots.Length; i++)
            {
                if (techniqueSlots[i] == null) continue;

                var tech = techController.GetQuickSlotTechnique(i);
                if (tech == null || tech.Data == null)
                {
                    techniqueSlots[i].SetState(TechniqueSlotState.Unavailable);
                    continue;
                }

                // Кулдаун
                if (tech.CooldownRemaining > 0)
                {
                    techniqueSlots[i].SetState(TechniqueSlotState.Cooldown);
                    techniqueSlots[i].UpdateCooldown(tech.CooldownRemaining, tech.Data.cooldown);
                    continue;
                }

                // Накачивается?
                if (techController.IsCharging && techController.ChargeSystem.ActiveCharge.Technique == tech)
                {
                    techniqueSlots[i].SetState(TechniqueSlotState.Charging);
                    continue;
                }

                // Можно использовать?
                if (techController.CanUseTechnique(tech))
                {
                    techniqueSlots[i].SetState(TechniqueSlotState.Ready);
                }
                else
                {
                    techniqueSlots[i].SetState(TechniqueSlotState.Unavailable);
                }
            }
        }

        /// <summary>
        /// Подписка на события TechniqueChargeSystem.
        /// </summary>
        public void SubscribeToChargeSystem(TechniqueChargeSystem chargeSystem)
        {
            if (chargeSystem == null) return;

            chargeSystem.OnChargeStarted += OnChargeStartedHandler;
            chargeSystem.OnChargeProgress += OnChargeProgressHandler;
            chargeSystem.OnChargeCompleted += OnChargeCompletedHandler;
            chargeSystem.OnChargeInterrupted += OnChargeInterruptedHandler;
            chargeSystem.OnChargeFired += OnChargeFiredHandler;
        }

        /// <summary>
        /// Отписка от событий TechniqueChargeSystem.
        /// </summary>
        public void UnsubscribeFromChargeSystem(TechniqueChargeSystem chargeSystem)
        {
            if (chargeSystem == null) return;

            chargeSystem.OnChargeStarted -= OnChargeStartedHandler;
            chargeSystem.OnChargeProgress -= OnChargeProgressHandler;
            chargeSystem.OnChargeCompleted -= OnChargeCompletedHandler;
            chargeSystem.OnChargeInterrupted -= OnChargeInterruptedHandler;
            chargeSystem.OnChargeFired -= OnChargeFiredHandler;
        }

        // === Обработчики событий накачки ===

        private void OnChargeStartedHandler(TechniqueChargeData data)
        {
            string name = data.Technique?.Data?.nameRu ?? "???";
            ShowChargeBar(name);

            // Уведомляем о скорости вливания Ци
            if (chargeRateText != null)
            {
                chargeRateText.text = $"{data.QiChargeRate:F0} Ци/с";
            }
        }

        private void OnChargeProgressHandler(TechniqueChargeData data)
        {
            UpdateChargeProgress(data.ChargeProgress, data.QiCharged, data.QiTotalRequired);
        }

        private void OnChargeCompletedHandler(TechniqueChargeData data)
        {
            string name = data.Technique?.Data?.nameRu ?? "???";
            AddLogEntry($"Накачка завершена: {name}!", LogType.Important);
        }

        private void OnChargeInterruptedHandler(TechniqueChargeData data, ChargeInterruptReason reason)
        {
            string name = data.Technique?.Data?.nameRu ?? "???";
            string reasonText = reason switch
            {
                ChargeInterruptReason.PlayerCancel => "отмена",
                ChargeInterruptReason.DamageInterrupt => "урон",
                ChargeInterruptReason.StunInterrupt => "оглушение",
                ChargeInterruptReason.DeathInterrupt => "смерть",
                ChargeInterruptReason.QiDepleted => "мало Ци",
                _ => "неизвестно"
            };
            AddLogEntry($"Накачка прервана: {name} ({reasonText}, возврат {data.QiReturnPercent * 100:F0}% Ци)", LogType.Damage);
            HideChargeBar();
        }

        private void OnChargeFiredHandler(TechniqueChargeData data)
        {
            string name = data.Technique?.Data?.nameRu ?? "???";
            AddLogEntry($"Техника сработала: {name}!", LogType.Technique);
            HideChargeBar();
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
        public long qiCost;
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

    // ============================================================================
    // ФАЗА 6: Состояние слота техники
    // ============================================================================

    /// <summary>
    /// Состояние слота техники в UI.
    /// </summary>
    public enum TechniqueSlotState
    {
        /// <summary>Готова к использованию</summary>
        Ready,
        /// <summary>Накачивается (заряжается)</summary>
        Charging,
        /// <summary>На кулдауне</summary>
        Cooldown,
        /// <summary>Недоступна (мало Ци/уровень)</summary>
        Unavailable
    }

    // ============================================================================
    // ФАЗА 6: UI слота техники (quickslot 1-9)
    // ============================================================================

    /// <summary>
    /// UI-компонент одного слота техники на панели боя.
    /// Показывает иконку, номер клавиши, состояние, кулдаун.
    /// </summary>
    [Serializable]
    public class TechniqueSlotUI : MonoBehaviour
    {
        [Header("UI элементы")]
        [Tooltip("Иконка техники")]
        public Image iconImage;

        [Tooltip("Номер клавиши (1-9)")]
        public TMP_Text keyNumberText;

        [Tooltip("Имя техники")]
        public TMP_Text techniqueNameText;

        [Tooltip("Стоимость Ци")]
        public TMP_Text qiCostText;

        [Tooltip("Оверлей кулдауна (затемнение)")]
        public Image cooldownOverlay;

        [Tooltip("Текст оставшегося кулдауна")]
        public TMP_Text cooldownText;

        [Tooltip("Оверлей накачки (свечение)")]
        public GameObject chargeOverlay;

        [Tooltip("Оверлей недоступности")]
        public GameObject unavailableOverlay;

        [Header("Цвета состояний")]
        public Color readyColor = Color.white;
        public Color chargingColor = new Color(1f, 0.8f, 0.2f); // Жёлто-золотой
        public Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        public Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // === Runtime ===
        private TechniqueSlotState currentState = TechniqueSlotState.Ready;

        /// <summary>Текущее состояние слота</summary>
        public TechniqueSlotState State => currentState;

        /// <summary>
        /// Инициализировать слот с данными техники.
        /// </summary>
        public void Initialize(string name, long qiCost, int keyNumber)
        {
            if (techniqueNameText != null)
                techniqueNameText.text = name;

            if (qiCostText != null)
                qiCostText.text = $"{qiCost}";

            if (keyNumberText != null)
                keyNumberText.text = keyNumber.ToString();

            SetState(TechniqueSlotState.Ready);
        }

        /// <summary>
        /// Очистить слот (нет техники).
        /// </summary>
        public void Clear()
        {
            if (techniqueNameText != null)
                techniqueNameText.text = "";
            if (qiCostText != null)
                qiCostText.text = "";
            if (iconImage != null)
                iconImage.color = Color.gray;

            SetState(TechniqueSlotState.Unavailable);
        }

        /// <summary>
        /// Установить состояние слота.
        /// </summary>
        public void SetState(TechniqueSlotState state)
        {
            currentState = state;

            // Сбрасываем все оверлеи
            if (chargeOverlay != null) chargeOverlay.SetActive(false);
            if (unavailableOverlay != null) unavailableOverlay.SetActive(false);
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";

            switch (state)
            {
                case TechniqueSlotState.Ready:
                    if (iconImage != null) iconImage.color = readyColor;
                    break;

                case TechniqueSlotState.Charging:
                    if (iconImage != null) iconImage.color = chargingColor;
                    if (chargeOverlay != null) chargeOverlay.SetActive(true);
                    break;

                case TechniqueSlotState.Cooldown:
                    if (iconImage != null) iconImage.color = cooldownColor;
                    break;

                case TechniqueSlotState.Unavailable:
                    if (iconImage != null) iconImage.color = unavailableColor;
                    if (unavailableOverlay != null) unavailableOverlay.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Обновить отображение кулдауна.
        /// </summary>
        /// <param name="remaining">Оставшееся время (сек)</param>
        /// <param name="total">Общее время кулдауна (сек)</param>
        public void UpdateCooldown(float remaining, float total)
        {
            if (cooldownOverlay != null && total > 0)
            {
                cooldownOverlay.fillAmount = remaining / total;
            }

            if (cooldownText != null)
            {
                if (remaining > 0)
                {
                    cooldownText.text = $"{remaining:F1}";
                }
                else
                {
                    cooldownText.text = "";
                }
            }
        }
    }
}
