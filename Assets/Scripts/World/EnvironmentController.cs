using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public class EnvironmentController : MonoBehaviour
    {
        public static EnvironmentController Instance { get; private set; }

        public ThemeId CurrentTheme { get { return targetTheme; } }
        public WeatherKind CurrentWeather { get { return targetWeather; } }
        public Color CurrentCityTint { get { return currentCity; } }
        public Color CurrentWindowColor { get { return currentWindow; } }
        public float CurrentWindowAlpha { get { return currentWindowAlpha; } }
        public Color CurrentRoadTint { get { return currentRoad; } }
        public float RainAmount { get { return rainAmount; } }

        public event Action LightningStrike;

        private CameraRig cameraRig;
        private ParallaxCity city;
        private RoadRenderer road;

        private Material skyMaterial;
        private Transform skyRoot;
        private SpriteRenderer sunGlow;
        private SpriteRenderer sunDisc;
        private SpriteRenderer lightningOverlay;
        private SpriteRenderer fogBand;
        private readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> mountains = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> clouds = new List<SpriteRenderer>();
        private readonly List<SkyLayer> skyLayers = new List<SkyLayer>();
        private readonly List<Flyer> flyers = new List<Flyer>();
        private readonly List<RainDrop> rainDrops = new List<RainDrop>();
        private readonly List<Vector3> starBase = new List<Vector3>();
        private readonly List<float> starSeed = new List<float>();

        private ThemeId targetTheme = ThemeId.Sunset;
        private WeatherKind targetWeather = WeatherKind.Clear;
        private ThemePalette targetPalette;
        private Color currentTop;
        private Color currentBottom;
        private Color currentSun;
        private Color currentCity;
        private Color currentWindow;
        private Color currentRoad;
        private Color currentFog;
        private float currentStarAlpha;
        private float currentWindowAlpha;
        private float rainAmount;
        private float targetRain;
        private float fogAmount;
        private float targetFog;
        private float cloudAmount = 0.5f;
        private float targetCloud;
        private float lightningTimer = 6f;
        private float flash;
        private float distance;
        private float scroll;
        private float menuTime;
        private bool useDistanceCycle;
        private bool menuMode = true;
        private LevelDef activeLevel;
        private int cycleIndex = -1;

        public static EnvironmentController Create(Transform parent, CameraRig cameraRig)
        {
            GameObject go = RuntimeUtil.Create("Environment", parent);
            EnvironmentController env = go.AddComponent<EnvironmentController>();
            env.cameraRig = cameraRig;
            env.Build();
            return env;
        }

        private void Awake()
        {
            Instance = this;
        }

        public void Wire(ParallaxCity parallaxCity, RoadRenderer roadRenderer)
        {
            city = parallaxCity;
            road = roadRenderer;
        }

        private void Build()
        {
            skyRoot = RuntimeUtil.Create("SkyRoot", transform).transform;

            // Full-screen gradient sky.
            skyMaterial = Art.GradientMaterial != null ? new Material(Art.GradientMaterial) : null;
            if (skyMaterial != null)
            {
                Mesh quad = BuildQuad(44f, 24f);
                MeshRenderer sky = RuntimeUtil.CreateMesh("SkyGradient", skyRoot, quad, skyMaterial, -1000);
                sky.transform.localPosition = new Vector3(0f, 0f, 12f);
                currentTop = Palette.Hex("#231452");
                currentBottom = Palette.Hex("#FF6B5B");
                skyMaterial.SetColor("_TopColor", currentTop);
                skyMaterial.SetColor("_BottomColor", currentBottom);
                skyMaterial.SetFloat("_HorizonPower", 1.35f);
            }
            else
            {
                // The custom shader was stripped from the build. Use the camera
                // clear colour instead of drawing an unshaded (magenta) quad.
                currentTop = Palette.Hex("#231452");
                currentBottom = Palette.Hex("#FF6B5B");
                if (cameraRig != null && cameraRig.Camera != null)
                    cameraRig.Camera.backgroundColor = currentBottom;
                Debug.LogWarning("[Skybound] Gradient shader missing; falling back to a solid sky colour.");
            }

            // Sun / moon.
            sunGlow = RuntimeUtil.CreateGlow("SunGlow", skyRoot, -940, new Color(1f, 0.7f, 0.4f, 0.45f),
                new Vector3(5.4f, 1.2f, 0f), 7f);
            sunDisc = RuntimeUtil.CreateSprite("SunDisc", skyRoot, Art.SoftCircle(), -939,
                new Color(1f, 0.9f, 0.7f, 0.9f), new Vector3(5.4f, 1.2f, 0f), new Vector3(1.7f, 1.7f, 1f));

            // Stars.
            System.Random rng = new System.Random(2026);
            for (int i = 0; i < 54; i++)
            {
                float x = -13f + (float)rng.NextDouble() * 26f;
                float y = 0.5f + (float)rng.NextDouble() * 5.0f;
                float size = 0.028f + (float)rng.NextDouble() * 0.055f;
                SpriteRenderer star = RuntimeUtil.CreateSprite("Star" + i, skyRoot, Art.Circle(32), -960,
                    new Color(1f, 1f, 1f, 0f), new Vector3(x, y, 0f), new Vector3(size, size, 1f), true);
                stars.Add(star);
                starBase.Add(new Vector3(x, y, 0f));
                starSeed.Add((float)rng.NextDouble() * 6.28f);
            }

            // Mountains (two copies for a seamless parallax).
            Sprite mountainSprite = Art.Mountain(91);
            for (int i = 0; i < 2; i++)
            {
                SpriteRenderer m = RuntimeUtil.CreateSprite("Mountain" + i, skyRoot, mountainSprite, -850,
                    new Color(0.3f, 0.35f, 0.55f, 0.75f), new Vector3(i * 20.3f, -1.55f, 0f),
                    new Vector3(2.0f, 1.5f, 1f));
                mountains.Add(m);
            }

            // Three skyline layers.
            BuildSkyLayer(-760, -1.1f, 2.5f, 1.3f, 0.018f, 0.72f, 0.45f, 11);
            BuildSkyLayer(-700, -1.9f, 2.15f, 1.55f, 0.042f, 0.92f, 0.70f, 27);
            BuildSkyLayer(-640, -2.75f, 1.85f, 1.85f, 0.076f, 1f, 1f, 53);

            // Fog band along the horizon.
            fogBand = RuntimeUtil.CreateSprite("FogBand", skyRoot, Art.Cloud(7), -600,
                new Color(0.7f, 0.8f, 0.9f, 0.15f), new Vector3(0f, 0.1f, 0f),
                new Vector3(24f, 2.2f, 1f));
            fogBand.gameObject.name = "FogBand";

            // Cloud bank.
            Sprite cloudSprite = Art.Cloud(2);
            for (int i = 0; i < 14; i++)
            {
                float x = -15f + (float)rng.NextDouble() * 30f;
                float y = -0.2f + (float)rng.NextDouble() * 4.2f;
                float scale = 0.8f + (float)rng.NextDouble() * 2.4f;
                SpriteRenderer c = RuntimeUtil.CreateSprite("Cloud" + i, skyRoot, cloudSprite, -670,
                    new Color(1f, 1f, 1f, 0.2f), new Vector3(x, y, 0f),
                    new Vector3(scale, scale * 0.6f, 1f));
                clouds.Add(c);
            }

            // Flying traffic.
            for (int i = 0; i < 7; i++)
            {
                float x = -14f + (float)rng.NextDouble() * 28f;
                float y = 1.2f + (float)rng.NextDouble() * 3.5f;
                Flyer flyer = new Flyer();
                flyer.Body = RuntimeUtil.CreateSprite("Flyer" + i, skyRoot, Art.RoundedSprite(48, 12, 6), -620,
                    new Color(0.3f, 0.95f, 1f, 0.85f), new Vector3(x, y, 0f),
                    new Vector3(0.38f, 0.13f, 1f), true);
                flyer.Trail = RuntimeUtil.CreateGlow("FlyerTrail" + i, skyRoot, -621,
                    new Color(0.2f, 0.7f, 1f, 0.18f), new Vector3(x - 0.4f, y, 0f), 1.1f);
                flyer.Speed = 0.8f + (float)rng.NextDouble() * 2.2f;
                flyer.Phase = (float)rng.NextDouble() * 6.28f;
                flyers.Add(flyer);
            }

            // Rain.
            Sprite dropSprite = Art.Raindrop();
            for (int i = 0; i < 90; i++)
            {
                RainDrop drop = new RainDrop();
                drop.Renderer = RuntimeUtil.CreateSprite("Rain" + i, skyRoot, dropSprite, 880,
                    new Color(0.75f, 0.9f, 1f, 0.0f), new Vector3(0f, 0f, 0f), Vector3.zero, true);
                drop.Renderer.gameObject.SetActive(false);
                drop.X = -12f + (float)rng.NextDouble() * 24f;
                drop.Y = -6f + (float)rng.NextDouble() * 13f;
                drop.Speed = 12f + (float)rng.NextDouble() * 10f;
                drop.Length = 0.45f + (float)rng.NextDouble() * 0.7f;
                drop.Alpha = 0.35f + (float)rng.NextDouble() * 0.45f;
                rainDrops.Add(drop);
            }

            // Lightning overlay.
            lightningOverlay = RuntimeUtil.CreateSprite("LightningFlash", skyRoot, Art.White, 950,
                new Color(1f, 1f, 1f, 0f), Vector3.zero, new Vector3(34f, 20f, 1f), false);
            lightningOverlay.gameObject.SetActive(false);

            SetTheme(ThemeId.Sunset, true);
            SetWeather(WeatherKind.Clear, true);
        }

        private void BuildSkyLayer(int baseOrder, float centerY, float scaleX, float scaleY,
            float scrollSpeed, float alpha, float windowScale, int seed)
        {
            SkylineSet set = Art.Skyline(seed);
            SkyLayer layer = new SkyLayer();
            layer.ScrollSpeed = scrollSpeed;
            layer.Width = 10.24f * scaleX;
            layer.Alpha = alpha;
            for (int i = 0; i < 2; i++)
            {
                SpriteRenderer sil = RuntimeUtil.CreateSprite("SkylineSil" + baseOrder + "_" + i, skyRoot,
                    set.Silhouette, baseOrder, new Color(0.3f, 0.35f, 0.5f, alpha),
                    new Vector3(i * layer.Width, centerY, 0f), new Vector3(scaleX, scaleY, 1f));
                SpriteRenderer win = RuntimeUtil.CreateSprite("SkylineWin" + baseOrder + "_" + i, skyRoot,
                    set.Windows, baseOrder + 1, new Color(0.7f, 0.9f, 1f, 0.3f * windowScale),
                    new Vector3(i * layer.Width, centerY, 0f), new Vector3(scaleX, scaleY, 1f), true);
                layer.Silhouettes.Add(sil);
                layer.Windows.Add(win);
            }
            skyLayers.Add(layer);
        }

        public void SetMenuMode(bool menu)
        {
            menuMode = menu;
            if (menu)
            {
                useDistanceCycle = false;
                SetTheme(ThemeId.Sunset, false);
                SetWeather(WeatherKind.Clear, false);
            }
        }

        public void ConfigureForLevel(LevelDef level)
        {
            activeLevel = level;
            useDistanceCycle = false;
            menuMode = false;
            SetTheme(level.Theme, false);
            SetWeather(level.Weather, false);
            cycleIndex = -1;
        }

        public void ConfigureEndless()
        {
            activeLevel = null;
            useDistanceCycle = true;
            menuMode = false;
            cycleIndex = -1;
            SetTheme(ThemeId.Day, false);
            SetWeather(WeatherKind.Clear, false);
        }

        public void SetTheme(ThemeId theme, bool instant)
        {
            targetTheme = theme;
            targetPalette = GameConfig.GetTheme(theme);
            if (instant)
            {
                currentTop = targetPalette.SkyTop;
                currentBottom = targetPalette.SkyBottom;
                currentSun = targetPalette.Sun;
                currentCity = targetPalette.CityTint;
                currentWindow = targetPalette.WindowGlow;
                currentRoad = targetPalette.RoadTint;
                currentFog = targetPalette.Fog;
                currentStarAlpha = targetPalette.StarAlpha;
                currentWindowAlpha = targetPalette.WindowAlpha;
                ApplyTheme();
            }
        }

        public void SetWeather(WeatherKind weather, bool instant)
        {
            targetWeather = weather;
            switch (weather)
            {
                case WeatherKind.Rain:
                    targetRain = 1f; targetFog = 0.35f; targetCloud = 0.85f; break;
                case WeatherKind.Storm:
                    targetRain = 1.55f; targetFog = 0.5f; targetCloud = 1f; break;
                case WeatherKind.Fog:
                    targetRain = 0f; targetFog = 1f; targetCloud = 0.7f; break;
                case WeatherKind.Cloudy:
                    targetRain = 0f; targetFog = 0.2f; targetCloud = 0.9f; break;
                default:
                    targetRain = 0f; targetFog = 0f; targetCloud = 0.35f; break;
            }
            if (instant)
            {
                rainAmount = targetRain;
                fogAmount = targetFog;
                cloudAmount = targetCloud;
                ApplyWeather();
            }
        }

        public void OnDistance(float meters, float worldSpeed)
        {
            distance = meters;
            if (useDistanceCycle && !menuMode)
            {
                int index = Mathf.FloorToInt(distance / 520f);
                if (index != cycleIndex)
                {
                    cycleIndex = index;
                    ThemeId[] cycle = { ThemeId.Day, ThemeId.Sunset, ThemeId.Night, ThemeId.NeonNight, ThemeId.Dawn };
                    SetTheme(cycle[index % cycle.Length], false);
                    if (index > 0)
                    {
                        WeatherKind[] weatherCycle =
                        {
                            WeatherKind.Clear, WeatherKind.Cloudy, WeatherKind.Rain,
                            WeatherKind.Fog, WeatherKind.Storm
                        };
                        SetWeather(weatherCycle[(index / 2) % weatherCycle.Length], false);
                    }
                }
            }
            scroll = distance;
        }

        public void SetWeatherImmediate(WeatherKind weather)
        {
            SetWeather(weather, false);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                dt = Time.deltaTime;
            menuTime += dt;

            float blend = 1f - Mathf.Exp(-1.7f * dt);
            if (targetPalette != null)
            {
                currentTop = Color.Lerp(currentTop, targetPalette.SkyTop, blend);
                currentBottom = Color.Lerp(currentBottom, targetPalette.SkyBottom, blend);
                currentSun = Color.Lerp(currentSun, targetPalette.Sun, blend);
                currentCity = Color.Lerp(currentCity, targetPalette.CityTint, blend);
                currentWindow = Color.Lerp(currentWindow, targetPalette.WindowGlow, blend);
                currentRoad = Color.Lerp(currentRoad, targetPalette.RoadTint, blend);
                currentFog = Color.Lerp(currentFog, targetPalette.Fog, blend);
                currentStarAlpha = Mathf.Lerp(currentStarAlpha, targetPalette.StarAlpha, blend);
                currentWindowAlpha = Mathf.Lerp(currentWindowAlpha, targetPalette.WindowAlpha, blend);
                ApplyTheme();
            }

            rainAmount = Mathf.Lerp(rainAmount, targetRain, blend * 0.7f);
            fogAmount = Mathf.Lerp(fogAmount, targetFog, blend * 0.7f);
            cloudAmount = Mathf.Lerp(cloudAmount, targetCloud, blend * 0.7f);
            ApplyWeather();
            UpdateSky(dt);
            UpdateRain(dt);
            UpdateLightning(dt);

            if (road != null) road.ApplyPalette(currentRoad, currentTop, currentBottom);
        }

        private void ApplyTheme()
        {
            if (skyMaterial != null)
            {
                skyMaterial.SetColor("_TopColor", currentTop);
                skyMaterial.SetColor("_BottomColor", currentBottom);
            }
            else if (cameraRig != null && cameraRig.Camera != null)
            {
                cameraRig.Camera.backgroundColor = currentBottom;
            }
            if (sunGlow != null)
            {
                sunGlow.color = RuntimeUtil.WithAlpha(currentSun, 0.38f);
                sunDisc.color = RuntimeUtil.WithAlpha(currentSun, 0.9f);
                float sunX = Mathf.Lerp(5.4f, 4.8f, currentStarAlpha);
                float sunY = Mathf.Lerp(1.15f, -0.15f, currentStarAlpha);
                sunGlow.transform.localPosition = new Vector3(sunX, sunY, 0f);
                sunDisc.transform.localPosition = new Vector3(sunX, sunY, 0f);
            }
            if (mountains.Count > 0)
            {
                Color mountainColor = Color.Lerp(currentCity, currentFog, 0.45f);
                mountainColor.a = 0.75f;
                for (int i = 0; i < mountains.Count; i++) mountains[i].color = mountainColor;
            }
            for (int l = 0; l < skyLayers.Count; l++)
            {
                SkyLayer layer = skyLayers[l];
                float layerFade = 0.55f + l * 0.18f;
                Color sil = Color.Lerp(currentCity, currentFog, 0.25f + (2 - l) * 0.13f);
                sil.a = layer.Alpha * (0.8f - l * 0.08f);
                for (int i = 0; i < layer.Silhouettes.Count; i++) layer.Silhouettes[i].color = sil;
                Color win = currentWindow;
                win.a = currentWindowAlpha * layerFade;
                for (int i = 0; i < layer.Windows.Count; i++) layer.Windows[i].color = win;
            }
            for (int i = 0; i < clouds.Count; i++)
            {
                Color c = Color.Lerp(Color.white, currentTop, 0.2f);
                c.a = 0.12f + cloudAmount * 0.22f;
                clouds[i].color = c;
            }
            for (int i = 0; i < stars.Count; i++)
            {
                float twinkle = 0.65f + 0.35f * Mathf.Sin(menuTime * 1.7f + starSeed[i]);
                stars[i].color = new Color(1f, 1f, 1f, currentStarAlpha * twinkle * 0.85f);
            }
            if (city != null) city.ApplyPalette(currentCity, currentWindow, currentWindowAlpha, currentFog);
        }

        private void ApplyWeather()
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                Color c = clouds[i].color;
                c.a = 0.1f + cloudAmount * 0.25f;
                clouds[i].color = c;
            }
            if (lightningOverlay != null && !lightningOverlay.gameObject.activeSelf && flash > 0.001f)
                lightningOverlay.gameObject.SetActive(true);
            if (fogBand != null)
            {
                Color fc = Color.Lerp(currentFog, currentBottom, 0.25f);
                fc.a = fogAmount * 0.28f;
                fogBand.color = fc;
                fogBand.transform.localScale = new Vector3(24f, 2f + fogAmount * 3.5f, 1f);
            }
        }

        private void UpdateSky(float dt)
        {
            float scrollMul = Mathf.Lerp(0.02f, 0.3f, Mathf.Clamp01(distance / 1200f));
            for (int l = 0; l < skyLayers.Count; l++)
            {
                SkyLayer layer = skyLayers[l];
                float move = scroll * layer.ScrollSpeed * scrollMul;
                for (int i = 0; i < layer.Silhouettes.Count; i++)
                {
                    Transform t = layer.Silhouettes[i].transform;
                    float x = i * layer.Width - Mathf.Repeat(move, layer.Width);
                    t.localPosition = new Vector3(x, t.localPosition.y, 0f);
                    Transform wt = layer.Windows[i].transform;
                    wt.localPosition = new Vector3(x, t.localPosition.y, 0f);
                }
            }
            for (int i = 0; i < mountains.Count; i++)
            {
                float move = Mathf.Repeat(scroll * 0.008f, 20.3f);
                mountains[i].transform.localPosition = new Vector3(i * 20.3f - move, -1.55f, 0f);
            }
            for (int i = 0; i < clouds.Count; i++)
            {
                Transform t = clouds[i].transform;
                Vector3 p = t.localPosition;
                p.x += (0.08f + i % 3 * 0.05f) * dt;
                if (p.x > 16f) p.x = -16f;
                t.localPosition = p;
            }
            for (int i = 0; i < flyers.Count; i++)
            {
                Flyer f = flyers[i];
                Vector3 p = f.Body.transform.localPosition;
                p.x -= f.Speed * dt;
                if (p.x < -15f)
                {
                    p.x = 15f;
                    p.y = 1.2f + Mathf.Repeat(i * 0.77f + menuTime * 0.13f, 3.6f);
                }
                p.y += Mathf.Sin(menuTime * 2f + f.Phase) * 0.0015f;
                f.Body.transform.localPosition = p;
                f.Trail.transform.localPosition = new Vector3(p.x - 0.55f, p.y - 0.02f, 0f);
            }
        }

        private void UpdateRain(float dt)
        {
            bool active = rainAmount > 0.02f;
            for (int i = 0; i < rainDrops.Count; i++)
            {
                RainDrop drop = rainDrops[i];
                if (!active)
                {
                    if (drop.Renderer.gameObject.activeSelf) drop.Renderer.gameObject.SetActive(false);
                    continue;
                }
                if (!drop.Renderer.gameObject.activeSelf) drop.Renderer.gameObject.SetActive(true);
                drop.Y -= drop.Speed * dt * Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(rainAmount));
                drop.X -= dt * 1.8f;
                if (drop.Y < -6.5f)
                {
                    drop.Y = 6.5f;
                    drop.X = -12f + UnityEngine.Random.value * 24f;
                }
                if (drop.X < -12f) drop.X = 12f;
                drop.Renderer.transform.localPosition = new Vector3(drop.X, drop.Y, 0f);
                float len = drop.Length * Mathf.Lerp(0.4f, 1.2f, Mathf.Clamp01(rainAmount));
                drop.Renderer.transform.localScale = new Vector3(0.07f, len, 1f);
                drop.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                drop.Renderer.color = new Color(0.75f, 0.9f, 1f, drop.Alpha * Mathf.Clamp01(rainAmount));
            }
        }

        private void UpdateLightning(float dt)
        {
            if (flash > 0f)
            {
                flash -= dt * 4f;
                if (flash <= 0f)
                {
                    flash = 0f;
                    if (lightningOverlay != null) lightningOverlay.gameObject.SetActive(false);
                }
                else if (lightningOverlay != null)
                {
                    Color c = lightningOverlay.color;
                    c.a = Mathf.Clamp01(flash) * 0.55f;
                    lightningOverlay.color = c;
                    if (cameraRig != null) cameraRig.AddShake(0.05f, 0.15f);
                }
            }
            if (targetWeather == WeatherKind.Storm)
            {
                lightningTimer -= dt;
                if (lightningTimer <= 0f)
                {
                    lightningTimer = UnityEngine.Random.Range(4.5f, 10f);
                    flash = 1f;
                    if (lightningOverlay != null) lightningOverlay.gameObject.SetActive(true);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayThunder();
                    if (LightningStrike != null) LightningStrike();
                }
            }
            else
            {
                lightningTimer = UnityEngine.Random.Range(5f, 11f);
            }
        }

        private static Mesh BuildQuad(float width, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Skybound Quad";
            float w = width * 0.5f;
            float h = height * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-w, -h, 0f),
                new Vector3(w, -h, 0f),
                new Vector3(w, h, 0f),
                new Vector3(-w, h, 0f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private class SkyLayer
        {
            public readonly List<SpriteRenderer> Silhouettes = new List<SpriteRenderer>();
            public readonly List<SpriteRenderer> Windows = new List<SpriteRenderer>();
            public float ScrollSpeed;
            public float Width;
            public float Alpha;
        }

        private class Flyer
        {
            public SpriteRenderer Body;
            public SpriteRenderer Trail;
            public float Speed;
            public float Phase;
        }

        private class RainDrop
        {
            public SpriteRenderer Renderer;
            public float X;
            public float Y;
            public float Speed;
            public float Length;
            public float Alpha;
        }
    }
}
