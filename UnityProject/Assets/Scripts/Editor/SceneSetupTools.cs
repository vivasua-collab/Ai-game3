using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.UI;

// Core enums
using CultivationGame.Core;

// Managers
using CultivationGame.Managers;

// World controllers
using CultivationGame.World;

// Player controllers
using CultivationGame.Player;

// Qi system
using CultivationGame.Qi;

// Body system
using CultivationGame.Body;

// Inventory system
using CultivationGame.Inventory;

// Combat system
using CultivationGame.Combat;

// Save system
using CultivationGame.Save;

// Interaction
using CultivationGame.Interaction;

#if UNITY_EDITOR // FIX GEN-C01: Wrap entire class in #if UNITY_EDITOR (2026-04-11)
/// <summary>
/// Инструменты для полуавтоматической настройки сцены.
/// Window → Scene Setup Tools
/// 
/// Совместимость: Unity 6.3+
/// 
/// Создано: 2026-03-31
/// Редактировано: 2026-04-11 14:33:20 UTC — Fix: Player.StatDevelopment→Core.StatDevelopment (not a Component)
/// </summary>
public class SceneSetupTools : EditorWindow
{
    private bool showBasicScene = true;
    private bool showPlayer = true;
    private bool showUI = true;

    // Настройки Player
    private string playerName = "Игрок";
    private float moveSpeed = 5f;
    private float runSpeedMultiplier = 1.5f;

    // Настройки QiController
    private int cultivationLevel = 1;
    private CoreQuality coreQuality = CoreQuality.Normal;
    private long currentQi = 100;

    // Настройки BodyController
    private BodyMaterial bodyMaterial = BodyMaterial.Organic;
    private int vitality = 10;

    // Настройки TimeController
    private TimeSpeed currentTimeSpeed = TimeSpeed.Normal;
    private bool autoAdvance = true;

    // Настройки SaveManager
    private bool autoSave = true;
    private int autoSaveInterval = 300;

    [MenuItem("Window/Scene Setup Tools")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneSetupTools>("Scene Setup Tools");
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Scene Setup Tools", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Полуавтоматическая настройка сцены", EditorStyles.helpBox);
        EditorGUILayout.Space(10);

        // === BASIC SCENE ===
        showBasicScene = EditorGUILayout.Foldout(showBasicScene, "1. Basic Scene Setup", true, EditorStyles.foldoutHeader);
        if (showBasicScene)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Создаёт GameManager, Systems и все контроллеры", MessageType.Info);

