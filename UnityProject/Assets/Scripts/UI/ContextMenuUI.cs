// ============================================================================
// ContextMenuUI.cs — UI контекстного меню
// Cultivation World Simulator
// Создано: 2026-04-27 18:38:00 UTC — выделено из InventoryUI.cs (legacy v1)
// ============================================================================
// Минимальная реализация контекстного меню для DragDropHandler.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CultivationGame.UI
{
    /// <summary>
    /// Опция контекстного меню.
    /// </summary>
    public class ContextMenuOption
    {
        public string label;
        public Action action;
    }

    /// <summary>
    /// UI контекстного меню — отображает список опций при правом клике.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] private Transform optionContainer;
        [SerializeField] private GameObject optionPrefab;

        private List<ContextMenuOption> currentOptions = new List<ContextMenuOption>();

        /// <summary>
        /// Устанавливает опции меню и создаёт кнопки.
        /// </summary>
        public void SetOptions(List<ContextMenuOption> options)
        {
            currentOptions = options;
            ClearOptions();

            if (optionContainer == null || optionPrefab == null)
            {
                // Fallback: создаём кнопки программно
                foreach (var opt in options)
                {
                    CreateOptionButton(opt);
                }
                return;
            }

            foreach (var opt in options)
            {
                var optionGO = Instantiate(optionPrefab, optionContainer);
                var button = optionGO.GetComponent<Button>();
                var text = optionGO.GetComponentInChildren<TMP_Text>();

                if (text != null)
                    text.text = opt.label;

                if (button != null)
                {
                    var capturedOpt = opt; // Замыкание
                    button.onClick.AddListener(() =>
                    {
                        capturedOpt.action?.Invoke();
                        Destroy(gameObject);
                    });
                }
            }
        }

        private void CreateOptionButton(ContextMenuOption opt)
        {
            var buttonGO = new GameObject($"Option_{opt.label}");
            buttonGO.transform.SetParent(transform, false);

            var rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 28);

            var button = buttonGO.AddComponent<Button>();
            var text = buttonGO.AddComponent<TextMeshProUGUI>();
            text.text = opt.label;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Midline;

            var capturedOpt = opt;
            button.onClick.AddListener(() =>
            {
                capturedOpt.action?.Invoke();
                Destroy(gameObject);
            });
        }

        private void ClearOptions()
        {
            if (optionContainer != null)
            {
                for (int i = optionContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(optionContainer.GetChild(i).gameObject);
                }
            }
        }

        private void Update()
        {
            // Закрытие по клику вне меню
            if (Input.GetMouseButtonDown(0))
            {
                Destroy(gameObject);
            }
        }
    }
}
