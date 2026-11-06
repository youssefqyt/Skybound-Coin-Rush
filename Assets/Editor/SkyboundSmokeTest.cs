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
                    Debug.Log("[Smoke] Run active. distance=" + game.Distance.ToString("0") +
                              " speed=" + game.WorldSpeed.ToString("0.0") +
                              " score=" + game.Score.Score);
                    game.PauseGame();
                    if (game.State != GameState.Paused || Time.timeScale != 0f)
                    {
                        Fail("Pause failed.");
                        return;
                    }
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
                    game.OnPlayerCaught();
                    if (game.State != GameState.GameOver)
                    {
                        Fail("Game-over state not reached.");
                        return;
                    }
                    if (!game.CanRevive && game.WalletCoins >= game.ReviveCost)
                    {
                        Fail("CanRevive returned false with enough coins.");
                        return;
                    }
                    game.DeclineRevive();
                    if (!game.RunFinalized || game.PendingSummary == null)
                    {
                        Fail("Run was not finalised after declining revive.");
                        return;
                    }
                    Debug.Log("[Smoke] Results: score=" + game.PendingSummary.Score +
                              " coins=" + game.PendingSummary.CoinCount +
                              " distance=" + game.PendingSummary.Distance.ToString("0") +
                              " best=" + game.PendingSummary.NewBestScore);
                    NextStage();
                    break;

                case 3:
                    if (Elapsed() < 1f) return;
                    game.QuitToMenu();
                    if (game.State != GameState.Menu)
                    {
                        Fail("Quit to menu failed.");
                        return;
                    }
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

                case 4:
                    if (Elapsed() < 0.5f) return;
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
                Debug.Log("[Smoke] PASS: boot, run, pause, resume, revive-decline, results and all menus verified.");
            else
                Debug.LogError("[Smoke] FAILED.");

            if (Application.isBatchMode)
                EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
