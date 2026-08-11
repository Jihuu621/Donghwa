using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal readonly struct YarnHealthSprites
{
    internal YarnHealthSprites(Sprite full, Sprite half)
    {
        Full = full;
        Half = half;
    }

    internal Sprite Full { get; }
    internal Sprite Half { get; }
}

internal static class YarnHealthSpriteProvider
{
    private const string TexturePath = "Assets/UI/YarnHealth/YarnHealthStates.png";
    private const string FullSpriteName = "Yarn_Full";
    private const string HalfSpriteName = "Yarn_HalfUnravelled";

    internal static YarnHealthSprites Load()
    {
        ConfigureImporter();
        Sprite full = LoadSprite(FullSpriteName);
        Sprite half = LoadSprite(HalfSpriteName);
        if (full == null || half == null)
        {
            throw new InvalidOperationException("Could not load yarn health sprites.");
        }

        return new YarnHealthSprites(full, half);
    }

    private static void ConfigureImporter()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Missing TextureImporter: {TexturePath}");

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
        return AssetDatabase.LoadAllAssetsAtPath(TexturePath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }
}
