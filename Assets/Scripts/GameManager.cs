using System;
using System.Collections;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Central game state machine. Owns the run lifecycle, speed/distance model,
    /// scoring, power-ups, progression hand-off and all UI transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;

        public PlayerController Player { get; private set; }
        public PowerUpManager PowerUps { get; private set; }
        public VFXManager Effects { get; private set; }
        public EndlessWorld World { get; private set; }
        public CameraRig Camera { get; private set; }
        public EnvironmentController Environment { get; private set; }
        public ParallaxCity City { get; private set; }
        public RoadRenderer Road { get; private set; }
        public ComboManager Combo { get; private set; }
        public ScoreManager Score { get; private set; }
        public MissionManager Missions { get; private set; }
        public AchievementManager Achievements { get; private set; }
        public LevelManager Levels { get; private set; }
        public QualityManager Quality { get; private set; }
        public UIManager UI { get; private set; }

        public float WorldSpeed { get; private set; }
        public float Speed01 { get; private set; }
        public float Distance { get; private set; }
        public float RunTime { get; private set; }
        public float TopSpeed { get; private set; }

        public int RunCoins { get; private set; }
        public int RunCoinValue { get; private set; }
        public int GoldCollected { get; private set; }
        public int SilverCollected { get; private set; }
        public int BronzeCollected { get; private set; }
        public int SpecialCollected { get; private set; }
        public int Jumps { get; private set; }
        public int Slides { get; private set; }
        public int PowerUpsUsed { get; private set; }
        public int NearMisses { get; private set; }
        public int HitsTaken { get; private set; }
        public bool RevivedThisRun { get; private set; }
        public bool RunFinalized { get; private set; }

        public int MaxHits { get { return 3; } }
        public bool IsEndless { get { return Levels != null && Levels.IsEndless; } }
        public LevelDef ActiveLevel { get { return Levels != null ? Levels.Current : null; } }
        public float LevelTarget { get { return ActiveLevel != null ? ActiveLevel.TargetDistance : 0f; } }
        public float LevelProgress01
        {
            get
            {
                if (ActiveLevel == null || ActiveLevel.TargetDistance <= 0f) return 0f;
                return Mathf.Clamp01(Distance / ActiveLevel.TargetDistance);
            }
        }

        public int ReviveCost { get { return GameConfig.ReviveCostCoins; } }

        public bool CanRevive
        {
            get
            {
                if (RevivedThisRun) return false;
                if (SaveManager.Instance == null) return false;
                return SaveManager.Instance.Data.Coins >= ReviveCost;
            }
        }

        public CharacterDef SelectedCharacter
        {
            get
            {
                if (SaveManager.Instance == null) return GameConfig.Characters[0];
                return GameConfig.GetCharacter(SaveManager.Instance.Data.SelectedCharacter);
            }
        }

        public SkinDef SelectedSkin
        {
            get
            {
                if (SaveManager.Instance == null) return GameConfig.Skins[0];
                return GameConfig.GetSkin(SaveManager.Instance.Data.SelectedSkin);
            }
        }

        public int WalletCoins
        {
            get { return SaveManager.Instance != null ? SaveManager.Instance.Data.Coins : 0; }
        }

        public event Action<GameState> StateChanged;
        public event Action<RunSummary> RunFinished;
        public event Action<PowerUpType> PowerUpActivated;
        public event Action<AchievementDef> AchievementUnlocked;
        public event Action<int> HitTaken;
        public event Action<string, Color> ToastRequested;
        public event Action<int, int> PlayerLeveledUp;

        private float trailTimer;
        private bool intenseMusic;
        private RunSummary pendingSummary;
        private int lastPlayerLevel = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsBuilt { get; private set; }

        /// <summary>
        /// Creates every system over several frames. A staged build keeps the first
        /// frame short on Android, which prevents the "app is not responding" dialog
        /// and lets the loading overlay animate while the world is generated.
        /// </summary>
        public IEnumerator BuildRoutine()
        {
            if (IsBuilt) yield break;

            lastPlayerLevel = SaveManager.Instance != null ? SaveManager.Instance.Data.PlayerLevel : 1;

            GameBootstrap.SetBootStage("BUILDING CAMERA", 0.30f);
            Camera = CameraRig.Create(transform);
            Camera.SetMode(CameraMode.Menu);
            Camera.SnapToMenu();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING SKYLINE", 0.38f);
            GameObject worldRoot = RuntimeUtil.Create("WorldRoot", transform);
            City = ParallaxCity.Create(worldRoot.transform);
            yield return null;

            GameBootstrap.SetBootStage("BUILDING ROAD", 0.46f);
            Road = RoadRenderer.Create(worldRoot.transform);
            Environment = EnvironmentController.Create(worldRoot.transform, Camera);
            Environment.Wire(City, Road);
            yield return null;

            GameBootstrap.SetBootStage("STARTING SYSTEMS", 0.54f);
            PowerUps = PowerUpManager.Create(transform);
            Effects = VFXManager.Create(transform);
            Score = ScoreManager.Create(transform);
            Combo = ComboManager.Create(transform);
            yield return null;

            GameBootstrap.SetBootStage("LOADING PROGRESS", 0.62f);
            Missions = MissionManager.Create(transform);
            Achievements = AchievementManager.Create(transform);
            Levels = LevelManager.Create(transform);
            Quality = QualityManager.Create(transform);
            Quality.Apply();
            yield return null;

            GameBootstrap.SetBootStage("BUILDING TRACK", 0.70f);
            World = EndlessWorld.Create(transform);
            yield return null;

            GameBootstrap.SetBootStage("BUILDING RUNNER", 0.78f);
            Player = PlayerController.Create(transform, SelectedCharacter, SelectedSkin);
            yield return null;

            GameBootstrap.SetBootStage("BUILDING INTERFACE", 0.84f);
            UI = UIManager.Create(this, transform);
            yield return StartCoroutine(UI.BuildRoutine());
            yield return null;

            PowerUps.Changed += OnPowerUpsChanged;
            Combo.Broken += OnComboBroken;
            Achievements.Unlocked += def =>
            {
                if (AchievementUnlocked != null) AchievementUnlocked(def);
                ShowToast("ACHIEVEMENT: " + def.Name, Palette.Accent);
            };

            World.CoinCollected += OnCoinCollected;
            World.PowerUpCollected += OnPowerUpCollected;
            World.NearMiss += OnNearMiss;

            GameBootstrap.SetBootStage("ENTERING MENU", 0.96f);
            EnterMenu();
            yield return null;

            IsBuilt = true;
            GameBootstrap.NotifyReady();
        }

        private void Update()
        {
            if (!IsBuilt) return;

            float dt = Time.unscaledDeltaTime;
            if (Quality != null) Quality.Tick(dt);
            HandlePauseInput();

            switch (State)
            {
                case GameState.Playing:
                    TickPlaying(Time.deltaTime);
                    break;
                case GameState.Menu:
                    TickAmbient(dt);
                    break;
                case GameState.GameOver:
                case GameState.LevelComplete:
                    TickAmbient(dt * 0.35f);
                    break;
            }
        }

        private void TickAmbient(float dt)
        {
            if (dt <= 0f) return;
            if (City != null) City.Tick(dt, 0f, false, Time.unscaledTime);
            if (Road != null) Road.Tick(dt, 0f, false);
            if (Effects != null) Effects.Tick(dt, 0f);
        }

        private void TickPlaying(float rawDt)
        {
            float dt = Mathf.Min(rawDt, 0.05f);
            if (dt <= 0f) return;

            RunTime += dt;
            PowerUps.Tick(dt);
            Combo.Tick(dt);

            float baseSpeed = GameConfig.WorldStartSpeed;
            float targetSpeed = GameConfig.WorldMaxSpeed;
            if (ActiveLevel != null)
            {
                baseSpeed = ActiveLevel.BaseSpeed;
                targetSpeed = Mathf.Min(GameConfig.WorldMaxSpeed, ActiveLevel.BaseSpeed + 13f);
            }
            float ramp = ActiveLevel != null ? Mathf.Clamp01(Distance / 1400f) : Mathf.Clamp01(Distance / 2000f);
            float desired = Mathf.Lerp(baseSpeed, targetSpeed, ramp);
            WorldSpeed = Mathf.MoveTowards(WorldSpeed, desired, 1.5f * dt);
            Speed01 = Mathf.InverseLerp(GameConfig.WorldStartSpeed, GameConfig.WorldMaxSpeed, WorldSpeed);

            float timeScale = PowerUps.WorldTimeScale;
            float worldDt = dt * timeScale;
            float effectiveSpeed = WorldSpeed * PowerUps.SpeedMultiplier;
            float deltaMeters = effectiveSpeed * worldDt;
            Distance += deltaMeters;
            if (effectiveSpeed > TopSpeed) TopSpeed = effectiveSpeed;

            Player.Speed01 = Speed01;
            Score.AddDistance(deltaMeters, Speed01, PowerUps.ScoreMultiplier);
            World.SetHunterThreat(HitsTaken / (float)MaxHits);
            World.Tick(worldDt, effectiveSpeed, true, Player);
            City.Tick(worldDt, effectiveSpeed, true, Time.time);
            Road.Tick(worldDt, effectiveSpeed, true);
            Effects.Tick(worldDt, effectiveSpeed);
            Camera.SetSpeed01(Speed01);
            Environment.OnDistance(Distance, effectiveSpeed);

            trailTimer -= dt;
            if (trailTimer <= 0f && effectiveSpeed > 19f)
            {
                trailTimer = 0.085f;
                Color accent = SelectedSkin.Accent;
                Effects.Trail(Player.transform.position + Vector3.up * 0.85f, accent);
            }

            bool shouldIntense = Speed01 > 0.62f;
            if (shouldIntense != intenseMusic)
            {
                intenseMusic = shouldIntense;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayMusic(shouldIntense ? MusicTrack.Intense : MusicTrack.Run, 1.1f);
            }

            if (!IsEndless && Distance >= LevelTarget)
            {
                CompleteLevelRun();
            }
        }

        private void HandlePauseInput()
        {
            if (State == GameState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                    PauseGame();
            }
            else if (State == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                    ResumeGame();
            }
        }

        // --------------------------------------------------------------- FLOW

        public void EnterMenu()
        {
            Time.timeScale = 1f;
            State = GameState.Menu;
            intenseMusic = false;
            RunFinalized = true;

            if (World != null) World.EnterMenu();
            if (Player != null)
            {
                Player.ResetForRun(SelectedCharacter, SelectedSkin);
                Player.SetMenuPose();
                Player.SetInputEnabled(false);
            }
            if (Camera != null)
            {
                Camera.SetMode(CameraMode.Menu);
                Camera.SnapToMenu();
            }
            if (Environment != null) Environment.SetMenuMode(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MusicTrack.Menu);
            if (UI != null)
            {
                UI.ShowHUD(false);
                UI.CloseOverlays();
                UI.ShowMenu();
            }
            RaiseState();
        }

        public void StartEndlessRun()
        {
            Levels.StartEndless();
            BeginRun(null);
        }

        public void StartLevelRun(int index)
        {
            if (Levels == null) return;
            if (!Levels.IsUnlocked(index)) return;
            Levels.StartLevel(index);
            BeginRun(GameConfig.GetLevel(index));
        }

        private void BeginRun(LevelDef level)
        {
            Time.timeScale = 1f;
            RunFinalized = false;
            RevivedThisRun = false;
            RunCoins = 0;
            RunCoinValue = 0;
            GoldCollected = 0;
            SilverCollected = 0;
            BronzeCollected = 0;
            SpecialCollected = 0;
            Jumps = 0;
            Slides = 0;
            PowerUpsUsed = 0;
            NearMisses = 0;
            HitsTaken = 0;
            Distance = 0f;
            RunTime = 0f;
            TopSpeed = 0f;
            Speed01 = 0f;
            intenseMusic = false;
            trailTimer = 0f;
            pendingSummary = null;

            Score.ResetRun();
            Combo.ResetRun();
            PowerUps.ResetAll();
            if (level != null) World.SetDifficultyBias(level.Difficulty);
            else World.SetDifficultyBias(0f);
            World.ClearAll();
            World.ResetRun();

            Player.ResetForRun(SelectedCharacter, SelectedSkin);
            Player.SetInputEnabled(true);

            WorldSpeed = level != null ? level.BaseSpeed : GameConfig.WorldStartSpeed;

            if (level != null)
            {
                Environment.ConfigureForLevel(level);
                Environment.SetMenuMode(false);
            }
            else
            {
                Environment.ConfigureEndless();
                Environment.SetMenuMode(false);
            }

            Camera.SetMode(CameraMode.Gameplay);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MusicTrack.Run);
            if (UI != null)
            {
                UI.CloseOverlays();
                UI.ShowHUD(true);
            }

            State = GameState.Playing;
            ShowToast(level != null ? level.Name : "RUN!", Palette.Primary);
            RaiseState();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (Player != null) Player.SetInputEnabled(false);
            if (AudioManager.Instance != null) AudioManager.Instance.SetPausedDuck(true);
            if (UI != null)
            {
                UI.ShowPause(true);
                UI.ShowHUD(false);
            }
            RaiseState();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (Player != null) Player.SetInputEnabled(true);
            if (AudioManager.Instance != null) AudioManager.Instance.SetPausedDuck(false);
            if (UI != null)
            {
                UI.ShowPause(false);
                UI.ShowHUD(true);
            }
            RaiseState();
        }

        public void RestartRun()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.SetPausedDuck(false);
            if (UI != null) UI.CloseOverlays();
            int index = Levels.CurrentIndex;
            if (index >= 0) StartLevelRun(index);
            else StartEndlessRun();
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.SetPausedDuck(false);
            if (UI != null) UI.CloseOverlays();
            EnterMenu();
        }

        // --------------------------------------------------------- COLLISIONS

        public void RegisterJump()
        {
            Jumps++;
        }

        public void RegisterSlide()
        {
            Slides++;
        }

        public void OnPlayerHit(int hits)
        {
            HitsTaken = hits;
            Combo.Break(true);
            if (Camera != null) Camera.AddShake(0.42f, 0.4f);
            if (World != null) World.SetHunterThreat(hits / (float)MaxHits);
            ShowToast("HULL DAMAGE  " + hits + " / " + MaxHits, Palette.Danger);
            if (HitTaken != null) HitTaken(hits);
        }

        public void OnHitAbsorbed()
        {
            ShowToast("SHIELD ABSORBED", Palette.Secondary);
        }

        public void OnPlayerCaught()
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            intenseMusic = false;
            if (World != null) World.HunterLunge();
            if (Camera != null) Camera.SetMode(CameraMode.GameOver);
            if (Player != null) Player.SetInputEnabled(false);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWarning();
                AudioManager.Instance.PlayMusic(MusicTrack.GameOver, 0.35f);
            }
            if (UI != null)
            {
                UI.ShowHUD(false);
                UI.ShowReviveOffer();
            }
            RaiseState();
        }

        public void Revive()
        {
            if (State != GameState.GameOver) return;
            if (RevivedThisRun)
            {
                ShowToast("NO REVIVES LEFT", Palette.Danger);
                return;
            }
            if (SaveManager.Instance == null || !SaveManager.Instance.SpendCoins(ReviveCost))
            {
                ShowToast("NOT ENOUGH COINS", Palette.Danger);
                return;
            }

            RevivedThisRun = true;
            HitsTaken = 0;
            Combo.ResetRun();
            World.ClearAll();
            World.SetHunterThreat(0f);
            Player.Revive();
            Player.SetInputEnabled(true);
            WorldSpeed = Mathf.Max(WorldSpeed * 0.85f, GameConfig.WorldStartSpeed);
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (Camera != null) Camera.SetMode(CameraMode.Gameplay);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetPausedDuck(false);
                AudioManager.Instance.PlayRevive();
                AudioManager.Instance.PlayMusic(MusicTrack.Run);
            }
            if (Effects != null) Effects.LevelUpBurst(Player.transform.position + Vector3.up * 0.9f);
            if (UI != null)
            {
                UI.CloseOverlays();
                UI.ShowHUD(true);
            }
            ShowToast("REVIVED", Palette.Success);
            RaiseState();
        }

        public void DeclineRevive()
        {
            if (State != GameState.GameOver) return;
            if (RunFinalized) return;
            RunSummary summary = BuildSummary(false, 0);
            ApplyRunResults(summary);
            if (UI != null) UI.ShowRunResults(summary, false);
        }

        // -------------------------------------------------------------- LEVEL

        private void CompleteLevelRun()
        {
            if (State != GameState.Playing) return;
            State = GameState.LevelComplete;
            intenseMusic = false;
            int index = Levels.CurrentIndex;
            int stars = Levels.CompleteLevel(index, Score.Score);
            World.ClearAll();
            Player.SetInputEnabled(false);
            Player.Celebrate();
            RunSummary summary = BuildSummary(true, stars);
            ApplyRunResults(summary);

            if (Camera != null) Camera.SetMode(CameraMode.LevelComplete);
            if (Effects != null) Effects.Confetti(Player.transform.position + Vector3.up * 1.4f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelComplete();
                AudioManager.Instance.PlayMusic(MusicTrack.Victory, 0.6f);
            }
            if (UI != null)
            {
                UI.ShowHUD(false);
                UI.ShowRunResults(summary, true);
            }
            RaiseState();
        }

        private RunSummary BuildSummary(bool levelComplete, int stars)
        {
            RunSummary summary = new RunSummary();
            summary.Score = Score != null ? Score.Score : 0;
            summary.Distance = Distance;
            summary.Coins = RunCoinValue;
            summary.CoinCount = RunCoins;
            summary.GoldCoins = GoldCollected;
            summary.SilverCoins = SilverCollected;
            summary.BronzeCoins = BronzeCollected;
            summary.SpecialCoins = SpecialCollected;
            summary.BestCombo = Combo != null ? Combo.Best : 0;
            summary.Time = RunTime;
            summary.Perfect = HitsTaken == 0;
            summary.Jumps = Jumps;
            summary.Slides = Slides;
            summary.NearMisses = NearMisses;
            summary.PowerUps = PowerUpsUsed;
            summary.TopSpeed = TopSpeed;
            summary.LevelIndex = Levels != null ? Levels.CurrentIndex : -1;
            summary.LevelCompleted = levelComplete;
            summary.Stars = stars;
            summary.Revived = RevivedThisRun;
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                summary.NewBestScore = summary.Score > SaveManager.Instance.Data.BestScore;
                summary.NewBestDistance = summary.Distance > SaveManager.Instance.Data.BestDistance;
            }
            pendingSummary = summary;
            return summary;
        }

        private void ApplyRunResults(RunSummary summary)
        {
            if (RunFinalized) return;
            RunFinalized = true;

            if (Missions != null) Missions.ReportRun(summary);
            if (Achievements != null) Achievements.EvaluateRun(summary);

            int xp = 55 + Mathf.RoundToInt(summary.Distance * 0.16f) + summary.Score / 140 +
                     summary.Stars * 120 + (summary.LevelCompleted ? 60 : 0);
            int levels = SaveManager.Instance != null ? SaveManager.Instance.AddXp(xp) : 0;
            int newLevel = SaveManager.Instance != null ? SaveManager.Instance.Data.PlayerLevel : 1;
            if (levels > 0 && newLevel > lastPlayerLevel)
            {
                int oldLevel = lastPlayerLevel;
                lastPlayerLevel = newLevel;
                if (PlayerLeveledUp != null) PlayerLeveledUp(oldLevel, newLevel);
                ShowToast("LEVEL " + newLevel + " REACHED", Palette.Accent);
                if (Effects != null) Effects.LevelUpBurst(Player.transform.position + Vector3.up * 1.1f);
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RecordRun(summary.Score, summary.Distance, summary.CoinCount,
                    summary.BestCombo, summary.Time, summary.Perfect && summary.Distance >= 500f,
                    summary.GoldCoins, summary.Jumps, summary.Slides, summary.NearMisses,
                    summary.PowerUps, summary.TopSpeed);
            }

            if (RunFinished != null) RunFinished(summary);
        }

        // ----------------------------------------------------------- GAMEPLAY

        private void OnCoinCollected(Coin coin)
        {
            int multiplier = PowerUps != null ? PowerUps.CoinMultiplier : 1;
            int value = GameConfig.CoinValues[(int)coin.Type] * multiplier;
            RunCoins++;
            RunCoinValue += value;
            switch (coin.Type)
            {
                case CoinType.Gold: GoldCollected++; break;
                case CoinType.Silver: SilverCollected++; break;
                case CoinType.Special: SpecialCollected++; break;
                default: BronzeCollected++; break;
            }

            int comboCount = Combo != null ? Combo.RegisterPickup() : 1;
            if (Score != null)
                Score.AddCoin(value, Combo != null ? Combo.Multiplier : 1f,
                    PowerUps != null ? PowerUps.ScoreMultiplier : 1);
            if (SaveManager.Instance != null) SaveManager.Instance.AddCoins(value);
            if (Effects != null)
                Effects.CoinCollect(coin.transform.position, GameConfig.CoinColors[(int)coin.Type],
                    coin.Type == CoinType.Gold || coin.Type == CoinType.Special);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCoin(comboCount, coin.Type == CoinType.Gold || coin.Type == CoinType.Special);
            if (comboCount > 0 && comboCount % 10 == 0)
                ShowToast("COMBO x" + comboCount, Palette.Accent);
        }

        private void OnPowerUpCollected(PowerUpType type, Vector3 position)
        {
            ActivatePowerUp(type, position);
        }

        public void ActivatePowerUp(PowerUpType type, Vector3 position)
        {
            if (PowerUps == null) return;
            PowerUps.Activate(type);
            PowerUpsUsed++;
            PowerUpDef def = GameConfig.GetPowerUp(type);
            if (Effects != null) Effects.PowerUpBurst(position, def.Color);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerUp();
            ShowToast(def.Name + " ACTIVE", def.Color);
            if (PowerUpActivated != null) PowerUpActivated(type);
        }

        private void OnNearMiss(Obstacle obstacle)
        {
            NearMisses++;
            int comboCount = Combo != null ? Combo.RegisterNearMiss() : 1;
            int bonus = 90 + Mathf.RoundToInt(WorldSpeed * 4.5f) +
                        (Combo != null ? Mathf.RoundToInt((Combo.Multiplier - 1f) * 60f) : 0);
            if (Score != null) Score.AddBonus(bonus, true);
            if (Effects != null)
            {
                float direction = obstacle.WorldX < Player.VisualX ? -1f : 1f;
                Effects.NearMiss(obstacle.transform.position, Mathf.RoundToInt(direction));
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySwoosh();
            ShowToast("NEAR MISS  +" + bonus, Palette.Accent);
        }

        private void OnComboBroken(int previous)
        {
            if (previous >= 5) ShowToast("COMBO LOST", Palette.Muted);
        }

        private void OnPowerUpsChanged()
        {
        }

        // ------------------------------------------------------------- OPTIONS

        public void SelectCharacter(int index)
        {
            if (SaveManager.Instance == null) return;
            if (!SaveManager.Instance.IsCharacterUnlocked(index)) return;
            SaveManager.Instance.Data.SelectedCharacter = index;
            SaveManager.Instance.MarkDirty();
            SaveManager.Instance.Save();
            RefreshPlayerLook();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        }

        public void SelectSkin(int index)
        {
            if (SaveManager.Instance == null) return;
            if (!SaveManager.Instance.IsSkinUnlocked(index)) return;
            SaveManager.Instance.Data.SelectedSkin = index;
            SaveManager.Instance.MarkDirty();
            SaveManager.Instance.Save();
            RefreshPlayerLook();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        }

        public bool BuyCharacter(int index)
        {
            if (SaveManager.Instance == null) return false;
            if (index < 0 || index >= GameConfig.Characters.Length) return false;
            if (SaveManager.Instance.Data.PlayerLevel < GameConfig.Characters[index].RequiredPlayerLevel)
            {
                ShowToast("REACH LEVEL " + GameConfig.Characters[index].RequiredPlayerLevel, Palette.Danger);
                return false;
            }
            bool ok = SaveManager.Instance.UnlockCharacter(index);
            if (ok)
            {
                SelectCharacter(index);
                ShowToast("RUNNER UNLOCKED", Palette.Success);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
            }
            else
            {
                ShowToast("NOT ENOUGH COINS", Palette.Danger);
            }
            return ok;
        }

        public bool BuySkin(int index)
        {
            if (SaveManager.Instance == null) return false;
            bool ok = SaveManager.Instance.UnlockSkin(index);
            if (ok)
            {
                SelectSkin(index);
                ShowToast("SKIN UNLOCKED", Palette.Success);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
            }
            else
            {
                ShowToast("NOT ENOUGH COINS", Palette.Danger);
            }
            return ok;
        }

        private void RefreshPlayerLook()
        {
            if (Player == null) return;
            if (State == GameState.Menu)
            {
                Player.ResetForRun(SelectedCharacter, SelectedSkin);
                Player.SetMenuPose();
                Player.SetInputEnabled(false);
            }
            else
            {
                Player.Animator.ApplyLook(SelectedCharacter, SelectedSkin);
            }
        }

        public void ShowToast(string message, Color color)
        {
            if (ToastRequested != null) ToastRequested(message, color);
        }

        private void RaiseState()
        {
            if (StateChanged != null) StateChanged(State);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && State == GameState.Playing) PauseGame();
        }

        public RunSummary PendingSummary { get { return pendingSummary; } }
    }
}