            if (GUILayout.Button("Create GameManager & Systems", GUILayout.Height(30)))
            {
                CreateGameManagerAndSystems();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Setup TimeController Settings", GUILayout.Height(25)))
            {
                SetupTimeController();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Setup SaveManager Settings", GUILayout.Height(25)))
            {
                SetupSaveManager();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // === PLAYER ===
        showPlayer = EditorGUILayout.Foldout(showPlayer, "2. Player Setup", true, EditorStyles.foldoutHeader);
        if (showPlayer)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("Создаёт Player со всеми компонентами", MessageType.Info);

            EditorGUILayout.Space(5);
            playerName = EditorGUILayout.TextField("Player Name:", playerName);
            moveSpeed = EditorGUILayout.FloatField("Move Speed:", moveSpeed);
            runSpeedMultiplier = EditorGUILayout.FloatField("Run Multiplier:", runSpeedMultiplier);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Body Settings:", EditorStyles.boldLabel);
            bodyMaterial = (BodyMaterial)EditorGUILayout.EnumPopup("Body Material:", bodyMaterial);
            vitality = EditorGUILayout.IntField("Vitality:", vitality);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Qi Settings:", EditorStyles.boldLabel);
            cultivationLevel = EditorGUILayout.IntSlider("Cultivation Level:", cultivationLevel, 1, 10);
            coreQuality = (CoreQuality)EditorGUILayout.EnumPopup("Core Quality:", coreQuality);
            currentQi = EditorGUILayout.LongField("Current Qi:", currentQi);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create Player GameObject", GUILayout.Height(30)))
            {
                CreatePlayer();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // === UI ===
        showUI = EditorGUILayout.Foldout(showUI, "3. UI Setup", true, EditorStyles.foldoutHeader);
        if (showUI)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("Создаёт Canvas, HUD и текстовые элементы", MessageType.Info);

            if (GUILayout.Button("Create GameUI Canvas", GUILayout.Height(30)))
            {
                CreateGameUI();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Create HUD Panel", GUILayout.Height(25)))
            {
                CreateHUD();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // === ALL IN ONE ===
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("SETUP ALL (Full Scene)", GUILayout.Height(40)))
        {
            SetupFullScene();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "После использования инструментов:\n" +
            "1. Проверь Console на ошибки\n" +
            "2. Настрой Project Settings (теги, слои)\n" +
            "3. Создай префаб Player (drag to Prefabs/)\n" +
            "4. См. инструкции *_SemiAuto.md",
            MessageType.Warning);
    }

    #region Basic Scene

    void CreateGameManagerAndSystems()
    {
        // Проверяем наличие GameManager (Unity 6.3+ метод)
        var existingGM = FindFirstObjectByType<GameManager>();
        if (existingGM != null)
        {
            if (!EditorUtility.DisplayDialog("GameManager Exists",
                "GameManager уже есть в сцене. Создать новый?", "Да", "Нет"))
                return;
        }

        // Создаём GameManager
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        // Создаём Systems как дочерний объект
        GameObject systems = new GameObject("Systems");
        systems.transform.SetParent(gameManager.transform);

        // Добавляем все контроллеры на Systems
        systems.AddComponent<WorldController>();
        systems.AddComponent<TimeController>();
        systems.AddComponent<LocationController>();
        systems.AddComponent<EventController>();
        systems.AddComponent<FactionController>();
        
        // GeneratorRegistry - проверяем через рефлексию
        var generatorRegistryType = System.Type.GetType("CultivationGame.World.GeneratorRegistry, Assembly-CSharp");
        if (generatorRegistryType != null)
        {
            systems.AddComponent(generatorRegistryType);
        }
        
        systems.AddComponent<SaveManager>();

        Undo.RegisterCreatedObjectUndo(gameManager, "Create GameManager");
        Debug.Log("[SceneSetupTools] GameManager и Systems созданы!");
        Selection.activeGameObject = gameManager;
    }

    void SetupTimeController()
    {
        var tc = FindFirstObjectByType<TimeController>();
        if (tc == null)
        {
            Debug.LogError("[SceneSetupTools] TimeController не найден! Сначала создай GameManager & Systems.");
            return;
        }

        // Настройка через SerializedObject
        SerializedObject so = new SerializedObject(tc);
        
        var propSpeed = so.FindProperty("currentTimeSpeed");
        var propAutoAdvance = so.FindProperty("autoAdvance");
        var propNormal = so.FindProperty("normalSpeedRatio");
        var propFast = so.FindProperty("fastSpeedRatio");
        var propVeryFast = so.FindProperty("veryFastSpeedRatio");
        var propDays = so.FindProperty("daysPerMonth");
        var propMonths = so.FindProperty("monthsPerYear");
        
        if (propSpeed != null) propSpeed.enumValueIndex = (int)currentTimeSpeed;
        if (propAutoAdvance != null) propAutoAdvance.boolValue = autoAdvance;
        if (propNormal != null) propNormal.intValue = 60;
        if (propFast != null) propFast.intValue = 300;
        if (propVeryFast != null) propVeryFast.intValue = 900;
        if (propDays != null) propDays.intValue = 30;
        if (propMonths != null) propMonths.intValue = 12;
        
        so.ApplyModifiedProperties();

        Debug.Log("[SceneSetupTools] TimeController настроен!");
    }

    void SetupSaveManager()
    {
        var sm = FindFirstObjectByType<SaveManager>();
        if (sm == null)
        {
            Debug.LogError("[SceneSetupTools] SaveManager не найден! Сначала создай GameManager & Systems.");
            return;
        }

        SerializedObject so = new SerializedObject(sm);
        
        SetProperty(so, "saveFolder", "Saves");
        SetProperty(so, "fileExtension", ".sav");
        SetProperty(so, "useEncryption", false);
        SetProperty(so, "autoSave", autoSave);
        SetProperty(so, "autoSaveInterval", autoSaveInterval);
        SetProperty(so, "maxSlots", 5);
        
        so.ApplyModifiedProperties();

        Debug.Log("[SceneSetupTools] SaveManager настроен!");
    }

    #endregion

    #region Player

    void CreatePlayer()
    {
        // Проверяем наличие Player
        var existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
        {
            if (!EditorUtility.DisplayDialog("Player Exists",
                "Player уже есть в сцене. Создать новый?", "Да", "Нет"))
                return;
        }

        // Создаём Player
        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;

        // Rigidbody2D
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Circle Collider 2D
        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
        col.radius = 0.5f;

        // Добавляем все компоненты
        player.AddComponent<PlayerController>();
        player.AddComponent<BodyController>();
        player.AddComponent<QiController>();
        player.AddComponent<InventoryController>();
        player.AddComponent<EquipmentController>();
        player.AddComponent<TechniqueController>();
        
        // StatDevelopment - NOT a Component (plain C# class in CultivationGame.Core)
        // It's managed by PlayerController as a field, not as a separate MonoBehaviour
        // Редактировано: 2026-04-11 14:33:20 UTC
        
        player.AddComponent<SleepSystem>();
        player.AddComponent<InteractionController>();

        // Настраиваем компоненты через SerializedObject
        SetupPlayerController(player);
        SetupBodyController(player);
        SetupQiController(player);
        SetupInventoryController(player);
        SetupEquipmentController(player);
        SetupTechniqueController(player);
        SetupSleepSystem(player);

        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        Debug.Log($"[SceneSetupTools] Player '{playerName}' создан со всеми компонентами!");
        Selection.activeGameObject = player;
    }
    
    void SetupPlayerController(GameObject player)
    {
        var pc = player.GetComponent<PlayerController>();
        if (pc == null) return;
        
        SerializedObject so = new SerializedObject(pc);
        SetProperty(so, "playerId", "player");
        SetProperty(so, "playerName", playerName);
        SetProperty(so, "moveSpeed", moveSpeed);
        SetProperty(so, "runSpeedMultiplier", runSpeedMultiplier);
        so.ApplyModifiedProperties();
    }
    
    void SetupBodyController(GameObject player)
    {
        var bc = player.GetComponent<BodyController>();
        if (bc == null) return;
        
        SerializedObject so = new SerializedObject(bc);
        SetProperty(so, "bodyMaterial", (int)bodyMaterial);
        SetProperty(so, "vitality", vitality);
        SetProperty(so, "cultivationLevel", cultivationLevel);
        SetProperty(so, "enableRegeneration", true);
        SetProperty(so, "regenRate", 1f);
        so.ApplyModifiedProperties();
    }
    
    void SetupQiController(GameObject player)
    {
        var qc = player.GetComponent<QiController>();
        if (qc == null) return;
        
        SerializedObject so = new SerializedObject(qc);
        SetProperty(so, "cultivationLevel", cultivationLevel);
        SetProperty(so, "cultivationSubLevel", 0);
        SetProperty(so, "coreQuality", (int)coreQuality);
        SetProperty(so, "currentQi", currentQi);
        SetProperty(so, "enablePassiveRegen", true);
        so.ApplyModifiedProperties();
    }
    
    void SetupInventoryController(GameObject player)
    {
        var ic = player.GetComponent<InventoryController>();
        if (ic == null) return;
        
        SerializedObject so = new SerializedObject(ic);
        SetProperty(so, "gridWidth", 8);
        SetProperty(so, "gridHeight", 6);
        SetProperty(so, "maxWeight", 100f);
        SetProperty(so, "useWeightLimit", true);
        so.ApplyModifiedProperties();
    }
    
    void SetupEquipmentController(GameObject player)
    {
        var ec = player.GetComponent<EquipmentController>();
        if (ec == null) return;
        
        // v2.0: useLayerSystem и maxLayersPerSlot убраны (нет слоёв)
        SerializedObject so = new SerializedObject(ec);
        SetProperty(so, "enforceRequirements", true);
        so.ApplyModifiedProperties();
    }
    
    void SetupTechniqueController(GameObject player)
    {
        var tc = player.GetComponent<TechniqueController>();
        if (tc == null) return;
        
        SerializedObject so = new SerializedObject(tc);
        SetProperty(so, "maxQuickSlots", 10);
        SetProperty(so, "maxUltimates", 1);
        so.ApplyModifiedProperties();
    }
    
    void SetupSleepSystem(GameObject player)
    {
        var ss = player.GetComponent<SleepSystem>();
        if (ss == null) return;
        
        SerializedObject so = new SerializedObject(ss);
        SetProperty(so, "minSleepHours", 4f);
        SetProperty(so, "maxSleepHours", 12f);
        SetProperty(so, "optimalSleepHours", 8f);
        so.ApplyModifiedProperties();
    }

    #endregion

    #region UI

    void CreateGameUI()
    {
        // Проверяем наличие Canvas
        var existingCanvas = GameObject.Find("GameUI");
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog("Canvas Exists",
                "GameUI уже есть в сцене. Создать новый?", "Да", "Нет"))
                return;
        }

        // Создаём Canvas
        GameObject canvasGO = new GameObject("GameUI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Проверяем EventSystem (Unity 6.3+ метод)
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Используем InputSystemUIInputModule вместо StandaloneInputModule
            // т.к. проект настроен на Input System Package
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[SceneSetupTools] EventSystem создан с InputSystemUIInputModule!");
        }

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameUI");
        Debug.Log("[SceneSetupTools] GameUI Canvas создан!");
        Selection.activeGameObject = canvasGO;
    }

    void CreateHUD()
    {
        var canvas = GameObject.Find("GameUI");
        if (canvas == null)
        {
            Debug.LogError("[SceneSetupTools] GameUI Canvas не найден! Сначала создай Canvas.");
            return;
        }

        // Создаём HUD Panel
        GameObject hud = new GameObject("HUD");
        hud.transform.SetParent(canvas.transform, false);

        RectTransform hudRect = hud.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(10, -10);
        hudRect.sizeDelta = new Vector2(300, 150);

        Image hudImage = hud.AddComponent<Image>();
        hudImage.color = new Color(0, 0, 0, 0.4f);

        // Создаём тексты
        CreateTextElement(hud, "TimeText", "День 1 - 06:00", new Vector2(0, 0), 24);
        CreateTextElement(hud, "HPText", "HP: 100%", new Vector2(0, -30), 20);
        CreateTextElement(hud, "QiText", "Ци: 0/100", new Vector2(0, -55), 20);

        Undo.RegisterCreatedObjectUndo(hud, "Create HUD");
        Debug.Log("[SceneSetupTools] HUD создан!");
        Selection.activeGameObject = hud;
    }

    void CreateTextElement(GameObject parent, string name, string text, Vector2 position, int fontSize)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(280, fontSize + 5);

        // Пытаемся использовать TextMeshPro
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            var tmp = textGO.AddComponent(tmpType) as TMP_Text;
            if (tmp != null)
            {
                tmp.text = text;
                tmp.fontSize = fontSize;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.TopLeft;
            }
        }
        else
        {
            var uiText = textGO.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.color = Color.white;
            uiText.alignment = TextAnchor.UpperLeft;
        }
    }

