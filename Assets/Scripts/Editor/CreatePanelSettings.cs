using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatePanelSettings
{
    [MenuItem("Tools/Create InGame PanelSettings")]
    public static void Create()
    {
        // InGame UI용 (HUD, LevelUp, GameOver)
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        ps.sortingOrder = 100;

        AssetDatabase.CreateAsset(ps, "Assets/Resources/Settings/InGamePanelSettings.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("[CreatePanelSettings] InGamePanelSettings.asset created in Resources/Settings/");
    }
}
