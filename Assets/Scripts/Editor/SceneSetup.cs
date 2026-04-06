using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSetup
{
    [MenuItem("Car Survivor/Setup Game Scene")]
    public static void SetupScene()
    {
        // Open or create SampleScene
        var scene = EditorSceneManager.GetActiveScene();

        // Clear existing objects except camera
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.GetComponent<Camera>() == null)
                Object.DestroyImmediate(root);
        }

        // Create GameBootstrap
        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Car Survivor scene setup complete! Press Play to start.");
    }

    [MenuItem("Car Survivor/Create Main Lobby Scene")]
    public static void CreateMainLobbyScene()
    {
        // 새 씬 생성
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 카메라 배경색 설정
        var camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null)
            camera.backgroundColor = new Color(0.89f, 0.55f, 0.35f);

        // MainLobby UI 오브젝트 생성
        var lobbyObj = new GameObject("MainLobbyUI");

        // PanelSettings 생성 또는 로드
        string panelSettingsPath = "Assets/Resources/Sprites/UI/OutGame/MainLobby/MainLobbyPanelSettings.asset";
        var panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(panelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            panelSettings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
            AssetDatabase.SaveAssets();
        }

        // UIDocument 추가 + UXML, PanelSettings 연결
        var uiDoc = lobbyObj.AddComponent<UnityEngine.UIElements.UIDocument>();
        uiDoc.panelSettings = panelSettings;
        var uxmlAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(
            "Assets/Resources/Sprites/UI/OutGame/MainLobby/main.uxml");
        if (uxmlAsset != null)
            uiDoc.visualTreeAsset = uxmlAsset;
        else
            Debug.LogWarning("main.uxml을 찾을 수 없습니다. UIDocument에 수동으로 연결해주세요.");

        // MainLobbyUI + MainLobbyBootstrap 추가
        lobbyObj.AddComponent<MainLobbyUI>();
        lobbyObj.AddComponent<MainLobbyBootstrap>();

        // 씬 저장
        string scenePath = "Assets/Scenes/MainLobby.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        EditorSceneManager.MarkSceneDirty(newScene);

        // Build Settings에 씬 추가
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool mainLobbyExists = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) mainLobbyExists = true;
        }

        if (!mainLobbyExists)
        {
            // MainLobby를 인덱스 0에 추가 (첫 번째로 로드되도록)
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        Debug.Log("Main Lobby scene created at: " + scenePath);
        Debug.Log("MainLobby scene added to Build Settings as first scene.");
    }

    [MenuItem("Car Survivor/Create Title Screen Scene")]
    public static void CreateTitleScreenScene()
    {
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null)
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.10f);

        var bootstrapObj = new GameObject("TitleScreenBootstrap");
        var type = System.Type.GetType("TitleScreenBootstrap");
        if (type != null)
            bootstrapObj.AddComponent(type);
        else
            Debug.LogWarning("TitleScreenBootstrap 스크립트를 찾을 수 없습니다. 수동으로 추가해주세요.");

        string scenePath = "Assets/Scenes/TitleScreen.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        EditorSceneManager.MarkSceneDirty(newScene);

        // Build Settings — TitleScreen을 맨 앞(인덱스 0)에 추가
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool exists = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) exists = true;
        }

        if (!exists)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        Debug.Log("Title Screen scene created at: " + scenePath);
        Debug.Log("타이틀 이미지를 Resources/Sprites/UI/OutGame/TitleScreen.png 에 넣어주세요!");
    }
}
