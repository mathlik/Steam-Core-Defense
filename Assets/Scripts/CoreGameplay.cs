using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Управляет температурой реактора и взаимодействием с оборудованием.</summary>
public class CoreGameplay : MonoBehaviour
{
    [Header("Temperature")]
    [Tooltip("Температура, при которой реактор перегревается и игра заканчивается.")]
    [SerializeField, Min(1f)] private float maxTemperature = 100f;
    [Tooltip("Температура в начале игры и температура, до которой охлаждается реактор.")]
    [SerializeField] private float startingTemperature = 25f;
    [SerializeField, Min(0f)] private float heatPerSecond = 3.5f;
    [SerializeField, Min(0f)] private float coolingPerSecond = 38f;

    [Header("Temperature Thresholds")]
    [Tooltip("Выше этой температуры появляется предупреждение в интерфейсе.")]
    [SerializeField] private float warningTemperature = 75f;
    [Tooltip("Выше этой температуры включается тревожная лампа и звук.")]
    [SerializeField] private float alarmTemperature = 65f;
    [Tooltip("Ниже этой температуры охлаждение завершается.")]
    [SerializeField] private float coolingTargetTemperature = 25f;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 3.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private Camera interactionCamera;
    [SerializeField, Min(0f)] private float alarmLightTemperatureFloor = 0f;
    [SerializeField] private Color temperatureBarSafeColor = Color.cyan;
    [SerializeField] private Color temperatureBarDangerColor = Color.red;
    [Tooltip("Клапаны проверяются сверху вниз. Первый элемент массива считается клапаном 1.")]
    [SerializeField] private Collider[] valves;
    [Tooltip("Пауза после поломки вентиля. До истечения этого времени паук не сможет выбрать этот вентиль снова после его закрытия.")]
    [SerializeField, Min(0f)] private float valveCooldownSeconds = 12f;
    [SerializeField] private int startingValve = 1;
    [SerializeField] private Collider cooler;

    [Header("Scene References")]
    [SerializeField] private Light warningLight;
    [SerializeField] private Light coolerLight;
    [SerializeField, Min(0f)] private float warningLightMinIntensity = 2f;
    [SerializeField, Min(0f)] private float warningLightMaxIntensity = 9f;
    [SerializeField, Min(0f)] private float warningLightBlinkRate = 5f;
    [SerializeField] private Color coreGlowStart = Color.yellow;
    [SerializeField] private Color coreGlowDanger = Color.red;
    [SerializeField, Min(0f)] private float coreGlowIntensity = 1f;
    [SerializeField, Min(0f)] private float coreGlowDangerBonus = 4f;
    [SerializeField] private Renderer coreRenderer;
    [SerializeField] private ParticleSystem[] steamEffects;
    [SerializeField] private AtmosphereAudio audioController;

    [Header("Spider Spawning")]
    [SerializeField] private SteamSpider spiderPrefab;
    [Tooltip("Точки, в которых могут появляться пауки. Для каждого нового паука выбирается случайная точка.")]
    [SerializeField] private Transform[] spiderSpawnPoints;
    [SerializeField, Min(1)] private int maxConcurrentSpiders = 3;
    [SerializeField, Min(0.1f)] private float spiderSpawnInterval = 6f;
    [SerializeField, Min(0.1f)] private float spiderSpawnDelayAfterDeath = 3f;
    [SerializeField] private bool spawnSpiderOnStart = true;

    [Header("Objective Panel")]
    [SerializeField] private bool showValveObjectives = true;
    [SerializeField] private Vector2 objectivePanelPosition = new Vector2(24f, -120f);
    [SerializeField] private Vector2 objectivePanelSize = new Vector2(330f, 300f);
    [SerializeField] private Color objectivePanelColor = new Color(0.02f, 0.04f, 0.07f, 0.85f);

    [Header("Scene UI")]
    [SerializeField] private Text temperatureText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text promptText;
    [SerializeField] private Text objectiveText;
    [Tooltip("Отдельный текст HUD с обратным отсчётом доступности вентилей для паука.")]
    [SerializeField] private Text valveCooldownText;
    [SerializeField] private Slider temperatureBar;
    [SerializeField] private Image barFill;
    [SerializeField] private GameOverUI gameOverScreen;

    private readonly HashSet<int> brokenValves = new HashSet<int>();
    private readonly Dictionary<int, float> valveCooldownUntil = new Dictionary<int, float>();
    private bool crisisOccurred;
    private Material coreMaterial;
    private float currentTemperature;

