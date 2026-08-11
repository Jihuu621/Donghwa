using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class YarnHealthSceneSetup
{
    static YarnHealthSceneSetup()
    {
        EditorApplication.delayCall += TryAutomaticSetup;
        EditorSceneManager.sceneOpened -= HandleSceneOpened;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
    }

    [MenuItem("Tools/Donghwa/Rebuild UI Test Scene")]
    private static void RebuildFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != UiTestSceneBuilder.TargetScenePath)
        {
            bool shouldOpen = EditorUtility.DisplayDialog(
                "UI Test Scene",
                "Open UI_TEST and rebuild its test UI?",
                "Open and Rebuild",
                "Cancel");

            if (!shouldOpen) return;
            scene = EditorSceneManager.OpenScene(UiTestSceneBuilder.TargetScenePath, OpenSceneMode.Single);
        }

        RemoveExistingCanvas(scene, true);
        UiTestSceneBuilder.Build(scene, true);
    }

    public static void RebuildUiTestFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(UiTestSceneBuilder.TargetScenePath, OpenSceneMode.Single);
        RemoveExistingCanvas(scene, false);
        UiTestSceneBuilder.Build(scene, false);
    }

    private static void TryAutomaticSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutomaticSetup;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != UiTestSceneBuilder.TargetScenePath) return;

        GameObject existing = FindCanvas(scene);
        YarnHealthDisplay display = existing != null
            ? existing.GetComponentInChildren<YarnHealthDisplay>(true)
            : null;

        if (display != null && display.ViewVersion >= YarnHealthDisplay.CurrentViewVersion) return;

        RemoveExistingCanvas(scene, false);
        UiTestSceneBuilder.Build(scene, false);
    }

    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == UiTestSceneBuilder.TargetScenePath)
        {
            EditorApplication.delayCall += TryAutomaticSetup;
        }
    }

    private static GameObject FindCanvas(Scene scene)
    {
        return scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == UiTestSceneBuilder.CanvasName);
    }

    private static void RemoveExistingCanvas(Scene scene, bool useUndo)
    {
        GameObject existing = FindCanvas(scene);
        if (existing == null) return;

        if (useUndo) Undo.DestroyObjectImmediate(existing);
        else Object.DestroyImmediate(existing);
    }
}
