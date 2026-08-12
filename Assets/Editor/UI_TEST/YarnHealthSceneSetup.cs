using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class YarnHealthSceneSetup
{
    [MenuItem("Tools/Donghwa/Legacy UI/Remove Generated UI_TEST Canvas")]
    private static void RemoveGeneratedCanvasFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != UiTestSceneBuilder.TargetScenePath)
        {
            bool shouldOpen = EditorUtility.DisplayDialog(
                "UI Test Scene",
                "Open UI_TEST and remove the old generated canvas?",
                "Open and Remove",
                "Cancel");

            if (!shouldOpen) return;
            scene = EditorSceneManager.OpenScene(UiTestSceneBuilder.TargetScenePath, OpenSceneMode.Single);
        }

        RemoveExistingCanvas(scene, true);
        EditorSceneManager.SaveScene(scene);
    }

    internal static GameObject FindGeneratedCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == UiTestSceneBuilder.CanvasName)
            {
                return root;
            }
        }

        return null;
    }

    internal static void RemoveExistingCanvas(Scene scene, bool useUndo)
    {
        GameObject existing = FindGeneratedCanvas(scene);
        if (existing == null) return;

        if (useUndo) Undo.DestroyObjectImmediate(existing);
        else Object.DestroyImmediate(existing);
    }
}
