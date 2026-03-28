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
}
