using UnityEditor;
using UnityEngine;

namespace BlastPuzzle.EditorTools
{
    public sealed class SpriteImportRules : AssetPostprocessor
    {
        private const string SpritesRoot = "Assets/_Project/Art/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            bool isBackground = assetPath.Contains("/Backgrounds/");
            importer.textureCompression = isBackground
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.Uncompressed;

            bool isTile = assetPath.Contains("/Blocks/") || assetPath.Contains("/Boosters/");
            importer.spritePixelsPerUnit = isTile ? 128f : 100f;
            importer.maxTextureSize = isBackground ? 1024 : isTile ? 128 : 512;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = isTile ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            if (assetPath.EndsWith("_9s.png"))
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out _);
                float border = Mathf.Floor(width / 3f);
                importer.spriteBorder = new Vector4(border, border, border, border);
            }
        }
    }
}
