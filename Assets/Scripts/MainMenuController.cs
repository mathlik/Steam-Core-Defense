using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Отображает главное меню и запускает переходы между сценами.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            CreateStyles();
        }

        GUI.Label(
            new Rect(0f, Screen.height * 0.18f, Screen.width, 110f),
            "STEAM CORE DEFENSE",
            titleStyle);

        float width = Mathf.Min(460f, Screen.width * 0.42f);
        float left = (Screen.width - width) * 0.5f;

        if (GUI.Button(
                new Rect(left, Screen.height * 0.47f, width, 78f),
                "START GAME",
                buttonStyle))
        {
            StartGame();
        }

        if (GUI.Button(
                new Rect(left, Screen.height * 0.59f, width, 78f),
                "EXIT",
                buttonStyle))
        {
            ExitGame();
        }
    }

    private void CreateStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 12, 42, 108),
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.Clamp(Screen.width / 42, 20, 34),
            fontStyle = FontStyle.Bold
        };
    }

    public void StartGame()
    {
        SceneManager.LoadScene("SteamCoreDefenseLevel");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
