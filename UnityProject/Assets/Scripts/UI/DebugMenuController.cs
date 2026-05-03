// ============================================================================
// DebugMenuController.cs — Runtime DEBUG overlay для спавна NPC и экипировки
// Cultivation World Simulator
// Создано: 2026-05-03 11:15:00 UTC
// Редактировано: 2026-05-04 12:00:00 UTC — FIX: 3 критических ошибки компиляции:
//   1) GUI.EnumPopup → runtime EnumCycle кнопка (GUI.EnumPopup только в Editor)
//   2) float→int: GUI.HorizontalSlider возвращает float, npcLevel = int → явный cast
//   3) CultivationGame.Editor → NPCRuntimeSpawner/EquipmentRuntimeSpawner
//      (runtime Assembly-CSharp не может ссылаться на Editor-сборку)
// Редактировано: 2026-05-04 12:30:00 UTC — кнопка + панель перенесены на правую сторону
// ============================================================================
//
// IMGUI-оверлей для Play-режима.
// Одна кнопка "⚙ DEBUG" → панель со спавном NPC и экипировки.
// Флаг включения/отключения: F12 или поле debugEnabled.
//
// Создаётся FullSceneBuilder'ом (Phase07 UI), переживает удаление Assets.
// ============================================================================

using UnityEngine;
using CultivationGame.Generators;
using CultivationGame.NPC;

namespace CultivationGame.UI
{
    /// <summary>
    /// Runtime DEBUG overlay — одна кнопка "⚙ DEBUG" на экране,
    /// при нажатии открывает панель спавна NPC и экипировки.
    /// Флаг включения: F12 (клавиша) или Inspector-поле debugEnabled.
    /// </summary>
    public class DebugMenuController : MonoBehaviour
    {
        // === Настройки ===

        [Header("DEBUG Mode")]
        [Tooltip("Включить/выключить DEBUG меню. Также переключается клавишей F12.")]
        public bool debugEnabled = true;

        [Header("Layout")]
        [SerializeField] private float buttonWidth = 120f;
        [SerializeField] private float buttonHeight = 32f;
        [SerializeField] private float panelWidth = 320f;
        [SerializeField] private float panelMargin = 10f;

        // === Runtime state ===
        private bool showPanel = false;
        private Vector2 scrollPos;
        private int npcLevel = 1;
        private NPCRole npcRole = NPCRole.Passerby;

        // Список всех NPCRole для циклического переключения
        private static readonly NPCRole[] AllRoles = System.Enum.GetValues(typeof(NPCRole)) as NPCRole[];
        private int roleIndex = 0;

        // === Цвета ===
        private static readonly Color BtnNPC = new Color(0.35f, 0.65f, 0.9f);
        private static readonly Color BtnEquip = new Color(0.9f, 0.7f, 0.25f);
        private static readonly Color BtnDanger = new Color(0.9f, 0.35f, 0.25f);
        private static readonly Color BtnBuild = new Color(0.35f, 0.8f, 0.35f);

        // === Unity Lifecycle ===

        private void Update()
        {
            // F12 — переключение DEBUG режима
            if (Input.GetKeyDown(KeyCode.F12))
            {
                debugEnabled = !debugEnabled;
                if (!debugEnabled) showPanel = false;
                Debug.Log($"[DebugMenu] DEBUG mode: {(debugEnabled ? "ON" : "OFF")}");
            }
        }

        private void OnGUI()
        {
            if (!debugEnabled) return;

            // ── Кнопка "⚙ DEBUG" (всегда видна, правая сторона) ──────
            float btnX = Screen.width - buttonWidth - panelMargin;
            float btnY = panelMargin;

            GUI.backgroundColor = showPanel ? BtnDanger : BtnNPC;
            if (GUI.Button(new Rect(btnX, btnY, buttonWidth, buttonHeight), "⚙ DEBUG"))
            {
                showPanel = !showPanel;
            }
            GUI.backgroundColor = Color.white;

            // ── Панель (правая сторона, под кнопкой) ────────────────
            if (showPanel)
            {
                float panelX = Screen.width - panelWidth - panelMargin;
                DrawPanel(panelX, btnY + buttonHeight + 4f);
            }
        }

        // ================================================================
        //  ПАНЕЛЬ
        // ================================================================

