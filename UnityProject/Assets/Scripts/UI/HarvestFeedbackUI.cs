// ============================================================================
// HarvestFeedbackUI.cs — Визуальная обратная связь при добыче ресурсов
// Cultivation World Simulator
// Версия: 2.0 — миграция на TMPro (fix C1: LegacyRuntime.ttf не существует в Unity 6.3)
// Создано: 2026-04-15 08:10:00 UTC
// Редактировано: 2026-04-16 — TMPro миграция + добавлены ShowHarvestPrompt/Hide
// ============================================================================
//
// Показывает прогресс-бар и текст над объектом при нажатии F.
// Цветовая индикация: жёлтый (начало) → зелёный (завершение).
// Автоуничтожение после завершения добычи или если игрок отошёл.
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 2.0:
// - FIX C1: UnityEngine.UI.Text → TMPro.TextMeshProUGUI
// - FIX C1: LegacyRuntime.ttf → TMP_DefaultResources (Unity 6.3 совместимость)
// - ДОБАВЛЕНО: ShowHarvestPrompt() — подсказка «F — Добыть [name]»
// - ДОБАВЛЕНО: HideHarvestPrompt() — скрыть подсказку
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CultivationGame.UI
{
    /// <summary>
    /// Визуальная обратная связь при добыче ресурсов.
    /// Создаёт WorldSpace Canvas с прогресс-баром и текстом (TMPro).
    /// </summary>
    public class HarvestFeedbackUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float verticalOffset = 1.2f;
        [SerializeField] private Color startColor = new Color(1f, 0.85f, 0.2f);   // Жёлтый
        [SerializeField] private Color progressColor = new Color(0.3f, 0.9f, 0.3f); // Зелёный
        [SerializeField] private Color completeColor = new Color(0.2f, 1f, 0.4f);   // Ярко-зелёный
        [SerializeField] private Color failColor = new Color(1f, 0.3f, 0.3f);       // Красный
        [SerializeField] private Color promptColor = new Color(1f, 0.9f, 0.3f);     // Жёлтый для подсказки

        // === Runtime ===
        private Canvas feedbackCanvas;
        private GameObject canvasGO;
        private Slider progressBar;
        private TextMeshProUGUI statusText;      // FIX C1: Text → TMPro
        private TextMeshProUGUI promptText;       // Подсказка "F — Добыть"
        private RectTransform canvasRect;
        private Transform targetTransform;
        private Coroutine activeCoroutine;
        private bool isPromptVisible = false;

        /// <summary>Событие при завершении отображения.</summary>
        public event System.Action OnFeedbackComplete;

        // === Public API ===

        /// <summary>
        /// Показать подсказку добычи: «F — Добыть [name]».
        /// Чекпоинт §7.1.
        /// </summary>
        /// <param name="target">Объект, над которым показывать подсказку.</param>
        /// <param name="resourceName">Название ресурса.</param>
        public void ShowHarvestPrompt(Transform target, string resourceName)
        {
            EnsureCanvasExists(target);
            targetTransform = target;

            if (promptText != null)
            {
                promptText.text = $"F — Добыть {resourceName}";
                promptText.color = promptColor;
                promptText.gameObject.SetActive(true);
            }

            isPromptVisible = true;

            // Скрыть прогресс-бар при показе подсказки
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);

            if (statusText != null)
                statusText.gameObject.SetActive(false);

            UpdatePosition();
        }

        /// <summary>
        /// Скрыть подсказку добычи.
        /// </summary>
        public void HideHarvestPrompt()
        {
            if (promptText != null)
                promptText.gameObject.SetActive(false);

            isPromptVisible = false;
        }

        /// <summary>
        /// Показать обратную связь: добыча начата.
        /// </summary>
        /// <param name="target">Объект, над которым показывать.</param>
        /// <param name="resourceName">Название ресурса (дерево, камень и т.д.).</param>
        /// <param name="progress">Прогресс 0..1.</param>
        public void ShowHarvestStarted(Transform target, string resourceName, float progress = 0f)
        {
            EnsureCanvasExists(target);
            targetTransform = target;

            // Скрыть подсказку при начале добычи
            HideHarvestPrompt();

            // Показать прогресс-бар
            if (progressBar != null)
                progressBar.gameObject.SetActive(true);

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = $"Добываю {resourceName}...";
                statusText.color = startColor;
            }

            UpdateProgress(progress);
            UpdatePosition();

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
        }

        /// <summary>
        /// Обновить прогресс добычи.
        /// </summary>
        /// <param name="progress">Прогресс 0..1.</param>
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);

                // Цветовая интерполяция: жёлтый → зелёный
                if (progressBar.fillRect != null)
                {
                    var fillImage = progressBar.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.color = Color.Lerp(startColor, progressColor, progress);
                    }
                }
            }

            UpdatePosition();
        }

        /// <summary>
        /// Показать: добыча завершена успешно.
        /// </summary>
        /// <param name="resourceName">Что получено.</param>
        /// <param name="amount">Количество.</param>
        public void ShowHarvestComplete(string resourceName, int amount)
        {
            EnsureCanvasExists(targetTransform);

            HideHarvestPrompt();

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = $"+{amount} {resourceName}";
                statusText.color = completeColor;
            }

            if (progressBar != null)
            {
                progressBar.value = 1f;
                progressBar.gameObject.SetActive(true);
            }

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// Показать: добыча невозможна (объект далеко / уже разрушен).
        /// </summary>
        /// <param name="message">Причина.</param>
        public void ShowHarvestFailed(string message)
        {
            EnsureCanvasExists(targetTransform);

            HideHarvestPrompt();

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = message;
                statusText.color = failColor;
            }

            if (progressBar != null)
                progressBar.gameObject.SetActive(false);

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(FadeOutAndDestroy());
        }

        // === Private Methods ===

        private void EnsureCanvasExists(Transform target)
        {
            if (canvasGO != null) return;

            // Создать WorldSpace Canvas
            canvasGO = new GameObject("HarvestFeedback_Canvas");
            canvasGO.transform.SetParent(transform);

            feedbackCanvas = canvasGO.AddComponent<Canvas>();
            feedbackCanvas.renderMode = RenderMode.WorldSpace;
            feedbackCanvas.sortingOrder = 100; // Поверх всего

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10;

            canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(3f, 1.0f); // 3 юнита шириной, 1.0 высотой

            // Прогресс-бар
            var sliderGO = new GameObject("ProgressBar");
            sliderGO.transform.SetParent(canvasGO.transform, false);
            progressBar = sliderGO.AddComponent<Slider>();
            progressBar.direction = Slider.Direction.LeftToRight;
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;

            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.05f, 0.55f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.85f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            progressBar.targetGraphic = bgImage;

            // Fill Area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            if (fillAreaRect == null)
                fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = startColor;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            progressBar.fillRect = fillRect;

            // Текст статуса (TMPro)
            // FIX C1: LegacyRuntime.ttf → TMPro.TextMeshProUGUI (Unity 6.3 совместимость)
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(canvasGO.transform, false);
            statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = startColor;
            statusText.text = "";
            statusText.richText = false;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Подсказка добычи (prompt) — «F — Добыть [name]»
            var promptGO = new GameObject("PromptText");
            promptGO.transform.SetParent(canvasGO.transform, false);
            promptText = promptGO.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 16;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = promptColor;
            promptText.text = "";
            promptText.richText = false;
            promptText.gameObject.SetActive(false); // Скрыт по умолчанию

            var promptRect = promptGO.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0f, 0.5f);
            promptRect.anchorMax = new Vector2(1f, 1f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (canvasGO == null || targetTransform == null) return;

            // Позиция над объектом
            Vector3 worldPos = targetTransform.position + Vector3.up * verticalOffset;
            canvasGO.transform.position = worldPos;

            // Масштаб — канвас не должен быть слишком большим
            float scale = 0.5f;
            canvasGO.transform.localScale = new Vector3(scale, scale, scale);
        }

        private IEnumerator FadeOutAndDestroy()
        {
            // Подождать displayDuration
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);

                if (feedbackCanvas != null)
                {
                    foreach (var graphic in feedbackCanvas.GetComponentsInChildren<Graphic>())
                    {
                        var c = graphic.color;
                        c.a = alpha;
                        graphic.color = c;
                    }

                    // TMP тексты не наследуют Graphic — обновляем отдельно
                    foreach (var tmp in feedbackCanvas.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        var c = tmp.color;
                        c.a = alpha;
                        tmp.color = c;
                    }
                }

                yield return null;
            }

            OnFeedbackComplete?.Invoke();

            // Уничтожить канвас
            if (canvasGO != null)
                Destroy(canvasGO);

            canvasGO = null;
            feedbackCanvas = null;
            progressBar = null;
            statusText = null;
            promptText = null;
            activeCoroutine = null;
        }

        private void OnDestroy()
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            if (canvasGO != null)
                Destroy(canvasGO);
        }
    }
}
