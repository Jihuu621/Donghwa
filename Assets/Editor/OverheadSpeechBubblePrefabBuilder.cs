using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class OverheadSpeechBubblePrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/OverheadSpeechBubble.prefab";

    static OverheadSpeechBubblePrefabBuilder()
    {
        EditorApplication.delayCall += EnsureRuntimePrefab;
    }

    [MenuItem("Tools/Dialogue/Rebuild Overhead Speech Bubble Prefab")]
    private static void RebuildRuntimePrefab()
    {
        BuildRuntimePrefab(true);
    }

    private static void EnsureRuntimePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null || prefab.transform.Find("World Speech Bubble") != null)
        {
            return;
        }

        BuildRuntimePrefab(false);
    }

    private static void BuildRuntimePrefab(bool forceRebuild)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"Unable to load speech bubble prefab at {PrefabPath}.");
            return;
        }

        try
        {
            Transform existingCanvas = root.transform.Find("World Speech Bubble");
            if (existingCanvas != null)
            {
                if (!forceRebuild)
                {
                    return;
                }

                Object.DestroyImmediate(existingCanvas.gameObject);
            }

            OverheadSpeechBubble bubble = root.GetComponent<OverheadSpeechBubble>();
            if (bubble == null)
            {
                bubble = root.AddComponent<OverheadSpeechBubble>();
            }

            GameObject canvasObject = new GameObject("World Speech Bubble", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup));
            canvasObject.layer = root.layer;
            canvasObject.transform.SetParent(root.transform, false);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.pivot = new Vector2(0.5f, 0f);
            canvasRect.sizeDelta = new Vector2(220f, 90f);
            canvasRect.localScale = Vector3.one * 0.015f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 120;

            CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject backgroundObject = new GameObject("Bubble And Tail", typeof(RectTransform),
                typeof(SpeechBubbleGraphic), typeof(Outline));
            backgroundObject.layer = root.layer;
            backgroundObject.transform.SetParent(canvasObject.transform, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            SpeechBubbleGraphic background = backgroundObject.GetComponent<SpeechBubbleGraphic>();
            background.color = new Color(0.075f, 0.075f, 0.09f, 0.96f);
            background.TailHeight = 18f;

            Outline outline = backgroundObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.56f, 0.68f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            GameObject textObject = new GameObject("Dialogue Text", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.layer = root.layer;
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 9f);
            textRect.sizeDelta = new Vector2(1f, 28f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "Speech Bubble";
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 28f;
            text.color = new Color(0.97f, 0.95f, 0.87f, 1f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            SerializedObject serializedBubble = new SerializedObject(bubble);
            serializedBubble.FindProperty("dynamicSourceFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Font>("Assets/PF스타더스트 3.0.ttf");
            serializedBubble.FindProperty("_canvasRect").objectReferenceValue = canvasRect;
            serializedBubble.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            serializedBubble.FindProperty("_background").objectReferenceValue = background;
            serializedBubble.FindProperty("_text").objectReferenceValue = text;
            serializedBubble.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt OverheadSpeechBubble as a serialized runtime UI prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
