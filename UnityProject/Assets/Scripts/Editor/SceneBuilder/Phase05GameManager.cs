// ============================================================================
// Phase05GameManager.cs — Фаза 05: GameManager и системы
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Managers;
using CultivationGame.World;
using CultivationGame.Save;
using CultivationGame.Generators;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase05GameManager : IScenePhase
    {
        public string Name => "GameManager";
        public string MenuPath => "Phase 05: GameManager and Systems";
        public int Order => 5;

        public bool IsNeeded()
        {
            // Если сцена не открыта — FindObjectOfType вернёт null → return true
            return UnityEngine.Object.FindFirstObjectByType<GameManager>() == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            if (UnityEngine.Object.FindFirstObjectByType<GameManager>() != null)
            {
                Debug.Log("[Phase05] GameManager already exists");
                return;
            }

            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
            gameManagerObj.AddComponent<GameInitializer>();

            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(gameManagerObj.transform);

            systems.AddComponent<WorldController>();
            systems.AddComponent<TimeController>();
            systems.AddComponent<LocationController>();
            systems.AddComponent<EventController>();
            systems.AddComponent<FactionController>();
            systems.AddComponent<GeneratorRegistry>();
            systems.AddComponent<SaveManager>();

            // CombatManager — синглтон для координации боевых взаимодействий
            GameObject combatObj = new GameObject("CombatManager");
            combatObj.transform.SetParent(gameManagerObj.transform);
            combatObj.AddComponent<CombatManager>();

            ConfigureTimeController(systems);
            ConfigureSaveManager(systems);

            Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create GameManager");
            Debug.Log("[Phase05] GameManager + Systems created");
        }

        private void ConfigureTimeController(GameObject systems)
        {
            var tc = systems.GetComponent<TimeController>();
            if (tc == null) return;

            SerializedObject so = new SerializedObject(tc);
            SceneBuilderUtils.SetProperty(so, "currentTimeSpeed", (int)TimeSpeed.Normal);
            SceneBuilderUtils.SetProperty(so, "autoAdvance", true);
            SceneBuilderUtils.SetProperty(so, "normalSpeedRatio", 60);
            SceneBuilderUtils.SetProperty(so, "fastSpeedRatio", 300);
            SceneBuilderUtils.SetProperty(so, "veryFastSpeedRatio", 900);
            SceneBuilderUtils.SetProperty(so, "daysPerMonth", 30);
            SceneBuilderUtils.SetProperty(so, "monthsPerYear", 12);
            so.ApplyModifiedProperties();
        }

        private void ConfigureSaveManager(GameObject systems)
        {
            var sm = systems.GetComponent<SaveManager>();
            if (sm == null) return;

            SerializedObject so = new SerializedObject(sm);
            SceneBuilderUtils.SetProperty(so, "saveFolder", "Saves");
            SceneBuilderUtils.SetProperty(so, "fileExtension", ".sav");
            SceneBuilderUtils.SetProperty(so, "useEncryption", false);
            SceneBuilderUtils.SetProperty(so, "autoSave", true);
            SceneBuilderUtils.SetProperty(so, "autoSaveInterval", 300);
            SceneBuilderUtils.SetProperty(so, "maxSlots", 5);
            so.ApplyModifiedProperties();
        }
    }
}
#endif
