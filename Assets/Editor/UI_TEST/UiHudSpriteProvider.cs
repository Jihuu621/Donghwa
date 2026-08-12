using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

internal readonly struct UiHudSprites
{
    internal UiHudSprites(
        Sprite background,
        Sprite healthFull,
        Sprite healthDamaged,
        Sprite healthEmpty,
        IReadOnlyList<Sprite> needleCharges)
    {
        Background = background;
        HealthFull = healthFull;
        HealthDamaged = healthDamaged;
        HealthEmpty = healthEmpty;
        NeedleCharges = needleCharges;
    }

    internal Sprite Background { get; }
    internal Sprite HealthFull { get; }
    internal Sprite HealthDamaged { get; }
    internal Sprite HealthEmpty { get; }
    internal IReadOnlyList<Sprite> NeedleCharges { get; }
}

internal static class UiHudSpriteProvider
{
    private const string BackgroundPath = "Assets/Sprites/UI/Back_Gruond.png";
    private const string HealthPath = "Assets/Sprites/UI/Hp_UI.png";
    private const string NeedleChargePath = "Assets/Sprites/UI/needle_Charge.png";

    internal static UiHudSprites Load()
    {
        ConfigurePixelArtImporter(BackgroundPath, 4096);
        ConfigurePixelArtImporter(HealthPath, 2048);
        ConfigureNeedleChargeImporter();

        Sprite background = LoadSprite(BackgroundPath, "Back_Gruond_0");
        Sprite healthFull = LoadSprite(HealthPath, "Hp_UI_0");
        Sprite healthDamaged = LoadSprite(HealthPath, "Hp_UI_1");
        Sprite healthEmpty = LoadSprite(HealthPath, "Hp_UI_2");
        Sprite[] needleCharges = Enumerable.Range(0, 4)
            .Select(index => LoadSprite(NeedleChargePath, $"needle_Charge_{index}"))
            .ToArray();

        if (background == null || healthFull == null || healthDamaged == null || healthEmpty == null ||
            needleCharges.Any(sprite => sprite == null))
        {
            throw new InvalidOperationException(
                "The new HUD sprites are missing or their Sprite Editor slices have changed.");
        }

        return new UiHudSprites(
            background,
            healthFull,
            healthDamaged,
            healthEmpty,
            needleCharges);
    }

    private static void ConfigurePixelArtImporter(string path, int maximumTextureSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Missing TextureImporter: {path}");
        }

        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.mipmapEnabled ||
                       !importer.alphaIsTransparency ||
                       importer.filterMode != FilterMode.Point ||
                       importer.wrapMode != TextureWrapMode.Clamp ||
                       importer.textureCompression != TextureImporterCompression.Uncompressed ||
                       importer.maxTextureSize != maximumTextureSize;

        if (!changed) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = maximumTextureSize;
        importer.SaveAndReimport();
    }

    private static void ConfigureNeedleChargeImporter()
    {
        ConfigurePixelArtImporter(NeedleChargePath, 2048);
        TextureImporter importer = AssetImporter.GetAtPath(NeedleChargePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Missing TextureImporter: {NeedleChargePath}");
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(NeedleChargePath) as TextureImporter;
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        SpriteRect[] frames = provider.GetSpriteRects();
        Rect[] expectedRects =
        {
            new Rect(0f, 640f, 640f, 640f),
            new Rect(640f, 640f, 640f, 640f),
            new Rect(0f, 0f, 640f, 640f),
            new Rect(640f, 0f, 640f, 640f)
        };
        bool changed = false;

        for (int index = 0; index < expectedRects.Length; index++)
        {
            string frameName = $"needle_Charge_{index}";
            SpriteRect frame = frames.FirstOrDefault(candidate => candidate.name == frameName);
            if (frame == null)
            {
                throw new InvalidOperationException($"Missing Sprite Editor slice: {frameName}");
            }

            if (frame.rect == expectedRects[index] &&
                frame.alignment == SpriteAlignment.Center &&
                frame.pivot == new Vector2(0.5f, 0.5f))
            {
                continue;
            }

            frame.rect = expectedRects[index];
            frame.alignment = SpriteAlignment.Center;
            frame.pivot = new Vector2(0.5f, 0.5f);
            changed = true;
        }

        if (changed)
        {
            provider.SetSpriteRects(frames);
            provider.Apply();
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }
}
