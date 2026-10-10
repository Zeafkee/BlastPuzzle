using System.Collections.Generic;
using System.IO;
using BlastPuzzle.Core;
using BlastPuzzle.Data;
using BlastPuzzle.View;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace BlastPuzzle.EditorTools
{
    internal static class AssetBuilder
    {
        public const string Root = "Assets/_Project";
        public const string Sprites = Root + "/Art/Sprites";
        public const string DataFolder = Root + "/Data";
        public const string LevelsFolder = DataFolder + "/Levels";
        public const string PrefabFolder = Root + "/Prefabs";
        public const string MaterialFolder = Root + "/Art/Materials";
        public const string AtlasFolder = Root + "/Art/Atlases";
        public const string FontFolder = Root + "/Art/Fonts";
        public const string AudioFolder = Root + "/Audio";

        public static readonly string[] ColorNames = { "Red", "Yellow", "Blue", "Green", "Purple", "Pink" };
        public static readonly Color32[] ColorTints =
        {
            new Color32(235, 64, 60, 255), new Color32(255, 188, 28, 255), new Color32(44, 146, 255, 255),
            new Color32(76, 196, 64, 255), new Color32(160, 84, 232, 255), new Color32(255, 104, 178, 255),
        };

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Sprite Sprite(string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}/{relativePath}.png");
            if (sprite == null) Debug.LogError($"Sprite not found: {relativePath}");
            return sprite;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Material SpriteMaterial()
        {
            string path = MaterialFolder + "/SpriteUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "SpriteUnlit" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void SetupFonts()
        {
            Ui.Font = FindProjectFont() ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            if (Ui.Font == null)
            {
                Debug.LogError("No TMP font found. Import 'TMP Essential Resources' (Window > TextMeshPro) and run the setup again.");
                return;
            }

            string path = MaterialFolder + "/UIText_Outline.mat";
            EnsureFolder(MaterialFolder);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Ui.Font.material) { name = "UIText_Outline" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = Ui.Font.material.shader;
                material.CopyPropertiesFromMaterial(Ui.Font.material);
            }

            material.SetTexture(ShaderUtilities.ID_MainTex, Ui.Font.atlasTexture);
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(40, 28, 96, 255));
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color32(30, 20, 80, 200));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            EditorUtility.SetDirty(material);
            Ui.OutlineMaterial = material;
        }

        private static TMP_FontAsset FindProjectFont()
        {
            if (!AssetDatabase.IsValidFolder(FontFolder)) return null;

            foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontFolder }))
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));

            foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontFolder }))
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
                if (fontAsset == null) continue;

                string assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                fontAsset.material.name = font.name + " Material";
                fontAsset.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                AssetDatabase.SaveAssets();
                return fontAsset;
            }

            return null;
        }

        public static void BuildAtlases()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            EnsureFolder(AtlasFolder);
            BuildAtlas("Gameplay", "Blocks", "Boosters", "Board", "FX");
            BuildAtlas("UI", "UI", "Icons");
        }

        private static void BuildAtlas(string name, params string[] folders)
        {
            string path = $"{AtlasFolder}/{name}.spriteatlasv2";

            var folderObjects = new List<Object>();
            foreach (string folder in folders)
                folderObjects.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>($"{Sprites}/{folder}"));

            var atlas = new SpriteAtlasAsset();
            atlas.Add(folderObjects.ToArray());
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Bilinear,
                generateMipMaps = false,
                sRGB = true,
                readable = false,
            };

            TextureImporterPlatformSettings android = importer.GetPlatformSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformSettings(android);

            importer.SaveAndReimport();
        }

        public static void ConfigureAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool isMusic = path.Contains("/Music/");

                importer.forceToMono = true;
                importer.loadInBackground = isMusic;

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (isMusic)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.45f;
                }
                else
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.ADPCM;
                }

                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        public static TileTheme BuildTheme()
        {
            var theme = LoadOrCreate<TileTheme>(DataFolder + "/TileTheme.asset");
            theme.colors = new ColorSprites[ColorNames.Length];

            for (int c = 0; c < ColorNames.Length; c++)
            {
                string name = ColorNames[c];
                theme.colors[c] = new ColorSprites
                {
                    name = name,
                    tint = ColorTints[c],
                    tiers = new[]
                    {
                        Sprite($"Blocks/Block_{name}_Default"),
                        Sprite($"Blocks/Block_{name}_Rocket"),
                        Sprite($"Blocks/Block_{name}_Bomb"),
                        Sprite($"Blocks/Block_{name}_Disco"),
                    },
                    disco = Sprite($"Boosters/Disco_{name}"),
                };
            }

            theme.rocket = Sprite("Boosters/Rocket");
            theme.bomb = Sprite("Boosters/Bomb");
            theme.box = Sprite("Boosters/Box");
            theme.boxReinforced = Sprite("Boosters/Box_Reinforced");
            theme.boxTint = new Color32(206, 142, 78, 255);
            EditorUtility.SetDirty(theme);
            return theme;
        }

        public static LevelCatalog BuildLevels()
        {
            EnsureFolder(LevelsFolder);
            LevelConfig[] configs = LevelDefinitions.All();
            var assets = new LevelData[configs.Length];

            for (int i = 0; i < configs.Length; i++)
            {
                LevelConfig config = configs[i];
                config.Validate();

                var level = LoadOrCreate<LevelData>($"{LevelsFolder}/Level_{i + 1:00}.asset");
                level.width = config.Width;
                level.height = config.Height;
                level.colorCount = config.ColorCount;
                level.moves = config.Moves;
                level.tierA = config.Tiers.A;
                level.tierB = config.Tiers.B;
                level.tierC = config.Tiers.C;
                level.layout = config.Layout != null ? string.Join("\n", config.Layout) : string.Empty;

                level.goals = new List<GoalEntry>();
                foreach (GoalConfig goal in config.Goals)
                    level.goals.Add(new GoalEntry { kind = goal.Kind, color = goal.Color, count = goal.Count });

                EditorUtility.SetDirty(level);
                assets[i] = level;
            }

            var catalog = LoadOrCreate<LevelCatalog>(DataFolder + "/LevelCatalog.asset");
            catalog.levels = assets;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        public static TileView BuildTilePrefab(Material material)
        {
            EnsureFolder(PrefabFolder);
            string path = PrefabFolder + "/Tile.prefab";

            var go = new GameObject("Tile");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            renderer.sprite = Sprite("Blocks/Block_Red_Default");
            var view = go.AddComponent<TileView>();
            Ui.Set(view, "spriteRenderer", renderer);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<TileView>();
        }
    }
}
