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
    }
}
