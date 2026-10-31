using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Управляет экраном завершения игры, созданным в сцене.</summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text victoryTitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryMainMenuButton;
    [SerializeField] private string levelScene = "SteamCoreDefenseLevel";
    [SerializeField] private string mainMenuScene = "MainMenu";

    private CoreGameplay gameplay;

    private void Awake()
    {
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (victoryRestartButton != null) victoryRestartButton.onClick.AddListener(RestartLevel);
        if (victoryMainMenuButton != null) victoryMainMenuButton.onClick.AddListener(LoadMainMenu);
        if (root != null) root.SetActive(false);
        if (victoryRoot != null) victoryRoot.SetActive(false);
    }

    public void Show(CoreGameplay source)
    {
        gameplay = source;
        bool isVictory = source.IsVictory();
        if (titleText != null)
        {
            titleText.text = "REACTOR MELTDOWN";
            titleText.color = Color.red;
        }
        if (victoryTitleText != null)
        {
            victoryTitleText.text = "REACTOR SECURED";
            victoryTitleText.color = Color.green;
        }
        if (root != null) root.SetActive(!isVictory);
        if (victoryRoot != null) victoryRoot.SetActive(isVictory);
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelScene);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
