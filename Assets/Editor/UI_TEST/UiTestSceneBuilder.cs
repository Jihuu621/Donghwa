using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UiTestEditorUiFactory;

internal static class UiTestSceneBuilder
{
    internal const string TargetScenePath = "Assets/Scenes/UI_TEST.unity";
    internal const string CanvasName = "YarnHealthHUD_Canvas";

    internal static void Build(Scene scene, bool useUndo)
    {
        try
        {
            YarnHealthSprites sprites = YarnHealthSpriteProvider.Load();
            Health playerHealth = FindPlayerHealth();
            NeedleSkillManager needleSkill = FindNeedleSkill(playerHealth);
            ConfigureTestHealth(playerHealth);

            GameObject canvasObject = CreateCanvas(useUndo);
            YarnHealthHudBuilder.Build(canvasObject.transform, playerHealth, sprites);
            ParryNeedleGaugeBuilder.Build(canvasObject.transform, needleSkill);
            PauseMenuBuilder.Build(canvasObject);

            EnsureEventSystem();
            EditorSceneManager.MoveGameObjectToScene(canvasObject, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UiTestSceneBuilder] Rebuilt UI_TEST interface.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static GameObject CreateCanvas(bool useUndo)
    {
        GameObject canvasObject = CreateObject(CanvasName, null);
        if (useUndo) Undo.RegisterCreatedObjectUndo(canvasObject, "Create UI Test Interface");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private static Health FindPlayerHealth()
    {
        return UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Include)
            .FirstOrDefault(health => health.gameObject.CompareTag("Player"));
    }

    private static NeedleSkillManager FindNeedleSkill(Health playerHealth)
    {
        if (playerHealth != null) return playerHealth.GetComponent<NeedleSkillManager>();

        return UnityEngine.Object.FindObjectsByType<NeedleSkillManager>(FindObjectsInactive.Include)
            .FirstOrDefault(skill => skill.gameObject.CompareTag("Player"));
    }

    private static void ConfigureTestHealth(Health playerHealth)
    {
        if (playerHealth == null) return;

        SerializedObject serializedHealth = new SerializedObject(playerHealth);
        SerializedProperty maxHealth = serializedHealth.FindProperty("maxHP");
        if (maxHealth == null) return;

        maxHealth.floatValue = 30f;
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(playerHealth);
    }
}
