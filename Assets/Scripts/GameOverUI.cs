using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Показывает экран проигрыша после перегрева реактора.</summary>
public class GameOverUI : MonoBehaviour
{
    private CoreGameplay gameplay;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private bool cursorShown;

    public void Initialize(CoreGameplay source)
    {
        gameplay = source;
    }

    private void OnGUI()
    {
        if (gameplay == null || !gameplay.IsGameOver()) return;

        if (!cursorShown)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorShown = true;
        }

        if (titleStyle == null) CreateStyles();

        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.Label(new Rect(0f, Screen.height * 0.25f, Screen.width, 90f), "REACTOR MELTDOWN", titleStyle);

        float width = Mathf.Min(420f, Screen.width * 0.4f);
        float left = (Screen.width - width) * 0.5f;

        if (GUI.Button(new Rect(left, Screen.height * 0.48f, width, 70f), "RESTART", buttonStyle))
        {
            SceneManager.LoadScene("SteamCoreDefenseLevel");
        }

        if (GUI.Button(new Rect(left, Screen.height * 0.6f, width, 70f), "MAIN MENU", buttonStyle))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void CreateStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 64,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.red;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold
        };
    }
}
