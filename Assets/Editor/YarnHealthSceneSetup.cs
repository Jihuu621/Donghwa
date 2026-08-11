using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class YarnHealthSceneSetup
{
    private const string TargetScenePath = "Assets/Scenes/UI_TEST.unity";
    private const string TexturePath = "Assets/UI/YarnHealth/YarnHealthStates.png";
    private const string CanvasName = "YarnHealthHUD_Canvas";
    private const string FullSpriteName = "Yarn_Full";
    private const string HalfSpriteName = "Yarn_HalfUnravelled";

    static YarnHealthSceneSetup()
    {
        EditorApplication.delayCall += TryAutomaticSetup;
        EditorSceneManager.sceneOpened -= HandleSceneOpened;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
    }

    [MenuItem("Tools/Donghwa/Rebuild Yarn Health UI Test")]
    private static void RebuildFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            bool openScene = EditorUtility.DisplayDialog(
                "Yarn Health UI",
                "UI_TEST 씬을 열고 실타래 체력 UI를 구성할까요?",
                "열고 구성",
                "취소");

            if (!openScene)
            {
                return;
            }

            scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == CanvasName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        Build(scene, true);
    }

    private static void TryAutomaticSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutomaticSetup;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TargetScenePath)
        {
            return;
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == CanvasName);
        YarnHealthDisplay existingDisplay = existing != null
            ? existing.GetComponentInChildren<YarnHealthDisplay>(true)
            : null;

        bool currentVersion = existingDisplay != null &&
                              existingDisplay.ViewVersion >= YarnHealthDisplay.CurrentViewVersion;

        if (!currentVersion)
        {
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            Build(scene, false);
        }
    }

    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == TargetScenePath)
        {
            EditorApplication.delayCall += TryAutomaticSetup;
        }
    }

    private static void Build(Scene scene, bool useUndo)
    {
        try
        {
            ConfigureTextureImporter();
            Sprite fullSprite = LoadSprite(FullSpriteName);
            Sprite halfSprite = LoadSprite(HalfSpriteName);

            if (fullSprite == null || halfSprite == null)
            {
                throw new InvalidOperationException("실타래 스프라이트를 불러오지 못했습니다.");
            }

            Health playerHealth = UnityEngine.Object
                .FindObjectsByType<Health>(FindObjectsInactive.Include)
                .FirstOrDefault(health => health.gameObject.CompareTag("Player"));

            NeedleSkillManager needleSkill = playerHealth != null
                ? playerHealth.GetComponent<NeedleSkillManager>()
                : UnityEngine.Object.FindObjectsByType<NeedleSkillManager>(FindObjectsInactive.Include)
                    .FirstOrDefault(skill => skill.gameObject.CompareTag("Player"));

            if (playerHealth != null)
            {
                SerializedObject serializedHealth = new SerializedObject(playerHealth);
                SerializedProperty maxHealth = serializedHealth.FindProperty("maxHP");
                if (maxHealth != null)
                {
                    // UI_TEST uses six half-steps. Rabbit's base 5 damage therefore equals one half-yarn.
                    maxHealth.floatValue = 30f;
                    serializedHealth.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(playerHealth);
                }
            }

            GameObject canvasObject = CreateObject(CanvasName, null);
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Yarn Health UI");
            }

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform hud = CreateRect("YarnHealthHUD", canvasObject.transform);
            SetFixedTopLeft(hud, new Vector2(42f, -42f), new Vector2(540f, 174f));

            Image background = hud.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.035f, 0.07f, 0.93f);
            background.raycastTarget = false;

            Shadow shadow = hud.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(7f, -7f);
            shadow.useGraphicAlpha = true;

            RectTransform accent = CreateRect("CrimsonThread", hud);
            SetTopStretch(accent, 0f, 4f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);
            accentImage.raycastTarget = false;

            TextMeshProUGUI title = CreateText(
                "Title",
                hud,
                "LIFE THREAD",
                22f,
                FontStyles.Bold,
                new Color(1f, 0.78f, 0.52f, 1f),
                TextAlignmentOptions.Left);
            SetFixedTopLeft(title.rectTransform, new Vector2(22f, -16f), new Vector2(230f, 34f));
            title.characterSpacing = 3f;

            TextMeshProUGUI healthText = CreateText(
                "HealthValue",
                hud,
                "30 / 30",
                22f,
                FontStyles.Bold,
                new Color(1f, 0.92f, 0.82f, 1f),
                TextAlignmentOptions.Right);
            SetFixedTopLeft(healthText.rectTransform, new Vector2(397f, -69f), new Vector2(120f, 42f));

            RectTransform divider = CreateRect("Divider", hud);
            SetFixedTopLeft(divider, new Vector2(386f, -57f), new Vector2(2f, 74f));
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(1f, 0.55f, 0.38f, 0.25f);
            dividerImage.raycastTarget = false;

            RectTransform row = CreateRect("YarnRow", hud);
            SetFixedTopLeft(row, new Vector2(18f, -51f), new Vector2(356f, 112f));
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RectTransform flashRect = CreateRect("DamageFlash", hud);
            SetStretch(flashRect);
            Image flash = flashRect.gameObject.AddComponent<Image>();
            flash.color = new Color(1f, 0.08f, 0.08f, 0f);
            flash.raycastTarget = false;
            flashRect.SetAsLastSibling();

            YarnHealthDisplay display = row.gameObject.AddComponent<YarnHealthDisplay>();
            display.Configure(playerHealth, row, fullSprite, halfSprite, healthText, flash, 3);

            RectTransform demoPanel = CreateRect("YarnHealthDemoControls", canvasObject.transform);
            SetFixedTopLeft(demoPanel, new Vector2(42f, -230f), new Vector2(540f, 96f));
            Image demoBackground = demoPanel.gameObject.AddComponent<Image>();
            demoBackground.color = new Color(0.035f, 0.025f, 0.045f, 0.86f);
            demoBackground.raycastTarget = false;

            TextMeshProUGUI hint = CreateText(
                "Hint",
                demoPanel,
                "UI TEST  /  1 HIT = 1/2 YARN  /  BOSS = 3 HALF-STEPS",
                13f,
                FontStyles.Normal,
                new Color(0.82f, 0.76f, 0.82f, 1f),
                TextAlignmentOptions.Center);
            SetFixedTopLeft(hint.rectTransform, new Vector2(18f, -8f), new Vector2(504f, 25f));

            YarnHealthDemoController demo = demoPanel.gameObject.AddComponent<YarnHealthDemoController>();
            demo.Configure(playerHealth, display, 3, hint);

            Button hitButton = CreateButton(demoPanel, "StandardHitButton", "1 HIT", new Vector2(18f, -43f), new Color(0.72f, 0.10f, 0.14f, 1f));
            Button bossButton = CreateButton(demoPanel, "BossHitButton", "BOSS HIT", new Vector2(146f, -43f), new Color(0.39f, 0.17f, 0.48f, 1f));
            Button healButton = CreateButton(demoPanel, "HealButton", "+ HALF", new Vector2(274f, -43f), new Color(0.10f, 0.42f, 0.39f, 1f));
            Button resetButton = CreateButton(demoPanel, "ResetButton", "RESET", new Vector2(402f, -43f), new Color(0.24f, 0.22f, 0.28f, 1f));

            UnityEventTools.AddPersistentListener(hitButton.onClick, demo.StandardHit);
            UnityEventTools.AddPersistentListener(bossButton.onClick, demo.BossHit);
            UnityEventTools.AddPersistentListener(healButton.onClick, demo.HealHalf);
            UnityEventTools.AddPersistentListener(resetButton.onClick, demo.ResetHealth);

            CreateParryNeedleGauge(canvasObject.transform, needleSkill);

            EnsureEventSystem();
            EditorSceneManager.MoveGameObjectToScene(canvasObject, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[YarnHealthSceneSetup] UI_TEST 씬에 실타래 체력 HUD와 피해 테스트 컨트롤을 구성했습니다.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void CreateParryNeedleGauge(Transform canvasTransform, NeedleSkillManager needleSkill)
    {
        RectTransform panel = CreateRect("ParryNeedleGaugeHUD", canvasTransform);
        SetFixedTopLeft(panel, new Vector2(42f, -338f), new Vector2(540f, 106f));

        Image panelBackground = panel.gameObject.AddComponent<Image>();
        panelBackground.color = new Color(0.035f, 0.025f, 0.045f, 0.9f);
        panelBackground.raycastTarget = false;

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(5f, -5f);
        shadow.useGraphicAlpha = true;

        RectTransform accent = CreateRect("ParryAccent", panel);
        SetTopStretch(accent, 0f, 3f);
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.92f, 0.13f, 0.18f, 1f);
        accentImage.raycastTarget = false;

        TextMeshProUGUI title = CreateText(
            "ParryTitle",
            panel,
            "PERFECT GUARD CHARGE",
            14f,
            FontStyles.Bold,
            new Color(1f, 0.78f, 0.52f, 1f),
            TextAlignmentOptions.Left);
        SetFixedTopLeft(title.rectTransform, new Vector2(18f, -9f), new Vector2(300f, 27f));
        title.characterSpacing = 1.5f;

        TextMeshProUGUI chargeText = CreateText(
            "NeedleChargeValue",
            panel,
            "NEEDLE  0 / 3",
            14f,
            FontStyles.Bold,
            new Color(1f, 0.9f, 0.82f, 1f),
            TextAlignmentOptions.Right);
        SetFixedTopLeft(chargeText.rectTransform, new Vector2(340f, -9f), new Vector2(182f, 27f));

        RectTransform gaugeRow = CreateRect("ParryGaugeRow", panel);
        SetFixedTopLeft(gaugeRow, new Vector2(18f, -44f), new Vector2(504f, 46f));

        List<Image> halfUnitFills = new List<Image>(6);
        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            RectTransform slot = CreateRect($"ParryGaugeSlot_{slotIndex + 1}", gaugeRow);
            SetFixedTopLeft(slot, new Vector2(slotIndex * 172f, 0f), new Vector2(160f, 42f));

            Image slotBackground = slot.gameObject.AddComponent<Image>();
            slotBackground.color = new Color(0.075f, 0.025f, 0.04f, 0.95f);
            slotBackground.raycastTarget = false;

            Image leftFill = CreateGaugePart("LeftHalf", slot, new Vector2(5f, -5f), new Vector2(73.5f, 32f));
            Image rightFill = CreateGaugePart("RightHalf", slot, new Vector2(81.5f, -5f), new Vector2(73.5f, 32f));
            halfUnitFills.Add(leftFill);
            halfUnitFills.Add(rightFill);

            Color borderColor = new Color(0.94f, 0.08f, 0.16f, 1f);
            CreateGaugeLine("BorderTop", slot, new Vector2(0f, 0f), new Vector2(160f, 4f), borderColor);
            CreateGaugeLine("BorderBottom", slot, new Vector2(0f, -38f), new Vector2(160f, 4f), borderColor);
            CreateGaugeLine("BorderLeft", slot, new Vector2(0f, -4f), new Vector2(4f, 34f), borderColor);
            CreateGaugeLine("BorderRight", slot, new Vector2(156f, -4f), new Vector2(4f, 34f), borderColor);
            CreateGaugeLine("HalfDivider", slot, new Vector2(78.5f, -4f), new Vector2(3f, 34f), new Color(0.38f, 0.1f, 0.16f, 1f));
        }

        ParryNeedleGaugeDisplay display = panel.gameObject.AddComponent<ParryNeedleGaugeDisplay>();
        display.Configure(needleSkill, halfUnitFills, chargeText, panel);
    }

    private static Image CreateGaugePart(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.035f, 0.055f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    private static void CreateGaugeLine(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void ConfigureTextureImporter()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"TextureImporter를 찾지 못했습니다: {TexturePath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;

#pragma warning disable CS0618
        importer.spritesheet = new[]
        {
            new SpriteMetaData
            {
                name = FullSpriteName,
                rect = new Rect(129f, 305f, 565f, 462f),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            },
            new SpriteMetaData
            {
                name = HalfSpriteName,
                rect = new Rect(842f, 273f, 565f, 462f),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            }
        };
#pragma warning restore CS0618

        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string spriteName)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(TexturePath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystem = CreateObject("EventSystem", null);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        return (RectTransform)CreateObject(name, parent).transform;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 topLeftPosition,
        Color baseColor)
    {
        RectTransform rect = CreateRect(name, parent);
        SetFixedTopLeft(rect, topLeftPosition, new Vector2(120f, 38f));

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = baseColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI buttonText = CreateText(
            "Label",
            rect,
            label,
            15f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        SetStretch(buttonText.rectTransform);

        return button;
    }

    private static void SetFixedTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetTopStretch(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
