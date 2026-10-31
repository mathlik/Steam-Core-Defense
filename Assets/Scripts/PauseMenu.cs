using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Управляет меню паузы, созданным в сцене.</summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool paused;
    private PlayerController player;
    private WrenchTool wrench;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        wrench = FindFirstObjectByType<WrenchTool>();

        if (continueButton != null) continueButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    private void Update()
    {
        CoreGameplay gameplay = FindFirstObjectByType<CoreGameplay>();
        if (gameplay != null && (gameplay.IsGameOver() || gameplay.IsVictory())) return;

        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
    }

    public void ResumeGame() => SetPaused(false);

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
        if (pauseRoot != null) pauseRoot.SetActive(paused);
        if (player != null) player.SetControlsLocked(paused);
        if (wrench != null) wrench.enabled = !paused;
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
