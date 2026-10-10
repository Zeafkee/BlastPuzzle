using BlastPuzzle.Audio;
using BlastPuzzle.Data;
using BlastPuzzle.Gameplay;
using BlastPuzzle.InputHandling;
using BlastPuzzle.UI;
using BlastPuzzle.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlastPuzzle.EditorTools
{
    internal static class SceneBuilder
    {
        public const string ScenePath = AssetBuilder.Root + "/Scenes/Main.unity";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Reference = new Vector2(1080f, 1920f);

        private static Material _spriteMaterial;

        public static void Build()
        {
            AssetBuilder.EnsureFolder(AssetBuilder.Root + "/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _spriteMaterial = AssetBuilder.SpriteMaterial();
            AssetBuilder.SetupFonts();
            var theme = AssetDatabase.LoadAssetAtPath<TileTheme>(AssetBuilder.DataFolder + "/TileTheme.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetBuilder.DataFolder + "/LevelCatalog.asset");
            var tilePrefab = AssetDatabase.LoadAssetAtPath<TileView>(AssetBuilder.PrefabFolder + "/Tile.prefab");
            if (theme == null || catalog == null || tilePrefab == null)
            {
                Debug.LogError("Scene build aborted: theme, catalog or tile prefab is missing. Run 'Build Everything'.");
                return;
            }

            Camera camera = BuildCamera();
            BuildBackground(camera);
            BoardView boardView = BuildBoard(theme, tilePrefab, camera, out FxPlayer fx);
            BuildEventSystem();

            MenuScreen menu = BuildMenuCanvas(catalog);
            HudView hud = BuildHudCanvas(out UIPanel hudPanel);
            BuildPopupCanvas(out Banner banner, out SettingsPopup settings, out ResultPopup result);

            var systems = new GameObject("Systems");
            var tapInput = systems.AddComponent<TapInput>();
            Ui.Set(tapInput, "worldCamera", camera);

            var audio = systems.AddComponent<AudioManager>();
            WireAudio(audio);

            var game = systems.AddComponent<GameController>();
            Ui.Set(game, "catalog", catalog);
            Ui.Set(game, "boardView", boardView);
            Ui.Set(game, "tapInput", tapInput);
            Ui.Set(game, "hud", hud);
            Ui.Set(game, "banner", banner);
            Ui.Set(game, "fx", fx);
            Ui.Set(game, "worldCamera", camera);

            var flow = systems.AddComponent<AppFlow>();
            Ui.Set(flow, "catalog", catalog);
            Ui.Set(flow, "game", game);
            Ui.Set(flow, "menu", menu);
            Ui.Set(flow, "hudPanel", hudPanel);
            Ui.Set(flow, "hud", hud);
            Ui.Set(flow, "settings", settings);
            Ui.Set(flow, "result", result);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9.6f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(52, 70, 190, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;

            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            return camera;
        }

        private static void BuildBackground(Camera camera)
        {
            var go = new GameObject("Background");
            go.transform.position = new Vector3(0f, 0f, 5f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetBuilder.Sprite("Backgrounds/BG_Game");
            renderer.sharedMaterial = _spriteMaterial;
            renderer.sortingOrder = -100;

            var fitter = go.AddComponent<BackgroundFitter>();
            Ui.Set(fitter, "targetCamera", camera);
            Ui.Set(fitter, "spriteRenderer", renderer);

            Vector2 spriteSize = renderer.sprite.bounds.size;
            float viewHeight = camera.orthographicSize * 2f;
            float viewWidth = viewHeight * (Reference.x / Reference.y);
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static BoardView BuildBoard(TileTheme theme, TileView tilePrefab, Camera camera, out FxPlayer fx)
        {
            var board = new GameObject("Board");

            var frame = NewSprite("Frame", board.transform, AssetBuilder.Sprite("Board/BoardFrame_9s"), -10);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(8.6f, 8.6f);
            frame.gameObject.SetActive(false);

            var tiles = new GameObject("Tiles");
            tiles.transform.SetParent(board.transform, false);

            var fxRoot = new GameObject("Fx");
            fxRoot.transform.SetParent(board.transform, false);

            ParticleSystem shards = BuildShards(fxRoot.transform);
            ParticleSystem sparkles = BuildSparkles(fxRoot.transform);
            ParticleSystem confetti = BuildConfetti(camera.transform);

            var rings = new SpriteRenderer[3];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = NewSprite("Ring" + i, fxRoot.transform, AssetBuilder.Sprite("FX/Ring"), 25);
                rings[i].gameObject.SetActive(false);
            }

            var beams = new SpriteRenderer[4];
            for (int i = 0; i < beams.Length; i++)
            {
                beams[i] = NewSprite("Beam" + i, fxRoot.transform, AssetBuilder.Sprite("FX/Beam"), 26);
                beams[i].gameObject.SetActive(false);
            }

            var layout = board.AddComponent<BoardLayout>();

            var pool = board.AddComponent<TileViewPool>();
            Ui.Set(pool, "prefab", tilePrefab);
            Ui.Set(pool, "parent", tiles.transform);

            fx = board.AddComponent<FxPlayer>();
            Ui.Set(fx, "shards", shards);
            Ui.Set(fx, "sparkles", sparkles);
            Ui.Set(fx, "confetti", confetti);
            Ui.Set(fx, "rings", rings);
            Ui.Set(fx, "beams", beams);
            Ui.Set(fx, "shakeTarget", board.transform);

            var animator = board.AddComponent<TileAnimator>();
            Ui.Set(animator, "pool", pool);
            Ui.Set(animator, "fx", fx);

            var view = board.AddComponent<BoardView>();
            Ui.Set(view, "layout", layout);
            Ui.Set(view, "pool", pool);
            Ui.Set(view, "animator", animator);
            Ui.Set(view, "fx", fx);
            Ui.Set(view, "theme", theme);
            Ui.Set(view, "frame", frame);
            return view;
        }

        private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _spriteMaterial;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static ParticleSystem NewParticles(string name, Transform parent, Sprite sprite, int order, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = maxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(sprite);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _spriteMaterial;
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static ParticleSystem BuildShards(Transform parent)
        {
            ParticleSystem ps = NewParticles("Shards", parent, AssetBuilder.Sprite("FX/Shard"), 20, 500);

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.2f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            return ps;
        }

        private static ParticleSystem BuildSparkles(Transform parent)
        {
            ParticleSystem ps = NewParticles("Sparkles", parent, AssetBuilder.Sprite("FX/Sparkle"), 22, 200);

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.92f, 0.55f));

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
            return ps;
        }

        private static ParticleSystem BuildConfetti(Transform cameraTransform)
        {
            ParticleSystem ps = NewParticles("Confetti", cameraTransform, AssetBuilder.Sprite("FX/Shard"), 40, 220);
            ps.transform.localPosition = new Vector3(0f, 13f, 5f);

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.55f;

            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(AssetBuilder.ColorTints[0], 0f), new GradientColorKey(AssetBuilder.ColorTints[1], 0.2f),
                new GradientColorKey(AssetBuilder.ColorTints[2], 0.4f), new GradientColorKey(AssetBuilder.ColorTints[3], 0.6f),
                new GradientColorKey(AssetBuilder.ColorTints[4], 0.8f), new GradientColorKey(AssetBuilder.ColorTints[5], 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 70), new ParticleSystem.Burst(0.3f, 50), new ParticleSystem.Burst(0.7f, 40),
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.5f, 0.1f);
            shape.rotation = new Vector3(0f, 0f, 180f);

            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            return ps;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas NewCanvas(string name, int sortingOrder, out RectTransform safeArea)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            go.AddComponent<GraphicRaycaster>();

            safeArea = Ui.Rect("SafeArea", go.transform).Stretch();
            safeArea.gameObject.AddComponent<BlastPuzzle.UI.SafeArea>();
            return canvas;
        }

        private static Sprite S(string path) => AssetBuilder.Sprite(path);

        private static MenuScreen BuildMenuCanvas(LevelCatalog catalog)
        {
            Canvas canvas = NewCanvas("Canvas_Menu", 0, out RectTransform safe);
            var menu = canvas.gameObject.AddComponent<MenuScreen>();
            Ui.Set(menu, "group", Ui.Group(canvas.gameObject));

            Image starPill = Ui.Image("StarPill", safe, S("UI/Pill_9s"), new Color(0.1f, 0.08f, 0.3f, 0.55f));
            starPill.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -44f), new Vector2(300f, 96f));
            Image starIcon = Ui.Image("Star", starPill.transform, S("UI/Star_Full"), Color.white);
            starIcon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(92f, 92f));
            TMP_Text starTotal = Ui.Text("StarTotal", starPill.transform, "0 / 60", 46f, Color.white);
            ((RectTransform)starTotal.transform).Stretch(96f, 0f, 16f, 0f);

            Button settingsButton = Ui.IconButton("SettingsButton", safe, S("UI/Button_Blue_9s"), S("Icons/Icon_Gear"), 72f, out _);
            ((RectTransform)settingsButton.transform).Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(124f, 124f));

            RectTransform logo = Ui.Rect("Logo", safe).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 380f));
            AddLogoBlock(logo, "Blocks/Block_Red_Default", new Vector2(-360f, 40f), 150f, 14f);
            AddLogoBlock(logo, "Blocks/Block_Yellow_Default", new Vector2(370f, 70f), 130f, -12f);
            AddLogoBlock(logo, "Blocks/Block_Blue_Default", new Vector2(330f, -110f), 110f, 10f);
            AddLogoBlock(logo, "Blocks/Block_Green_Default", new Vector2(-340f, -120f), 104f, -9f);
            TMP_Text title = Ui.Text("Title", logo, "BLAST", 190f, Color.white);
            ((RectTransform)title.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(900f, 200f));
            title.characterSpacing = 4f;
            TMP_Text subtitle = Ui.Text("Subtitle", logo, "PUZZLE", 124f, new Color32(255, 214, 64, 255));
            ((RectTransform)subtitle.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(900f, 140f));
            subtitle.characterSpacing = 10f;

            Image panel = Ui.Image("LevelPanel", safe, S("UI/PanelDark_9s"), Color.white);
            panel.rectTransform.Stretch(44f, 310f, 44f, 580f);

            RectTransform viewport = Ui.Rect("Viewport", panel.transform).Stretch(46f, 52f, 46f, 46f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image dragArea = viewport.gameObject.AddComponent<Image>();
            dragArea.color = new Color(0f, 0f, 0f, 0f);
            dragArea.raycastTarget = true;

            RectTransform content = Ui.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 600f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196f, 216f);
            grid.spacing = new Vector2(18f, 22f);
            grid.padding = new RectOffset(6, 6, 10, 20);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;

            Button play = Ui.TextButton("PlayButton", safe, S("UI/Button_Green_9s"), "LEVEL 1", 88f, out TMP_Text playLabel);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(640f, 196f));

            LevelButton buttonPrefab = BuildLevelButtonPrefab();
            for (int i = 0; i < catalog.Count; i++)
            {
                var instance = (LevelButton)PrefabUtility.InstantiatePrefab(buttonPrefab, content);
                instance.name = "LevelButton_" + (i + 1).ToString("00");
                instance.Setup(i, i == 0 ? LevelButtonState.Current : LevelButtonState.Locked, 0, null);
            }

            Ui.Set(menu, "catalog", catalog);
            Ui.Set(menu, "levelButtonPrefab", buttonPrefab);
            Ui.Set(menu, "gridContent", content);
            Ui.Set(menu, "scroll", scroll);
            Ui.Set(menu, "playButton", play);
            Ui.Set(menu, "playLabel", playLabel);
            Ui.Set(menu, "starTotalLabel", starTotal);
            Ui.Set(menu, "settingsButton", settingsButton);
            return menu;
        }

        private static void AddLogoBlock(RectTransform parent, string sprite, Vector2 position, float size, float angle)
        {
            Image image = Ui.Image("Block", parent, S(sprite), Color.white);
            image.rectTransform.Place(Center, Center, position, new Vector2(size, size));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static LevelButton BuildLevelButtonPrefab()
        {
            string path = AssetBuilder.PrefabFolder + "/LevelButton.prefab";
            AssetBuilder.EnsureFolder(AssetBuilder.PrefabFolder);

            var holder = new GameObject("PrefabHolder", typeof(RectTransform));
            Button button = Ui.Button("LevelButton", holder.transform, S("UI/Button_Blue_9s"));
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(196f, 216f);

            TMP_Text number = Ui.Text("Number", rect, "1", 92f, Color.white);
            ((RectTransform)number.transform).Stretch(0f, 56f, 0f, 10f);

            Image lockIcon = Ui.Image("Lock", rect, S("Icons/Icon_Lock"), new Color(1f, 1f, 1f, 0.85f));
            lockIcon.rectTransform.Place(Center, Center, new Vector2(0f, 10f), new Vector2(96f, 96f));

            RectTransform starRow = Ui.Rect("Stars", rect).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(170f, 56f));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Ui.Image("Star" + i, starRow, S("UI/Star_Full"), Color.white);
                stars[i].rectTransform.Place(Center, Center, new Vector2((i - 1) * 54f, i == 1 ? 4f : 0f), new Vector2(58f, 58f));
            }

            var levelButton = button.gameObject.AddComponent<LevelButton>();
            Ui.Set(levelButton, "button", button);
            Ui.Set(levelButton, "background", button.GetComponent<Image>());
            Ui.Set(levelButton, "numberLabel", number);
            Ui.Set(levelButton, "lockIcon", lockIcon.gameObject);
            Ui.Set(levelButton, "starRow", starRow.gameObject);
            Ui.Set(levelButton, "stars", stars);
            Ui.Set(levelButton, "lockedSprite", S("UI/Button_Grey_9s"));
            Ui.Set(levelButton, "currentSprite", S("UI/Button_Green_9s"));
            Ui.Set(levelButton, "completedSprite", S("UI/Button_Blue_9s"));
            Ui.Set(levelButton, "starFull", S("UI/Star_Full"));
            Ui.Set(levelButton, "starEmpty", S("UI/Star_Empty"));

            button.transform.SetParent(null, false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(button.gameObject, path);
            Object.DestroyImmediate(button.gameObject);
            Object.DestroyImmediate(holder);
            return prefab.GetComponent<LevelButton>();
        }

        private static HudView BuildHudCanvas(out UIPanel hudPanel)
        {
            Canvas canvas = NewCanvas("Canvas_HUD", 1, out RectTransform safe);
            hudPanel = canvas.gameObject.AddComponent<UIPanel>();
            Ui.Set(hudPanel, "group", Ui.Group(canvas.gameObject));
            var hud = canvas.gameObject.AddComponent<HudView>();

            Image top = Ui.Image("TopPanel", safe, S("UI/PanelDark_9s"), Color.white);
            top.rectTransform.PlaceRow(1f, -20f, 250f, 22f);

            RectTransform moves = Ui.Rect("Moves", top.transform).Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 4f), new Vector2(210f, 190f));
            TMP_Text movesTitle = Ui.Text("Title", moves, "MOVES", 38f, new Color32(190, 200, 255, 255));
            ((RectTransform)movesTitle.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(210f, 50f));
            TMP_Text movesLabel = Ui.Text("Value", moves, "20", 112f, Color.white);
            ((RectTransform)movesLabel.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(210f, 136f));

            RectTransform goals = Ui.Rect("Goals", top.transform).Place(Center, Center, new Vector2(0f, 4f), new Vector2(470f, 196f));
            TMP_Text levelLabel = Ui.Text("LevelLabel", goals, "LEVEL 1", 38f, new Color32(190, 200, 255, 255));
            ((RectTransform)levelLabel.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(470f, 50f));

            RectTransform goalRow = Ui.Rect("GoalRow", goals).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(470f, 144f));
            var rowLayout = goalRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 20f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var goalItems = new GoalItemView[3];
            for (int i = 0; i < goalItems.Length; i++)
                goalItems[i] = BuildGoalItem(goalRow, i);

            Button pause = Ui.IconButton("PauseButton", top.transform, S("UI/Button_Blue_9s"), S("Icons/Icon_Pause"), 64f, out _);
            ((RectTransform)pause.transform).Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-52f, 4f), new Vector2(124f, 124f));

            RectTransform flyerRoot = Ui.Rect("Flyers", canvas.transform).Stretch();
            var flyers = new Image[12];
            for (int i = 0; i < flyers.Length; i++)
            {
                flyers[i] = Ui.Image("Flyer" + i, flyerRoot, S("Blocks/Block_Red_Default"), Color.white);
                flyers[i].rectTransform.Place(Center, Center, Vector2.zero, new Vector2(96f, 96f));
                flyers[i].gameObject.SetActive(false);
            }

            Ui.Set(hud, "levelLabel", levelLabel);
            Ui.Set(hud, "movesLabel", movesLabel);
            Ui.Set(hud, "goalItems", goalItems);
            Ui.Set(hud, "pauseButton", pause);
            Ui.Set(hud, "flyerRoot", flyerRoot);
            Ui.Set(hud, "flyers", flyers);

            canvas.gameObject.SetActive(false);
            return hud;
        }

        private static GoalItemView BuildGoalItem(RectTransform parent, int index)
        {
            RectTransform item = Ui.Rect("Goal" + index, parent);
            item.sizeDelta = new Vector2(130f, 144f);

            Image icon = Ui.Image("Icon", item, S("Blocks/Block_Red_Default"), Color.white);
            icon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(96f, 96f));

            TMP_Text count = Ui.Text("Count", item, "12", 50f, Color.white);
            ((RectTransform)count.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(130f, 58f));

            Image check = Ui.Image("Check", item, S("Icons/Icon_Check"), new Color32(120, 240, 110, 255));
            check.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(54f, 54f));
            check.gameObject.SetActive(false);

            var view = item.gameObject.AddComponent<GoalItemView>();
            Ui.Set(view, "icon", icon);
            Ui.Set(view, "countLabel", count);
            Ui.Set(view, "check", check.gameObject);
            return view;
        }

        private static void BuildPopupCanvas(out Banner banner, out SettingsPopup settings, out ResultPopup result)
        {
            Canvas canvas = NewCanvas("Canvas_Popups", 10, out RectTransform safe);
            banner = BuildBanner(safe);
            settings = BuildSettingsPopup(canvas.transform);
            result = BuildResultPopup(canvas.transform);
        }

        private static Banner BuildBanner(RectTransform parent)
        {
            RectTransform root = Ui.Rect("Banner", parent).Place(Center, Center, new Vector2(0f, 60f), new Vector2(900f, 170f));
            CanvasGroup group = Ui.Group(root.gameObject);
            group.interactable = false;
            group.blocksRaycasts = false;

            Image body = Ui.Image("Body", root, S("UI/Pill_9s"), new Color(0.1f, 0.07f, 0.32f, 0.82f));
            body.rectTransform.Stretch();
            TMP_Text label = Ui.Text("Label", body.transform, "LEVEL 1", 70f, Color.white);
            ((RectTransform)label.transform).Stretch(30f, 10f, 30f, 10f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 36f;
            label.fontSizeMax = 70f;

            var component = root.gameObject.AddComponent<Banner>();
            Ui.Set(component, "group", group);
            Ui.Set(component, "body", body.rectTransform);
            Ui.Set(component, "label", label);
            root.gameObject.SetActive(false);
            return component;
        }

        private static RectTransform PopupShell(string name, Transform parent, Vector2 windowSize, string title,
            out GameObject root, out TMP_Text titleLabel)
        {
            RectTransform rootRect = Ui.Rect(name, parent).Stretch();
            root = rootRect.gameObject;
            Ui.Group(root);

            Image dim = Ui.Image("Dim", rootRect, null, new Color(0.04f, 0.02f, 0.14f, 0.72f), raycast: true);
            dim.rectTransform.Stretch();

            Image window = Ui.Image("Window", rootRect, S("UI/Panel_9s"), Color.white, raycast: true);
            window.rectTransform.Place(Center, Center, new Vector2(0f, -20f), windowSize);

            Image ribbon = Ui.Image("Ribbon", window.transform, S("UI/Ribbon"), Color.white);
            ribbon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(700f, 210f));
            titleLabel = Ui.Text("Title", ribbon.transform, title, 74f, Color.white);
            ((RectTransform)titleLabel.transform).Stretch(90f, 60f, 90f, 22f);
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 40f;
            titleLabel.fontSizeMax = 74f;

            return window.rectTransform;
        }

        private static SettingsPopup BuildSettingsPopup(Transform parent)
        {
            RectTransform window = PopupShell("SettingsPopup", parent, new Vector2(840f, 980f), "SETTINGS", out GameObject root, out TMP_Text title);
            var popup = root.AddComponent<SettingsPopup>();
            Ui.Set(popup, "group", root.GetComponent<CanvasGroup>());
            Ui.Set(popup, "window", window);

            Button close = Ui.IconButton("CloseButton", window, S("UI/Button_Red_9s"), S("Icons/Icon_Close"), 56f, out _);
            ((RectTransform)close.transform).Place(new Vector2(1f, 1f), Center, new Vector2(-30f, -40f), new Vector2(108f, 108f));

            ToggleIcon sound = BuildToggle(window, "SoundToggle", new Vector2(-150f, 190f), "Icons/Icon_SoundOn", "Icons/Icon_SoundOff");
            ToggleIcon music = BuildToggle(window, "MusicToggle", new Vector2(150f, 190f), "Icons/Icon_MusicOn", "Icons/Icon_MusicOff");

            RectTransform pause = Ui.Rect("PauseButtons", window).Stretch();
            Button resume = Ui.TextButton("ResumeButton", pause, S("UI/Button_Green_9s"), "RESUME", 76f, out _);
            ((RectTransform)resume.transform).Place(Center, Center, new Vector2(0f, -70f), new Vector2(580f, 170f));
            Button restart = Ui.IconButton("RestartButton", pause, S("UI/Button_Yellow_9s"), S("Icons/Icon_Retry"), 86f, out _);
            ((RectTransform)restart.transform).Place(Center, Center, new Vector2(-150f, -280f), new Vector2(190f, 180f));
            Button home = Ui.IconButton("HomeButton", pause, S("UI/Button_Blue_9s"), S("Icons/Icon_Home"), 86f, out _);
            ((RectTransform)home.transform).Place(Center, Center, new Vector2(150f, -280f), new Vector2(190f, 180f));

            RectTransform menu = Ui.Rect("MenuButtons", window).Stretch();
            Button reset = Ui.TextButton("ResetButton", menu, S("UI/Button_Red_9s"), "RESET PROGRESS", 50f, out TMP_Text resetLabel);
            ((RectTransform)reset.transform).Place(Center, Center, new Vector2(0f, -120f), new Vector2(620f, 150f));
            resetLabel.enableAutoSizing = true;
            resetLabel.fontSizeMin = 30f;
            resetLabel.fontSizeMax = 50f;
            TMP_Text credit = Ui.Text("Credit", menu, "Made with Unity by Sefa Akgun", 36f, Ui.Ink, outlined: false);
            ((RectTransform)credit.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(700f, 50f));
            credit.fontStyle = FontStyles.Normal;

            Ui.Set(popup, "titleLabel", title);
            Ui.Set(popup, "soundToggle", sound);
            Ui.Set(popup, "musicToggle", music);
            Ui.Set(popup, "closeButton", close);
            Ui.Set(popup, "pauseButtons", pause.gameObject);
            Ui.Set(popup, "resumeButton", resume);
            Ui.Set(popup, "restartButton", restart);
            Ui.Set(popup, "homeButton", home);
            Ui.Set(popup, "menuButtons", menu.gameObject);
            Ui.Set(popup, "resetButton", reset);
            Ui.Set(popup, "resetLabel", resetLabel);

            root.SetActive(false);
            return popup;
        }

        private static ToggleIcon BuildToggle(RectTransform parent, string name, Vector2 position, string onIcon, string offIcon)
        {
            Button button = Ui.IconButton(name, parent, S("UI/Button_Green_9s"), S(onIcon), 104f, out Image icon);
            ((RectTransform)button.transform).Place(Center, Center, position, new Vector2(220f, 210f));

            var toggle = button.gameObject.AddComponent<ToggleIcon>();
            Ui.Set(toggle, "button", button);
            Ui.Set(toggle, "icon", icon);
            Ui.Set(toggle, "onSprite", S(onIcon));
            Ui.Set(toggle, "offSprite", S(offIcon));
            Ui.Set(toggle, "background", button.GetComponent<Image>());
            Ui.Set(toggle, "onBackground", S("UI/Button_Green_9s"));
            Ui.Set(toggle, "offBackground", S("UI/Button_Grey_9s"));
            return toggle;
        }

        private static ResultPopup BuildResultPopup(Transform parent)
        {
            RectTransform window = PopupShell("ResultPopup", parent, new Vector2(860f, 1140f), "LEVEL 1", out GameObject root, out TMP_Text title);
            var popup = root.AddComponent<ResultPopup>();
            Ui.Set(popup, "group", root.GetComponent<CanvasGroup>());
            Ui.Set(popup, "window", window);

            RectTransform starRow = Ui.Rect("Stars", window).Place(Center, Center, new Vector2(0f, 250f), new Vector2(700f, 260f));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 250f : 200f;
                stars[i] = Ui.Image("Star" + i, starRow, S("UI/Star_Empty"), Color.white);
                stars[i].rectTransform.Place(Center, Center, new Vector2((i - 1) * 215f, i == 1 ? 26f : -10f), new Vector2(size, size));
                stars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 12f);
            }

            TMP_Text message = Ui.Text("Message", window, "WELL DONE!", 92f, Ui.Ink, outlined: false);
            ((RectTransform)message.transform).Place(Center, Center, new Vector2(0f, 20f), new Vector2(740f, 120f));
            message.enableAutoSizing = true;
            message.fontSizeMin = 50f;
            message.fontSizeMax = 92f;

            TMP_Text score = Ui.Text("Score", window, "SCORE  1200", 54f, new Color32(140, 110, 210, 255), outlined: false);
            ((RectTransform)score.transform).Place(Center, Center, new Vector2(0f, -80f), new Vector2(740f, 70f));

            Button primary = Ui.TextButton("PrimaryButton", window, S("UI/Button_Green_9s"), "NEXT", 84f, out TMP_Text primaryLabel);
            ((RectTransform)primary.transform).Place(Center, Center, new Vector2(0f, -230f), new Vector2(600f, 184f));
            primaryLabel.enableAutoSizing = true;
            primaryLabel.fontSizeMin = 48f;
            primaryLabel.fontSizeMax = 84f;

            Button retry = Ui.IconButton("RetryButton", window, S("UI/Button_Yellow_9s"), S("Icons/Icon_Retry"), 78f, out _);
            ((RectTransform)retry.transform).Place(Center, Center, new Vector2(-130f, -430f), new Vector2(170f, 160f));
            Button home = Ui.IconButton("HomeButton", window, S("UI/Button_Blue_9s"), S("Icons/Icon_Home"), 78f, out _);
            ((RectTransform)home.transform).Place(Center, Center, new Vector2(130f, -430f), new Vector2(170f, 160f));

            Ui.Set(popup, "titleLabel", title);
            Ui.Set(popup, "messageLabel", message);
            Ui.Set(popup, "scoreLabel", score);
            Ui.Set(popup, "starRow", starRow.gameObject);
            Ui.Set(popup, "stars", stars);
            Ui.Set(popup, "starFull", S("UI/Star_Full"));
            Ui.Set(popup, "starEmpty", S("UI/Star_Empty"));
            Ui.Set(popup, "primaryButton", primary);
            Ui.Set(popup, "primaryLabel", primaryLabel);
            Ui.Set(popup, "retryButton", retry);
            Ui.Set(popup, "homeButton", home);

            root.SetActive(false);
            return popup;
        }

        private static void WireAudio(AudioManager audio)
        {
            var so = new SerializedObject(audio);
            SerializedProperty effects = so.FindProperty("effects");

            (Sfx id, float volume)[] table =
            {
                (Sfx.Pop, 0.75f), (Sfx.Land, 0.25f), (Sfx.Invalid, 0.5f), (Sfx.Click, 0.6f),
                (Sfx.Rocket, 0.7f), (Sfx.Bomb, 0.9f), (Sfx.Disco, 0.7f), (Sfx.BoosterCreate, 0.7f),
                (Sfx.BoxHit, 0.6f), (Sfx.BoxBreak, 0.7f), (Sfx.Shuffle, 0.6f),
                (Sfx.Star, 0.8f), (Sfx.Goal, 0.7f), (Sfx.Win, 0.8f), (Sfx.Lose, 0.7f),
            };

            effects.arraySize = table.Length;
            for (int i = 0; i < table.Length; i++)
            {
                SerializedProperty entry = effects.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").enumValueIndex = (int)table[i].id;
                entry.FindPropertyRelative("volume").floatValue = table[i].volume;
                entry.FindPropertyRelative("clip").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>($"{AssetBuilder.AudioFolder}/SFX/{table[i].id}.wav");
            }

            so.FindProperty("music").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{AssetBuilder.AudioFolder}/Music/MainLoop.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
