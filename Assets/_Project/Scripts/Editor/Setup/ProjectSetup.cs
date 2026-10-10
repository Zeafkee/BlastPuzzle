using System.Text;
using BlastPuzzle.Core;
using BlastPuzzle.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlastPuzzle.EditorTools
{
    public static class ProjectSetup
    {
        [MenuItem("BlastPuzzle/Setup/Build Everything")]
        public static void BuildEverything()
        {
            ApplyProjectSettings();
            AssetBuilder.ConfigureAudio();
            AssetBuilder.BuildAtlases();
            AssetBuilder.SetupFonts();

            AssetBuilder.BuildTheme();
            AssetBuilder.BuildLevels();
            AssetBuilder.BuildTilePrefab(AssetBuilder.SpriteMaterial());
            AssetDatabase.SaveAssets();

            SceneBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("BlastPuzzle: setup finished.");
        }

        [MenuItem("BlastPuzzle/Setup/Rebuild Scene Only")]
        public static void RebuildScene()
        {
            SceneBuilder.Build();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("BlastPuzzle/Setup/Generate Levels Only")]
        public static void GenerateLevels()
        {
            AssetBuilder.BuildLevels();
            AssetDatabase.SaveAssets();
            Debug.Log("BlastPuzzle: levels regenerated from LevelDefinitions.");
        }

        [MenuItem("BlastPuzzle/Tools/Simulate Levels (bot win rates)")]
        public static void SimulateLevels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetBuilder.DataFolder + "/LevelCatalog.asset");
            if (catalog == null)
            {
                Debug.LogError("LevelCatalog not found. Run BlastPuzzle > Setup > Build Everything first.");
                return;
            }

            const int runs = 300;
            var report = new StringBuilder("Level simulation (300 games each)\nlevel  size   colors moves | casual  average  expert\n");
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelConfig config = catalog.Get(i).ToConfig();
                float casual = AutoPlayer.EstimateWinRate(config, runs, 0.3f, 100);
                float average = AutoPlayer.EstimateWinRate(config, runs, 0.6f, 100);
                float expert = AutoPlayer.EstimateWinRate(config, runs, 1f, 100);
                report.AppendLine($"{i + 1,5}  {config.Width}x{config.Height,-4} {config.ColorCount,6} {config.Moves,5} | {casual,6:P0} {average,8:P0} {expert,7:P0}");
            }

            Debug.Log(report.ToString());
        }

        [MenuItem("BlastPuzzle/Tools/Delete Save Data")]
        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey("blastpuzzle.save");
            PlayerPrefs.Save();
            Debug.Log("BlastPuzzle: save data deleted.");
        }

        [MenuItem("BlastPuzzle/Setup/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Zeafkee";
            PlayerSettings.productName = "BlastPuzzle";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zeafkee.blastpuzzle");

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetBuilder.Root + "/Art/AppIcon/AppIcon.png");
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            PlayerSettings.gcIncremental = true;
            PlayerSettings.accelerometerFrequency = 0;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.supportsHDR = false;
                urp.msaaSampleCount = 1;
                urp.supportsCameraDepthTexture = false;
                urp.supportsCameraOpaqueTexture = false;
                urp.renderScale = 1f;
                EditorUtility.SetDirty(urp);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
