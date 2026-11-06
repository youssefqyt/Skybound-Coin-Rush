using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Entry point for the whole game. Creates the persistent services in the
    /// correct order and forces the mobile presentation settings.
    /// The build is spread over several frames and shows a native loading overlay
    /// so a slow first launch can never trigger Android's "not responding" dialog.
    /// A runtime fallback also boots the game even if the bootstrap object is
    /// missing from the active scene, which keeps iteration painless.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        public bool AutoStart = true;

        // ------------------------------------------------------------ BOOT STATE

        public static bool IsReady { get; private set; }
        public static string CurrentStage { get; private set; } = "STARTING";
        public static float Progress { get; private set; }
        public static string LastError { get; private set; }

        private static bool booted;
        private static GameBootstrap instance;
        private static bool bootStarted;
        private static float bootStartedAt;
        private static bool capturing;
        private static readonly List<string> bootMessages = new List<string>(32);
        private static bool logWritten;

        private static GUIStyle titleStyle;
        private static GUIStyle accentStyle;
        private static GUIStyle stageStyle;
        private static GUIStyle hintStyle;
        private static GUIStyle errorTitleStyle;
        private static GUIStyle errorStyle;
        private static GUIStyle errorHintStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBoot()
        {
            if (booted) return;
            GameObject go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (booted)
            {
                Destroy(gameObject);
                return;
            }

            booted = true;
            instance = this;
            IsReady = false;
            LastError = null;
            Progress = 0f;
            CurrentStage = "STARTING";
            bootMessages.Clear();
            logWritten = false;

            DontDestroyOnLoad(gameObject);
            ConfigureApplication();
            BeginCapture();

            if (AutoStart) Boot();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                EndCapture();
                bootStarted = false;
                booted = false;
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Save();
            WriteBootLog();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && SaveManager.Instance != null) SaveManager.Instance.Save();
        }

        private static void ConfigureApplication()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif
        }

        /// <summary>Creates save, audio and the game state machine in dependency order.</summary>
        public static void Boot()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameBootstrap");
                go.AddComponent<GameBootstrap>();
                return;
            }

            if (bootStarted) return;
            bootStarted = true;
            bootStartedAt = Time.realtimeSinceStartup;
            instance.StartCoroutine(instance.BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            // Let the first frame and the loading overlay appear before any work.
            yield return null;

            SetBootStage("LOADING SAVE DATA", 0.10f);
            if (SaveManager.Instance == null)
                SaveManager.Create(transform);
            yield return null;

            SetBootStage("STARTING AUDIO", 0.18f);
            if (AudioManager.Instance == null)
                AudioManager.Create(transform);
            yield return null;

            SetBootStage("PREPARING GAME", 0.24f);
            if (GameManager.Instance == null)
            {
                GameObject gameGo = RuntimeUtil.Create("Game", transform);
                GameManager game = gameGo.AddComponent<GameManager>();
                yield return StartCoroutine(game.BuildRoutine());
            }
        }

        /// <summary>Updates the text shown on the loading overlay.</summary>
        public static void SetBootStage(string label, float progress)
        {
            CurrentStage = label;
            Progress = Mathf.Clamp01(progress);
        }

        /// <summary>Called by GameManager once every system is alive.</summary>
        public static void NotifyReady()
        {
            IsReady = true;
            Progress = 1f;
            CurrentStage = "READY";
            bootStartedAt = -10000f;
            WriteBootLog();
        }

        /// <summary>Shows a fatal startup message and keeps it on screen.</summary>
        public static void ReportBootError(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (LastError == null) LastError = message;
            AppendBootLog("[ERROR] " + message);
            WriteBootLog();
        }

        private void Update()
        {
            if (IsReady) return;

            // If the build never finishes, explain it instead of freezing silently.
            if (bootStartedAt > 0f && Time.realtimeSinceStartup - bootStartedAt > 120f &&
                string.IsNullOrEmpty(LastError))
            {
                LastError = "Startup is taking unusually long.\n" +
                            "Please close the app and open it again.";
            }
        }

        // ------------------------------------------------------------ DIAGNOSTICS

        private static void BeginCapture()
        {
            if (capturing) return;
            capturing = true;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void EndCapture()
        {
            if (!capturing) return;
            capturing = false;
            Application.logMessageReceived -= OnLogMessage;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;

            string lower = condition == null ? string.Empty : condition.ToLowerInvariant();
            if (lower.Contains("audio") || lower.Contains("fmod") || lower.Contains("listener"))
                return;

            string message = condition ?? "Unknown error";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                string[] lines = stackTrace.Split('\n');
                StringBuilder trimmed = new StringBuilder(message);
                for (int i = 0; i < lines.Length && i < 3; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    trimmed.Append('\n').Append(line);
                }
                message = trimmed.ToString();
            }

            AppendBootLog("[ERROR] " + message);
            if (!IsReady) LastError = message;
        }

        private static void AppendBootLog(string line)
        {
            if (bootMessages.Count > 60) bootMessages.RemoveAt(0);
            bootMessages.Add(line.Length > 2000 ? line.Substring(0, 2000) : line);
        }

        private static void WriteBootLog()
        {
            if (logWritten) return;
            logWritten = true;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SKYBOUND: COIN RUSH - boot log");
                sb.AppendLine("Time: " + DateTime.Now.ToString("u"));
                sb.AppendLine("Device: " + SystemInfo.deviceModel + " / " + SystemInfo.operatingSystem);
                sb.AppendLine("Graphics: " + SystemInfo.graphicsDeviceType + " " + SystemInfo.graphicsDeviceName);
                sb.AppendLine("Ready: " + IsReady + "  Stage: " + CurrentStage + "  Progress: " + Progress);
                sb.AppendLine();
                for (int i = 0; i < bootMessages.Count; i++) sb.AppendLine(bootMessages[i]);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "skybound_boot.log"), sb.ToString());
            }
            catch (Exception)
            {
                // Logging must never break the game.
            }
        }

        // ---------------------------------------------------------------- OVERLAY

        private void OnGUI()
        {
            if (IsReady) return;

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0) return;

            BuildStyles(h);

            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);

            // Navy background with a subtle cyan wash at the top.
            GUI.color = new Color(0.027f, 0.067f, 0.122f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0.9f, 1f, 0.07f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h * 0.42f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float titleSize = Mathf.Clamp(h * 0.115f, 40f, 190f);
            titleStyle.fontSize = Mathf.RoundToInt(titleSize);
            accentStyle.fontSize = Mathf.RoundToInt(titleSize * 0.5f);
            stageStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(h * 0.03f, 16f, 52f));
            hintStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(h * 0.022f, 12f, 38f));

            GUI.Label(new Rect(w * 0.08f, h * 0.20f, w * 0.84f, titleSize * 1.25f), "SKYBOUND", titleStyle);
            GUI.Label(new Rect(w * 0.085f, h * 0.20f + titleSize * 1.05f, w * 0.84f, titleSize * 0.8f),
                "COIN RUSH", accentStyle);

            // Progress bar.
            float barX = w * 0.085f;
            float barY = h * 0.62f;
            float barW = w * 0.83f;
            float barH = Mathf.Clamp(h * 0.016f, 6f, 22f);
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0.9f, 1f, 1f);
            GUI.DrawTexture(new Rect(barX, barY, barW * Mathf.Clamp01(Progress), barH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string stage = string.IsNullOrEmpty(LastError) ? CurrentStage : "STARTUP ERROR";
            GUI.Label(new Rect(barX, barY - stageStyle.fontSize * 1.9f, barW, stageStyle.fontSize * 1.6f),
                stage, stageStyle);

            if (string.IsNullOrEmpty(LastError))
            {
                GUI.Label(new Rect(barX, barY + barH * 2.6f, barW, hintStyle.fontSize * 2.4f),
                    "First launch can take up to 30 seconds while the city, audio and interface are generated.",
                    hintStyle);
            }
            else
            {
                float errTop = h * 0.30f;
                float errHeight = h * 0.62f;
                GUI.color = new Color(0.55f, 0.07f, 0.10f, 0.94f);
                GUI.DrawTexture(new Rect(w * 0.05f, errTop, w * 0.90f, errHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                errorTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(h * 0.045f, 20f, 70f));
                errorStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(h * 0.024f, 12f, 34f));
                errorHintStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(h * 0.022f, 11f, 30f));

                GUI.Label(new Rect(w * 0.08f, errTop + h * 0.02f, w * 0.84f, errorTitleStyle.fontSize * 1.6f),
                    "STARTUP ERROR", errorTitleStyle);

                string shown = LastError.Length > 700 ? LastError.Substring(0, 700) + "..." : LastError;
                GUI.Label(new Rect(w * 0.08f, errTop + h * 0.09f, w * 0.84f, errHeight * 0.68f),
                    shown, errorStyle);

                GUI.Label(new Rect(w * 0.08f, errTop + errHeight - h * 0.075f, w * 0.84f, h * 0.06f),
                    "Screenshot this screen and send it to the developer. Details were also saved to skybound_boot.log.",
                    errorHintStyle);
            }

            GUI.color = old;
        }

        private static void BuildStyles(int screenHeight)
        {
            if (titleStyle != null) return;

            int title = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.115f, 40f, 190f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = title;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.UpperLeft;

            accentStyle = new GUIStyle(titleStyle);
            accentStyle.fontSize = Mathf.RoundToInt(title * 0.5f);
            accentStyle.normal.textColor = new Color(0f, 0.9f, 1f, 1f);

            stageStyle = new GUIStyle(GUI.skin.label);
            stageStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.03f, 16f, 52f));
            stageStyle.fontStyle = FontStyle.Bold;
            stageStyle.normal.textColor = new Color(0.78f, 0.86f, 0.95f, 1f);
            stageStyle.alignment = TextAnchor.UpperLeft;

            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.022f, 12f, 38f));
            hintStyle.normal.textColor = new Color(0.55f, 0.66f, 0.79f, 1f);
            hintStyle.wordWrap = true;
            hintStyle.alignment = TextAnchor.UpperLeft;

            errorTitleStyle = new GUIStyle(GUI.skin.label);
            errorTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.045f, 20f, 70f));
            errorTitleStyle.fontStyle = FontStyle.Bold;
            errorTitleStyle.normal.textColor = Color.white;
            errorTitleStyle.alignment = TextAnchor.UpperLeft;

            errorStyle = new GUIStyle(GUI.skin.label);
            errorStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.024f, 12f, 34f));
            errorStyle.normal.textColor = Color.white;
            errorStyle.wordWrap = true;
            errorStyle.alignment = TextAnchor.UpperLeft;

            errorHintStyle = new GUIStyle(GUI.skin.label);
            errorHintStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(screenHeight * 0.022f, 11f, 30f));
            errorHintStyle.normal.textColor = new Color(1f, 0.86f, 0.86f, 1f);
            errorHintStyle.wordWrap = true;
            errorHintStyle.alignment = TextAnchor.UpperLeft;
        }
    }
}
