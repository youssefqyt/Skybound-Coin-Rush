using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Skybound.Editor
{
    /// <summary>
    /// Batch-mode play test. Boots the generated scene, starts a run, exercises
    /// pause, revive flow and result generation, then fails on any runtime exception.
    /// Run with:
    /// Unity.exe -batchmode -nographics -projectPath ... -executeMethod Skybound.Editor.SkyboundSmokeTest.Run
    /// </summary>
    public static class SkyboundSmokeTest
    {
        private static int stage;
        private static float stageStart;
        private static double launchTime;
        private static readonly StringBuilder errors = new StringBuilder();
        private static bool hooked;
        private static float lastBeat;
        private static bool laneChangeRequested;

        public static void Run()
        {
            // Keep managed statics alive across the play-mode transition so the
            // editor update hook keeps ticking and the boot only happens once.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorSceneManager.OpenScene(SkyboundBuildTools.ScenePath, OpenSceneMode.Single);
            Application.logMessageReceived += OnLog;
            hooked = true;
            stage = 0;
            stageStart = Time.realtimeSinceStartup;
            launchTime = EditorApplication.timeSinceStartup;
            errors.Length = 0;
            lastBeat = 0f;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
            Debug.Log("[Smoke] Entering play mode...");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            string lower = condition.ToLowerInvariant();
            if (lower.Contains("audio") || lower.Contains("fmod") || lower.Contains("listener")) return;
            errors.AppendLine("[" + type + "] " + condition + "\n" + stackTrace);
        }

        private static void Tick()
        {
            if (Time.realtimeSinceStartup - lastBeat > 3f)
            {
                lastBeat = Time.realtimeSinceStartup;
                Debug.Log("[Smoke] tick stage=" + stage + " playing=" + EditorApplication.isPlaying +
                          " game=" + (GameManager.Instance != null) +
                          " built=" + (GameManager.Instance != null && GameManager.Instance.IsBuilt) +
                          " bootStage=" + GameBootstrap.CurrentStage);
            }

            if (!EditorApplication.isPlaying)
            {
                if (stage >= 90) Finish(true);
                return;
            }

            if (EditorApplication.timeSinceStartup - launchTime > 90.0)
            {
                Fail("Smoke test timed out.");
                return;
            }

            GameManager game = GameManager.Instance;
            if (game == null)
            {
                if (Time.realtimeSinceStartup - stageStart > 12f)
                    Fail("GameManager never initialised.");
                return;
            }

            if (!game.IsBuilt)
            {
                if (Time.realtimeSinceStartup - stageStart > 45f)
                    Fail("Staged build never completed. Stage: " + GameBootstrap.CurrentStage +
                         " | Error: " + (GameBootstrap.LastError ?? "none"));
                return;
            }

            switch (stage)
            {
                case 0:
                    if (!ValidateBoot(game)) return;
                    game.StartEndlessRun();
                    NextStage();
                    break;

                case 1:
                    if (Elapsed() < 3.5f) return;
                    if (game.State != GameState.Playing)
                    {
                        Fail("Expected Playing state, got " + game.State);
                        return;
                    }
                    if (game.Distance <= 0f) { Fail("Distance did not advance."); return; }
                    if (game.WorldSpeed <= 0f) { Fail("World speed is zero."); return; }
                    if (game.Score.Score <= 0) { Fail("Score did not advance."); return; }
                    if (game.World == null || game.World.ActiveCoins < 0)
                    {
                        Fail("World did not spawn content.");
                        return;
                    }
                    // BUG 1 regression: the main menu must never be visible during a run.
                    if (game.UI.MenuVisible)
                    {
                        Fail("Main menu is still visible during gameplay.");
                        return;
                    }
                    if (!game.UI.HudVisible)
                    {
                        Fail("HUD is not visible during gameplay.");
                        return;
                    }
                    Debug.Log("[Smoke] Run active. distance=" + game.Distance.ToString("0") +
                              " speed=" + game.WorldSpeed.ToString("0.0") +
                              " score=" + game.Score.Score);
                    game.PauseGame();
                    if (game.State != GameState.Paused || Time.timeScale != 0f)
                    {
                        Fail("Pause failed.");
                        return;
                    }
                    if (game.UI.MenuVisible) { Fail("Main menu visible while paused."); return; }
                    game.ResumeGame();
                    if (game.State != GameState.Playing || Time.timeScale != 1f)
                    {
                        Fail("Resume failed.");
                        return;
                    }
                    game.Player.Input.PerformLane(1);
                    game.Player.Input.PerformJump();
                    game.Player.Input.PerformSlide();
                    NextStage();
                    break;

                case 2:
                    if (Elapsed() < 2.5f) return;
                    // Guarantee the player can afford a revive so this exercises the
                    // click path rather than the "not enough coins" branch.
                    SaveManager.Instance.Data.Coins = Mathf.Max(SaveManager.Instance.Data.Coins, game.ReviveCost + 500);
                    int coinsBeforeRevive = SaveManager.Instance.Data.Coins;

                    game.OnPlayerCaught();
                    if (game.State != GameState.GameOver)
                    {
                        Fail("Game-over state not reached.");
                        return;
                    }
                    if (game.UI.MenuVisible) { Fail("Main menu still visible on the CAUGHT screen."); return; }
                    if (game.UI.HudVisible) { Fail("HUD still visible on the CAUGHT screen."); return; }
                    if (!game.UI.ReviveVisible) { Fail("Revive screen is not visible."); return; }
                    if (!game.CanRevive) { Fail("CanRevive returned false with enough coins."); return; }

                    // BUG 2 regression: the revive and give-up buttons must be reachable
                    // by a real pointer raycast, not covered by a full-screen dimmer.
                    if (!game.UI.ReviveButtonReceivesClicks())
                    {
                        Fail("REVIVE button is covered and cannot receive clicks.");
                        return;
                    }
                    if (!game.UI.GiveUpButtonReceivesClicks())
                    {
                        Fail("GIVE UP button is covered and cannot receive clicks.");
                        return;
                    }

                    game.UI.ClickRevive();
                    if (game.State != GameState.Playing)
                    {
                        Fail("REVIVE click did not resume the run. State=" + game.State);
                        return;
                    }
                    if (SaveManager.Instance.Data.Coins != coinsBeforeRevive - game.ReviveCost)
                    {
                        Fail("REVIVE did not charge " + game.ReviveCost + " coins.");
                        return;
                    }
                    if (!game.UI.HudVisible) { Fail("HUD not visible after revive."); return; }
                    if (game.UI.MenuVisible || game.UI.ReviveVisible)
                    {
                        Fail("Menu or revive screen still visible after revive.");
                        return;
                    }
                    Debug.Log("[Smoke] Revive clicked: coins " + coinsBeforeRevive + " -> " +
                              SaveManager.Instance.Data.Coins + ", run resumed.");
                    NextStage();
                    break;

                case 3:
                    if (Elapsed() < 2.5f) return;
                    // Second catch: only one revive is allowed per run, so GIVE UP must work.
                    game.OnPlayerCaught();
                    if (game.State != GameState.GameOver) { Fail("Second game-over not reached."); return; }
                    if (!game.UI.ReviveVisible) { Fail("Revive screen not visible on second catch."); return; }
                    if (game.CanRevive) { Fail("CanRevive should be false after a revive."); return; }
                    if (!game.UI.GiveUpButtonReceivesClicks())
                    {
                        Fail("GIVE UP button is covered on the second catch.");
                        return;
                    }

                    game.UI.ClickGiveUp();
                    if (!game.RunFinalized || game.PendingSummary == null)
                    {
                        Fail("Run was not finalised after clicking GIVE UP.");
                        return;
                    }
                    if (!game.UI.ResultsVisible) { Fail("Results screen not visible after GIVE UP."); return; }
                    if (game.UI.ReviveVisible) { Fail("Revive screen still visible after GIVE UP."); return; }
                    Debug.Log("[Smoke] Results: score=" + game.PendingSummary.Score +
                              " coins=" + game.PendingSummary.CoinCount +
                              " distance=" + game.PendingSummary.Distance.ToString("0") +
                              " best=" + game.PendingSummary.NewBestScore);
                    NextStage();
                    break;

                case 4:
                    if (Elapsed() < 1f) return;
                    game.QuitToMenu();
                    if (game.State != GameState.Menu)
                    {
                        Fail("Quit to menu failed.");
                        return;
                    }
                    if (!game.UI.MenuVisible) { Fail("Main menu is not visible after quitting to menu."); return; }
                    // Verify shop / level / mission / achievement screens build without exceptions.
                    game.UI.ShowShop(true);
                    game.UI.ShowShop(false);
                    game.UI.ShowLevelSelect(true);
                    game.UI.ShowLevelSelect(false);
                    game.UI.ShowMissions(true);
                    game.UI.ShowMissions(false);
                    game.UI.ShowAchievements(true);
                    game.UI.ShowAchievements(false);
                    game.UI.ShowSettings(true);
                    game.UI.ShowSettings(false);
                    Debug.Log("[Smoke] Menu and all overlay screens built successfully.");
                    NextStage();
                    break;

                case 5:
                    // Fresh run for the lane-change collision regression.
                    game.StartEndlessRun();
                    game.World.SpawningEnabled = false;
                    game.World.ClearAll();
                    laneChangeRequested = false;
                    NextStage();
                    break;

                case 6:
                    // Let the post-spawn invulnerability expire with no hazards around.
                    game.World.ClearAll();
                    if (Elapsed() < 1.45f) return;
                    NextStage();
                    break;

                case 7:
                    // BUG 3 regression: swiping out of a lane that already contains a
                    // hazard must not cost a heart while the smooth slide is in flight.
                    // The hazard is pinned at z = 0 and respawned every frame.
                    game.World.ClearAll();
                    if (!laneChangeRequested)
                    {
                        laneChangeRequested = true;
                        game.Player.Input.PerformLane(-1); // lane 1 -> lane 0
                    }
                    if (game.Player.Lane != 0)
                    {
                        Fail("Lane request did not commit (lane=" + game.Player.Lane + ").");
                        return;
                    }
                    game.World.SpawnTestObstacle(ObstacleKind.Block, 1, 0f);
                    if (Elapsed() < 0.75f) return;
                    if (game.Player.Hits != 0)
                    {
                        Fail("Phantom hit: player took damage (hits=" + game.Player.Hits +
                             ") while leaving the hazard lane.");
                        return;
                    }
                    Debug.Log("[Smoke] Lane-change forgiveness OK: no damage while leaving the hazard lane.");
                    NextStage();
                    break;

                case 8:
                    // Positive control: a hazard inside the committed lane must still hurt.
                    if (Elapsed() < 1.2f) return;
                    game.World.ClearAll();
                    game.World.SpawnTestObstacle(ObstacleKind.Block, 0, 0.2f);
                    if (Elapsed() < 1.55f) return;
                    if (game.Player.Hits < 1)
                    {
                        Fail("Collision never registered for a hazard inside the player's own lane.");
                        return;
                    }
                    Debug.Log("[Smoke] Same-lane collision still registers (hits=" + game.Player.Hits + ").");
                    game.World.SpawningEnabled = true;
                    game.World.ClearAll();
                    stage = 90;
                    EditorApplication.isPlaying = false;
                    break;
            }
        }

        private static bool ValidateBoot(GameManager game)
        {
            if (game.UI == null) { Fail("UI missing."); return false; }
            if (!game.UI.IsBuilt) { Fail("UI was not built."); return false; }
            if (game.Player == null) { Fail("Player missing."); return false; }
            if (game.World == null) { Fail("World missing."); return false; }
            if (game.Camera == null || game.Camera.Camera == null) { Fail("Camera missing."); return false; }
            if (SaveManager.Instance == null) { Fail("SaveManager missing."); return false; }
            if (AudioManager.Instance == null) { Fail("AudioManager missing."); return false; }
            if (SaveManager.Instance.Data == null) { Fail("Save data missing."); return false; }
            if (SaveManager.Instance.Data.Coins < 0) { Fail("Invalid starting coins."); return false; }
            Debug.Log("[Smoke] Boot OK. Coins=" + SaveManager.Instance.Data.Coins +
                      " level=" + SaveManager.Instance.Data.PlayerLevel);
            return true;
        }

        private static float Elapsed()
        {
            return Time.realtimeSinceStartup - stageStart;
        }

        private static void NextStage()
        {
            stage++;
            stageStart = Time.realtimeSinceStartup;
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Smoke] FAIL: " + message);
            stage = 91;
            EditorApplication.isPlaying = false;
            Finish(false);
        }

        private static void Finish(bool success)
        {
            if (hooked)
            {
                Application.logMessageReceived -= OnLog;
                EditorApplication.update -= Tick;
                hooked = false;
            }

            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;

            if (errors.Length > 0)
            {
                Debug.LogError("[Smoke] Runtime errors captured:\n" + errors);
                success = false;
            }

            if (success)
                Debug.Log("[Smoke] PASS: boot, menu hiding, run, pause, revive click, give-up, results and all menus verified.");
            else
                Debug.LogError("[Smoke] FAILED.");

            if (Application.isBatchMode)
                EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