    #endregion

    #region Full Scene

    void SetupFullScene()
    {
        // 1. GameManager & Systems
        CreateGameManagerAndSystems();

        // 2. Player
        CreatePlayer();

        // 3. UI
        CreateGameUI();
        CreateHUD();

        // 4. Настраиваем контроллеры
        SetupTimeController();
        SetupSaveManager();

        Debug.Log("[SceneSetupTools] === ПОЛНАЯ НАСТРОЙКА ЗАВЕРШЕНА ===");
        Debug.Log("[SceneSetupTools] Теперь выполни ручные действия по инструкции *_SemiAuto.md");
    }

    #endregion
    
    #region Helpers
    
    /// <summary>
    /// Безопасная установка свойства SerializedObject.
    /// </summary>
    private void SetProperty(SerializedObject so, string propertyName, object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[SceneSetupTools] Property '{propertyName}' not found");
            return;
        }
        
        switch (value)
        {
            case int intVal:
                prop.intValue = intVal;
                break;
            case float floatVal:
                prop.floatValue = floatVal;
                break;
            case bool boolVal:
                prop.boolValue = boolVal;
                break;
            case string strVal:
                prop.stringValue = strVal;
                break;
            case long longVal:
                prop.longValue = longVal;
                break;
            case double doubleVal:
                prop.doubleValue = doubleVal;
                break;
            default:
                Debug.LogWarning($"[SceneSetupTools] Unsupported type for property '{propertyName}': {value?.GetType().Name}");
                break;
        }
    }
    
    #endregion
}
#endif // FIX GEN-C01: End #if UNITY_EDITOR (2026-04-11)
