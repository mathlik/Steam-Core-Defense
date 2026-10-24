using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public static class SteamCoreDefenseMenuBuilder
{
    const string MenuPath = "Assets/Scenes/MainMenu.unity";
    const string LevelPath = "Assets/Scenes/SteamCoreDefenseLevel.unity";

    [MenuItem("Steam Core Defense/Open Main Menu")]
    public static void OpenMenu()
    {
        if (System.IO.File.Exists(MenuPath))
        {
            EditorSceneManager.OpenScene(MenuPath);
            Debug.Log("Opened existing main menu. Manual changes were preserved.");
            return;
        }

        BuildMenuInternal();
    }

    [MenuItem("Steam Core Defense/Rebuild Main Menu (Overwrite)")]
    public static void RebuildMenu()
    {
        if (EditorUtility.DisplayDialog("Rebuild main menu", "This will overwrite the current menu and delete manual changes. Continue?", "Overwrite", "Cancel"))
        {
            BuildMenuInternal();
        }
    }

    private static void BuildMenuInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(.04f, .05f, .08f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(.015f, .025f, .06f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();

        var controllerObject = new GameObject("Main Menu Controller");
        controllerObject.AddComponent<MainMenuController>();

        EditorSceneManager.SaveScene(scene, MenuPath);
        AddBuildScene(MenuPath);
        AddBuildScene(LevelPath);
        Debug.Log("Created main menu: " + MenuPath);
    }

    static Image CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); var image = go.AddComponent<Image>(); image.color = color; return image;
    }
    static Text CreateText(string name, Transform parent, string content, int size, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); var text = go.AddComponent<Text>(); text.text = content; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleCenter; return text;
    }
    static Button CreateButton(string name, Transform parent, string content, Vector2 anchor)
    {
        var image = CreatePanel(name, parent, new Color(.08f, .22f, .35f, 1f)); SetRect(image.rectTransform, anchor, new Vector2(420, 76), Vector2.zero);
        var button = image.gameObject.AddComponent<Button>(); var text = CreateText("Text", image.transform, content, 28, Color.white); Stretch(text.rectTransform); return button;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;}
    static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset){rect.anchorMin=anchor;rect.anchorMax=anchor;rect.sizeDelta=size;rect.anchoredPosition=offset;}
    static void AddBuildScene(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string full = System.IO.Path.GetFullPath(path).Replace('\\','/');
        if (!scenes.Exists(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
