using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>
    /// All game screens: main menu, HUD, pause, revive, results, settings, shop,
    /// level select, missions, achievements and the first-run tutorial.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private GameManager game;
        private Canvas canvas;
        private RectTransform safeRoot;

        private RectTransform menuScreen;
        private RectTransform hudScreen;
        private RectTransform pauseScreen;
        private RectTransform reviveScreen;
        private RectTransform resultsScreen;
        private RectTransform settingsScreen;
        private RectTransform shopScreen;
        private RectTransform levelsScreen;
        private RectTransform missionsScreen;
        private RectTransform achievementsScreen;
        private RectTransform tutorialScreen;

        // Menu
        private TextMeshProUGUI menuCoins;
        private TextMeshProUGUI menuLevel;
        private TextMeshProUGUI menuBest;
        private TextMeshProUGUI menuName;
        private TextMeshProUGUI menuTitle;
        private TextMeshProUGUI menuTip;
        private UIFactory.ProgressBar[] menuStatBars = new UIFactory.ProgressBar[3];
        private Image menuAvatar;

        // HUD
        private TextMeshProUGUI hudScore;
        private TextMeshProUGUI hudDistance;
        private TextMeshProUGUI hudBest;
        private TextMeshProUGUI hudCoins;
        private TextMeshProUGUI hudCombo;
        private RectTransform hudComboRoot;
        private Image[] hearts = new Image[3];
        private UIFactory.ProgressBar hudLevelBar;
        private UIFactory.ProgressBar hudSpeedBar;
        private RectTransform hudLevelRoot;
        private RectTransform hudSpeedRoot;
        private RectTransform onScreenControls;
        private readonly PowerRowUI[] powerRows = new PowerRowUI[(int)PowerUpType.Count];

        // Overlays
        private TextMeshProUGUI reviveCoins;
        private TextMeshProUGUI reviveStats;
        private Button reviveButton;
        private Button giveUpButton;

        private TextMeshProUGUI resultsTitle;
        private TextMeshProUGUI resultsSubtitle;
        private Image[] resultStars = new Image[3];
        private TextMeshProUGUI resultsScore;
        private TextMeshProUGUI resultsDistance;
        private TextMeshProUGUI resultsCoins;
        private TextMeshProUGUI resultsCombo;
        private TextMeshProUGUI resultsNear;
        private TextMeshProUGUI resultsTime;
        private TextMeshProUGUI resultsBest;
        private RectTransform resultsStarsRoot;
        private Button resultsNext;
        private RectTransform resultsDynasty;

        // Settings
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider voiceSlider;
        private TextMeshProUGUI musicValue;
        private TextMeshProUGUI sfxValue;
        private TextMeshProUGUI voiceValue;
        private readonly Button[] qualityButtons = new Button[4];
        private Button fps30Button;
        private Button fps60Button;
        private TextMeshProUGUI resetLabel;
        private float resetArmedTime = -10f;
        private Toggle vibToggle;
        private Toggle shakeToggle;
        private Toggle controlsToggle;

        // Shop
        private TextMeshProUGUI shopCoins;
        private RectTransform shopContent;
        private TextMeshProUGUI shopTabRunners;
        private TextMeshProUGUI shopTabSkins;
        private int shopPage = 0;

        // Levels / missions / achievements
        private RectTransform levelsContent;
        private RectTransform missionsContent;
        private RectTransform achievementsContent;
        private TextMeshProUGUI achievementCount;

        // Feedback
        private CanvasGroup toastGroup;
        private TextMeshProUGUI toastText;
        private float toastTimer;
        private Image flashImage;
        private float flashAmount;
        private bool tutorialActive;
        private float tutorialTimer;
        private TextMeshProUGUI tutorialText;
        private int tutorialStep;

        private static readonly string[] TutorialSteps =
        {
            "SWIPE LEFT OR RIGHT TO CHANGE LANE",
            "SWIPE UP TO JUMP OVER BARRIERS",
            "SWIPE DOWN TO SLIDE UNDER GATES",
            "COLLECT COINS TO BUILD YOUR COMBO"
        };

        public bool IsBuilt { get; private set; }

        public static UIManager Create(GameManager game, Transform parent)
        {
            GameObject go = RuntimeUtil.Create("UI", parent);
            UIManager ui = go.AddComponent<UIManager>();
            ui.game = game;
            return ui;
        }

        /// <summary>
        /// Builds the screens in small groups so no single frame does all of the
        /// uGUI/TMP work. Keeps the first launch responsive on low-end phones.
        /// </summary>
        public IEnumerator BuildRoutine()
        {
            if (IsBuilt) yield break;

            GameBootstrap.SetBootStage("BUILDING INTERFACE", 0.86f);
            UIFactory.EnsureEventSystem();
            canvas = UIFactory.CreateCanvas(transform);
            RectTransform root = UIFactory.Stretch(canvas.transform, "Root");
            safeRoot = UIFactory.Stretch(root, "SafeArea");
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING MENU", 0.88f);
            BuildMenu();
            BuildHUD();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING SCREENS", 0.90f);
            BuildPause();
            BuildRevive();
            BuildResults();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING SCREENS", 0.92f);
            BuildSettings();
            BuildShop();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING SCREENS", 0.94f);
            BuildLevels();
            BuildMissions();
            BuildAchievements();
            BuildTutorial();
            BuildToast();
            BuildFlash();
            yield return null;

            game.ToastRequested += Toast;
            game.HitTaken += OnHit;

            HideAll();
            menuScreen.gameObject.SetActive(true);
            IsBuilt = true;
        }

        private RectTransform NewScreen(string name)
        {
            RectTransform rt = UIFactory.Stretch(safeRoot, name);
            rt.gameObject.SetActive(false);
            return rt;
        }

        private Chain Overlay(string name, Vector2 size, Color color, out RectTransform panel)
        {
            RectTransform screen = NewScreen(name);
            UIFactory.Dim(screen, 0.8f);
            panel = UIFactory.Panel(screen, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, size, color).rectTransform;
            return new Chain(screen, panel);
        }

        private struct Chain
        {
            public RectTransform Screen;
            public RectTransform Panel;
            public Chain(RectTransform screen, RectTransform panel) { Screen = screen; Panel = panel; }
        }

        // ------------------------------------------------------------------ MENU

        private void BuildMenu()
        {
            menuScreen = NewScreen("MenuScreen");
            UIFactory.FullPanel(menuScreen, "Tint", new Color(0.015f, 0.03f, 0.07f, 0.28f));

            RectTransform left = UIFactory.Rect(menuScreen, "LeftPanel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(860f, 0f));
            UIFactory.Image(left, Art.White, new Color(0.02f, 0.05f, 0.1f, 0.82f));
            RectTransform edge = UIFactory.Rect(left, "Edge", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 0f));
            UIFactory.Image(edge, Art.White, new Color(0f, 0.9f, 1f, 0.35f));

            UIFactory.Sprite(left, Art.Glow(), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(330f, -190f), new Vector2(640f, 360f), new Color(0f, 0.9f, 1f, 0.18f));

            TextMeshProUGUI logo = UIFactory.Text(left, "Logo", "SKYBOUND", 104f, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -64f), new Vector2(740f, 130f),
                TextAlignmentOptions.Left, true);
            logo.characterSpacing = 2f;

            TextMeshProUGUI sub = UIFactory.Text(left, "SubLogo", "COIN RUSH", 54f, Palette.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -178f), new Vector2(700f, 80f),
                TextAlignmentOptions.Left, true);
            sub.characterSpacing = 14f;

            UIFactory.Text(left, "Tagline", "RUN. DODGE. OWN THE SKY.", 21f, Palette.Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -258f), new Vector2(680f, 34f),
                TextAlignmentOptions.Left, true);

            UIFactory.Button(left, "Play", "PLAY", Palette.Primary, new Color(0.01f, 0.08f, 0.12f),
                new Vector2(0f, 1f), new Vector2(74f, -356f), new Vector2(640f, 92f),
                () => game.StartEndlessRun(), "play", 38f);

            UIFactory.Button(left, "Levels", "LEVELS", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0f, 1f), new Vector2(74f, -474f), new Vector2(310f, 76f),
                () => ShowLevelSelect(true), "star", 26f);
            UIFactory.Button(left, "Shop", "SKY SHOP", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0f, 1f), new Vector2(404f, -474f), new Vector2(310f, 76f),
                () => ShowShop(true), "diamond", 26f);
            UIFactory.Button(left, "Missions", "MISSIONS", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0f, 1f), new Vector2(74f, -566f), new Vector2(310f, 76f),
                () => ShowMissions(true), "trophy", 26f);
            UIFactory.Button(left, "Achievements", "AWARDS", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0f, 1f), new Vector2(404f, -566f), new Vector2(310f, 76f),
                () => ShowAchievements(true), "shield", 26f);
            UIFactory.Button(left, "Settings", "SETTINGS", new Color(0.05f, 0.09f, 0.15f, 0.9f), Palette.Muted,
                new Vector2(0f, 1f), new Vector2(74f, -658f), new Vector2(640f, 60f),
                () => ShowSettings(true), "settings", 22f);

            // Character showcase card.
            RectTransform card = UIFactory.Rect(left, "RunnerCard", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(74f, 56f), new Vector2(700f, 228f));
            UIFactory.Image(card, Art.Panel, new Color(0.05f, 0.1f, 0.17f, 0.95f), true);

            RectTransform avatar = UIFactory.Rect(card, "Avatar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(88f, 12f), new Vector2(132f, 132f));
            menuAvatar = UIFactory.Image(avatar, Art.Circle(), Palette.Primary);
            UIFactory.Icon(avatar, "boost", new Vector2(0.5f, 0.5f), Vector2.zero, 74f,
                new Color(0.02f, 0.06f, 0.1f, 0.9f));

            menuName = UIFactory.Text(card, "Name", "NOVA", 34f, Palette.Text, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(172f, -22f), new Vector2(460f, 44f),
                TextAlignmentOptions.Left, true);
            menuTitle = UIFactory.Text(card, "Title", "THE SKY COURIER", 17f, Palette.Primary,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(174f, -64f), new Vector2(460f, 30f),
                TextAlignmentOptions.Left, true);

            string[] stats = { "SPEED", "JUMP", "RECOVERY" };
            for (int i = 0; i < 3; i++)
            {
                UIFactory.Text(card, "StatLabel" + i, stats[i], 14f, Palette.Muted,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(174f, -102f - i * 30f),
                    new Vector2(90f, 24f), TextAlignmentOptions.Left, true);
                menuStatBars[i] = UIFactory.Bar(card, "StatBar" + i, null, new Vector2(0f, 1f),
                    new Vector2(354f, -112f - i * 30f), new Vector2(230f, 12f), Palette.Primary);
            }

            // Top-right currency + progression.
            RectTransform coinPill = UIFactory.Rect(menuScreen, "CoinPill", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(246f, 62f));
            UIFactory.Image(coinPill, Art.Pill, new Color(0.05f, 0.1f, 0.17f, 0.95f), true);
            UIFactory.Icon(coinPill, "coin", new Vector2(0f, 0.5f), new Vector2(36f, 0f), 36f, Color.white);
            menuCoins = UIFactory.Text(coinPill, "Value", "0", 27f, Palette.Accent, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(170f, 44f),
                TextAlignmentOptions.Right, true);

            RectTransform levelPill = UIFactory.Rect(menuScreen, "LevelPill", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-306f, -36f), new Vector2(246f, 62f));
            UIFactory.Image(levelPill, Art.Pill, new Color(0.05f, 0.1f, 0.17f, 0.95f), true);
            UIFactory.Icon(levelPill, "star", new Vector2(0f, 0.5f), new Vector2(36f, 0f), 32f, Palette.Accent);
            menuLevel = UIFactory.Text(levelPill, "Value", "LEVEL 1", 23f, Palette.Text,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(175f, 44f),
                TextAlignmentOptions.Right, true);

            menuBest = UIFactory.Text(menuScreen, "Best", "BEST  0", 21f, Palette.Muted,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -112f), new Vector2(320f, 32f),
                TextAlignmentOptions.Right, true);

            RectTransform tip = UIFactory.Rect(menuScreen, "Tip", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-40f, 34f), new Vector2(620f, 108f));
            UIFactory.Image(tip, Art.Panel, new Color(0.04f, 0.08f, 0.14f, 0.88f), true);
            UIFactory.Text(tip, "TipLabel", "TIP", 16f, Palette.Accent, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(100f, 26f),
                TextAlignmentOptions.Left, true);
            menuTip = UIFactory.Text(tip, "TipText", GameConfig.Tips[0], 20f, Palette.Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -46f), new Vector2(570f, 52f),
                TextAlignmentOptions.TopLeft);

            UIFactory.Text(menuScreen, "Version", "v" + GameConfig.Version + "  •  SKYBOUND STUDIO",
                15f, new Color(0.45f, 0.55f, 0.68f, 0.9f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(38f, 18f), new Vector2(460f, 24f), TextAlignmentOptions.Left);
        }

        // ------------------------------------------------------------------- HUD

        private void BuildHUD()
        {
            hudScreen = NewScreen("HUDScreen");

            RectTransform scoreBox = UIFactory.Rect(hudScreen, "ScoreBox", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(430f, 112f));
            UIFactory.Text(scoreBox, "ScoreLabel", "SCORE", 16f, Palette.Muted, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(200f, 24f),
                TextAlignmentOptions.Left, true);
            hudScore = UIFactory.Text(scoreBox, "Score", "0", 56f, Color.white, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, -18f), new Vector2(420f, 74f),
                TextAlignmentOptions.Left, true);
            hudDistance = UIFactory.Text(scoreBox, "Distance", "0 M", 21f, Palette.Primary,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -88f), new Vector2(300f, 28f),
                TextAlignmentOptions.Left, true);
            hudBest = UIFactory.Text(scoreBox, "Best", "BEST 0", 17f, Palette.Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -88f), new Vector2(220f, 28f),
                TextAlignmentOptions.Left, true);

            hudComboRoot = UIFactory.Rect(hudScreen, "Combo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(300f, 64f));
            UIFactory.Image(hudComboRoot, Art.Pill, new Color(0.05f, 0.1f, 0.17f, 0.92f), true);
            UIFactory.Icon(hudComboRoot, "boost", new Vector2(0f, 0.5f), new Vector2(36f, 0f), 28f, Palette.Accent);
            hudCombo = UIFactory.Text(hudComboRoot, "Value", "COMBO x2", 24f, Palette.Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(230f, 44f),
                TextAlignmentOptions.Center, true);
            hudComboRoot.gameObject.SetActive(false);

            RectTransform coinPill = UIFactory.Rect(hudScreen, "CoinPill", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-108f, -26f), new Vector2(220f, 58f));
            UIFactory.Image(coinPill, Art.Pill, new Color(0.05f, 0.1f, 0.17f, 0.92f), true);
            UIFactory.Icon(coinPill, "coin", new Vector2(0f, 0.5f), new Vector2(32f, 0f), 30f, Color.white);
            hudCoins = UIFactory.Text(coinPill, "Value", "0", 24f, Palette.Accent, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(160f, 40f),
                TextAlignmentOptions.Right, true);

            UIFactory.IconButton(hudScreen, "PauseButton", "pause", new Color(0.05f, 0.1f, 0.17f, 0.92f),
                Palette.Text, new Vector2(1f, 1f), new Vector2(-44f, -26f), new Vector2(58f, 58f),
                game.PauseGame);

            for (int i = 0; i < 3; i++)
            {
                hearts[i] = UIFactory.Icon(hudScreen, "heart", new Vector2(0f, 0f),
                    new Vector2(46f + i * 60f, 44f), 46f, Palette.Danger);
                ((RectTransform)hearts[i].transform).pivot = new Vector2(0f, 0f);
                ((RectTransform)hearts[i].transform).anchoredPosition = new Vector2(46f + i * 60f, 44f);
            }

            hudLevelRoot = UIFactory.Rect(hudScreen, "LevelProgress", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(640f, 44f));
            hudLevelBar = UIFactory.Bar(hudLevelRoot, "Bar", "CITY RUN", new Vector2(0f, 0f),
                Vector2.zero, new Vector2(640f, 20f), Palette.Primary, true);

            hudSpeedRoot = UIFactory.Rect(hudScreen, "SpeedProgress", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(640f, 44f));
            hudSpeedBar = UIFactory.Bar(hudSpeedRoot, "Bar", "SPEED", new Vector2(0f, 0f),
                Vector2.zero, new Vector2(640f, 20f), Palette.Secondary, true);

            for (int i = 0; i < (int)PowerUpType.Count; i++)
            {
                PowerUpType type = (PowerUpType)i;
                PowerRowUI row = new PowerRowUI();
                row.Type = type;
                RectTransform rt = UIFactory.Rect(hudScreen, "PowerRow" + i, new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(44f, 86f - i * 54f), new Vector2(286f, 46f));
                UIFactory.Image(rt, Art.PanelSmall, new Color(0.04f, 0.08f, 0.14f, 0.85f), true);
                row.Root = rt;
                row.Icon = UIFactory.Icon(rt, Art.IconKeyForPowerUp(type), new Vector2(0f, 0.5f),
                    new Vector2(30f, 0f), 30f, GameConfig.GetPowerUp(type).Color);
                row.Bar = UIFactory.Bar(rt, "Timer", null, new Vector2(0f, 0.5f),
                    new Vector2(148f, 0f), new Vector2(176f, 10f), GameConfig.GetPowerUp(type).Color);
                row.Time = UIFactory.Text(rt, "Time", "0.0", 17f, Palette.Text, new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(58f, 26f),
                    TextAlignmentOptions.Right, true);
                rt.gameObject.SetActive(false);
                powerRows[i] = row;
            }

            BuildOnScreenControls();
        }

        private void BuildOnScreenControls()
        {
            onScreenControls = UIFactory.Rect(hudScreen, "OnScreenControls", new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(340f, 250f));
            Color bg = new Color(0.04f, 0.09f, 0.15f, 0.55f);
            UIFactory.IconButton(onScreenControls, "Left", "arrow_left", bg, Palette.Primary,
                new Vector2(0f, 1f), new Vector2(52f, -172f), new Vector2(74f, 74f),
                () => game.Player.Input.PerformLane(-1));
            UIFactory.IconButton(onScreenControls, "Right", "arrow_right", bg, Palette.Primary,
                new Vector2(1f, 1f), new Vector2(-52f, -172f), new Vector2(74f, 74f),
                () => game.Player.Input.PerformLane(1));
            UIFactory.IconButton(onScreenControls, "Jump", "arrow_up", bg, Palette.Accent,
                new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(78f, 78f),
                () => game.Player.Input.PerformJump());
            UIFactory.IconButton(onScreenControls, "Slide", "arrow_down", bg, Palette.Secondary,
                new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(78f, 78f),
                () => game.Player.Input.PerformSlide());
            onScreenControls.gameObject.SetActive(false);
        }

        // ----------------------------------------------------------------- PAUSE

        private void BuildPause()
        {
            RectTransform panel;
            Chain chain = Overlay("PauseScreen", new Vector2(780f, 620f), new Color(0.05f, 0.1f, 0.17f, 0.99f), out panel);
            pauseScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "PAUSED", 64f, Color.white, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(600f, 80f),
                TextAlignmentOptions.Center, true);
            UIFactory.Text(panel, "Sub", "TAKE A BREATH. THE SKY WILL WAIT.", 18f, Palette.Muted,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(600f, 32f),
                TextAlignmentOptions.Center, true);

            UIFactory.Button(panel, "Resume", "RESUME", Palette.Primary, new Color(0.01f, 0.08f, 0.12f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(480f, 80f),
                game.ResumeGame, "play", 28f);
            UIFactory.Button(panel, "Restart", "RESTART", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(480f, 72f),
                game.RestartRun, "retry", 25f);
            UIFactory.Button(panel, "Settings", "SETTINGS", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(480f, 72f),
                () => ShowSettings(true), "settings", 25f);
            UIFactory.Button(panel, "Quit", "QUIT TO MENU", new Color(0.12f, 0.08f, 0.12f, 1f), Palette.Danger,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(480f, 72f),
                game.QuitToMenu, "home", 25f);
        }

        // ---------------------------------------------------------------- REVIVE

        private void BuildRevive()
        {
            RectTransform panel;
            Chain chain = Overlay("ReviveScreen", new Vector2(860f, 600f), new Color(0.07f, 0.08f, 0.14f, 0.99f), out panel);
            reviveScreen = chain.Screen;

            UIFactory.Sprite(panel, Art.Glow(), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -26f), new Vector2(520f, 260f), new Color(1f, 0.15f, 0.2f, 0.18f));
            UIFactory.Icon(panel, "warning", new Vector2(0.5f, 1f), new Vector2(0f, -60f), 40f, Palette.Danger);

            UIFactory.Text(panel, "Title", "CAUGHT", 76f, Palette.Danger, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(700f, 86f),
                TextAlignmentOptions.Center, true);
            UIFactory.Text(panel, "Sub", "THE SKY HUNTER HAS YOU", 22f, Palette.Muted,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(700f, 34f),
                TextAlignmentOptions.Center, true);
            UIFactory.Text(panel, "Desc", "Spend coins to break free and keep this run alive.",
                19f, Palette.Text, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f),
                new Vector2(700f, 50f), TextAlignmentOptions.Center);

            reviveStats = UIFactory.Text(panel, "Stats", "SCORE 0   •   0 M", 22f, Palette.Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 92f), new Vector2(700f, 40f),
                TextAlignmentOptions.Center, true);

            reviveButton = UIFactory.Button(panel, "Revive", "REVIVE  •  150", Palette.Success, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(520f, 84f),
                game.Revive, "coin", 30f);
            giveUpButton = UIFactory.Button(panel, "GiveUp", "GIVE UP", new Color(0.12f, 0.1f, 0.16f, 1f), Palette.Muted,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(360f, 58f),
                game.DeclineRevive, null, 22f);

            reviveCoins = UIFactory.Text(panel, "Coins", "0 COINS", 18f, Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(400f, 30f),
                TextAlignmentOptions.Center, true);
        }

        // --------------------------------------------------------------- RESULTS

        private void BuildResults()
        {
            RectTransform panel;
            Chain chain = Overlay("ResultsScreen", new Vector2(980f, 820f), new Color(0.05f, 0.1f, 0.17f, 0.99f), out panel);
            resultsScreen = chain.Screen;

            resultsTitle = UIFactory.Text(panel, "Title", "RUN COMPLETE", 52f, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(850f, 66f),
                TextAlignmentOptions.Center, true);
            resultsSubtitle = UIFactory.Text(panel, "Sub", "SKYBOUND CITY", 19f, Palette.Primary,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(800f, 32f),
                TextAlignmentOptions.Center, true);

            resultsStarsRoot = UIFactory.Rect(panel, "Stars", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(400f, 84f));
            for (int i = 0; i < 3; i++)
            {
                resultStars[i] = UIFactory.Icon(resultsStarsRoot, "star", new Vector2(0.5f, 0.5f),
                    new Vector2(-124f + i * 124f, 0f), 80f, new Color(0.2f, 0.25f, 0.33f, 1f));
            }

            resultsScore = MakeResultRow(panel, "SCORE", 0f, out resultsScore);
            resultsDistance = MakeResultRow(panel, "DISTANCE", -62f, out resultsDistance);
            resultsCoins = MakeResultRow(panel, "COINS", -124f, out resultsCoins);
            resultsCombo = MakeResultRow(panel, "BEST COMBO", -186f, out resultsCombo);
            resultsNear = MakeResultRow(panel, "NEAR MISSES", -248f, out resultsNear);
            resultsTime = MakeResultRow(panel, "TIME", -310f, out resultsTime);

            resultsBest = UIFactory.Text(panel, "BestBadge", "", 18f, Palette.Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -292f), new Vector2(760f, 30f),
                TextAlignmentOptions.Center, true);

            resultsNext = UIFactory.Button(panel, "Next", "NEXT LEVEL", Palette.Primary,
                new Color(0.01f, 0.08f, 0.12f), new Vector2(0.5f, 0f), new Vector2(220f, 100f),
                new Vector2(300f, 76f), NextLevel, "chevron", 24f);
            UIFactory.Button(panel, "Retry", "RETRY", new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text,
                new Vector2(0.5f, 0f), new Vector2(-30f, 46f), new Vector2(240f, 66f),
                game.RestartRun, "retry", 22f);
            UIFactory.Button(panel, "Menu", "MENU", new Color(0.08f, 0.12f, 0.2f, 1f), Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(210f, 46f), new Vector2(200f, 66f),
                game.QuitToMenu, "home", 22f);
            resultsNext.gameObject.SetActive(false);
        }

        private TextMeshProUGUI MakeResultRow(RectTransform panel, string label, float y, out TextMeshProUGUI value)
        {
            UIFactory.Text(panel, label + "Label", label, 20f, Palette.Muted, new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(110f, y + 78f), new Vector2(360f, 30f),
                TextAlignmentOptions.Left, true);
            value = UIFactory.Text(panel, label + "Value", "0", 30f, Color.white, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-110f, y + 78f), new Vector2(360f, 40f),
                TextAlignmentOptions.Right, true);
            RectTransform line = UIFactory.Rect(panel, label + "Line", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y + 56f), new Vector2(760f, 1.5f));
            UIFactory.Image(line, Art.White, new Color(1f, 1f, 1f, 0.07f));
            return value;
        }

        // -------------------------------------------------------------- SETTINGS

        private void BuildSettings()
        {
            RectTransform panel;
            Chain chain = Overlay("SettingsScreen", new Vector2(1120f, 900f), new Color(0.05f, 0.1f, 0.17f, 0.99f), out panel);
            settingsScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "SETTINGS", 52f, Color.white, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 66f),
                TextAlignmentOptions.Center, true);

            SettingsSlider(panel, "MUSIC", 260f, 0.7f, out musicSlider, out musicValue, Palette.Primary);
            SettingsSlider(panel, "SOUND FX", 192f, 0.9f, out sfxSlider, out sfxValue, Palette.Primary);
            SettingsSlider(panel, "VOICE", 124f, 0.8f, out voiceSlider, out voiceValue, Palette.Primary);

            vibToggle = UIFactory.Toggle(panel, "Vibration", "VIBRATION", new Vector2(0.5f, 0.5f),
                new Vector2(-270f, 34f), new Vector2(500f, 62f), true, value =>
                {
                    SaveManager.Instance.Data.Settings.Vibration = value;
                    SaveManager.Instance.MarkDirty();
                });
            shakeToggle = UIFactory.Toggle(panel, "Shake", "SCREEN SHAKE", new Vector2(0.5f, 0.5f),
                new Vector2(270f, 34f), new Vector2(500f, 62f), true, value =>
                {
                    SaveManager.Instance.Data.Settings.ScreenShake = value;
                    SaveManager.Instance.MarkDirty();
                });
            controlsToggle = UIFactory.Toggle(panel, "Controls", "ON-SCREEN CONTROLS", new Vector2(0.5f, 0.5f),
                new Vector2(-270f, -46f), new Vector2(500f, 62f), false, value =>
                {
                    SaveManager.Instance.Data.Settings.OnScreenControls = value;
                    SaveManager.Instance.MarkDirty();
                    if (onScreenControls != null)
                        onScreenControls.gameObject.SetActive(value && game.State == GameState.Playing);
                });

            UIFactory.Text(panel, "QualityLabel", "QUALITY", 20f, Palette.Muted, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(160f, 40f),
                TextAlignmentOptions.Left, true);
            string[] qNames = { "LOW", "MED", "HIGH", "ULTRA" };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                qualityButtons[i] = UIFactory.Button(panel, "Quality" + i, qNames[i],
                    new Color(0.1f, 0.17f, 0.28f, 1f), Palette.Text, new Vector2(0.5f, 0.5f),
                    new Vector2(-252f + i * 168f, -176f), new Vector2(156f, 56f),
                    () => SetQuality(index), null, 19f);
            }

            UIFactory.Text(panel, "FpsLabel", "FRAME RATE", 20f, Palette.Muted, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -238f), new Vector2(200f, 40f),
                TextAlignmentOptions.Left, true);
            fps30Button = UIFactory.Button(panel, "Fps30", "30 FPS", new Color(0.1f, 0.17f, 0.28f, 1f),
                Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(-240f, -292f), new Vector2(160f, 54f),
                () => SetFps(30), null, 19f);
            fps60Button = UIFactory.Button(panel, "Fps60", "60 FPS", new Color(0.1f, 0.17f, 0.28f, 1f),
                Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(-60f, -292f), new Vector2(160f, 54f),
                () => SetFps(60), null, 19f);

            Button reset = UIFactory.Button(panel, "Reset", "RESET PROGRESS", new Color(0.18f, 0.07f, 0.09f, 1f),
                Palette.Danger, new Vector2(0.5f, 0.5f), new Vector2(300f, -238f), new Vector2(340f, 66f),
                ArmReset, null, 20f);
            resetLabel = reset.GetComponentInChildren<TextMeshProUGUI>();

            UIFactory.Button(panel, "Close", "CLOSE", Palette.Secondary, Color.white,
                new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(420f, 70f),
                () => ShowSettings(false), "close", 24f);

            LoadSettings();
        }

        private void SettingsSlider(RectTransform panel, string label, float y, float value, out Slider slider,
            out TextMeshProUGUI valueText, Color accent)
        {
            UIFactory.Text(panel, label + "Label", label, 19f, Palette.Muted, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-470f, y + 62f), new Vector2(220f, 34f),
                TextAlignmentOptions.Left, true);
            Slider s = UIFactory.Slider(panel, label + "Slider", new Vector2(0.5f, 0.5f),
                new Vector2(-80f, y + 62f), new Vector2(640f, 22f), value, accent, null);
            slider = s;
            valueText = UIFactory.Text(panel, label + "Value", Mathf.RoundToInt(value * 100f) + "%", 20f,
                Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(370f, y + 62f),
                new Vector2(120f, 34f), TextAlignmentOptions.Right, true);
        }

        // ------------------------------------------------------------------ SHOP

        private void BuildShop()
        {
            RectTransform panel;
            Chain chain = Overlay("ShopScreen", new Vector2(1760f, 940f), new Color(0.04f, 0.08f, 0.14f, 0.99f), out panel);
            shopScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "SKY SHOP", 60f, Color.white, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(56f, -34f), new Vector2(500f, 72f),
                TextAlignmentOptions.Left, true);
            UIFactory.Text(panel, "Sub", "RUNNERS, SKINS AND STYLE FOR THE SKYWAY", 18f, Palette.Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -102f), new Vector2(700f, 30f),
                TextAlignmentOptions.Left, true);

            RectTransform coinPill = UIFactory.Rect(panel, "CoinPill", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-250f, -34f), new Vector2(230f, 58f));
            UIFactory.Image(coinPill, Art.Pill, new Color(0.08f, 0.13f, 0.2f, 1f), true);
            UIFactory.Icon(coinPill, "coin", new Vector2(0f, 0.5f), new Vector2(32f, 0f), 30f, Color.white);
            shopCoins = UIFactory.Text(coinPill, "Value", "0", 24f, Palette.Accent, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(160f, 40f),
                TextAlignmentOptions.Right, true);

            Button runnersTab = UIFactory.Button(panel, "RunnersTab", "RUNNERS", Palette.Primary,
                new Color(0.01f, 0.08f, 0.12f), new Vector2(0.5f, 1f), new Vector2(-120f, -52f),
                new Vector2(220f, 58f), () => SwitchShopPage(0), null, 22f);
            Button skinsTab = UIFactory.Button(panel, "SkinsTab", "SKINS", new Color(0.1f, 0.17f, 0.28f, 1f),
                Palette.Text, new Vector2(0.5f, 1f), new Vector2(120f, -52f), new Vector2(220f, 58f),
                () => SwitchShopPage(1), null, 22f);
            shopTabRunners = runnersTab.GetComponentInChildren<TextMeshProUGUI>();
            shopTabSkins = skinsTab.GetComponentInChildren<TextMeshProUGUI>();

            shopContent = UIFactory.Rect(panel, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1660f, 720f));

            UIFactory.Button(panel, "Back", "BACK", new Color(0.08f, 0.12f, 0.2f, 1f), Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 58f),
                () => ShowShop(false), "arrow_left", 22f);
        }

        private void BuildLevels()
        {
            RectTransform panel;
            Chain chain = Overlay("LevelsScreen", new Vector2(1760f, 940f), new Color(0.04f, 0.08f, 0.14f, 0.99f), out panel);
            levelsScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "SELECT SKYWAY", 56f, Color.white, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(56f, -34f), new Vector2(700f, 70f),
                TextAlignmentOptions.Left, true);
            UIFactory.Text(panel, "Sub", "EIGHT DISTRICTS. ONE PERFECT LINE.", 18f, Palette.Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -100f), new Vector2(700f, 30f),
                TextAlignmentOptions.Left, true);

            levelsContent = UIFactory.Rect(panel, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1660f, 700f));

            UIFactory.Button(panel, "Back", "BACK", new Color(0.08f, 0.12f, 0.2f, 1f), Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 58f),
                () => ShowLevelSelect(false), "arrow_left", 22f);
        }

        private void BuildMissions()
        {
            RectTransform panel;
            Chain chain = Overlay("MissionsScreen", new Vector2(1240f, 860f), new Color(0.05f, 0.1f, 0.17f, 0.99f), out panel);
            missionsScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "DAILY MISSIONS", 52f, Color.white, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(52f, -34f), new Vector2(700f, 66f),
                TextAlignmentOptions.Left, true);
            UIFactory.Text(panel, "Sub", "FRESH OBJECTIVES EVERY DAY. CLAIM THEM BEFORE THE RESET.",
                17f, Palette.Muted, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -98f),
                new Vector2(1000f, 30f), TextAlignmentOptions.Left, true);

            missionsContent = UIFactory.Rect(panel, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(1120f, 620f));

            UIFactory.Button(panel, "Back", "BACK", new Color(0.08f, 0.12f, 0.2f, 1f), Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 58f),
                () => ShowMissions(false), "arrow_left", 22f);
        }

        private void BuildAchievements()
        {
            RectTransform panel;
            Chain chain = Overlay("AchievementsScreen", new Vector2(1760f, 940f), new Color(0.04f, 0.08f, 0.14f, 0.99f), out panel);
            achievementsScreen = chain.Screen;

            UIFactory.Text(panel, "Title", "AWARDS", 56f, Color.white, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(56f, -34f), new Vector2(500f, 70f),
                TextAlignmentOptions.Left, true);
            achievementCount = UIFactory.Text(panel, "Count", "0 / 12 UNLOCKED", 20f, Palette.Accent,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -44f), new Vector2(420f, 34f),
                TextAlignmentOptions.Right, true);

            achievementsContent = UIFactory.Rect(panel, "Content", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(1660f, 720f));

            UIFactory.Button(panel, "Back", "BACK", new Color(0.08f, 0.12f, 0.2f, 1f), Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 58f),
                () => ShowAchievements(false), "arrow_left", 22f);
        }

        // ---------------------------------------------------------------- TUTORIAL

        private void BuildTutorial()
        {
            tutorialScreen = NewScreen("TutorialScreen");
            RectTransform panel = UIFactory.Rect(tutorialScreen, "Panel", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 148f), new Vector2(900f, 110f));
            Image panelBackground = UIFactory.Image(panel, Art.Panel,
                new Color(0.03f, 0.07f, 0.12f, 0.9f), true);
            panelBackground.raycastTarget = true;
            RectTransform outline = UIFactory.Stretch(panel, "Outline");
            UIFactory.Image(outline, Art.PanelOutline, new Color(0f, 0.9f, 1f, 0.35f), true);
            UIFactory.Icon(panel, "arrow_right", new Vector2(0f, 0.5f), new Vector2(46f, 0f), 34f, Palette.Primary);
            tutorialText = UIFactory.Text(panel, "Text", TutorialSteps[0], 23f, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 8f), new Vector2(700f, 46f),
                TextAlignmentOptions.Left, true);
            UIFactory.Text(panel, "Hint", "TAP TO SKIP", 14f, Palette.Muted, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(88f, 12f), new Vector2(200f, 20f),
                TextAlignmentOptions.Left, true);
            Button skip = panel.gameObject.AddComponent<Button>();
            skip.targetGraphic = panelBackground;
            skip.onClick.AddListener(() => game.Player.Input.PerformJump());
        }

        private void BuildToast()
        {
            RectTransform rt = UIFactory.Rect(safeRoot, "Toast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 58f));
            toastGroup = rt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastText = UIFactory.Text(rt, "Text", "", 25f, Palette.Text, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(750f, 54f),
                TextAlignmentOptions.Center, true);
        }

        private void BuildFlash()
        {
            RectTransform rt = UIFactory.Stretch(safeRoot, "HitFlash");
            flashImage = UIFactory.Image(rt, Art.White, new Color(1f, 0.1f, 0.15f, 0f));
            flashImage.raycastTarget = false;
        }

        // ------------------------------------------------------------- RESULT API

        public void ShowMenu()
        {
            HideAll();
            menuScreen.gameObject.SetActive(true);
            RefreshMenu();
        }

        /// <summary>Hides the main menu so it cannot cover gameplay or swallow taps.</summary>
        public void HideMenu()
        {
            if (menuScreen != null) menuScreen.gameObject.SetActive(false);
        }

        // ------------------------------------------------------- SCREEN STATE API

        public bool MenuVisible { get { return menuScreen != null && menuScreen.gameObject.activeSelf; } }
        public bool HudVisible { get { return hudScreen != null && hudScreen.gameObject.activeSelf; } }
        public bool ReviveVisible { get { return reviveScreen != null && reviveScreen.gameObject.activeSelf; } }
        public bool ResultsVisible { get { return resultsScreen != null && resultsScreen.gameObject.activeSelf; } }

        /// <summary>True when a real pointer raycast reaches the revive button.</summary>
        public bool ReviveButtonReceivesClicks()
        {
            return IsTopmostAt(reviveButton);
        }

        /// <summary>True when a real pointer raycast reaches the give-up button.</summary>
        public bool GiveUpButtonReceivesClicks()
        {
            return IsTopmostAt(giveUpButton);
        }

        public void ClickRevive()
        {
            if (reviveButton != null && reviveButton.interactable) reviveButton.onClick.Invoke();
        }

        public void ClickGiveUp()
        {
            if (giveUpButton != null && giveUpButton.interactable) giveUpButton.onClick.Invoke();
        }

        private static bool IsTopmostAt(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return false;
            if (EventSystem.current == null) return false;

            RectTransform rt = (RectTransform)button.transform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rt.position);

            // In -nographics batch mode Unity never builds the canvas, so every
            // Graphic has depth -1 and GraphicRaycaster skips it. Fall back to a
            // structural occlusion test that mirrors what the raycaster would do.
            if (!RaycastReady(button))
                return !HasOccluder(button, screenPoint);

            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);

            if (hits.Count == 0)
            {
                Debug.LogWarning("[UI] Raycast hit nothing at " + screenPoint + " for " + button.name +
                                 "; using structural test.");
                return !HasOccluder(button, screenPoint);
            }

            Transform hit = hits[0].gameObject.transform;
            while (hit != null)
            {
                if (hit == button.transform) return true;
                hit = hit.parent;
            }

            Debug.LogWarning("[UI] Top raycast at " + screenPoint + " is '" + hits[0].gameObject.name +
                             "', not " + button.name);
            return false;
        }

        /// <summary>True when the canvas has been processed and real raycasts work.</summary>
        private static bool RaycastReady(Button button)
        {
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            IList<Graphic> list = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].depth >= 0) return true;
            return false;
        }

        /// <summary>
        /// Walks the canvas hierarchy in draw order and reports any raycast-blocking
        /// graphic drawn after the button whose rect covers the button centre.
        /// </summary>
        private static bool HasOccluder(Button button, Vector2 screenPoint)
        {
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null) return true;

            List<Transform> order = new List<Transform>();
            CollectDrawOrder(canvas.transform, order);
            int buttonIndex = order.IndexOf(button.transform);

            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(false);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || !graphic.raycastTarget || !graphic.isActiveAndEnabled) continue;

                Transform t = graphic.transform;
                if (t == button.transform || t.IsChildOf(button.transform)) continue;
                if (button.transform.IsChildOf(t)) continue;

                int index = order.IndexOf(t);
                if (index <= buttonIndex) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPoint, null))
                {
                    Debug.LogWarning("[UI] '" + graphic.gameObject.name + "' is drawn over " +
                                     button.name + " and would swallow the tap.");
                    return true;
                }
            }
            return false;
        }

        private static void CollectDrawOrder(Transform parent, List<Transform> order)
        {
            order.Add(parent);
            for (int i = 0; i < parent.childCount; i++)
                CollectDrawOrder(parent.GetChild(i), order);
        }

        public void ShowHUD(bool show)
        {
            if (show) HideMenu();
            else HideTutorial();
            hudScreen.gameObject.SetActive(show);
            if (show) RefreshHUDStatic();
            if (!show)
            {
                onScreenControls.gameObject.SetActive(false);
                hudComboRoot.gameObject.SetActive(false);
            }
        }

        public void ShowPause(bool show)
        {
            if (show)
            {
                HideMenu();
                HideTutorial();
            }
            pauseScreen.gameObject.SetActive(show);
        }

        public void ShowReviveOffer()
        {
            HideMenu();
            HideTutorial();
            reviveScreen.gameObject.SetActive(true);
            RefreshRevive();
        }

        public void ShowRunResults(RunSummary summary, bool levelComplete)
        {
            HideMenu();
            HideTutorial();
            reviveScreen.gameObject.SetActive(false);
            resultsScreen.gameObject.SetActive(true);

            resultsTitle.text = levelComplete ? "SKYWAY CLEARED" : "RUN OVER";
            resultsTitle.color = levelComplete ? Palette.Accent : Color.white;
            if (levelComplete && summary.LevelIndex >= 0)
            {
                LevelDef def = GameConfig.GetLevel(summary.LevelIndex);
                resultsSubtitle.text = def.Name + "  •  " + def.Subtitle;
            }
            else
            {
                resultsSubtitle.text = "KEEP PUSHING THE SKYLINE";
            }

            resultsStarsRoot.gameObject.SetActive(levelComplete);
            for (int i = 0; i < 3; i++)
                resultStars[i].color = i < summary.Stars ? Palette.Accent : new Color(0.2f, 0.25f, 0.33f, 1f);

            resultsScore.text = summary.Score.ToString("N0");
            resultsDistance.text = Mathf.FloorToInt(summary.Distance).ToString("N0") + " M";
            resultsCoins.text = summary.CoinCount.ToString("N0");
            resultsCombo.text = "x" + summary.BestCombo;
            resultsNear.text = summary.NearMisses.ToString("N0");
            resultsTime.text = FormatTime(summary.Time);

            string badge = "";
            if (summary.NewBestScore) badge += "NEW BEST SCORE   ";
            if (summary.NewBestDistance) badge += "FARTHEST RUN   ";
            if (summary.LevelCompleted) badge += "STARS " + summary.Stars + " / 3";
            resultsBest.text = badge;

            bool canNext = levelComplete && summary.LevelIndex + 1 < GameConfig.Levels.Length &&
                           game.Levels.IsUnlocked(summary.LevelIndex + 1);
            resultsNext.gameObject.SetActive(canNext);
            if (canNext)
            {
                resultsNext.onClick.RemoveAllListeners();
                int nextIndex = summary.LevelIndex + 1;
                resultsNext.onClick.AddListener(() => game.StartLevelRun(nextIndex));
            }
            LayoutResultButtons(canNext);
        }

        private void LayoutResultButtons(bool canNext)
        {
            RectTransform retry = (RectTransform)resultsScreen.Find("Panel/Retry");
            RectTransform menu = (RectTransform)resultsScreen.Find("Panel/Menu");
            RectTransform next = (RectTransform)resultsNext.transform;
            if (canNext)
            {
                retry.anchoredPosition = new Vector2(-30f, 108f);
                menu.anchoredPosition = new Vector2(210f, 108f);
                next.anchoredPosition = new Vector2(-30f, 28f);
            }
            else
            {
                retry.anchoredPosition = new Vector2(-130f, 48f);
                menu.anchoredPosition = new Vector2(130f, 48f);
            }
        }

        public void CloseOverlays()
        {
            pauseScreen.gameObject.SetActive(false);
            reviveScreen.gameObject.SetActive(false);
            resultsScreen.gameObject.SetActive(false);
            settingsScreen.gameObject.SetActive(false);
            shopScreen.gameObject.SetActive(false);
            levelsScreen.gameObject.SetActive(false);
            missionsScreen.gameObject.SetActive(false);
            achievementsScreen.gameObject.SetActive(false);
        }

        public void ShowSettings(bool show)
        {
            settingsScreen.gameObject.SetActive(show);
            if (show) LoadSettings();
        }

        public void ShowShop(bool show)
        {
            if (show) { RefreshShop(); shopPage = 0; }
            shopScreen.gameObject.SetActive(show);
        }

        public void ShowLevelSelect(bool show)
        {
            if (show) RefreshLevels();
            levelsScreen.gameObject.SetActive(show);
        }

        public void ShowMissions(bool show)
        {
            if (show) RefreshMissions();
            missionsScreen.gameObject.SetActive(show);
        }

        public void ShowAchievements(bool show)
        {
            if (show) RefreshAchievements();
            achievementsScreen.gameObject.SetActive(show);
        }

        public void Toast(string message, Color color)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.color = color;
            toastTimer = 2.1f;
            toastGroup.alpha = 1f;
            RectTransform rt = (RectTransform)toastGroup.transform;
            rt.anchoredPosition = new Vector2(0f, -128f);
        }

        private void OnHit(int hits)
        {
            flashAmount = 0.5f;
        }

        private void HideAll()
        {
            menuScreen.gameObject.SetActive(false);
            hudScreen.gameObject.SetActive(false);
            CloseOverlays();
            HideTutorial();
        }

        // ------------------------------------------------------------------ HUD

        private void RefreshHUDStatic()
        {
            if (SaveManager.Instance == null) return;
            hudBest.text = "BEST " + SaveManager.Instance.Data.BestScore.ToString("N0");
            hudLevelRoot.gameObject.SetActive(!game.IsEndless);
            hudSpeedRoot.gameObject.SetActive(game.IsEndless);
            if (!game.IsEndless && game.ActiveLevel != null)
            {
                hudLevelBar.Title.text = game.ActiveLevel.Name;
            }
        }

        private void Update()
        {
            if (!IsBuilt || menuScreen == null || hudScreen == null) return;
            float dt = Time.unscaledDeltaTime;

            if (hudScreen.gameObject.activeSelf) UpdateHUD(dt);
            if (menuScreen.gameObject.activeSelf) UpdateMenuLive();
            if (reviveScreen.gameObject.activeSelf) RefreshRevive();
            if (settingsScreen.gameObject.activeSelf) UpdateSettingsLive();

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.4f);
                RectTransform rt = (RectTransform)toastGroup.transform;
                Vector2 p = rt.anchoredPosition;
                p.y = Mathf.Lerp(p.y, -150f, 1f - Mathf.Exp(-8f * dt));
                rt.anchoredPosition = p;
            }
            else if (toastGroup.alpha > 0f)
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, dt * 4f);
            }

            if (flashAmount > 0f)
            {
                flashAmount = Mathf.MoveTowards(flashAmount, 0f, dt * 1.8f);
                flashImage.color = new Color(1f, 0.12f, 0.18f, flashAmount * 0.55f);
            }

            UpdateTutorial(dt);
        }

        private void UpdateHUD(float dt)
        {
            hudScore.text = game.Score.Score.ToString("N0");
            hudDistance.text = Mathf.FloorToInt(game.Distance).ToString("N0") + " M";
            hudCoins.text = game.WalletCoins.ToString("N0");

            int combo = game.Combo.Count;
            if (combo > 1)
            {
                hudComboRoot.gameObject.SetActive(true);
                hudCombo.text = "COMBO x" + combo + "   " + game.Combo.Multiplier.ToString("0.0") + "x";
            }
            else
            {
                hudComboRoot.gameObject.SetActive(false);
            }

            int remaining = Mathf.Clamp(game.MaxHits - game.HitsTaken, 0, 3);
            for (int i = 0; i < 3; i++)
                hearts[i].color = i < remaining ? Palette.Danger : new Color(0.18f, 0.22f, 0.3f, 0.75f);

            if (!game.IsEndless && game.ActiveLevel != null)
            {
                float progress = game.LevelProgress01;
                hudLevelBar.Set(progress, Mathf.RoundToInt(progress * 100f) + "%");
            }
            else
            {
                hudSpeedBar.Set(game.Speed01, Mathf.RoundToInt(game.Speed01 * 100f) + "%");
            }

            for (int i = 0; i < powerRows.Length; i++)
            {
                PowerRowUI row = powerRows[i];
                bool active = game.PowerUps.IsActive(row.Type);
                if (active != row.Root.gameObject.activeSelf)
                    row.Root.gameObject.SetActive(active);
                if (!active) continue;
                float remainingTime = game.PowerUps.Remaining(row.Type);
                float duration = game.PowerUps.DisplayDuration(row.Type);
                row.Bar.Set(duration > 0f ? remainingTime / duration : 0f);
                row.Time.text = remainingTime.ToString("0.0");
            }

            bool showControls = SaveManager.Instance.Data.Settings.OnScreenControls &&
                                game.State == GameState.Playing;
            if (onScreenControls.gameObject.activeSelf != showControls)
                onScreenControls.gameObject.SetActive(showControls);
        }

        private void UpdateMenuLive()
        {
            if (SaveManager.Instance == null) return;
            menuCoins.text = SaveManager.Instance.Data.Coins.ToString("N0");
            menuLevel.text = "LEVEL " + SaveManager.Instance.Data.PlayerLevel;
            menuBest.text = "BEST  " + SaveManager.Instance.Data.BestScore.ToString("N0");
        }

        private void RefreshMenu()
        {
            if (SaveManager.Instance == null) return;
            SaveData data = SaveManager.Instance.Data;
            menuCoins.text = data.Coins.ToString("N0");
            menuLevel.text = "LEVEL " + data.PlayerLevel;
            menuBest.text = "BEST  " + data.BestScore.ToString("N0");

            CharacterDef character = game.SelectedCharacter;
            SkinDef skin = game.SelectedSkin;
            menuName.text = character.Name;
            menuTitle.text = character.Title;
            menuAvatar.color = skin.Primary;
            menuStatBars[0].Set(Mathf.InverseLerp(0.85f, 1.2f, character.SpeedStat));
            menuStatBars[1].Set(Mathf.InverseLerp(0.85f, 1.2f, character.JumpStat));
            menuStatBars[2].Set(Mathf.InverseLerp(0.85f, 1.2f, character.RecoveryStat));
            menuStatBars[0].Fill.color = Palette.Primary;
            menuStatBars[1].Fill.color = Palette.Accent;
            menuStatBars[2].Fill.color = Palette.Success;
            menuTip.text = GameConfig.Tips[UnityEngine.Random.Range(0, GameConfig.Tips.Length)];
        }

        private void RefreshRevive()
        {
            if (SaveManager.Instance == null) return;
            reviveCoins.text = SaveManager.Instance.Data.Coins.ToString("N0") + " COINS AVAILABLE";
            reviveStats.text = "SCORE " + game.Score.Score.ToString("N0") + "   •   " +
                               Mathf.FloorToInt(game.Distance).ToString("N0") + " M";
            bool can = game.CanRevive;
            reviveButton.interactable = can;
            TextMeshProUGUI label = reviveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (game.RevivedThisRun) label.text = "NO REVIVES LEFT";
                else if (!can) label.text = "NEED " + game.ReviveCost + " COINS";
                else label.text = "REVIVE  •  " + game.ReviveCost;
            }
        }

        // --------------------------------------------------------------- SETTINGS

        private void LoadSettings()
        {
            if (SaveManager.Instance == null) return;
            SettingsData s = SaveManager.Instance.Data.Settings;
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(s.MusicVolume);
                musicValue.text = Mathf.RoundToInt(s.MusicVolume * 100f) + "%";
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(s.SfxVolume);
                sfxValue.text = Mathf.RoundToInt(s.SfxVolume * 100f) + "%";
            }
            if (voiceSlider != null)
            {
                voiceSlider.SetValueWithoutNotify(s.VoiceVolume);
                voiceValue.text = Mathf.RoundToInt(s.VoiceVolume * 100f) + "%";
            }
            vibToggle.SetIsOnWithoutNotify(s.Vibration);
            shakeToggle.SetIsOnWithoutNotify(s.ScreenShake);
            controlsToggle.SetIsOnWithoutNotify(s.OnScreenControls);

            musicSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.RemoveAllListeners();
            voiceSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(value =>
            {
                s.MusicVolume = value;
                musicValue.text = Mathf.RoundToInt(value * 100f) + "%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
                if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
            });
            sfxSlider.onValueChanged.AddListener(value =>
            {
                s.SfxVolume = value;
                sfxValue.text = Mathf.RoundToInt(value * 100f) + "%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
                if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
            });
            voiceSlider.onValueChanged.AddListener(value =>
            {
                s.VoiceVolume = value;
                voiceValue.text = Mathf.RoundToInt(value * 100f) + "%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetVoiceVolume(value);
                if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
            });

            UpdateSettingsLive();
        }

        private void UpdateSettingsLive()
        {
            if (SaveManager.Instance == null) return;
            SettingsData s = SaveManager.Instance.Data.Settings;
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                Image img = qualityButtons[i].targetGraphic as Image;
                if (img != null)
                    img.color = i == s.QualityLevel ? Palette.Primary :
                        new Color(0.1f, 0.17f, 0.28f, 1f);
                TextMeshProUGUI label = qualityButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = i == s.QualityLevel ? new Color(0.01f, 0.08f, 0.12f) : Palette.Text;
            }
            SetButtonAccent(fps30Button, s.FpsTarget <= 30);
            SetButtonAccent(fps60Button, s.FpsTarget > 30);

            if (resetArmedTime > 0f && Time.unscaledTime - resetArmedTime > 3f)
            {
                resetArmedTime = -10f;
                if (resetLabel != null) resetLabel.text = "RESET PROGRESS";
            }
        }

        private static void SetButtonAccent(Button button, bool active)
        {
            if (button == null) return;
            Image img = button.targetGraphic as Image;
            if (img != null)
                img.color = active ? Palette.Accent : new Color(0.1f, 0.17f, 0.28f, 1f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.color = active ? new Color(0.08f, 0.05f, 0.0f) : Palette.Text;
        }

        private void SetQuality(int level)
        {
            if (game.Quality != null) game.Quality.SetLevel(level);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            UpdateSettingsLive();
        }

        private void SetFps(int fps)
        {
            SaveManager.Instance.Data.Settings.FpsTarget = fps;
            SaveManager.Instance.MarkDirty();
            if (game.Quality != null) game.Quality.Apply();
            UpdateSettingsLive();
        }

        private void ArmReset()
        {
            if (resetArmedTime > 0f && Time.unscaledTime - resetArmedTime <= 3f)
            {
                SaveManager.Instance.ResetProgress();
                resetArmedTime = -10f;
                if (resetLabel != null) resetLabel.text = "RESET PROGRESS";
                game.EnterMenu();
                Toast("PROGRESS RESET", Palette.Danger);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                return;
            }
            resetArmedTime = Time.unscaledTime;
            if (resetLabel != null) resetLabel.text = "TAP AGAIN TO CONFIRM";
        }

        // ------------------------------------------------------------------ SHOP

        private void SwitchShopPage(int page)
        {
            shopPage = page;
            RefreshShop();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        }

        private void RefreshShop()
        {
            if (SaveManager.Instance == null) return;
            shopCoins.text = SaveManager.Instance.Data.Coins.ToString("N0");
            shopTabRunners.color = shopPage == 0 ? new Color(0.01f, 0.08f, 0.12f) : Palette.Text;
            shopTabSkins.color = shopPage == 1 ? new Color(0.01f, 0.08f, 0.12f) : Palette.Text;
            UIFactory.Clear(shopContent);

            if (shopPage == 0) BuildRunnerCards();
            else BuildSkinCards();
        }

        private void BuildRunnerCards()
        {
            for (int i = 0; i < GameConfig.Characters.Length; i++)
            {
                int index = i;
                CharacterDef character = GameConfig.Characters[i];
                bool unlocked = SaveManager.Instance.IsCharacterUnlocked(i);
                bool selected = SaveManager.Instance.Data.SelectedCharacter == i;
                float x = -600f + i * 400f;
                RectTransform card = Card(shopContent, "Runner" + i, new Vector2(x, 20f), new Vector2(370f, 620f),
                    new Color(0.07f, 0.12f, 0.19f, 0.98f), () => OnRunnerCard(index));
                if (selected) AddCardOutline(card, Palette.Primary);

                Image portrait = UIFactory.Sprite(card, Art.Circle(), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(190f, 190f), character.Primary);
                portrait.transform.SetAsFirstSibling();
                UIFactory.Icon(card, "boost", new Vector2(0.5f, 1f), new Vector2(0f, -70f), 96f,
                    new Color(0.02f, 0.06f, 0.1f, 0.9f));

                UIFactory.Text(card, "Name", character.Name, 36f, Color.white, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(340f, 46f),
                    TextAlignmentOptions.Center, true);
                UIFactory.Text(card, "Title", character.Title, 15f, character.Accent, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -282f), new Vector2(340f, 26f),
                    TextAlignmentOptions.Center, true);
                UIFactory.Text(card, "Desc", character.Description, 16f, Palette.Muted,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f),
                    new Vector2(324f, 70f), TextAlignmentOptions.Center);

                StatBar(card, "SPEED", -420f, character.SpeedStat, Palette.Primary);
                StatBar(card, "JUMP", -450f, character.JumpStat, Palette.Accent);
                StatBar(card, "RECOVERY", -480f, character.RecoveryStat, Palette.Success);

                string status;
                Color color;
                if (selected) { status = "SELECTED"; color = Palette.Success; }
                else if (unlocked) { status = "SELECT"; color = Palette.Primary; }
                else if (SaveManager.Instance.Data.PlayerLevel < character.RequiredPlayerLevel)
                {
                    status = "LEVEL " + character.RequiredPlayerLevel;
                    color = Palette.Muted;
                }
                else { status = character.Price.ToString("N0") + " COINS"; color = Palette.Accent; }

                UIFactory.Button(card, "Action", status, color,
                    color == Palette.Accent ? new Color(0.1f, 0.06f, 0.0f) : new Color(0.02f, 0.07f, 0.1f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(310f, 64f),
                    () => OnRunnerCard(index), null, 22f);
            }
        }

        private void BuildSkinCards()
        {
            for (int i = 0; i < GameConfig.Skins.Length; i++)
            {
                int index = i;
                SkinDef skin = GameConfig.Skins[i];
                bool unlocked = SaveManager.Instance.IsSkinUnlocked(i);
                bool selected = SaveManager.Instance.Data.SelectedSkin == i;
                float x = -600f + (i % 4) * 400f;
                float y = i < 4 ? 190f : -120f;
                RectTransform card = Card(shopContent, "Skin" + i, new Vector2(x, y), new Vector2(370f, 280f),
                    new Color(0.07f, 0.12f, 0.19f, 0.98f), () => OnSkinCard(index));
                if (selected) AddCardOutline(card, Palette.Primary);

                UIFactory.Sprite(card, Art.RoundedSprite(96, 96, 20), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(112f, 112f), skin.Primary);
                UIFactory.Sprite(card, Art.Circle(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -20f), new Vector2(62f, 62f), skin.Accent);
                UIFactory.Sprite(card, Art.Circle(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -20f), new Vector2(26f, 26f), skin.Secondary);

                UIFactory.Text(card, "Name", skin.Name, 28f, Color.white, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(330f, 38f),
                    TextAlignmentOptions.Center, true);

                string status;
                Color color;
                if (selected) { status = "SELECTED"; color = Palette.Success; }
                else if (unlocked) { status = "SELECT"; color = Palette.Primary; }
                else { status = skin.Price.ToString("N0") + " COINS"; color = Palette.Accent; }

                UIFactory.Button(card, "Action", status, color,
                    color == Palette.Accent ? new Color(0.1f, 0.06f, 0.0f) : new Color(0.02f, 0.07f, 0.1f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(300f, 54f),
                    () => OnSkinCard(index), null, 19f);
            }
        }

        private void StatBar(RectTransform card, string label, float y, float value, Color color)
        {
            UIFactory.Text(card, label + "L", label, 12f, Palette.Muted, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(90f, 20f),
                TextAlignmentOptions.Left, true);
            UIFactory.Bar(card, label + "B", null, new Vector2(0f, 1f), new Vector2(184f, y - 10f),
                new Vector2(180f, 11f), color).Set(Mathf.InverseLerp(0.85f, 1.2f, value));
        }

        private void OnRunnerCard(int index)
        {
            if (SaveManager.Instance.IsCharacterUnlocked(index)) game.SelectCharacter(index);
            else game.BuyCharacter(index);
            RefreshShop();
        }

        private void OnSkinCard(int index)
        {
            if (SaveManager.Instance.IsSkinUnlocked(index)) game.SelectSkin(index);
            else game.BuySkin(index);
            RefreshShop();
        }

        private RectTransform Card(Transform parent, string name, Vector2 position, Vector2 size, Color color,
            UnityAction onClick)
        {
            RectTransform rt = UIFactory.Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), position, size);
            Image bg = UIFactory.Image(rt, Art.Panel, color, true, true);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            return rt;
        }

        private void AddCardOutline(RectTransform card, Color color)
        {
            RectTransform outline = UIFactory.Stretch(card, "Outline", 3f);
            outline.SetAsFirstSibling();
            UIFactory.Image(outline, Art.PanelOutline, color, true);
        }

        // ----------------------------------------------------------------- LEVELS

        private void RefreshLevels()
        {
            if (SaveManager.Instance == null) return;
            UIFactory.Clear(levelsContent);
            for (int i = 0; i < GameConfig.Levels.Length; i++)
            {
                int index = i;
                LevelDef level = GameConfig.Levels[i];
                bool unlocked = game.Levels.IsUnlocked(i);
                int stars = game.Levels.StarsFor(i);
                float x = -600f + (i % 4) * 400f;
                float y = i < 4 ? 190f : -120f;

                RectTransform card = Card(levelsContent, "Level" + i, new Vector2(x, y), new Vector2(370f, 280f),
                    unlocked ? new Color(0.07f, 0.12f, 0.19f, 0.98f) : new Color(0.05f, 0.07f, 0.11f, 0.96f),
                    () => OnLevelCard(index));
                if (unlocked) AddCardOutline(card, level.Accent);

                UIFactory.Sprite(card, Art.Glow(), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(-40f, -30f), new Vector2(220f, 180f),
                    unlocked ? new Color(level.Accent.r, level.Accent.g, level.Accent.b, 0.18f)
                             : new Color(0f, 0f, 0f, 0.15f));

                UIFactory.Text(card, "Index", (i + 1).ToString("00"), 54f,
                    unlocked ? level.Accent : Palette.Muted, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(26f, -18f), new Vector2(120f, 64f), TextAlignmentOptions.Left, true);
                UIFactory.Text(card, "Name", level.Name, 28f, unlocked ? Color.white : Palette.Muted,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -84f), new Vector2(320f, 38f),
                    TextAlignmentOptions.Left, true);
                UIFactory.Text(card, "Sub", level.Subtitle, 14f, Palette.Muted, new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(26f, -118f), new Vector2(320f, 24f),
                    TextAlignmentOptions.Left, true);
                UIFactory.Text(card, "Distance", Mathf.FloorToInt(level.TargetDistance).ToString("N0") + " M",
                    17f, Palette.Primary, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -150f),
                    new Vector2(200f, 26f), TextAlignmentOptions.Left, true);

                for (int s = 0; s < 3; s++)
                {
                    UIFactory.Icon(card, "star", new Vector2(0f, 0f), new Vector2(36f + s * 40f, 28f), 32f,
                        s < stars ? Palette.Accent : new Color(0.2f, 0.24f, 0.32f, 1f));
                }

                if (!unlocked)
                {
                    UIFactory.Icon(card, "lock", new Vector2(1f, 1f), new Vector2(-46f, -46f), 46f, Palette.Muted);
                    UIFactory.Text(card, "LockText", "COMPLETE PREVIOUS", 13f, Palette.Muted,
                        new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 26f), new Vector2(200f, 22f),
                        TextAlignmentOptions.Right, true);
                }
                else
                {
                    UIFactory.Button(card, "Play", stars > 0 ? "REPLAY" : "RUN", level.Accent,
                        new Color(0.02f, 0.07f, 0.1f), new Vector2(1f, 0f), new Vector2(-96f, 42f),
                        new Vector2(160f, 48f), () => game.StartLevelRun(index), "play", 18f);
                }
            }
        }

        private void OnLevelCard(int index)
        {
            if (!game.Levels.IsUnlocked(index))
            {
                Toast("COMPLETE THE PREVIOUS SKYWAY", Palette.Danger);
                return;
            }
            game.StartLevelRun(index);
        }

        // --------------------------------------------------------------- MISSIONS

        private void RefreshMissions()
        {
            if (SaveManager.Instance == null) return;
            UIFactory.Clear(missionsContent);
            MissionState[] missions = game.Missions.Missions;
            for (int i = 0; i < missions.Length; i++)
            {
                MissionState state = missions[i];
                MissionDef def = game.Missions.GetDef(state.Id);
                if (def == null) continue;
                float y = 220f - i * 200f;
                RectTransform card = UIFactory.Rect(missionsContent, "Mission" + i, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1080f, 178f));
                UIFactory.Image(card, Art.Panel, new Color(0.07f, 0.12f, 0.19f, 0.98f), true);

                bool complete = state.Progress >= def.Target;
                bool claimed = state.Claimed;
                UIFactory.Text(card, "Title", def.Title, 26f, complete ? Palette.Accent : Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -22f), new Vector2(600f, 36f),
                    TextAlignmentOptions.Left, true);
                UIFactory.Text(card, "Desc", def.Description, 17f, Palette.Muted, new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(32f, -62f), new Vector2(700f, 26f),
                    TextAlignmentOptions.Left);
                UIFactory.Text(card, "Reward", "+" + def.RewardCoins + " COINS   •   +" + def.RewardXp + " XP",
                    15f, Palette.Accent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -96f),
                    new Vector2(500f, 24f), TextAlignmentOptions.Left, true);

                UIFactory.ProgressBar bar = UIFactory.Bar(card, "Bar", null, new Vector2(0f, 0f),
                    new Vector2(360f, 44f), new Vector2(600f, 18f), Palette.Primary, true);
                float progress = Mathf.Clamp01(state.Progress / def.Target);
                bar.Set(progress, Mathf.FloorToInt(state.Progress).ToString("N0") + " / " +
                                    Mathf.FloorToInt(def.Target).ToString("N0"));
                ((RectTransform)bar.Root).pivot = new Vector2(0f, 0f);
                ((RectTransform)bar.Root).anchoredPosition = new Vector2(32f, 30f);

                string action = claimed ? "CLAIMED" : (complete ? "CLAIM" : "IN PROGRESS");
                Color color = claimed ? Palette.Muted : (complete ? Palette.Success : new Color(0.1f, 0.17f, 0.28f, 1f));
                Button claim = UIFactory.Button(card, "Claim", action, color,
                    claimed ? Palette.Muted : new Color(0.02f, 0.07f, 0.1f),
                    new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(220f, 62f), null, null, 20f);
                claim.interactable = complete && !claimed;
                string id = state.Id;
                claim.onClick.AddListener(() =>
                {
                    if (game.Missions.Claim(id))
                    {
                        Toast("MISSION REWARD CLAIMED", Palette.Success);
                        RefreshMissions();
                    }
                });
            }
        }

        // ----------------------------------------------------------- ACHIEVEMENTS

        private void RefreshAchievements()
        {
            if (SaveManager.Instance == null) return;
            UIFactory.Clear(achievementsContent);
            int unlocked = 0;
            for (int i = 0; i < GameConfig.Achievements.Length; i++)
            {
                AchievementDef def = GameConfig.Achievements[i];
                bool isUnlocked = game.Achievements.IsUnlocked(def.Id);
                if (isUnlocked) unlocked++;

                float x = -600f + (i % 4) * 400f;
                float y = 200f - (i / 4) * 240f;
                RectTransform card = UIFactory.Rect(achievementsContent, "Ach" + i, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(370f, 210f));
                UIFactory.Image(card, Art.Panel, isUnlocked ? new Color(0.08f, 0.14f, 0.2f, 0.98f)
                    : new Color(0.05f, 0.08f, 0.12f, 0.96f), true);
                if (isUnlocked) AddCardOutline(card, Palette.Accent);

                UIFactory.Icon(card, "trophy", new Vector2(0f, 1f), new Vector2(42f, -42f), 42f,
                    isUnlocked ? Palette.Accent : Palette.Muted);
                UIFactory.Text(card, "Name", def.Name, 21f, isUnlocked ? Color.white : Palette.Muted,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -24f), new Vector2(270f, 30f),
                    TextAlignmentOptions.Left, true);
                UIFactory.Text(card, "Desc", def.Description, 14f, Palette.Muted, new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(20f, -74f), new Vector2(330f, 44f),
                    TextAlignmentOptions.TopLeft);

                float progress = Mathf.Clamp01(game.Achievements.Progress(def.Id) / def.Target);
                UIFactory.ProgressBar bar = UIFactory.Bar(card, "Bar", null, new Vector2(0f, 0f),
                    new Vector2(209f, 34f), new Vector2(330f, 14f), isUnlocked ? Palette.Accent : Palette.Primary);
                bar.Set(progress);
                UIFactory.Text(card, "Reward", isUnlocked ? "UNLOCKED" : "+" + def.RewardCoins + " COINS",
                    14f, isUnlocked ? Palette.Success : Palette.Accent, new Vector2(0f, 0f),
                    new Vector2(0f, 0f), new Vector2(20f, 16f), new Vector2(250f, 22f),
                    TextAlignmentOptions.Left, true);
            }
            achievementCount.text = unlocked + " / " + GameConfig.Achievements.Length + " UNLOCKED";
        }

        // ---------------------------------------------------------------- TUTORIAL

        private void UpdateTutorial(float dt)
        {
            if (!tutorialActive)
            {
                if (game.State == GameState.Playing && SaveManager.Instance != null &&
                    !SaveManager.Instance.Data.TutorialCompleted && game.RunTime < 1.5f &&
                    hudScreen.gameObject.activeSelf)
                {
                    tutorialActive = true;
                    tutorialTimer = 16f;
                    tutorialStep = 0;
                    tutorialScreen.gameObject.SetActive(true);
                    tutorialText.text = TutorialSteps[0];
                }
                return;
            }

            tutorialTimer -= dt;
            tutorialStep = Mathf.FloorToInt((16f - tutorialTimer) / 4f) % TutorialSteps.Length;
            tutorialText.text = TutorialSteps[tutorialStep];

            if (tutorialTimer <= 0f || game.State != GameState.Playing)
                HideTutorial();
        }

        private void HideTutorial()
        {
            if (!tutorialActive) return;
            tutorialActive = false;
            tutorialScreen.gameObject.SetActive(false);
            if (SaveManager.Instance != null && !SaveManager.Instance.Data.TutorialCompleted)
            {
                SaveManager.Instance.Data.TutorialCompleted = true;
                SaveManager.Instance.MarkDirty();
                SaveManager.Instance.Save();
            }
        }

        private static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int mins = s / 60;
            int secs = s % 60;
            return mins.ToString("00") + ":" + secs.ToString("00");
        }

        private void NextLevel()
        {
            RunSummary summary = game.PendingSummary;
            if (summary == null) return;
            int next = summary.LevelIndex + 1;
            if (next < GameConfig.Levels.Length) game.StartLevelRun(next);
        }
    }
}
