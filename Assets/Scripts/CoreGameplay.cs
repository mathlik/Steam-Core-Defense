using UnityEngine;
using UnityEngine.UI;

/// <summary>Управляет температурой реактора и взаимодействием с оборудованием.</summary>
public class CoreGameplay : MonoBehaviour
{
    [Header("Temperature")]
    [SerializeField] private float maxTemperature = 100f;
    [SerializeField] private float currentTemperature = 25f;
    [SerializeField] private float heatPerSecond = 3.5f;
    [SerializeField] private float coolingPerSecond = 38f;
    [SerializeField] private float valveCooldown = 7f;
    [SerializeField] private float interactionDistance = 3.5f;

    private Text temperatureText;
    private Text statusText;
    private Text promptText;
    private Slider temperatureBar;
    private Image barFill;
    private Light warningLight;
    private Material coreMaterial;
    private AtmosphereAudio audioController;
    private ParticleSystem[] steamEffects;

    private float nextValveTime;
    private int activeValve = 1;
    private bool valveOpened;
    private bool cooling;
    private bool gameOver;

    private void Awake()
    {
        steamEffects = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem steam in steamEffects)
        {
            if (steam != null) steam.Stop();
        }

        audioController = gameObject.AddComponent<AtmosphereAudio>();
        warningLight = GameObject.Find("BoilerWarningLight")?.GetComponent<Light>();

        GameObject core = GameObject.Find("BoilerCore");
        if (core != null) coreMaterial = core.GetComponent<Renderer>().material;