    private float nextSpiderSpawnTime;
    private int activeValve = 1;
    private bool valveDamaged;
    private bool valveClosedReadyForCooling;
    private bool cooling;
    private bool gameOver;
    private bool victory;
    private float survivalTime;
    [SerializeField, Min(1f)] private float survivalTimeToWin = 120f;

    private void Awake()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (audioController == null) audioController = FindFirstObjectByType<AtmosphereAudio>();

        startingTemperature = Mathf.Clamp(startingTemperature, 0f, maxTemperature);
        coolingTargetTemperature = Mathf.Clamp(coolingTargetTemperature, 0f, maxTemperature);
        warningTemperature = Mathf.Clamp(warningTemperature, 0f, maxTemperature);
        alarmTemperature = Mathf.Clamp(alarmTemperature, 0f, maxTemperature);
        currentTemperature = startingTemperature;

        activeValve = valves != null && valves.Length > 0
            ? Mathf.Clamp(startingValve, 1, valves.Length)
            : 0;

        crisisOccurred = false;

        if (coreRenderer != null) coreMaterial = coreRenderer.material;
        if (audioController != null) audioController.SetAlarm(false);

        if (steamEffects != null)
        {
            foreach (ParticleSystem steam in steamEffects)
            {
                if (steam != null) steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (warningLight != null) warningLight.enabled = false;
        if (coolerLight != null) coolerLight.enabled = false;

        nextSpiderSpawnTime = spawnSpiderOnStart ? Time.time : float.PositiveInfinity;
        UpdateObjectivePanel();
    }

    private void Update()
    {
        if (gameOver || victory) return;

        survivalTime += Time.deltaTime;
        UpdateObjectivePanel();
        if (survivalTime >= survivalTimeToWin)
        {
            if (CanWinGame())
            {
                WinGame();
                return;
            }

            
        }

        if (spiderPrefab != null && Time.time >= nextSpiderSpawnTime && GetActiveSpiderCount() < Mathf.Max(1, maxConcurrentSpiders))
        {
            Transform spawnPoint = GetRandomSpiderSpawnPoint();
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Instantiate(spiderPrefab, spawnPosition, spawnPoint != null ? spawnPoint.rotation : Quaternion.identity);
            nextSpiderSpawnTime = Time.time + spiderSpawnInterval;
        }

        if (brokenValves.Count > 0)
        {
            currentTemperature += heatPerSecond * Time.deltaTime;
        }
        HandleInteraction();

        if (cooling)
        {
            currentTemperature -= coolingPerSecond * Time.deltaTime;
            if (currentTemperature <= coolingTargetTemperature)
            {
                currentTemperature = coolingTargetTemperature;
                cooling = false;
                if (valves != null && valves.Length > 0)
                {
                    activeValve = activeValve % valves.Length + 1;
                }
                else
                {
                    activeValve = 0;
                }

                if (audioController != null) audioController.SetCooling(false);
            }
        }

        currentTemperature = Mathf.Clamp(currentTemperature, 0f, maxTemperature);
        UpdateFeedback();
        if (currentTemperature >= maxTemperature) LoseGame();
    }

    public void ScheduleSpiderSpawnAfterDeath()
    {
        nextSpiderSpawnTime = Mathf.Min(nextSpiderSpawnTime, Time.time + spiderSpawnDelayAfterDeath);
    }

    private int GetActiveSpiderCount()
    {
        return FindObjectsByType<SteamSpider>(FindObjectsSortMode.None).Length;
    }

    private Transform GetRandomSpiderSpawnPoint()
    {
        if (spiderSpawnPoints == null || spiderSpawnPoints.Length == 0) return null;
        List<Transform> validPoints = new List<Transform>();
        foreach (Transform point in spiderSpawnPoints)
        {
            if (point != null) validPoints.Add(point);
        }
        return validPoints.Count == 0 ? null : validPoints[Random.Range(0, validPoints.Count)];
    }

    private void HandleInteraction()
    {
        if (!Input.GetKeyDown(interactionKey) || interactionCamera == null) return;

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        int valveNumber = GetValveNumber(hit.collider);
        if (valveNumber > 0)
        {
            UseValve(valveNumber);
        }
        else if (IsCooler(hit.collider))
        {
            UseCooler();
        }
    }

    private void UseValve(int valveNumber)
    {
        if (brokenValves.Contains(valveNumber))
        {
            brokenValves.Remove(valveNumber);
            valveDamaged = brokenValves.Count > 0;
            if (valveDamaged)
            {
                foreach (int remainingValve in brokenValves)
                {
                    activeValve = remainingValve;
                    break;
                }
            }
            UpdateObjectivePanel();

            if (brokenValves.Count == 0)
            {
                valveClosedReadyForCooling = true;
                SetSteam(false);
                SetStatus("ALL VALVES CLOSED - USE COOLER", Color.cyan);
            }
            else
            {
                SetStatus("VALVE " + valveNumber + " CLOSED - CLOSE REMAINING", Color.yellow);
            }
            return;
        }

        SetStatus("ONLY BROKEN VALVES CAN BE CLOSED", Color.yellow);
    }

    private int GetValveNumber(Collider hitCollider)
    {
        if (valves == null) return 0;

        for (int i = 0; i < valves.Length; i++)
        {
            Collider configuredValve = valves[i];
            if (configuredValve == null) continue;
            if (hitCollider == configuredValve || hitCollider.transform.IsChildOf(configuredValve.transform))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private bool IsCooler(Collider hitCollider)
    {
        return cooler != null && (hitCollider == cooler || hitCollider.transform.IsChildOf(cooler.transform));
    }

    private void UseCooler()
    {
        if (brokenValves.Count > 0)
        {
            SetStatus("CLOSE ALL BROKEN VALVES FIRST", Color.yellow);
            return;
        }

        valveClosedReadyForCooling = false;
        cooling = true;
        if (audioController != null) audioController.SetCooling(true);
        SetSteam(false);
        SetStatus("COOLING SYSTEM ACTIVE", Color.cyan);
    }

    private void UpdateFeedback()
    {
        float temperature01 = Mathf.Clamp01(currentTemperature / maxTemperature);
        float danger01 = Mathf.InverseLerp(alarmTemperature, maxTemperature, currentTemperature);
        bool alarm = currentTemperature > alarmTemperature;
        bool warning = currentTemperature > warningTemperature;

        if (temperatureText != null) temperatureText.text = "REACTOR TEMPERATURE  " + Mathf.RoundToInt(currentTemperature) + " °C";
        if (temperatureBar != null) temperatureBar.value = temperature01;
        if (barFill != null) barFill.color = Color.Lerp(temperatureBarSafeColor, temperatureBarDangerColor, temperature01);
        if (audioController != null) audioController.SetAlarm(alarm);

        if (warningLight != null)
        {
            warningLight.intensity = alarm ? Mathf.Lerp(warningLightMinIntensity, warningLightMaxIntensity, danger01) : 0f;
            bool blinkOn = warningLightBlinkRate <= 0f || Mathf.FloorToInt(Time.time * warningLightBlinkRate) % 2 == 0;
            warningLight.enabled = alarm && currentTemperature >= alarmLightTemperatureFloor && (currentTemperature < warningTemperature || blinkOn);
        }

        if (coreMaterial != null)
        {
            Color glow = Color.Lerp(coreGlowStart, coreGlowDanger, danger01);
            Color litGlow = glow * (coreGlowIntensity + danger01 * coreGlowDangerBonus);
            if (coreMaterial.HasProperty("_BaseColor")) coreMaterial.SetColor("_BaseColor", glow);
            if (coreMaterial.HasProperty("_Color")) coreMaterial.SetColor("_Color", glow);
            if (coreMaterial.HasProperty("_EmissionColor"))
            {
                coreMaterial.EnableKeyword("_EMISSION");
                coreMaterial.SetColor("_EmissionColor", litGlow);
            }
        }

        if (promptText != null) promptText.text = "E  INTERACT";

        if (coolerLight != null) coolerLight.enabled = cooling;

        if (valveDamaged) SetStatus("VALVE DAMAGED - CLOSE IT", Color.red);
        else if (valveClosedReadyForCooling) SetStatus("VALVE CLOSED - USE COOLER", Color.cyan);
        else if (currentTemperature > warningTemperature) SetStatus("WARNING: ACTIVE VALVE " + activeValve, Color.yellow);
        else SetStatus("SYSTEM STABLE", Color.green);
    }

    public bool IsVictory() => victory;

    private void WinGame()
    {
        victory = true;
        Time.timeScale = 0f;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetControlsLocked(true);
        WrenchTool wrench = FindFirstObjectByType<WrenchTool>();
        if (wrench != null) wrench.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetStatus("REACTOR SECURED", Color.green);
        if (gameOverScreen != null) gameOverScreen.Show(this);
    }

    private bool CanWinGame()
    {
        if (!crisisOccurred) return false;
        if (valves == null || valves.Length == 0) return false;
        if (brokenValves.Count > 0) return false;
        if (cooling) return false;
        if (currentTemperature > coolingTargetTemperature + 1f) return false;
        return true;
    }

    private void LoseGame()
    {
        gameOver = true;
        SetSteam(true);
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetControlsLocked(true);
        WrenchTool wrench = FindFirstObjectByType<WrenchTool>();
        if (wrench != null) wrench.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetStatus("REACTOR MELTDOWN", Color.red);
        if (promptText != null) promptText.text = "SYSTEM FAILURE";
        if (gameOverScreen != null) gameOverScreen.Show(this);
    }

    private void SetSteam(bool enabled)
    {
        if (steamEffects == null) return;

        foreach (ParticleSystem steam in steamEffects)
        {
            if (steam == null) continue;
            if (enabled) steam.Play();
            else steam.Stop();
        }

        if (audioController != null) audioController.SetSteam(enabled);
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }

    public bool IsGameOver() => gameOver;

    public Transform GetSpiderTarget()
    {
        if (valves == null || valves.Length == 0) return null;

        List<int> available = new List<int>();
        for (int i = 0; i < valves.Length; i++)
        {
            int valveNumber = i + 1;
            if (valves[i] != null && !brokenValves.Contains(valveNumber) && IsValveAvailableToSpider(valveNumber))
                available.Add(i);
        }

        if (available.Count == 0) return null;
        int selectedIndex = available[Random.Range(0, available.Count)];
        return valves[selectedIndex].transform;
    }

    public bool IsSpiderTargetAvailable(Transform target)
    {
        if (target == null || valves == null) return false;

        for (int i = 0; i < valves.Length; i++)
        {
            int valveNumber = i + 1;
            if (valves[i] != null && valves[i].transform == target &&
                !brokenValves.Contains(valveNumber) && IsValveAvailableToSpider(valveNumber))
                return true;
        }

        return false;
    }

    public bool BreakValveFromSpider(Transform spider, Transform target, float breakDistance)
    {
        if (spider == null || target == null || valves == null || valves.Length == 0) return false;
        breakDistance = Mathf.Max(0.1f, breakDistance);

        for (int index = 0; index < valves.Length; index++)
        {
            Collider valve = valves[index];
            int valveNumber = index + 1;
            if (valve == null || valve.transform != target || brokenValves.Contains(valveNumber) ||
                !IsValveAvailableToSpider(valveNumber)) continue;

            Vector3 closestPoint = valve.ClosestPoint(spider.position);
            if ((closestPoint - spider.position).sqrMagnitude > breakDistance * breakDistance) return false;

            BreakValve(index);
            return true;
        }

        return false;
    }

    private bool IsValveAvailableToSpider(int valveNumber)
    {
        return !valveCooldownUntil.TryGetValue(valveNumber, out float cooldownEnd) || Time.time >= cooldownEnd;
    }

    public float GetValveCooldownRemaining(int valveNumber)
    {
        if (!valveCooldownUntil.TryGetValue(valveNumber, out float cooldownEnd)) return 0f;
        return Mathf.Max(0f, cooldownEnd - Time.time);
    }

    private void BreakValve(int valveIndex)
    {
        valveCooldownUntil[valveIndex + 1] = Time.time + valveCooldownSeconds;
        brokenValves.Add(valveIndex + 1);
        activeValve = valveIndex + 1;
        valveDamaged = true;
        crisisOccurred = true;
        SetSteam(true);
        SetStatus($"VALVE {GetValveName(valveIndex)} BROKEN - CLOSE IT AND USE COOLER", Color.red);
        UpdateObjectivePanel();
    }

    private string GetValveName(int index)
    {
        if (index == 0) return "1";
        if (index == 1) return "2";
        if (index == 2) return "3";
        return (index + 1).ToString();
    }

    private void UpdateObjectivePanel()
    {
        if (!showValveObjectives || objectiveText == null) return;
        string text = "TASKS\n";
        if (brokenValves.Count == 0)
        {
            text += valveClosedReadyForCooling ? "Use the cooler" : "No broken valves";
        }
        else
        {
            text += "Broken valves - close:\n";
            foreach (int v in brokenValves)
            {
                text += $"[ ] Valve {v}\n";
            }
        }
        

        objectiveText.text = text;

        if (valveCooldownText != null)
        {
            string cooldownText = "VALVE SPIDER COOLDOWN\n";
            bool hasCooldown = false;
            if (valves != null)
            {
                for (int i = 0; i < valves.Length; i++)
                {
                    float remaining = GetValveCooldownRemaining(i + 1);
                    if (remaining <= 0f) continue;
                    hasCooldown = true;
                    cooldownText += $"Valve {i + 1}: {remaining:0.0}s\n";
                }
            }
            valveCooldownText.text = hasCooldown ? cooldownText : "VALVE SPIDER COOLDOWN\nREADY";
        }
    }

}
