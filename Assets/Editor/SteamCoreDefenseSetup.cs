using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class SteamCoreDefenseSetup
{
    [MenuItem("Tools/Steam Core Defense/Create Scene UI")]
    public static void CreateSceneUI()
    {
        CoreGameplay gameplay = Object.FindFirstObjectByType<CoreGameplay>();
        if (gameplay == null) { EditorUtility.DisplayDialog("Steam Core Defense", "В открытой сцене не найден CoreGameplay.", "OK"); return; }
        Canvas canvas = FindOrCreateCanvas("Gameplay UI");
        Transform root = FindOrCreateChild(canvas.transform, "Gameplay HUD").transform;
        Stretch(root.GetComponent<RectTransform>());

        Text temperature = CreateText(root, "TemperatureText", "REACTOR TEMPERATURE  25 °C", 24, Color.white);
        SetRect(temperature.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -35), new Vector2(600, 45));
        Slider bar = CreateSlider(root, "TemperatureBar");
        SetRect(bar.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -85), new Vector2(500, 24));
        Image fill = bar.fillRect.GetComponent<Image>();
        Text status = CreateText(root, "StatusText", "SYSTEM READY", 24, Color.white);
        SetRect(status.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -125), new Vector2(850, 45));
        Text prompt = CreateText(root, "PromptText", "E  INTERACT", 22, Color.white);
        SetRect(prompt.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 45), new Vector2(500, 40));
        Text objectives = CreateText(root, "ObjectiveText", "TASKS", 22, Color.white);
        SetRect(objectives.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -235f), new Vector2(360f, 280f));
        Text valveCooldowns = CreateText(root, "ValveCooldownText", "VALVE SPIDER COOLDOWN\nREADY", 20, Color.yellow);
        SetRect(valveCooldowns.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -180f), new Vector2(360f, 180f));
        valveCooldowns.alignment = TextAnchor.UpperCenter;
        valveCooldowns.horizontalOverflow = HorizontalWrapMode.Wrap;
        valveCooldowns.verticalOverflow = VerticalWrapMode.Overflow;

        Transform pause = FindOrCreateChild(canvas.transform, "Pause Root").transform;
        Stretch(pause.GetComponent<RectTransform>());
        Image pauseBg = CreatePanel(pause, "Pause Background", new Color(0, 0, 0, .78f)); Stretch(pauseBg.rectTransform);
        Text pauseTitle = CreateText(pause, "Pause Title", "PAUSED", 64, Color.white);
        SetRect(pauseTitle.rectTransform, new Vector2(.5f, .72f), new Vector2(.5f, .72f), Vector2.zero, new Vector2(700, 100));
        Button continueButton = CreateButton(pause, "Continue Button", "CONTINUE");
        SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(420, 70));
        Button pauseMenuButton = CreateButton(pause, "Main Menu Button", "MAIN MENU");
        SetRect(pauseMenuButton.GetComponent<RectTransform>(), new Vector2(.5f, .36f), new Vector2(.5f, .36f), Vector2.zero, new Vector2(420, 70));
        pause.gameObject.SetActive(false);

        Transform over = FindOrCreateChild(canvas.transform, "Game Over Root").transform;
        Stretch(over.GetComponent<RectTransform>());
        Image overBg = CreatePanel(over, "Game Over Background", new Color(0, 0, 0, .82f)); Stretch(overBg.rectTransform);
        Text overTitle = CreateText(over, "Game Over Title", "REACTOR MELTDOWN", 58, Color.red);
        SetRect(overTitle.rectTransform, new Vector2(.5f, .72f), new Vector2(.5f, .72f), Vector2.zero, new Vector2(1000, 100));
        Button restart = CreateButton(over, "Restart Button", "RESTART");
        SetRect(restart.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(420, 70));
        Button overMenuButton = CreateButton(over, "Game Over Main Menu Button", "MAIN MENU");
        SetRect(overMenuButton.GetComponent<RectTransform>(), new Vector2(.5f, .36f), new Vector2(.5f, .36f), Vector2.zero, new Vector2(420, 70));
        over.gameObject.SetActive(false);

        Transform victory = FindOrCreateChild(canvas.transform, "Victory Root").transform;
        Stretch(victory.GetComponent<RectTransform>());
        Image victoryBg = CreatePanel(victory, "Victory Background", new Color(0.01f, 0.08f, 0.04f, .86f)); Stretch(victoryBg.rectTransform);
        Text victoryTitle = CreateText(victory, "Victory Title", "REACTOR SECURED", 58, Color.green);
        SetRect(victoryTitle.rectTransform, new Vector2(.5f, .72f), new Vector2(.5f, .72f), Vector2.zero, new Vector2(1000, 100));
        Button victoryRestart = CreateButton(victory, "Victory Restart Button", "PLAY AGAIN");
        SetRect(victoryRestart.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(420, 70));
        Button victoryMenuButton = CreateButton(victory, "Victory Main Menu Button", "MAIN MENU");
        SetRect(victoryMenuButton.GetComponent<RectTransform>(), new Vector2(.5f, .36f), new Vector2(.5f, .36f), Vector2.zero, new Vector2(420, 70));
        victory.gameObject.SetActive(false);

        SetObject(gameplay, "temperatureText", temperature); SetObject(gameplay, "statusText", status);
        SetObject(gameplay, "promptText", prompt); SetObject(gameplay, "objectiveText", objectives);
        SetObject(gameplay, "valveCooldownText", valveCooldowns);
        SetObject(gameplay, "temperatureBar", bar); SetObject(gameplay, "barFill", fill);
        GameOverUI gameOver = gameplay.GetComponent<GameOverUI>() ?? gameplay.gameObject.AddComponent<GameOverUI>();
        SetObject(gameplay, "gameOverScreen", gameOver); SetObject(gameOver, "root", over.gameObject);
        SetObject(gameOver, "victoryRoot", victory.gameObject);
        SetObject(gameOver, "titleText", overTitle); SetObject(gameOver, "victoryTitleText", victoryTitle);
        SetObject(gameOver, "restartButton", restart); SetObject(gameOver, "mainMenuButton", overMenuButton);
        SetObject(gameOver, "victoryRestartButton", victoryRestart); SetObject(gameOver, "victoryMainMenuButton", victoryMenuButton);
        PauseMenu pauseMenu = gameplay.GetComponent<PauseMenu>() ?? gameplay.gameObject.AddComponent<PauseMenu>();
        SetObject(pauseMenu, "pauseRoot", pause.gameObject); SetObject(pauseMenu, "continueButton", continueButton); SetObject(pauseMenu, "mainMenuButton", pauseMenuButton);
        EnsureEventSystem(); EditorSceneManager.MarkSceneDirty(gameplay.gameObject.scene); Selection.activeGameObject = canvas.gameObject;
        EditorUtility.DisplayDialog("Steam Core Defense", "UI создан. Теперь его можно редактировать и переносить.", "OK");
    }

    [MenuItem("Tools/Steam Core Defense/Setup Spider Navigation")]
    public static void SetupSpiderNavigation()
    {
        GameObject navigationRoot = GameObject.Find("Spider Navigation");
        if (navigationRoot == null)
        {
            navigationRoot = new GameObject("Spider Navigation");
            Undo.RegisterCreatedObjectUndo(navigationRoot, "Create Spider Navigation");
        }

        NavMeshSurface surface = navigationRoot.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(navigationRoot);
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.overrideTileSize = true;
        surface.tileSize = 128;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.1f;
        surface.BuildNavMesh();

        SteamSpider[] spiders = Object.FindObjectsByType<SteamSpider>(FindObjectsSortMode.None);
        foreach (SteamSpider spider in spiders)
        {
            NavMeshAgent agent = spider.GetComponent<NavMeshAgent>();
            if (agent == null) agent = Undo.AddComponent<NavMeshAgent>(spider.gameObject);
            agent.radius = 0.45f;
            agent.height = 1f;
            agent.speed = 3f;
            agent.angularSpeed = 720f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1.2f;
            agent.autoBraking = true;
        }

        EditorSceneManager.MarkSceneDirty(navigationRoot.scene);
        Selection.activeGameObject = navigationRoot;
        EditorUtility.DisplayDialog("Steam Core Defense", "NavMeshSurface создан и сетка навигации построена. Компоненты NavMeshAgent назначены паукам.", "OK");
    }

    [MenuItem("Tools/Steam Core Defense/Create Wrench In Scene")]
    public static void CreateWrenchInScene()
    {
        WrenchTool tool = Object.FindFirstObjectByType<WrenchTool>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wrench.prefab");
        if (tool == null || prefab == null) { EditorUtility.DisplayDialog("Steam Core Defense", "Не найден WrenchTool или Wrench.prefab.", "OK"); return; }
        Transform model = tool.transform.Find("Wrench Model");
        if (model == null) model = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, tool.transform)).transform;
        model.name = "Wrench Model";
        model.localPosition = new Vector3(0.35f, -0.35f, 0.55f);
        model.localRotation = Quaternion.Euler(-110f, 180f, 0f);
        model.localScale = Vector3.one * 0.55f;
        Camera playerCamera = tool.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        SetObject(tool, "aimCamera", playerCamera);
        SetObject(tool, "wrenchModel", model);
        EditorSceneManager.MarkSceneDirty(tool.gameObject.scene); Selection.activeGameObject = model.gameObject;
    }

    private static Canvas FindOrCreateCanvas(string name)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length > 0) return canvases[0];
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        return canvas;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name); if (child != null) return child.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Color color)
    {
        GameObject go = FindOrCreateChild(parent, name); Text text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleCenter; return text;
    }

    private static Image CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = FindOrCreateChild(parent, name); Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>(); image.color = color; return image;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = FindOrCreateChild(parent, name); Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>(); image.color = new Color(.12f, .18f, .24f, 1f);
        Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>(); Text text = CreateText(go.transform, "Label", label, 28, Color.white); Stretch(text.rectTransform); return button;
    }

    private static Slider CreateSlider(Transform parent, string name)
    {
        GameObject go = FindOrCreateChild(parent, name); Slider slider = go.GetComponent<Slider>() ?? go.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        Image background = CreatePanel(go.transform, "Background", new Color(.1f, .12f, .15f, 1f)); Stretch(background.rectTransform);
        Image fill = CreatePanel(go.transform, "Fill", Color.cyan); Stretch(fill.rectTransform); slider.fillRect = fill.rectTransform; slider.targetGraphic = fill; slider.direction = Slider.Direction.LeftToRight; return slider;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size) { rect.anchorMin = min; rect.anchorMax = max; rect.anchoredPosition = position; rect.sizeDelta = size; }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
    private static void SetObject(Object target, string property, Object value) { SerializedObject so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedProperties(); }
    private static void EnsureEventSystem() { if (Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); }
}