        CreateUserInterface();
    }

    private void Update()
    {
        if (gameOver) return;

        currentTemperature += heatPerSecond * Time.deltaTime;
        HandleInteraction();

        if (cooling)
        {
            currentTemperature -= coolingPerSecond * Time.deltaTime;
            if (currentTemperature <= 25f)
            {
                currentTemperature = 25f;
                cooling = false;
                valveOpened = false;
                activeValve = activeValve % 3 + 1;
                audioController.SetCooling(false);
            }
        }

        currentTemperature = Mathf.Clamp(currentTemperature, 0f, maxTemperature);
        UpdateFeedback();
        if (currentTemperature >= maxTemperature) LoseGame();
    }

    private void HandleInteraction()
    {
        if (!Input.GetKeyDown(KeyCode.E) || Camera.main == null) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        string name = hit.collider.gameObject.name;
        if (name.Contains("Valve_0")) UseValve(name);
        else if (name.Contains("Cooler")) UseCooler();
    }

    private void UseValve(string objectName)
    {
        int valveNumber = objectName.Contains("Valve_01") ? 1 :
            objectName.Contains("Valve_02") ? 2 : 3;

        if (valveNumber != activeValve)
        {
            SetStatus("WRONG VALVE - ACTIVE: " + activeValve, Color.red);
            return;
        }

        if (Time.time < nextValveTime)
        {
            SetStatus("VALVE COOLDOWN: " + Mathf.CeilToInt(nextValveTime - Time.time) + "s", Color.yellow);
            return;
        }

        valveOpened = true;
        nextValveTime = Time.time + valveCooldown;
        SetSteam(true);
        SetStatus("PRESSURE RELEASED - GO TO COOLER", Color.cyan);
    }

    private void UseCooler()
    {
        if (!valveOpened)
        {
            SetStatus("OPEN ACTIVE VALVE FIRST", Color.yellow);
            return;
        }

        cooling = true;
        audioController.SetCooling(true);
        SetSteam(false);
        SetStatus("COOLING SYSTEM ACTIVE", Color.cyan);
    }

    private void UpdateFeedback()
    {
        float temperature01 = currentTemperature / maxTemperature;
        float danger01 = Mathf.InverseLerp(55f, maxTemperature, currentTemperature);
        bool alarm = currentTemperature > 75f;

        temperatureText.text = "REACTOR TEMPERATURE  " + Mathf.RoundToInt(currentTemperature) + " °C";
        temperatureBar.value = temperature01;
        barFill.color = Color.Lerp(Color.cyan, Color.red, temperature01);
        audioController.SetAlarm(alarm);

        if (warningLight != null)
        {
            warningLight.intensity = alarm ? 2f + danger01 * 7f : 0f;
            warningLight.enabled = alarm && (currentTemperature < 85f || Mathf.FloorToInt(Time.time * 5f) % 2 == 0);
        }

        if (coreMaterial != null)
        {
            Color glow = Color.Lerp(Color.yellow, Color.red, danger01);
            coreMaterial.SetColor("_EmissionColor", glow * (1f + danger01 * 4f));
        }

        promptText.text = valveOpened ? "E  USE COOLER" : "E  OPEN ACTIVE VALVE " + activeValve;

        if (!valveOpened && currentTemperature > 75f) SetStatus("WARNING: ACTIVE VALVE " + activeValve, Color.yellow);
        else if (!valveOpened) SetStatus("SYSTEM STABLE", Color.green);
    }

    private void LoseGame()
    {
        gameOver = true;
        SetSteam(true);
        SetStatus("REACTOR MELTDOWN", Color.red);
        promptText.text = "SYSTEM FAILURE";
        GameOverUI screen = gameObject.AddComponent<GameOverUI>();
        screen.Initialize(this);
    }

    private void SetSteam(bool enabled)
    {
        foreach (ParticleSystem steam in steamEffects)
        {
            if (steam == null) continue;
            if (enabled) steam.Play();
            else steam.Stop();
        }

        audioController.SetSteam(enabled);
    }

    private void CreateUserInterface()
    {
        GameObject canvasObject = new GameObject("Gameplay UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("TopPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.04f, 0.07f, 0.92f);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 170f));

        temperatureText = CreateText("Temperature", canvasObject.transform, "REACTOR TEMPERATURE", 32, Color.white);
        SetRect(temperatureText.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(800f, 45f));

        statusText = CreateText("Status", canvasObject.transform, "SYSTEM STABLE", 27, Color.green);
        SetRect(statusText.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(800f, 40f));

        promptText = CreateText("Prompt", canvasObject.transform, "E OPEN ACTIVE VALVE", 22, Color.white);
        SetRect(promptText.rectTransform, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(900f, 42f));

        GameObject barObject = new GameObject("TemperatureBar");
        barObject.transform.SetParent(canvasObject.transform, false);
        temperatureBar = barObject.AddComponent<Slider>();
        temperatureBar.minValue = 0f;
        temperatureBar.maxValue = 1f;
        SetRect(barObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(650f, 28f));

        GameObject background = new GameObject("Background");
        background.transform.SetParent(barObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.12f, 0.15f);
        SetRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barObject.transform, false);
        barFill = fill.AddComponent<Image>();
        barFill.color = Color.cyan;
        SetRect(fill.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.25f, 1f), Vector2.zero, Vector2.zero);
        temperatureBar.fillRect = fill.GetComponent<RectTransform>();
    }

    private Text CreateText(string objectName, Transform parent, string value, int size, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnGUI()
    {
        if (!gameOver) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 64,
            fontStyle = FontStyle.Bold
        };
        title.normal.textColor = Color.red;

        GUI.Label(new Rect(0f, Screen.height * 0.25f, Screen.width, 90f), "REACTOR MELTDOWN", title);

        GUIStyle button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold
        };

        float width = Mathf.Min(420f, Screen.width * 0.4f);
        float left = (Screen.width - width) * 0.5f;

        if (GUI.Button(new Rect(left, Screen.height * 0.48f, width, 70f), "RESTART", button))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SteamCoreDefenseLevel");
        }

        if (GUI.Button(new Rect(left, Screen.height * 0.6f, width, 70f), "MAIN MENU", button))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }

    public bool IsGameOver() => gameOver;

}