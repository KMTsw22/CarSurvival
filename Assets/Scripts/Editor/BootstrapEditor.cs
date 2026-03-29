using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainLobbyBootstrap))]
public class MainLobbyBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(TitleScreenBootstrap))]
public class TitleScreenBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        if (GUILayout.Button("UI 미리보기 생성", GUILayout.Height(35)))
        {
            var bootstrap = (TitleScreenBootstrap)target;
            bootstrap.GenerateUI();
            EditorUtility.SetDirty(bootstrap);
        }
    }
}
