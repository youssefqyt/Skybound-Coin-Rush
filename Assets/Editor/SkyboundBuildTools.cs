using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Skybound.Editor
{
    /// <summary>
    /// One-stop editor tooling for SKYBOUND: COIN RUSH.
    /// Generates the procedural app icon, creates the bootstrap scene, applies all
    /// Android player settings and produces APK / AAB builds from the command line.
    /// </summary>
    public static class SkyboundBuildTools
    {
        public const string ScenePath = "Assets/Scenes/Skybound.unity";
        public const string IconPath = "Assets/Art/AppIcon.png";
        private const string AppId = "com.skybound.coinrush";
        private const string Company = "Skybound Studio";
        private const string Product = "Skybound: Coin Rush";
        private const int IconSize = 512;

        // ------------------------------------------------------------------ SETUP

        [MenuItem("Skybound/Setup/Apply All")]
        public static void ApplyAll()
        {
            ConfigureProject();
            GenerateIcon();
            GenerateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Skybound] Setup complete. Scene, icon and Android settings are ready.");
        }

        /// <summary>
        /// Detects the Android SDK / NDK / JDK from the machine and stores the
        /// paths in Unity's editor preferences so headless builds can find them.
        /// </summary>
        private static void ConfigureExternalTools()
        {
            try
            {
                string sdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
                if (string.IsNullOrEmpty(sdk) || !Directory.Exists(sdk))
                {
                    string local = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Android", "Sdk");
                    if (Directory.Exists(local)) sdk = local;
                }

                if (!string.IsNullOrEmpty(sdk) && Directory.Exists(sdk))
                {
                    EditorPrefs.SetString("AndroidSdkRoot", sdk);
                    string ndkParent = Path.Combine(sdk, "ndk");
                    if (Directory.Exists(ndkParent))
                    {
                        string[] ndks = Directory.GetDirectories(ndkParent);
                        if (ndks.Length > 0)
                        {
                            Array.Sort(ndks, StringComparer.OrdinalIgnoreCase);
                            EditorPrefs.SetString("AndroidNdkRoot", ndks[ndks.Length - 1]);
                        }
                    }
                }

                string jdk = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (string.IsNullOrEmpty(jdk) || !Directory.Exists(jdk))
                {
                    string javaDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java");
                    if (Directory.Exists(javaDir))
                    {
                        string[] jdks = Directory.GetDirectories(javaDir, "jdk*");
                        if (jdks.Length > 0)
                        {
                            Array.Sort(jdks, StringComparer.OrdinalIgnoreCase);
                            jdk = jdks[jdks.Length - 1];
                        }
                    }
                }

                if (!string.IsNullOrEmpty(jdk) && Directory.Exists(jdk))
                    EditorPrefs.SetString("JdkPath", jdk);

                Debug.Log("[Skybound] External tools: SDK=" + sdk + " JDK=" + jdk);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Skybound] External tool detection failed: " + exception.Message);
            }
        }

        [MenuItem("Skybound/Setup/Configure Android Player Settings")]
        public static void ConfigureProject()
        {
            ConfigureExternalTools();

            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = GameConfig.Version;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = false;
            PlayerSettings.SplashScreen.backgroundColor = Palette.Hex("#07111F");
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = false;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android,
                Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            // The game finds these shaders with Shader.Find at runtime. A shader that
            // is never referenced by an asset is stripped from the player, which used
            // to leave the sky unshaded (magenta) on device. Force them into the build.
            EnsureAlwaysIncludedShaders(
                "Skybound/Gradient",
                "Skybound/AdditiveSprite",
                "Sprites/Default",
                "UI/Default");

            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;

            Debug.Log("[Skybound] Android settings applied: " + AppId + " v" + GameConfig.Version +
                      " (ARM64 / IL2CPP / landscape).");
        }

        [MenuItem("Skybound/Setup/Generate Bootstrap Scene")]
        public static void GenerateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Skybound] Bootstrap scene generated at " + ScenePath);
        }

        // ------------------------------------------------------------------- ICON

        /// <summary>Adds the given shaders to Graphics Settings / Always Included Shaders.</summary>
        private static void EnsureAlwaysIncludedShaders(params string[] shaderNames)
        {
            try
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (assets == null || assets.Length == 0)
                {
                    Debug.LogWarning("[Skybound] Could not open GraphicsSettings.asset.");
                    return;
                }

                SerializedObject settings = new SerializedObject(assets[0]);
                SerializedProperty list = settings.FindProperty("m_AlwaysIncludedShaders");
                if (list == null)
                {
                    Debug.LogWarning("[Skybound] m_AlwaysIncludedShaders not found.");
                    return;
                }

                for (int n = 0; n < shaderNames.Length; n++)
                {
                    Shader shader = Shader.Find(shaderNames[n]);
                    if (shader == null)
                    {
                        Debug.LogWarning("[Skybound] Shader not found: " + shaderNames[n]);
                        continue;
                    }

                    bool present = false;
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                        {
                            present = true;
                            break;
                        }
                    }
                    if (present) continue;

                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                    Debug.Log("[Skybound] Added always-included shader: " + shaderNames[n]);
                }

                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Skybound] Could not update always-included shaders: " + exception.Message);
            }
        }

        [MenuItem("Skybound/Setup/Generate App Icon")]
        public static void GenerateIcon()
        {
            Directory.CreateDirectory("Assets/Art");
            Color32[] pixels = BuildIconPixels();
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogError("[Skybound] Could not load generated icon.");
                return;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);
            Debug.Log("[Skybound] App icon generated at " + IconPath);
        }

        private static Color32[] BuildIconPixels()
        {
            const int S = IconSize;
            Color32[] px = new Color32[S * S];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color top = Palette.Hex("#0C1B30");
            Color bottom = Palette.Hex("#040912");
            Color cyan = Palette.Primary;
            Color purple = Palette.Secondary;
            Vector2 center = new Vector2(256f, 256f);
            float coinRadius = 104f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float fx = x + 0.5f;
                    float fy = y + 0.5f;

                    float dx = Mathf.Max(Mathf.Abs(fx - 256f) - 146f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(fy - 256f) - 146f, 0f);
                    if (dx * dx + dy * dy > 110f * 110f)
                    {
                        px[y * S + x] = clear;
                        continue;
                    }

                    Color color = Color.Lerp(bottom, top, fy / S);

                    float glowDistance = Vector2.Distance(new Vector2(fx, fy), new Vector2(148f, 378f)) / 300f;
                    float glow = Mathf.Clamp01(1f - glowDistance);
                    color += cyan * (glow * glow * 0.22f);

                    float purpleDistance = Vector2.Distance(new Vector2(fx, fy), new Vector2(392f, 92f)) / 320f;
                    float purpleGlow = Mathf.Clamp01(1f - purpleDistance);
                    color += purple * (purpleGlow * purpleGlow * 0.18f);

                    color.a = 1f;
                    px[y * S + x] = color;
                }
            }

            PaintRing(px, S, center, 164f, 8f, new Color(cyan.r, cyan.g, cyan.b, 0.9f));
            PaintRing(px, S, center, 182f, 3f, new Color(purple.r, purple.g, purple.b, 0.55f));
            PaintSoftCircle(px, S, center, 142f, new Color(cyan.r, cyan.g, cyan.b, 0.12f));

            // Coin.
            float coinTop = center.y + coinRadius;
            float coinBottom = center.y - coinRadius;
            Color goldDark = Palette.Hex("#8A5A00");
            Color goldMid = Palette.Accent;
            Color goldLight = Palette.Hex("#FFE68A");

            for (int y = (int)(center.y - coinRadius - 10f); y <= center.y + coinRadius + 10f; y++)
            {
                for (int x = (int)(center.x - coinRadius - 10f); x <= center.x + coinRadius + 10f; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    if (dist > coinRadius + 9f) continue;
                    int index = y * S + x;

                    Color color;
                    if (dist > coinRadius)
                    {
                        color = goldDark;
                    }
                    else if (dist > coinRadius - 13f)
                    {
                        color = Color.Lerp(goldDark, goldMid, 0.55f);
                    }
                    else
                    {
                        float v = Mathf.InverseLerp(coinBottom, coinTop, y);
                        color = Color.Lerp(goldMid, goldLight, v * 0.75f);
                        float highlight = Mathf.Clamp01(1f - Vector2.Distance(
                            new Vector2(x + 0.5f, y + 0.5f), new Vector2(center.x - 34f, center.y + 42f)) / 80f);
                        color = Color.Lerp(color, Color.white, highlight * 0.25f);
                    }

                    color.a = 1f;
                    px[index] = Blend(px[index], color);
                }
            }

            // Engraved inner ring.
            PaintRing(px, S, center, 82f, 5f, new Color(0.42f, 0.26f, 0f, 0.8f));

            // Lightning bolt: cyan outline plus navy core.
            Vector2[] bolt =
            {
                new Vector2(282f, 358f), new Vector2(206f, 262f), new Vector2(250f, 262f),
                new Vector2(224f, 152f), new Vector2(314f, 272f), new Vector2(268f, 272f)
            };
            PaintPolygon(px, S, bolt, new Color(cyan.r, cyan.g, cyan.b, 1f));
            Vector2[] boltCore = ScalePolygon(bolt, new Vector2(258f, 258f), 0.84f);
            PaintPolygon(px, S, boltCore, Palette.Hex("#0A1524"));

            // Sparkles.
            DrawSparkle(px, S, new Vector2(112f, 384f), 16f, Color.white);
            DrawSparkle(px, S, new Vector2(412f, 340f), 12f, cyan);
            DrawSparkle(px, S, new Vector2(398f, 148f), 9f, purple);

            return px;
        }

        private static Vector2[] ScalePolygon(Vector2[] points, Vector2 center, float scale)
        {
            Vector2[] result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                result[i] = center + (points[i] - center) * scale;
            return result;
        }

        private static void DrawSparkle(Color32[] px, int size, Vector2 center, float radius, Color color)
        {
            Vector2[] diamond =
            {
                new Vector2(center.x, center.y + radius),
                new Vector2(center.x + radius * 0.32f, center.y),
                new Vector2(center.x, center.y - radius),
                new Vector2(center.x - radius * 0.32f, center.y)
            };
            PaintPolygon(px, size, diamond, color);
        }

        private static void PaintSoftCircle(Color32[] px, int size, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float falloff = Mathf.Clamp01(1f - distance / radius);
                    if (falloff <= 0f) continue;
                    Color c = color;
                    c.a *= falloff * falloff;
                    px[y * size + x] = Blend(px[y * size + x], c);
                }
            }
        }

        private static void PaintRing(Color32[] px, int size, Vector2 center, float radius, float thickness,
            Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - thickness));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - thickness));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius + thickness));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float coverage = Mathf.Clamp01(thickness * 0.5f + 0.75f - Mathf.Abs(distance - radius));
                    if (coverage <= 0f) continue;
                    Color c = color;
                    c.a *= coverage;
                    px[y * size + x] = Blend(px[y * size + x], c);
                }
            }
        }

        private static void PaintPolygon(Color32[] px, int size, Vector2[] points, Color color)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxY));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!PointInPolygon(points, x + 0.5f, y + 0.5f)) continue;
                    px[y * size + x] = Blend(px[y * size + x], color);
                }
            }
        }

        private static bool PointInPolygon(Vector2[] points, float x, float y)
        {
            bool inside = false;
            int count = points.Length;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if ((points[i].y > y) != (points[j].y > y) &&
                    x < (points[j].x - points[i].x) * (y - points[i].y) /
                    (points[j].y - points[i].y) + points[i].x)
                    inside = !inside;
            }
            return inside;
        }

        private static Color32 Blend(Color32 background, Color foreground)
        {
            float alpha = Mathf.Clamp01(foreground.a);
            float inverse = 1f - alpha;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(foreground.r * 255f * alpha + background.r * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(foreground.g * 255f * alpha + background.g * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(foreground.b * 255f * alpha + background.b * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((alpha + background.a / 255f * inverse) * 255f), 0, 255));
        }

        // ------------------------------------------------------------------ BUILD

        [MenuItem("Skybound/Build/Android APK")]
        public static void BuildApk()
        {
            ApplyAll();
            BuildAndroid(false);
        }

        [MenuItem("Skybound/Build/Android AAB (Play Store)")]
        public static void BuildAab()
        {
            ApplyAll();
            BuildAndroid(true);
        }

        private static void BuildAndroid(bool appBundle)
        {
            if (!File.Exists(ScenePath))
            {
                GenerateScene();
            }

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android"));
            Directory.CreateDirectory(folder);
            string extension = appBundle ? ".aab" : ".apk";
            string outputPath = Path.Combine(folder, "SkyboundCoinRush-" + GameConfig.Version + extension);

            EditorUserBuildSettings.buildAppBundle = appBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log("[Skybound] Building " + (appBundle ? "AAB" : "APK") + " -> " + outputPath);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Skybound] Build succeeded: " + outputPath + " (" +
                          (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB) in " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[Skybound] Build failed: " + summary.result + " (" + summary.totalErrors + " errors)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
