using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public class SkylineSet
    {
        public Sprite Silhouette;
        public Sprite Windows;
    }

    public class BuildingSet
    {
        public Sprite Body;
        public Sprite Windows;
    }

    /// <summary>
    /// Runtime procedural art. Everything the game renders is generated here so the
    /// build has no external texture dependencies and stays small and consistent.
    /// </summary>
    public static class Art
    {
        public const float PPU = 100f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static Material spriteMaterial;
        private static Material additiveMaterial;
        private static Material gradientMaterial;
        private static Texture2D whiteTexture;
        private static Sprite whiteSprite;
        private static Sprite shadowSprite;

        private static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }
            return null;
        }

        private static Shader SafeShader(params string[] names)
        {
            Shader shader = FindShader(names);
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            return shader;
        }

        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial == null)
                {
                    Shader shader = SafeShader(
                        "Sprites/Default",
                        "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                        "UI/Default");
                    spriteMaterial = new Material(shader);
                    spriteMaterial.name = "Skybound Sprite Material";
                }
                return spriteMaterial;
            }
        }

        public static Material AdditiveMaterial
        {
            get
            {
                if (additiveMaterial == null)
                {
                    Shader shader = FindShader("Skybound/AdditiveSprite");
                    if (shader == null) shader = SpriteMaterial.shader;
                    additiveMaterial = new Material(shader);
                    additiveMaterial.name = "Skybound Additive Material";
                }
                return additiveMaterial;
            }
        }

        /// <summary>True when the custom gradient shader made it into the build.</summary>
        public static bool HasGradientShader
        {
            get { return FindShader("Skybound/Gradient") != null; }
        }

        /// <summary>
        /// Null when the custom gradient shader is unavailable. Callers must fall
        /// back to a camera clear colour instead of rendering a magenta quad.
        /// </summary>
        public static Material GradientMaterial
        {
            get
            {
                if (gradientMaterial == null)
                {
                    Shader shader = FindShader("Skybound/Gradient");
                    if (shader == null) return null;
                    gradientMaterial = new Material(shader);
                    gradientMaterial.name = "Skybound Gradient Material";
                }
                return gradientMaterial;
            }
        }

        public static Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    Color32[] px = new Color32[16];
                    for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                    whiteTexture.SetPixels32(px);
                    whiteTexture.Apply(false, false);
                    whiteTexture.name = "White";
                    whiteTexture.wrapMode = TextureWrapMode.Clamp;
                }
                return whiteTexture;
            }
        }

        public static Sprite White
        {
            get
            {
                if (whiteSprite == null)
                {
                    whiteSprite = Sprite.Create(WhiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), PPU);
                    whiteSprite.name = "WhiteSprite";
                }
                return whiteSprite;
            }
        }

        public static Sprite Shadow
        {
            get
            {
                if (shadowSprite == null)
                {
                    int w = 128, h = 64;
                    Color32[] px = NewPixels(w, h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float nx = (x - w * 0.5f) / (w * 0.5f);
                            float ny = (y - h * 0.5f) / (h * 0.5f);
                            float d = nx * nx + ny * ny;
                            float a = Mathf.Clamp01(1f - d);
                            a = a * a * 0.55f;
                            px[y * w + x] = new Color32(0, 0, 0, (byte)(a * 255));
                        }
                    }
                    shadowSprite = MakeSprite(px, w, h, "Shadow");
                }
                return shadowSprite;
            }
        }

        private static Color32[] NewPixels(int w, int h)
        {
            return new Color32[w * h];
        }

        private static Sprite MakeSprite(Color32[] px, int w, int h, string name, Vector4 border = default(Vector4))
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }

        private static Sprite Cached(string key, System.Func<Sprite> factory)
        {
            Sprite sprite;
            if (SpriteCache.TryGetValue(key, out sprite) && sprite != null) return sprite;
            sprite = factory();
            SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite RoundedSprite(int width, int height, int radius, bool sliced = false, int border = 32)
        {
            string key = "round_" + width + "_" + height + "_" + radius + (sliced ? "_s" + border : "");
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(width, height);
                DrawRoundedRect(px, width, height, 1f, 1f, width - 1f, height - 1f, radius, Color.white);
                Vector4 b = sliced ? new Vector4(border, border, border, border) : default(Vector4);
                return MakeSprite(px, width, height, key, b);
            });
        }

        public static Sprite RoundedOutline(int width, int height, int radius, int thickness = 3, int border = 32, bool sliced = true)
        {
            string key = "roundout_" + width + "_" + height + "_" + radius + "_" + thickness + (sliced ? "_s" : "");
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(width, height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float d = RoundedBoxDistance(x + 0.5f, y + 0.5f, width, height, radius);
                        float a = Mathf.Clamp01(0.5f - (Mathf.Abs(d + thickness * 0.5f) - thickness * 0.5f));
                        if (d < -0.5f)
                        {
                            float inner = Mathf.Clamp01(0.5f - (Mathf.Abs(d + thickness * 0.5f) - thickness * 0.5f));
                            a = Mathf.Max(a, inner);
                        }
                        if (a > 0.002f) px[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                Vector4 b = sliced ? new Vector4(border, border, border, border) : default(Vector4);
                return MakeSprite(px, width, height, key, b);
            });
        }

        public static Sprite Panel
        {
            get { return RoundedSprite(128, 128, 28, true, 34); }
        }

        public static Sprite PanelSmall
        {
            get { return RoundedSprite(96, 96, 22, true, 26); }
        }

        public static Sprite PanelOutline
        {
            get { return RoundedOutline(128, 128, 28, 2, 34); }
        }

        public static Sprite Pill
        {
            get { return RoundedSprite(128, 64, 31, true, 28); }
        }

        public static Sprite Circle(int size = 128)
        {
            string key = "circle_" + size;
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(size, size);
                DrawCircle(px, size, size, size * 0.5f, size * 0.5f, size * 0.5f - 1f, Color.white);
                return MakeSprite(px, size, size, key);
            });
        }

        public static Sprite Ring(int size = 128, float thickness = 8f)
        {
            string key = "ring_" + size + "_" + thickness;
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(size, size);
                DrawRing(px, size, size, size * 0.5f, size * 0.5f, size * 0.5f - thickness, thickness, Color.white);
                return MakeSprite(px, size, size, key);
            });
        }

        public static Sprite Glow(int size = 256)
        {
            string key = "glow_" + size;
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(size, size);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x - size * 0.5f) / (size * 0.5f);
                        float ny = (y - size * 0.5f) / (size * 0.5f);
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float a = Mathf.Clamp01(1f - d);
                        a = Mathf.Pow(a, 2.4f);
                        if (a > 0.002f) px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                return MakeSprite(px, size, size, key);
            });
        }

        public static Sprite SoftCircle(int size = 128)
        {
            string key = "soft_" + size;
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(size, size);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x - size * 0.5f) / (size * 0.5f);
                        float ny = (y - size * 0.5f) / (size * 0.5f);
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float a = Mathf.Clamp01(1f - d);
                        a = Mathf.SmoothStep(0f, 1f, a);
                        if (a > 0.002f) px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                return MakeSprite(px, size, size, key);
            });
        }

        public static Sprite Capsule(int width, int height)
        {
            int radius = Mathf.Min(width, height) / 2;
            return RoundedSprite(width, height, radius);
        }

        public static Sprite Tapered(int width, int height)
        {
            string key = "taper_" + width + "_" + height;
            return Cached(key, () =>
            {
                Color32[] px = NewPixels(width, height);
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    float w = Mathf.Lerp(width * 0.72f, width * 0.08f, t);
                    float cx = width * 0.5f;
                    for (int x = 0; x < width; x++)
                    {
                        float d = Mathf.Abs(x - cx) - w * 0.5f;
                        float a = Mathf.Clamp01(0.5f - d);
                        if (a > 0.002f) px[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                return MakeSprite(px, width, height, key);
            });
        }

        public static Sprite ShadowEllipse()
        {
            return Shadow;
        }

        // ------------------------------------------------------------------ ICONS

        public static Sprite Icon(string name)
        {
            string key = "icon_" + name;
            return Cached(key, () =>
            {
                int s = 128;
                Color32[] px = NewPixels(s, s);
                Color white = Color.white;
                switch (name)
                {
                    case "pause":
                        DrawRoundedRect(px, s, s, 30, 24, 54, 104, 10, white);
                        DrawRoundedRect(px, s, s, 74, 24, 98, 104, 10, white);
                        break;
                    case "play":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(40, 22), new Vector2(104, 64), new Vector2(40, 106)
                        }, white);
                        break;
                    case "home":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 112), new Vector2(112, 68), new Vector2(98, 68),
                            new Vector2(98, 18), new Vector2(30, 18), new Vector2(30, 68),
                            new Vector2(16, 68)
                        }, white);
                        break;
                    case "retry":
                        DrawRingArc(px, s, s, 64, 64, 40, 12, 30f, 330f, white);
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(88, 84), new Vector2(116, 84), new Vector2(102, 108)
                        }, white);
                        break;
                    case "settings":
                        DrawCircle(px, s, s, 64, 64, 34, white);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * Mathf.PI * 2f / 8f;
                            float cx = 64 + Mathf.Cos(ang) * 44f;
                            float cy = 64 + Mathf.Sin(ang) * 44f;
                            DrawCircle(px, s, s, cx, cy, 12, white);
                        }
                        DrawCircle(px, s, s, 64, 64, 15, new Color(0, 0, 0, 0), true);
                        break;
                    case "close":
                        DrawLine(px, s, s, 36, 36, 92, 92, 11, white);
                        DrawLine(px, s, s, 92, 36, 36, 92, 11, white);
                        break;
                    case "chevron":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(42, 96), new Vector2(86, 64), new Vector2(42, 32)
                        }, white);
                        break;
                    case "arrow_left":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(18, 64), new Vector2(62, 108), new Vector2(62, 84),
                            new Vector2(110, 84), new Vector2(110, 44), new Vector2(62, 44),
                            new Vector2(62, 20)
                        }, white);
                        break;
                    case "arrow_right":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(110, 64), new Vector2(66, 108), new Vector2(66, 84),
                            new Vector2(18, 84), new Vector2(18, 44), new Vector2(66, 44),
                            new Vector2(66, 20)
                        }, white);
                        break;
                    case "arrow_up":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 112), new Vector2(108, 68), new Vector2(84, 68),
                            new Vector2(84, 18), new Vector2(44, 18), new Vector2(44, 68),
                            new Vector2(20, 68)
                        }, white);
                        break;
                    case "arrow_down":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 16), new Vector2(20, 60), new Vector2(44, 60),
                            new Vector2(44, 110), new Vector2(84, 110), new Vector2(84, 60),
                            new Vector2(108, 60)
                        }, white);
                        break;
                    case "magnet":
                        DrawRingArc(px, s, s, 64, 52, 34, 18, 0f, 180f, white);
                        DrawRoundedRect(px, s, s, 22, 28, 48, 60, 10, white);
                        DrawRoundedRect(px, s, s, 80, 28, 106, 60, 10, white);
                        break;
                    case "shield":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 114), new Vector2(106, 96), new Vector2(100, 52),
                            new Vector2(64, 14), new Vector2(28, 52), new Vector2(22, 96)
                        }, white);
                        break;
                    case "boost":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(72, 118), new Vector2(26, 62), new Vector2(58, 62),
                            new Vector2(50, 10), new Vector2(102, 72), new Vector2(68, 72)
                        }, white);
                        break;
                    case "double_coin":
                        DrawCircle(px, s, s, 48, 64, 30, white);
                        DrawCircle(px, s, s, 80, 64, 30, white);
                        DrawCircle(px, s, s, 48, 64, 21, new Color(0, 0, 0, 0), true);
                        DrawCircle(px, s, s, 80, 64, 21, new Color(0, 0, 0, 0), true);
                        break;
                    case "score":
                        DrawStar(px, s, s, 64, 64, 48, 21, 5, white);
                        break;
                    case "slow":
                        DrawRing(px, s, s, 64, 64, 44, 10, white);
                        DrawLine(px, s, s, 64, 64, 64, 94, 8, white);
                        DrawLine(px, s, s, 64, 64, 88, 50, 8, white);
                        break;
                    case "trophy":
                        DrawRoundedRect(px, s, s, 30, 62, 98, 110, 14, white);
                        DrawRingArc(px, s, s, 32, 90, 18, 8, 90f, 270f, white);
                        DrawRingArc(px, s, s, 96, 90, 18, 8, 270f, 90f, white);
                        DrawRoundedRect(px, s, s, 56, 36, 72, 66, 6, white);
                        DrawRoundedRect(px, s, s, 38, 16, 90, 34, 8, white);
                        break;
                    case "lock":
                        DrawRoundedRect(px, s, s, 28, 16, 100, 74, 12, white);
                        DrawRingArc(px, s, s, 64, 74, 24, 9, 0f, 180f, white);
                        break;
                    case "check":
                        DrawLine(px, s, s, 22, 64, 50, 34, 10, white);
                        DrawLine(px, s, s, 50, 34, 106, 96, 10, white);
                        break;
                    case "coin":
                        return Coin(CoinType.Gold);
                    case "music":
                        DrawCircle(px, s, s, 42, 34, 20, white);
                        DrawCircle(px, s, s, 88, 44, 20, white);
                        DrawRoundedRect(px, s, s, 55, 34, 66, 100, 5, white);
                        DrawRoundedRect(px, s, s, 101, 44, 112, 106, 5, white);
                        DrawRoundedRect(px, s, s, 55, 92, 112, 108, 5, white);
                        break;
                    case "vibrate":
                        DrawRoundedRect(px, s, s, 46, 24, 82, 104, 10, white);
                        DrawLine(px, s, s, 22, 48, 22, 80, 6, white);
                        DrawLine(px, s, s, 106, 48, 106, 80, 6, white);
                        DrawLine(px, s, s, 10, 36, 10, 92, 5, white);
                        DrawLine(px, s, s, 118, 36, 118, 92, 5, white);
                        break;
                    case "star":
                        DrawStar(px, s, s, 64, 64, 52, 23, 5, white);
                        break;
                    case "heart":
                        DrawCircle(px, s, s, 44, 80, 31, white);
                        DrawCircle(px, s, s, 84, 80, 31, white);
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(14, 76), new Vector2(114, 76), new Vector2(64, 12)
                        }, white);
                        break;
                    case "diamond":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 12), new Vector2(112, 64), new Vector2(64, 116), new Vector2(16, 64)
                        }, white);
                        break;
                    case "warning":
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 120), new Vector2(122, 16), new Vector2(6, 16)
                        }, white);
                        DrawRoundedRect(px, s, s, 58, 50, 70, 92, 6,
                            new Color(0.02f, 0.04f, 0.09f, 1f));
                        DrawCircle(px, s, s, 64, 34, 6, new Color(0.02f, 0.04f, 0.09f, 1f));
                        break;
                    default:
                        DrawCircle(px, s, s, 64, 64, 40, white);
                        break;
                }
                return MakeSprite(px, s, s, key);
            });
        }

        public static Sprite Coin(CoinType type)
        {
            string key = "coin_" + type;
            return Cached(key, () =>
            {
                int s = 128;
                Color32[] px = NewPixels(s, s);
                Color outer;
                Color main;
                Color highlight;
                switch (type)
                {
                    case CoinType.Bronze:
                        outer = Palette.Hex("#7A4726");
                        main = Palette.Hex("#D08A4E");
                        highlight = Palette.Hex("#FFD7A8");
                        break;
                    case CoinType.Silver:
                        outer = Palette.Hex("#64758A");
                        main = Palette.Hex("#C7D5E5");
                        highlight = Palette.Hex("#FFFFFF");
                        break;
                    case CoinType.Special:
                        outer = Palette.Hex("#7A005F");
                        main = Palette.Hex("#FF5CE1");
                        highlight = Palette.Hex("#FFD1F8");
                        break;
                    default:
                        outer = Palette.Hex("#A86F00");
                        main = Palette.Hex("#FFD54A");
                        highlight = Palette.Hex("#FFF6C2");
                        break;
                }
                DrawCircle(px, s, s, 64, 64, 60, outer);
                DrawCircle(px, s, s, 64, 64, 53, main);
                DrawCircle(px, s, s, 64, 64, 43, outer);
                DrawCircle(px, s, s, 64, 64, 39, main);
                DrawRingArc(px, s, s, 64, 64, 57, 4, 30f, 140f, highlight);
                if (type == CoinType.Special)
                    DrawStar(px, s, s, 64, 64, 32, 14, 4, highlight);
                else
                    DrawPolygon(px, s, s, new[]
                    {
                        new Vector2(64, 34), new Vector2(82, 64), new Vector2(64, 94), new Vector2(46, 64)
                    }, highlight);
                return MakeSprite(px, s, s, key);
            });
        }

        public static Sprite PowerUpBadge(PowerUpType type)
        {
            string key = "badge_" + type;
            return Cached(key, () =>
            {
                int s = 128;
                Color32[] px = NewPixels(s, s);
                Color c = GameConfig.GetPowerUp(type).Color;
                DrawCircle(px, s, s, 64, 64, 60, new Color(0.02f, 0.05f, 0.11f, 1f));
                DrawRing(px, s, s, 64, 64, 57, 5, c);
                Sprite icon = Icon(IconKeyForPowerUp(type));
                // Draw icon from its own pixel source is not possible here; use simple masks.
                switch (type)
                {
                    case PowerUpType.Magnet:
                        DrawRingArc(px, s, s, 64, 54, 30, 15, 0f, 180f, c);
                        DrawRoundedRect(px, s, s, 30, 26, 50, 56, 8, c);
                        DrawRoundedRect(px, s, s, 78, 26, 98, 56, 8, c);
                        break;
                    case PowerUpType.Shield:
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(64, 108), new Vector2(96, 92), new Vector2(90, 52),
                            new Vector2(64, 20), new Vector2(38, 52), new Vector2(32, 92)
                        }, c);
                        break;
                    case PowerUpType.Boost:
                        DrawPolygon(px, s, s, new[]
                        {
                            new Vector2(72, 110), new Vector2(32, 62), new Vector2(60, 62),
                            new Vector2(52, 18), new Vector2(98, 72), new Vector2(68, 72)
                        }, c);
                        break;
                    case PowerUpType.DoubleCoins:
                        DrawCircle(px, s, s, 48, 64, 26, c);
                        DrawCircle(px, s, s, 80, 64, 26, c);
                        DrawCircle(px, s, s, 48, 64, 18, new Color(0.02f, 0.05f, 0.11f, 1f), true);
                        DrawCircle(px, s, s, 80, 64, 18, new Color(0.02f, 0.05f, 0.11f, 1f), true);
                        break;
                    case PowerUpType.ScoreMultiplier:
                        DrawStar(px, s, s, 64, 64, 42, 18, 5, c);
                        break;
                    case PowerUpType.SlowMotion:
                        DrawRing(px, s, s, 64, 64, 40, 9, c);
                        DrawLine(px, s, s, 64, 64, 64, 90, 7, c);
                        DrawLine(px, s, s, 64, 64, 84, 52, 7, c);
                        break;
                }
                return MakeSprite(px, s, s, key);
            });
        }

        public static string IconKeyForPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "magnet";
                case PowerUpType.Shield: return "shield";
                case PowerUpType.Boost: return "boost";
                case PowerUpType.DoubleCoins: return "double_coin";
                case PowerUpType.ScoreMultiplier: return "score";
                default: return "slow";
            }
        }

        // --------------------------------------------------------------- ENVIRONMENT

        public static SkylineSet Skyline(int seed, int width = 1024, int height = 512, int buildingCount = 26)
        {
            string key = "skyline_" + seed + "_" + width + "_" + height + "_" + buildingCount;
            Cached(key, () =>
            {
                // placeholder to keep the cache API consistent
                return White;
            });

            SkylineSet set;
            if (SkylineCache.TryGetValue(key, out set) && set.Silhouette != null) return set;

            Color32[] sil = NewPixels(width, height);
            Color32[] win = NewPixels(width, height);
            System.Random rng = new System.Random(seed);
            int x = 0;
            while (x < width - 4)
            {
                int bw = rng.Next(24, 78);
                if (x + bw > width) bw = width - x - 2;
                int bh = rng.Next((int)(height * 0.28f), (int)(height * 0.92f));
                int top = height - bh;
                Color32 body = new Color32(255, 255, 255, 255);
                for (int yy = 0; yy < bh; yy++)
                {
                    for (int xx = 0; xx < bw; xx++)
                    {
                        int px = x + xx;
                        int py = top + yy;
                        if (px < 0 || px >= width || py < 0 || py >= height) continue;
                        sil[py * width + px] = body;
                    }
                }
                // Antenna / rooftop details
                if (rng.NextDouble() < 0.35f)
                {
                    int ax = x + bw / 2;
                    int ah = rng.Next(10, 36);
                    for (int yy = 0; yy < ah; yy++)
                    {
                        int py = top - yy;
                        if (py >= 0 && ax >= 0 && ax < width) sil[py * width + ax] = body;
                    }
                }
                // Windows
                int cols = Mathf.Max(2, bw / 12);
                int rows = Mathf.Max(2, bh / 18);
                float litChance = 0.28f + (float)rng.NextDouble() * 0.35f;
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        if (rng.NextDouble() > litChance) continue;
                        int wx = x + 4 + col * (bw - 8) / cols;
                        int wy = top + 5 + row * (bh - 10) / rows;
                        int ww = Mathf.Max(2, (bw - 8) / cols - 4);
                        int wh = 2;
                        for (int yy = 0; yy < wh; yy++)
                        {
                            for (int xx = 0; xx < ww; xx++)
                            {
                                int px = wx + xx;
                                int py = wy + yy;
                                if (px >= 0 && px < width && py >= 0 && py < height)
                                {
                                    Color32 c = new Color32(255, 255, 235, 255);
                                    win[py * width + px] = c;
                                }
                            }
                        }
                    }
                }
                x += bw + rng.Next(0, 7);
            }
            set = new SkylineSet
            {
                Silhouette = MakeSprite(sil, width, height, key + "_sil"),
                Windows = MakeSprite(win, width, height, key + "_win")
            };
            SkylineCache[key] = set;
            return set;
        }

        private static readonly Dictionary<string, SkylineSet> SkylineCache = new Dictionary<string, SkylineSet>();
        private static readonly Dictionary<string, BuildingSet> BuildingCache = new Dictionary<string, BuildingSet>();

        public static BuildingSet Building(int variant)
        {
            string key = "building_" + variant;
            BuildingSet set;
            if (BuildingCache.TryGetValue(key, out set) && set.Body != null) return set;

            int w = 96;
            int h = 384;
            Color32[] bodyPx = NewPixels(w, h);
            Color32[] winPx = NewPixels(w, h);
            Color white = Color.white;
            DrawRoundedRect(bodyPx, w, h, 2, 2, w - 2, h - 2, 5, white);
            System.Random rng = new System.Random(500 + variant * 97);
            int cols = 4;
            int rows = 12;
            float lit = 0.30f + (variant % 4) * 0.08f;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (rng.NextDouble() > lit) continue;
                    int wx = 10 + col * 20;
                    int wy = 24 + row * 28;
                    for (int yy = 0; yy < 9; yy++)
                    {
                        for (int xx = 0; xx < 10; xx++)
                        {
                            int px = wx + xx;
                            int py = wy + yy;
                            if (px < w && py < h) winPx[py * w + px] = new Color32(255, 255, 240, 255);
                        }
                    }
                }
            }
            // Rooftop ledge
            DrawRoundedRect(bodyPx, w, h, 4, h - 10, w - 4, h - 2, 3, white);
            set = new BuildingSet
            {
                Body = MakeSprite(bodyPx, w, h, key + "_body"),
                Windows = MakeSprite(winPx, w, h, key + "_win")
            };
            BuildingCache[key] = set;
            return set;
        }

        public static Sprite Mountain(int seed)
        {
            string key = "mountain_" + seed;
            return Cached(key, () =>
            {
                int w = 1024, h = 320;
                Color32[] px = NewPixels(w, h);
                System.Random rng = new System.Random(seed);
                int points = 18;
                float[] heights = new float[points + 1];
                for (int i = 0; i <= points; i++)
                {
                    heights[i] = (float)(0.25 + rng.NextDouble() * 0.7) * h;
                }
                heights[0] = h * 0.45f;
                heights[points] = h * 0.45f;
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)(w - 1) * points;
                    int i0 = Mathf.Clamp(Mathf.FloorToInt(t), 0, points);
                    int i1 = Mathf.Clamp(i0 + 1, 0, points);
                    float f = t - i0;
                    float hh = Mathf.Lerp(heights[i0], heights[i1], f);
                    for (int y = 0; y < hh; y++)
                    {
                        px[y * w + x] = new Color32(255, 255, 255, 255);
                    }
                }
                return MakeSprite(px, w, h, key);
            });
        }

        public static Sprite Cloud(int variant)
        {
            string key = "cloud_" + variant;
            return Cached(key, () =>
            {
                int w = 256, h = 128;
                Color32[] px = NewPixels(w, h);
                System.Random rng = new System.Random(1200 + variant * 37);
                int blobs = 7 + variant % 3;
                for (int i = 0; i < blobs; i++)
                {
                    float cx = 40 + (float)rng.NextDouble() * (w - 80);
                    float cy = 38 + (float)rng.NextDouble() * 44;
                    float r = 22 + (float)rng.NextDouble() * 26;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float dx = (x - cx) / r;
                            float dy = (y - cy) / r;
                            float d = dx * dx + dy * dy;
                            if (d >= 1f) continue;
                            float a = Mathf.Clamp01(1f - d);
                            a = Mathf.SmoothStep(0f, 1f, a) * 0.5f;
                            int idx = y * w + x;
                            float existing = px[idx].a / 255f;
                            px[idx] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(existing + a) * 255));
                        }
                    }
                }
                return MakeSprite(px, w, h, key);
            });
        }

        public static Texture2D RoadTexture()
        {
            const string key = "road_tex";
            Texture2D cached;
            if (TextureCache.TryGetValue(key, out cached) && cached != null) return cached;
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = key;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[size * size];
            System.Random rng = new System.Random(42);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = (float)rng.NextDouble();
                    int v = 24 + Mathf.RoundToInt(n * 18f);
                    Color32 c = new Color32((byte)v, (byte)(v + 4), (byte)(v + 12), 255);
                    px[y * size + x] = c;
                }
            }
            // Faint long scuff marks
            for (int i = 0; i < 24; i++)
            {
                int x0 = rng.Next(0, size);
                int y0 = rng.Next(0, size);
                int len = rng.Next(20, 90);
                for (int l = 0; l < len; l++)
                {
                    int yy = (y0 + l) % size;
                    px[yy * size + x0] = new Color32(40, 45, 55, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            TextureCache[key] = tex;
            return tex;
        }

        public static Sprite Raindrop()
        {
            return Cached("raindrop", () =>
            {
                int w = 8, h = 48;
                Color32[] px = NewPixels(w, h);
                for (int y = 0; y < h; y++)
                {
                    float t = y / (float)(h - 1);
                    float a = Mathf.Sin(t * Mathf.PI) * 0.85f;
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x - (w - 1) * 0.5f) / (w * 0.5f);
                        float aa = Mathf.Clamp01(1f - dx * dx) * a;
                        px[y * w + x] = new Color32(200, 235, 255, (byte)(aa * 255));
                    }
                }
                return MakeSprite(px, w, h, "Raindrop");
            });
        }

        public static Sprite Spark()
        {
            return Cached("spark", () =>
            {
                int s = 32;
                Color32[] px = NewPixels(s, s);
                DrawStar(px, s, s, 16, 16, 15, 4, 4, Color.white);
                return MakeSprite(px, s, s, "Spark");
            });
        }

        public static Sprite Streak()
        {
            return Cached("streak", () =>
            {
                int w = 256, h = 10;
                Color32[] px = NewPixels(w, h);
                for (int y = 0; y < h; y++)
                {
                    float vy = 1f - Mathf.Abs(y - h * 0.5f) / (h * 0.5f);
                    for (int x = 0; x < w; x++)
                    {
                        float vx = Mathf.Sin(x / (float)(w - 1) * Mathf.PI);
                        float a = Mathf.Clamp01(vx * vy);
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                return MakeSprite(px, w, h, "Streak");
            });
        }

        public static Sprite LaserBeam()
        {
            return Cached("laser_beam", () =>
            {
                int w = 256, h = 24;
                Color32[] px = NewPixels(w, h);
                for (int y = 0; y < h; y++)
                {
                    float vy = 1f - Mathf.Abs(y - h * 0.5f) / (h * 0.5f);
                    for (int x = 0; x < w; x++)
                    {
                        float a = Mathf.Clamp01(vy);
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                return MakeSprite(px, w, h, "LaserBeam");
            });
        }

        public static Sprite DiamondSmall()
        {
            return Icon("diamond");
        }

        public static Sprite StarSmall()
        {
            return Icon("star");
        }

        // --------------------------------------------------------------- DRAWING

        public static void DrawCircle(Color32[] px, int w, int h, float cx, float cy, float radius, Color color, bool erase = false)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 2));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radius + 2));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 2));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radius + 2));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0.003f) continue;
                    int idx = y * w + x;
                    if (erase) Erase(ref px[idx], a);
                    else Blend(ref px[idx], color, a * color.a);
                }
            }
        }

        public static void DrawRing(Color32[] px, int w, int h, float cx, float cy, float radius, float thickness, Color color)
        {
            DrawRingArc(px, w, h, cx, cy, radius, thickness, 0f, 360f, color);
        }

        public static void DrawRingArc(Color32[] px, int w, int h, float cx, float cy, float radius,
            float thickness, float startDeg, float endDeg, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - thickness));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radius + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - thickness));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radius + thickness));
            float start = startDeg * Mathf.Deg2Rad;
            float end = endDeg * Mathf.Deg2Rad;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float d = Mathf.Abs(dist - radius) - thickness * 0.5f;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0.003f) continue;
                    float angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f) angle += Mathf.PI * 2f;
                    float s = start % (Mathf.PI * 2f);
                    if (s < 0) s += Mathf.PI * 2f;
                    float e = end % (Mathf.PI * 2f);
                    if (e < 0) e += Mathf.PI * 2f;
                    bool inside;
                    if (end - start >= Mathf.PI * 2f - 0.01f) inside = true;
                    else if (s <= e) inside = angle >= s && angle <= e;
                    else inside = angle >= s || angle <= e;
                    if (!inside) continue;
                    Blend(ref px[y * w + x], color, a * color.a);
                }
            }
        }

        public static void DrawRoundedRect(Color32[] px, int w, int h, float x0, float y0, float x1, float y1,
            float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(x0 - 2));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(x1 + 2));
            int minY = Mathf.Max(0, Mathf.FloorToInt(y0 - 2));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(y1 + 2));
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float halfW = (x1 - x0) * 0.5f;
            float halfH = (y1 - y0) * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = RoundedBoxDistance(x + 0.5f, y + 0.5f, cx, cy, halfW, halfH, r);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0.003f) continue;
                    Blend(ref px[y * w + x], color, a * color.a);
                }
            }
        }

        private static float RoundedBoxDistance(float x, float y, float width, float height, float radius)
        {
            return RoundedBoxDistance(x, y, width * 0.5f, height * 0.5f, width * 0.5f - 1f, height * 0.5f - 1f, radius);
        }

        private static float RoundedBoxDistance(float x, float y, float cx, float cy, float halfW, float halfH, float r)
        {
            float px = Mathf.Abs(x - cx) - (halfW - r);
            float py = Mathf.Abs(y - cy) - (halfH - r);
            float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
            float inside = Mathf.Min(Mathf.Max(px, py), 0f);
            return outside + inside - r;
        }

        public static void DrawLine(Color32[] px, int w, int h, float x0, float y0, float x1, float y1,
            float thickness, Color color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f) return;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - thickness - 1));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + thickness + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - thickness - 1));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + thickness + 1));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float pxv = x + 0.5f - x0;
                    float pyv = y + 0.5f - y0;
                    float t = Mathf.Clamp01((pxv * dx + pyv * dy) / lenSq);
                    float sx = x0 + dx * t;
                    float sy = y0 + dy * t;
                    float ddx = x + 0.5f - sx;
                    float ddy = y + 0.5f - sy;
                    float d = Mathf.Sqrt(ddx * ddx + ddy * ddy) - thickness * 0.5f;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0.003f) continue;
                    Blend(ref px[y * w + x], color, a * color.a);
                }
            }
        }

        public static void DrawPolygon(Color32[] px, int w, int h, Vector2[] points, Color color)
        {
            if (points == null || points.Length < 3) return;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(maxY));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!PointInPolygon(x + 0.5f, y + 0.5f, points)) continue;
                    Blend(ref px[y * w + x], color, color.a);
                }
            }
        }

        private static bool PointInPolygon(float x, float y, Vector2[] poly)
        {
            bool inside = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        public static void DrawStar(Color32[] px, int w, int h, float cx, float cy, float outer, float inner,
            int points, Color color)
        {
            Vector2[] verts = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float radius = (i % 2 == 0) ? outer : inner;
                float ang = Mathf.PI * 0.5f + i * Mathf.PI / points;
                verts[i] = new Vector2(cx + Mathf.Cos(ang) * radius, cy + Mathf.Sin(ang) * radius);
            }
            DrawPolygon(px, w, h, verts, color);
        }

        private static void Blend(ref Color32 dst, Color src, float alpha)
        {
            if (alpha <= 0.0001f) return;
            float sa = Mathf.Clamp01(src.a * alpha);
            float da = dst.a / 255f;
            float outA = sa + da * (1f - sa);
            if (outA <= 0.0001f)
            {
                dst = new Color32(0, 0, 0, 0);
                return;
            }
            float sr = src.r, sg = src.g, sb = src.b;
            float dr = dst.r / 255f, dg = dst.g / 255f, db = dst.b / 255f;
            float outR = (sr * sa + dr * da * (1f - sa)) / outA;
            float outG = (sg * sa + dg * da * (1f - sa)) / outA;
            float outB = (sb * sa + db * da * (1f - sa)) / outA;
            dst = new Color32((byte)(Mathf.Clamp01(outR) * 255), (byte)(Mathf.Clamp01(outG) * 255),
                (byte)(Mathf.Clamp01(outB) * 255), (byte)(Mathf.Clamp01(outA) * 255));
        }

        private static void Erase(ref Color32 dst, float alpha)
        {
            float da = dst.a / 255f;
            da = Mathf.Clamp01(da - alpha * da);
            dst = new Color32(dst.r, dst.g, dst.b, (byte)(da * 255));
        }

        public static Color Hex(string hex)
        {
            return Palette.Hex(hex);
        }
    }
}
