// ============================================================================
// SceneToolsWindow.cs — Единое окно инструментов сцены
// Cultivation World Simulator
// Создано: 2026-05-03 09:46:34 UTC
// ============================================================================
//
// Заменяет хоткеи и разрозненные меню единым EditorWindow с кнопками.
// Секции: Full Scene Builder, NPC Spawner, Equipment Spawner.
//
// Окно: Window → Scene Tools
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.NPC;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Единое окно инструментов сцены.
    /// Кнопки вместо хоткеев — наглядно и без конфликтов.
    /// </summary>
    public class SceneToolsWindow : EditorWindow
    {
        // === Foldout состояния ===
        private bool showSceneBuilder = true;
        private bool showNPC = true;
        private bool showEquipment = true;

        // === NPC настройки ===
        private int npcCultivationLevel = 1;
        private NPCRole npcRole = NPCRole.Passerby;
        private Vector2 scrollPos;

        // === Цвета кнопок ===
        private static readonly Color ColorBuild = new Color(0.4f, 0.8f, 0.4f);
        private static readonly Color ColorNPC = new Color(0.4f, 0.7f, 0.9f);
        private static readonly Color ColorEquip = new Color(0.9f, 0.7f, 0.3f);
        private static readonly Color ColorDanger = new Color(0.9f, 0.4f, 0.3f);

        [MenuItem("Window/Scene Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneToolsWindow>("Scene Tools");
            window.minSize = new Vector2(320, 500);
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Scene Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Инструменты создания сцены, NPC и экипировки",
                EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            DrawSceneBuilderSection();
            EditorGUILayout.Space(8);

            DrawNPCSection();
            EditorGUILayout.Space(8);

            DrawEquipmentSection();
            EditorGUILayout.Space(8);

            DrawUtilitySection();

            EditorGUILayout.EndScrollView();
        }

        // ================================================================
        //  СЕКЦИЯ: FULL SCENE BUILDER
        // ================================================================

        private void DrawSceneBuilderSection()
        {
            showSceneBuilder = EditorGUILayout.Foldout(showSceneBuilder,
                "Scene Builder (20 фаз)", true, EditorStyles.foldoutHeader);

            if (!showSceneBuilder) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("Полная генерация сцены через оркестратор FullSceneBuilder.",
                MessageType.Info);

            // Build All
            GUI.backgroundColor = ColorBuild;
            if (GUILayout.Button("Build All — Полная генерация сцены", GUILayout.Height(32)))
            {
                FullSceneBuilder.BuildAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Отдельные фазы:", EditorStyles.miniBoldLabel);

            // Фазы 0-19
            var phases = FullSceneBuilder.PHASES;
            if (phases != null)
            {
                for (int i = 0; i < phases.Length; i++)
                {
                    if (phases[i] == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // Кнопка выполнения фазы
                    string phaseName = $"[{i:D2}] {phases[i].Name}";
                    if (GUILayout.Button(phaseName, GUILayout.Height(22)))
                    {
                        FullSceneBuilder.RunSinglePhase(i);
                    }

                    // Индикатор IsNeeded
                    bool needed = phases[i].IsNeeded();
                    var style = needed
                        ? new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } }
                        : new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                    GUILayout.Label(needed ? "⚠ Нужен" : "✓ OK", style, GUILayout.Width(55));

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        // ================================================================
        //  СЕКЦИЯ: NPC SPAWNER
        // ================================================================

        private void DrawNPCSection()
        {
            showNPC = EditorGUILayout.Foldout(showNPC,
                "NPC Spawner", true, EditorStyles.foldoutHeader);

            if (!showNPC) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("Спавн NPC рядом с Player. Настройки роли и уровня ниже.",
                MessageType.Info);

            // Настройки
            npcRole = (NPCRole)EditorGUILayout.EnumPopup("Роль:", npcRole);
            npcCultivationLevel = EditorGUILayout.IntSlider("Уровень культивации:",
                npcCultivationLevel, 0, 10);

            EditorGUILayout.Space(4);

            // Кнопка: Спавн 1 NPC выбранной роли
            GUI.backgroundColor = ColorNPC;
            if (GUILayout.Button($"Спавн: {npcRole} L{npcCultivationLevel}", GUILayout.Height(28)))
            {
                SpawnNPC(npcRole, npcCultivationLevel);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Быстрый спавн:", EditorStyles.miniBoldLabel);

            // Быстрые кнопки по ролям
            DrawNPCQuickButton("Merchant", NPCRole.Merchant, 2);
            DrawNPCQuickButton("Guard", NPCRole.Guard, 3);
            DrawNPCQuickButton("Elder", NPCRole.Elder, 5);
            DrawNPCQuickButton("Cultivator", NPCRole.Cultivator, 4);
            DrawNPCQuickButton("Disciple", NPCRole.Disciple, 1);
            DrawNPCQuickButton("Monster", NPCRole.Monster, 1);
            DrawNPCQuickButton("Enemy", NPCRole.Enemy, 1);
            DrawNPCQuickButton("Passerby", NPCRole.Passerby, 0);

            EditorGUILayout.Space(4);

            // Спавн 5 NPC
            GUI.backgroundColor = ColorNPC;
            if (GUILayout.Button("Спавн 5 NPC разных ролей", GUILayout.Height(26)))
            {
                var roles = new[] { NPCRole.Merchant, NPCRole.Guard, NPCRole.Cultivator,
                    NPCRole.Elder, NPCRole.Monster };
                var levels = new[] { 2, 3, 4, 5, 1 };

                for (int i = 0; i < roles.Length; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * 3f;
                    offset.z = 0;
                    NPCSceneSpawner.SpawnNPCInScene(roles[i], levels[i],
                        GetPlayerPosition() + offset);
                }
                Debug.Log("[SceneTools] Спавн: 5 NPC разных ролей");
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // Спавн полного набора (как Phase19)
            GUI.backgroundColor = ColorBuild;
            if (GUILayout.Button("Спавн полного набора (Phase19)", GUILayout.Height(26)))
            {
                SpawnFullNPCSet();
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.indentLevel--;
        }

        private void DrawNPCQuickButton(string label, NPCRole role, int level)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            if (GUILayout.Button($"{label} L{level}", GUILayout.Height(22)))
            {
                SpawnNPC(role, level);
            }
            EditorGUILayout.EndHorizontal();
        }

        // ================================================================
        //  СЕКЦИЯ: EQUIPMENT SPAWNER
        // ================================================================

        private void DrawEquipmentSection()
        {
            showEquipment = EditorGUILayout.Foldout(showEquipment,
                "Equipment Spawner", true, EditorStyles.foldoutHeader);

            if (!showEquipment) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("Спавн экипировки рядом с Player или в инвентарь.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Спавн в сцену:", EditorStyles.miniBoldLabel);

            GUI.backgroundColor = ColorEquip;
            if (GUILayout.Button("3 случайных предмета (в сцену)", GUILayout.Height(26)))
            {
                EquipmentSceneSpawner.SpawnRandomLootNearPlayer(3);
            }

            if (GUILayout.Button("10 случайных предметов (в сцену)", GUILayout.Height(26)))
            {
                EquipmentSceneSpawner.SpawnRandomLootNearPlayer(10);
            }

            if (GUILayout.Button("5 предметов L3 (в сцену)", GUILayout.Height(26)))
            {
                EquipmentSceneSpawner.SpawnLevelLootNearPlayer(3, 5);
            }

            if (GUILayout.Button("Оружие (в сцену)", GUILayout.Height(24)))
            {
                EquipmentSceneSpawner.SpawnWeaponInScene();
            }

            if (GUILayout.Button("Броня (в сцену)", GUILayout.Height(24)))
            {
                EquipmentSceneSpawner.SpawnArmorInScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Добавить в инвентарь Player:", EditorStyles.miniBoldLabel);

            GUI.backgroundColor = ColorEquip;
            if (GUILayout.Button("Оружие → инвентарь", GUILayout.Height(24)))
            {
                EquipmentSceneSpawner.AddWeaponToInventory();
            }

            if (GUILayout.Button("Броня → инвентарь", GUILayout.Height(24)))
            {
                EquipmentSceneSpawner.AddArmorToInventory();
            }

            if (GUILayout.Button("3 случайных → инвентарь", GUILayout.Height(24)))
            {
                EquipmentSceneSpawner.AddRandomToInventory(3);
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.indentLevel--;
        }

        // ================================================================
        //  СЕКЦИЯ: УТИЛИТЫ
        // ================================================================

        private void DrawUtilitySection()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("Утилиты:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();

            // Удалить всех NPC
            GUI.backgroundColor = ColorDanger;
            if (GUILayout.Button("Удалить всех NPC", GUILayout.Height(24)))
            {
                int count = NPCSceneSpawner.ClearAllNPCs();
                Debug.Log($"[SceneTools] Удалено {count} NPC");
            }

            // Удалить весь лут
            if (GUILayout.Button("Удалить весь лут", GUILayout.Height(24)))
            {
                int count = EquipmentSceneSpawner.ClearAllLoot();
                Debug.Log($"[SceneTools] Удалено {count} предметов");
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Информация о сцене
            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            var player = GameObject.Find("Player");
            EditorGUILayout.HelpBox(
                $"NPC в сцене: {npcs.Length}\n" +
                $"Player: {(player != null ? "✓ найден" : "✗ НЕ найден")}",
                MessageType.Info);
        }

        // ================================================================
        //  ПРИВАТНЫЕ МЕТОДЫ
        // ================================================================

        /// <summary>
        /// Спавн 1 NPC рядом с Player.
        /// </summary>
        private void SpawnNPC(NPCRole role, int level)
        {
            Vector3 center = GetPlayerPosition();
            Vector3 offset = Random.insideUnitSphere * 3f;
            offset.z = 0;

            NPCSceneSpawner.SpawnNPCInScene(role, level, center + offset);
        }

        /// <summary>
        /// Спавн полного набора NPC (аналог Phase19).
        /// </summary>
        private void SpawnFullNPCSet()
        {
            var placements = new (NPCRole role, int level, float x, float y)[]
            {
                (NPCRole.Merchant,   2,   3f,  -2f),
                (NPCRole.Guard,      3,   8f,   0f),
                (NPCRole.Guard,      2,  -5f,   3f),
                (NPCRole.Elder,      5,  -3f,  -4f),
                (NPCRole.Cultivator, 4,   5f,   5f),
                (NPCRole.Monster,    1, -10f,  -8f),
                (NPCRole.Monster,    2,  12f, -10f),
            };

            int spawned = 0;
            foreach (var (role, level, x, y) in placements)
            {
                var ctrl = NPCSceneSpawner.SpawnNPCInScene(role, level,
                    new Vector3(x, y, 0f));
                if (ctrl != null) spawned++;
            }

            Debug.Log($"[SceneTools] Спавн полного набора: {spawned}/{placements.Length} NPC");
        }

        /// <summary>
        /// Получить позицию Player.
        /// </summary>
        private Vector3 GetPlayerPosition()
        {
            var player = GameObject.Find("Player");
            if (player != null)
                return player.transform.position;

            Debug.LogWarning("[SceneTools] Player не найден! Спавн в (0,0,0)");
            return Vector3.zero;
        }
    }
}
#endif
