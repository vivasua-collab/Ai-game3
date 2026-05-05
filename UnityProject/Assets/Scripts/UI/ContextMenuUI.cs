// ============================================================================
// ContextMenuUI.cs — UI контекстного меню
// Cultivation World Simulator
// Создано: 2026-04-27 18:38:00 UTC — выделено из InventoryUI.cs (legacy v1)
// Редактировано: 2026-05-05 — FIX BUG-INTERACT-02: Update() убивал меню при ЛЮБОМ
//   клике, даже на собственные кнопки, ДО того как onClick успевал сработать.
//   Исправление: проверяем, что клик НЕ по элементам меню.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// UI контекстного меню — отображает список опций при клике.
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

            // Фон кнопки — чтобы raycast работал
            var bgImage = buttonGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            button.targetGraphic = bgImage;

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
            // FIX BUG-INTERACT-02: Закрытие по клику ВНЕ меню.
            // Раньше: Input.GetMouseButtonDown(0) убивал меню при ЛЮБОМ клике,
            // включая клик по собственным кнопкам — onClick НЕ успевал сработать.
            // Теперь: проверяем, что клик НЕ по элементам этого меню.
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // Если указатель над элементом меню — НЕ закрываем (кнопка обработает)
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    // Проверяем, попал ли клик на дочерний элемент этого меню
                    var eventData = new PointerEventData(EventSystem.current);
                    eventData.position = Input.mousePosition;
                    var results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);

                    foreach (var result in results)
                    {
                        if (result.gameObject != null && IsChildOfThisMenu(result.gameObject))
                        {
                            // Клик по элементу меню — НЕ закрываем, кнопка обработает
                            return;
                        }
                    }
                }

                // Клик вне меню — закрываем
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Проверяет, является ли gameObject дочерним объектом этого меню.
        /// </summary>
        private bool IsChildOfThisMenu(GameObject go)
        {
            var current = go.transform;
            while (current != null)
            {
                if (current == transform)
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