        private void DrawPanel(float x, float y)
        {
            float height = Mathf.Min(620f, Screen.height - y - 10f);
            var panelRect = new Rect(x, y, panelWidth, height);

            // Фон
            GUI.Box(panelRect, "");

            // Стиль заголовка
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            // ScrollView
            var innerRect = new Rect(0, 0, panelWidth - 20f, 900f);
            scrollPos = GUI.BeginScrollView(panelRect, scrollPos, innerRect);

            float cy = 8f;

            // Заголовок
            GUI.Label(new Rect(8f, cy, panelWidth - 16f, 24f), "DEBUG Menu", titleStyle);
            cy += 30f;

            // ── NPC Spawner ──────────────────────────────────────────
            cy = DrawSectionHeader(cy, "NPC Spawner", BtnNPC);

            // Выбор роли — кнопка-циклер (вместо GUI.EnumPopup — Editor-only API)
            GUI.Label(new Rect(12f, cy, 80f, 20f), "Роль:");
            if (GUI.Button(new Rect(95f, cy, panelWidth - 110f, 20f), npcRole.ToString()))
            {
                roleIndex = (roleIndex + 1) % AllRoles.Length;
                npcRole = AllRoles[roleIndex];
            }
            cy += 24f;

            // Выбор уровня — FIX: явный cast float→int
            GUI.Label(new Rect(12f, cy, 80f, 20f), "Уровень:");
            npcLevel = Mathf.RoundToInt(
                GUI.HorizontalSlider(new Rect(95f, cy + 4f, panelWidth - 150f, 16f), npcLevel, 0, 10));
            npcLevel = Mathf.Clamp(npcLevel, 0, 10);
            GUI.Label(new Rect(panelWidth - 50f, cy, 40f, 20f), $"L{npcLevel}");
            cy += 24f;

            // Спавн выбранного
            GUI.backgroundColor = BtnNPC;
            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 26f),
                $"Спавн: {npcRole} L{npcLevel}"))
            {
                NPCRuntimeSpawner.SpawnNearPlayer(npcRole, npcLevel);
            }
            GUI.backgroundColor = Color.white;
            cy += 32f;

            // Быстрые кнопки NPC
            cy = DrawQuickNPCButton(cy, "Merchant L2", NPCRole.Merchant, 2);
            cy = DrawQuickNPCButton(cy, "Guard L3", NPCRole.Guard, 3);
            cy = DrawQuickNPCButton(cy, "Elder L5", NPCRole.Elder, 5);
            cy = DrawQuickNPCButton(cy, "Cultivator L4", NPCRole.Cultivator, 4);
            cy = DrawQuickNPCButton(cy, "Monster L1", NPCRole.Monster, 1);
            cy = DrawQuickNPCButton(cy, "Enemy L1", NPCRole.Enemy, 1);
            cy = DrawQuickNPCButton(cy, "Disciple L1", NPCRole.Disciple, 1);
            cy = DrawQuickNPCButton(cy, "Passerby L0", NPCRole.Passerby, 0);
            cy += 4f;

            // Спавн полного набора
            GUI.backgroundColor = BtnBuild;
            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 26f),
                "Полный набор (7 NPC)"))
            {
                int spawned = NPCRuntimeSpawner.SpawnFullSet(GetPlayerPosition());
                Debug.Log($"[DebugMenu] Спавн полного набора: {spawned}/7 NPC");
            }
            GUI.backgroundColor = Color.white;
            cy += 34f;

            // ── Equipment Spawner ────────────────────────────────────
            cy = DrawSectionHeader(cy, "Equipment Spawner", BtnEquip);

            GUI.backgroundColor = BtnEquip;
            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "3 предмета (в сцену)"))
            {
                EquipmentRuntimeSpawner.SpawnRandomLootNearPlayer(3);
            }
            cy += 28f;

            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "10 предметов (в сцену)"))
            {
                EquipmentRuntimeSpawner.SpawnRandomLootNearPlayer(10);
            }
            cy += 28f;

            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "Оружие (в сцену)"))
            {
                EquipmentRuntimeSpawner.SpawnWeaponInScene();
            }
            cy += 28f;

            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "Броня (в сцену)"))
            {
                EquipmentRuntimeSpawner.SpawnArmorInScene();
            }
            cy += 28f;

            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "Оружие → инвентарь"))
            {
                EquipmentRuntimeSpawner.AddWeaponToInventory();
            }
            cy += 28f;

            if (GUI.Button(new Rect(12f, cy, panelWidth - 24f, 24f),
                "3 предмета → инвентарь"))
            {
                EquipmentRuntimeSpawner.AddRandomToInventory(3);
            }
            cy += 34f;

            // ── Утилиты ─────────────────────────────────────────────
            cy = DrawSectionHeader(cy, "Утилиты", BtnDanger);

            GUI.backgroundColor = BtnDanger;
            if (GUI.Button(new Rect(12f, cy, (panelWidth - 30f) / 2f, 24f),
                "Удалить NPC"))
            {
                int count = NPCRuntimeSpawner.ClearAllNPCs();
                Debug.Log($"[DebugMenu] Удалено {count} NPC");
            }
            if (GUI.Button(new Rect(16f + (panelWidth - 30f) / 2f, cy, (panelWidth - 30f) / 2f, 24f),
                "Удалить лут"))
            {
                int count = EquipmentRuntimeSpawner.ClearAllLoot();
                Debug.Log($"[DebugMenu] Удалено {count} предметов");
            }
            GUI.backgroundColor = Color.white;
            cy += 30f;

            // Инфо
            var npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            var player = GameObject.Find("Player");
            string info = $"NPC: {npcs.Length} | Player: {(player != null ? "+" : "-")}";
            GUI.Label(new Rect(12f, cy, panelWidth - 24f, 20f), info);
            cy += 24f;

            GUI.EndScrollView();
        }

        // ================================================================
        //  HELPERS: Рисование
        // ================================================================

        private float DrawSectionHeader(float y, string title, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(12f, y, panelWidth - 24f, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            y += 4f;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(12f, y, panelWidth - 24f, 20f), title, style);
            y += 24f;
            return y;
        }

        private float DrawQuickNPCButton(float y, string label, NPCRole role, int level)
        {
            GUI.backgroundColor = BtnNPC;
            if (GUI.Button(new Rect(20f, y, panelWidth - 40f, 22f), label))
            {
                NPCRuntimeSpawner.SpawnNearPlayer(role, level);
            }
            GUI.backgroundColor = Color.white;
            return y + 26f;
        }

        // ================================================================
        //  УТИЛИТЫ
        // ================================================================

        private Vector3 GetPlayerPosition()
        {
            var player = GameObject.Find("Player");
            if (player != null)
                return player.transform.position;
            return Vector3.zero;
        }
    }
}
