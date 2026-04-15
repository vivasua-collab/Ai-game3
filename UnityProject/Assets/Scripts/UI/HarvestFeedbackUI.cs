// ============================================================================
// HarvestFeedbackUI.cs — Визуальная обратная связь при добыче ресурсов
// Cultivation World Simulator
// Создано: 2026-04-15 08:10:00 UTC
// ============================================================================
// Показывает прогресс-бар и текст над объектом при нажатии F.
// Цветовая индикация: жёлтый (начало) → зелёный (завершение).
// Автоуничтожение после завершения добычи или если игрок отошёл.
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CultivationGame.UI
{
    /// <summary>
    /// Визуальная обратная связь при добыче ресурсов.
    /// Создаёт WorldSpace Canvas с прогресс-баром и текстом.
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

        // === Runtime ===
        private Canvas feedbackCanvas;
        private GameObject canvasGO;
        private Slider progressBar;
        private Text statusText;
        private RectTransform canvasRect;
        private Transform targetTransform;
        private Coroutine activeCoroutine;

        /// <summary>Событие при завершении отображения.</summary>
        public event System.Action OnFeedbackComplete;

        // === Public API ===

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

            if (statusText != null)
            {
                statusText.text = $"⏳ Добываю {resourceName}...";
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

            if (statusText != null)
            {
                statusText.text = $"+{amount} {resourceName}";
                statusText.color = completeColor;
            }

            if (progressBar != null)
                progressBar.value = 1f;

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

            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = failColor;
            }

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
            canvasRect.sizeDelta = new Vector2(2f, 0.6f); // 2 юнита шириной, 0.6 высотой

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
            // Explicitly add RectTransform — new GameObject() only creates a regular Transform
            // and it may not auto-convert when parented under a Slider
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

            // Handle
            // (не нужен для прогресс-бара добычи)

            // Текст статуса
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(canvasGO.transform, false);
            statusText = textGO.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 14;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = startColor;
            statusText.text = "";

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

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
            float startAlpha = 1f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);

                if (feedbackCanvas != null)
                {
                    foreach (var graphic in feedbackCanvas.GetComponentsInChildren<Graphic>())
                    {
                        var c = graphic.color;
                        c.a = alpha;
                        graphic.color = c;
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
