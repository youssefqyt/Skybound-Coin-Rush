using System;
using UnityEngine;

namespace Skybound
{
    public enum GameState
    {
        Boot,
        Loading,
        Menu,
        Playing,
        Paused,
        GameOver,
        LevelComplete
    }

    public enum CoinType
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Special = 3
    }

    public enum PowerUpType
    {
        Magnet = 0,
        Shield = 1,
        Boost = 2,
        DoubleCoins = 3,
        ScoreMultiplier = 4,
        SlowMotion = 5,
        Count = 6
    }

    public enum CharacterId
    {
        Nova = 0,
        Blaze = 1,
        Aero = 2,
        Shadow = 3
    }

    public enum SkinId
    {
        Street = 0,
        Cyber = 1,
        Neon = 2,
        Shadow = 3,
        Golden = 4,
        Future = 5,
        Storm = 6
    }

    public enum ObstacleKind
    {
        JumpBarrier,
        SlideGate,
        Block,
        Car,
        Train,
        Crate,
        Laser,
        Drone,
        Gap
    }

    public enum WeatherKind
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Fog
    }

    public enum ThemeId
    {
        Dawn,
        Day,
        Sunset,
        Night,
        NeonNight,
        Storm
    }

    public enum MissionMetric
    {
        CollectCoins,
        RunDistance,
        PerformJumps,
        PerformSlides,
        CollectGoldCoins,
        NearMisses,
        UsePowerUps,
        ReachCombo,
        SurviveTime,
        CompleteRuns,
        ScoreInRun,
        CollectSilverCoins
    }

    public enum AchievementMetric
    {
        FirstRun,
        TotalCoins,
        BestDistance,
        MaxSpeed,
        TotalDistance,
        PerfectRun,
        BestCombo,
        SurvivalTime,
        TotalPowerUps,
        LevelCompleted,
        BestScore,
        TotalGold
    }

    [Serializable]
    public class CharacterDef
    {
        public string Id;
        public string Name;
        public string Title;
        public string Description;
        public int Price;
        public int RequiredPlayerLevel;
        public Color Primary;
        public Color Secondary;
        public Color Accent;
        public float SpeedStat;
        public float JumpStat;
        public float RecoveryStat;

        public CharacterDef(string id, string name, string title, string description, int price,
            int requiredLevel, Color primary, Color secondary, Color accent,
            float speedStat, float jumpStat, float recoveryStat)
        {
            Id = id;
            Name = name;
            Title = title;
            Description = description;
            Price = price;
            RequiredPlayerLevel = requiredLevel;
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
            SpeedStat = speedStat;
            JumpStat = jumpStat;
            RecoveryStat = recoveryStat;
        }

        public string UnlockText(int playerLevel)
        {
            if (Price <= 0) return "STARTER";
            if (playerLevel < RequiredPlayerLevel) return "REACH LEVEL " + RequiredPlayerLevel;
            return Price.ToString("N0") + " COINS";
        }
    }

    [Serializable]
    public class SkinDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int Price;
        public Color Primary;
        public Color Secondary;
        public Color Accent;

        public SkinDef(string id, string name, string description, int price, Color primary, Color secondary, Color accent)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
        }
    }

    [Serializable]
    public class LevelDef
    {
        public int Index;
        public string Name;
        public string Subtitle;
        public string Description;
        public float TargetDistance;
        public float BaseSpeed;
        public float Difficulty;
        public int RequiredPlayerLevel;
        public ThemeId Theme;
        public WeatherKind Weather;
        public Color Accent;
        public int TwoStarScore;
        public int ThreeStarScore;

        public LevelDef(int index, string name, string subtitle, string description, float targetDistance,
            float baseSpeed, float difficulty, int requiredPlayerLevel, ThemeId theme, WeatherKind weather,
            Color accent)
        {
            Index = index;
            Name = name;
            Subtitle = subtitle;
            Description = description;
            TargetDistance = targetDistance;
            BaseSpeed = baseSpeed;
            Difficulty = difficulty;
            RequiredPlayerLevel = requiredPlayerLevel;
            Theme = theme;
            Weather = weather;
            Accent = accent;
            TwoStarScore = Mathf.RoundToInt(targetDistance * 10f * 1.25f);
            ThreeStarScore = Mathf.RoundToInt(targetDistance * 10f * 1.75f);
        }
    }

    [Serializable]
    public class MissionDef
    {
        public string Id;
        public string Title;
        public string Description;
        public MissionMetric Metric;
        public float Target;
        public int RewardCoins;
        public int RewardXp;

        public MissionDef(string id, string title, string description, MissionMetric metric,
            float target, int rewardCoins, int rewardXp)
        {
            Id = id;
            Title = title;
            Description = description;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
            RewardXp = rewardXp;
        }
    }

    [Serializable]
    public class AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        public AchievementMetric Metric;
        public float Target;
        public int RewardCoins;

        public AchievementDef(string id, string name, string description, AchievementMetric metric,
            float target, int rewardCoins)
        {
            Id = id;
            Name = name;
            Description = description;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
        }
    }

    [Serializable]
    public class PowerUpDef
    {
        public PowerUpType Type;
        public string Name;
        public string ShortName;
        public string Description;
        public float Duration;
        public Color Color;

        public PowerUpDef(PowerUpType type, string name, string shortName, string description, float duration, Color color)
        {
            Type = type;
            Name = name;
            ShortName = shortName;
            Description = description;
            Duration = duration;
            Color = color;
        }
    }

    public class ThemePalette
    {
        public Color SkyTop;
        public Color SkyBottom;
        public Color Sun;
        public Color CityTint;
        public Color WindowGlow;
        public Color RoadTint;
        public Color Fog;
        public Color Ambient;
        public float StarAlpha;
        public float WindowAlpha;

        public ThemePalette(Color skyTop, Color skyBottom, Color sun, Color cityTint,
            Color windowGlow, Color roadTint, Color fog, Color ambient, float starAlpha, float windowAlpha)
        {
            SkyTop = skyTop;
            SkyBottom = skyBottom;
            Sun = sun;
            CityTint = cityTint;
            WindowGlow = windowGlow;
            RoadTint = roadTint;
            Fog = fog;
            Ambient = ambient;
            StarAlpha = starAlpha;
            WindowAlpha = windowAlpha;
        }
    }

    public static class Palette
    {
        public static readonly Color Background = Hex("#07111F");
        public static readonly Color Panel = Hex("#101C2E");
        public static readonly Color PanelLight = Hex("#162842");
        public static readonly Color Primary = Hex("#00E5FF");
        public static readonly Color Secondary = Hex("#7C3AED");
        public static readonly Color Accent = Hex("#FFD700");
        public static readonly Color Text = Color.white;
        public static readonly Color Muted = Hex("#8FA5BD");
        public static readonly Color Success = Hex("#22C55E");
        public static readonly Color Danger = Hex("#EF4444");
        public static readonly Color Bronze = Hex("#D08A4E");
        public static readonly Color Silver = Hex("#C7D5E5");
        public static readonly Color Gold = Hex("#FFD54A");
        public static readonly Color Special = Hex("#FF5CE1");
        public static readonly Color CyanDark = Hex("#006E88");
        public static readonly Color PurpleDark = Hex("#3B1A78");

        public static Color Hex(string hex)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return Color.magenta;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public static class GameConfig
    {
        public const string GameTitle = "SKYBOUND";
        public const string GameSubtitle = "COIN RUSH";
        public const string GameFullName = "SKYBOUND: COIN RUSH";
        public const string Version = "1.0.0";
        public const int SaveVersion = 3;

        public const int LaneCount = 3;
        public const int MiddleLane = 1;
        public const float LaneSpacing = 2.55f;
        public const float TrackLength = 30f;
        public const int MaxSegments = 7;
        public const float WorldStartSpeed = 13.5f;
        public const float WorldMaxSpeed = 31f;
        public const float PlayfieldDepth = 1f;

        public const int ReviveCostCoins = 150;

        public static readonly float[] LaneX = { -LaneSpacing, 0f, LaneSpacing };

        public static readonly Color[] CoinColors =
        {
            Palette.Bronze,
            Palette.Silver,
            Palette.Gold,
            Palette.Special
        };

        public static readonly int[] CoinValues = { 1, 3, 5, 10 };

        public static readonly CharacterDef[] Characters =
        {
            new CharacterDef("nova", "NOVA", "THE SKY COURIER", "Balanced speed, jump and recovery. The face of the Skybound circuit.",
                0, 1, Palette.Hex("#00E5FF"), Palette.Hex("#0B3A52"), Palette.Hex("#FFD700"), 1.00f, 1.00f, 1.00f),
            new CharacterDef("blaze", "BLAZE", "THE AFTERBURNER", "Built for velocity. Blaze trades a little air time for raw top speed.",
                3200, 3, Palette.Hex("#FF6B3D"), Palette.Hex("#5A1A12"), Palette.Hex("#FFE066"), 1.08f, 0.94f, 0.98f),
            new CharacterDef("aero", "AERO", "THE WIND DANCER", "Feather-light boots and a longer glide. Master the gaps of the Skyway.",
                4800, 5, Palette.Hex("#5CFFB1"), Palette.Hex("#0A3F31"), Palette.Hex("#00E5FF"), 0.97f, 1.13f, 1.00f),
            new CharacterDef("shadow", "SHADOW", "THE GHOST RUNNER", "Recovers faster from impacts. Harder to knock off balance.",
                7200, 8, Palette.Hex("#B388FF"), Palette.Hex("#1A1030"), Palette.Hex("#FF5CE1"), 1.03f, 1.00f, 1.15f)
        };

        public static readonly SkinDef[] Skins =
        {
            new SkinDef("street", "STREET", "Classic Skybound streetwear.", 0,
                Palette.Hex("#00E5FF"), Palette.Hex("#101C2E"), Palette.Hex("#FFD700")),
            new SkinDef("cyber", "CYBER", "Circuit-lined courier shell.", 1200,
                Palette.Hex("#4D8BFF"), Palette.Hex("#101C2E"), Palette.Hex("#00E5FF")),
            new SkinDef("neon", "NEON", "Maximum glow for the night run.", 1800,
                Palette.Hex("#FF38D0"), Palette.Hex("#2A0A3E"), Palette.Hex("#00E5FF")),
            new SkinDef("shadow", "SHADOW", "Silent, matte, untraceable.", 2400,
                Palette.Hex("#7C3AED"), Palette.Hex("#08060F"), Palette.Hex("#B388FF")),
            new SkinDef("golden", "GOLDEN", "For runners who own the sky.", 3000,
                Palette.Hex("#FFD700"), Palette.Hex("#4A3500"), Palette.Hex("#FFF3B0")),
            new SkinDef("future", "FUTURE", "A prototype suit from Skybound Labs.", 3600,
                Palette.Hex("#00FFF0"), Palette.Hex("#0B2340"), Palette.Hex("#FFFFFF")),
            new SkinDef("storm", "STORM", "Electrified armor for the Storm Zone.", 4200,
                Palette.Hex("#6EE7FF"), Palette.Hex("#13213A"), Palette.Hex("#FFD700"))
        };

        public static readonly LevelDef[] Levels =
        {
            new LevelDef(0, "CITY RUN", "SKYBOUND CITY", "Learn the skyway. Dodge, jump, slide and collect.",
                550f, 13.5f, 0.20f, 1, ThemeId.Day, WeatherKind.Clear, Palette.Hex("#00E5FF")),
            new LevelDef(1, "SKY BRIDGE", "THE GAP RUN", "Mind the gaps. The bridge ends without warning.",
                750f, 15.0f, 0.32f, 2, ThemeId.Sunset, WeatherKind.Clear, Palette.Hex("#FF9E5C")),
            new LevelDef(2, "NEON DISTRICT", "AFTER HOURS", "Night traffic, moving hazards and blinding neon.",
                950f, 16.5f, 0.44f, 3, ThemeId.Night, WeatherKind.Cloudy, Palette.Hex("#FF38D0")),
            new LevelDef(3, "STORM ZONE", "THUNDER RUN", "Rain, lightning and the Sky Hunter on your tail.",
                1150f, 18.0f, 0.56f, 5, ThemeId.Storm, WeatherKind.Storm, Palette.Hex("#7C3AED")),
            new LevelDef(4, "SKYLINE", "ABOVE THE CLOUDS", "Rooftop hops and long airtime at the edge of the world.",
                1350f, 19.5f, 0.66f, 7, ThemeId.Sunset, WeatherKind.Fog, Palette.Hex("#FFD700")),
            new LevelDef(5, "CYBER METRO", "THE FAST LINE", "Tunnel gates, high-speed trains and split-second calls.",
                1550f, 21.5f, 0.76f, 9, ThemeId.Night, WeatherKind.Rain, Palette.Hex("#00E5FF")),
            new LevelDef(6, "CLOUD CITY", "FLOATING SKYWAY", "Aerial obstacles and a city that never touches the ground.",
                1800f, 23.5f, 0.87f, 12, ThemeId.Day, WeatherKind.Cloudy, Palette.Hex("#5CFFB1")),
            new LevelDef(7, "FINAL SKYWAY", "THE LAST LIGHT", "Every hazard. Every mistake. One perfect run.",
                2100f, 26.0f, 1.00f, 15, ThemeId.NeonNight, WeatherKind.Storm, Palette.Hex("#FF5CE1"))
        };

        public static readonly PowerUpDef[] PowerUps =
        {
            new PowerUpDef(PowerUpType.Magnet, "COIN MAGNET", "MAGNET", "Pulls nearby coins toward you.", 10f, Palette.Hex("#00E5FF")),
            new PowerUpDef(PowerUpType.Shield, "SKY SHIELD", "SHIELD", "Blocks one collision.", 12f, Palette.Hex("#7C3AED")),
            new PowerUpDef(PowerUpType.Boost, "TURBO BOOST", "BOOST", "A burst of pure speed.", 6f, Palette.Hex("#FF6B3D")),
            new PowerUpDef(PowerUpType.DoubleCoins, "DOUBLE COINS", "X2 COINS", "Every coin counts twice.", 12f, Palette.Hex("#FFD700")),
            new PowerUpDef(PowerUpType.ScoreMultiplier, "SCORE SURGE", "X2 SCORE", "Doubles your score gain.", 12f, Palette.Hex("#5CFFB1")),
            new PowerUpDef(PowerUpType.SlowMotion, "TIME WARP", "TIME WARP", "Slows the world for a few seconds.", 4f, Palette.Hex("#B388FF"))
        };

        public static readonly MissionDef[] MissionPool =
        {
            new MissionDef("coins_100", "COIN RUNNER", "Collect 100 coins in a run.", MissionMetric.CollectCoins, 100, 250, 60),
            new MissionDef("coins_250", "COIN HOARDER", "Collect 250 coins in a run.", MissionMetric.CollectCoins, 250, 450, 110),
            new MissionDef("distance_800", "SKYWAY SPRINT", "Run 800 meters in a single run.", MissionMetric.RunDistance, 800, 250, 70),
            new MissionDef("distance_2000", "LONG HAUL", "Run 2,000 meters in a single run.", MissionMetric.RunDistance, 2000, 500, 150),
            new MissionDef("jumps_15", "AIR TIME", "Perform 15 jumps.", MissionMetric.PerformJumps, 15, 200, 50),
            new MissionDef("slides_12", "LOW PROFILE", "Perform 12 slides.", MissionMetric.PerformSlides, 12, 200, 50),
            new MissionDef("gold_12", "GOLD RUSH", "Collect 12 gold coins.", MissionMetric.CollectGoldCoins, 12, 300, 80),
            new MissionDef("near_8", "CLOSE CALL", "Perform 8 near misses.", MissionMetric.NearMisses, 8, 300, 90),
            new MissionDef("power_3", "OVERCHARGED", "Use 3 power-ups.", MissionMetric.UsePowerUps, 3, 220, 60),
            new MissionDef("combo_6", "CHAIN REACTION", "Reach a x6 combo.", MissionMetric.ReachCombo, 6, 350, 90),
            new MissionDef("survive_90", "SKY SURVIVOR", "Survive for 90 seconds.", MissionMetric.SurviveTime, 90, 280, 80),
            new MissionDef("silver_15", "SILVER LINING", "Collect 15 silver coins.", MissionMetric.CollectSilverCoins, 15, 250, 70),
            new MissionDef("score_8000", "HIGH FLYER", "Score 8,000 points in one run.", MissionMetric.ScoreInRun, 8000, 400, 120),
            new MissionDef("runs_2", "DOUBLE DUTY", "Complete 2 runs.", MissionMetric.CompleteRuns, 2, 200, 40)
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef("first_run", "FIRST RUN", "Complete your first run.", AchievementMetric.FirstRun, 1, 100),
            new AchievementDef("first_100", "COIN STARTER", "Collect 100 coins in total.", AchievementMetric.TotalCoins, 100, 150),
            new AchievementDef("coins_10000", "COIN MASTER", "Collect 10,000 coins in total.", AchievementMetric.TotalCoins, 10000, 1500),
            new AchievementDef("speed_demon", "SPEED DEMON", "Reach maximum world speed.", AchievementMetric.MaxSpeed, 31, 800),
            new AchievementDef("sky_runner", "SKY RUNNER", "Run 10,000 meters in total.", AchievementMetric.TotalDistance, 10000, 1000),
            new AchievementDef("perfect_run", "PERFECT RUN", "Finish a 500m+ run without a collision.", AchievementMetric.PerfectRun, 1, 1200),
            new AchievementDef("combo_king", "COMBO KING", "Reach a x10 combo.", AchievementMetric.BestCombo, 10, 700),
            new AchievementDef("survivor", "SURVIVOR", "Survive for 5 minutes in one run.", AchievementMetric.SurvivalTime, 300, 1500),
            new AchievementDef("powers_50", "OVERCLOCKED", "Use 50 power-ups.", AchievementMetric.TotalPowerUps, 50, 600),
            new AchievementDef("level_8", "SKYBOUND LEGEND", "Complete the Final Skyway.", AchievementMetric.LevelCompleted, 8, 5000),
            new AchievementDef("score_50k", "SKY ELITE", "Score 50,000 in one run.", AchievementMetric.BestScore, 50000, 2500),
            new AchievementDef("gold_500", "GOLDEN TOUCH", "Collect 500 gold coins in total.", AchievementMetric.TotalGold, 500, 2000)
        };

        public static readonly string[] Tips =
        {
            "Swipe left or right to switch lanes.",
            "Swipe up to jump over low barriers.",
            "Swipe down to slide under gates.",
            "Collect coins to unlock new runners.",
            "Power-ups can save a doomed run.",
            "Chain coin pickups to raise your combo.",
            "Near misses award bonus score.",
            "The Sky Hunter closes in every time you crash."
        };

        public static CharacterDef GetCharacter(int index)
        {
            if (index < 0 || index >= Characters.Length) return Characters[0];
            return Characters[index];
        }

        public static SkinDef GetSkin(int index)
        {
            if (index < 0 || index >= Skins.Length) return Skins[0];
            return Skins[index];
        }

        public static LevelDef GetLevel(int index)
        {
            if (index < 0) index = 0;
            if (index >= Levels.Length) return Levels[Levels.Length - 1];
            return Levels[index];
        }

        public static PowerUpDef GetPowerUp(PowerUpType type)
        {
            int index = (int)type;
            if (index < 0 || index >= PowerUps.Length) return PowerUps[0];
            return PowerUps[index];
        }

        public static ThemePalette GetTheme(ThemeId theme)
        {
            switch (theme)
            {
                case ThemeId.Dawn:
                    return new ThemePalette(
                        Palette.Hex("#1B2A5E"), Palette.Hex("#FF9A76"), Palette.Hex("#FFE1B0"),
                        Palette.Hex("#2C3F6E"), Palette.Hex("#FFD9A0"), Palette.Hex("#2A3346"),
                        Palette.Hex("#C9A9B8"), Palette.Hex("#8EA0C8"), 0.18f, 0.30f);
                case ThemeId.Sunset:
                    return new ThemePalette(
                        Palette.Hex("#231452"), Palette.Hex("#FF6B5B"), Palette.Hex("#FFD36E"),
                        Palette.Hex("#2B1A46"), Palette.Hex("#FFC46E"), Palette.Hex("#2A2238"),
                        Palette.Hex("#D37A72"), Palette.Hex("#9A7FB8"), 0.28f, 0.55f);
                case ThemeId.Night:
                    return new ThemePalette(
                        Palette.Hex("#050914"), Palette.Hex("#13284B"), Palette.Hex("#BFD8FF"),
                        Palette.Hex("#0A1730"), Palette.Hex("#8FE9FF"), Palette.Hex("#111A2E"),
                        Palette.Hex("#1A2A4A"), Palette.Hex("#4A6FA5"), 0.85f, 0.85f);
                case ThemeId.NeonNight:
                    return new ThemePalette(
                        Palette.Hex("#08020F"), Palette.Hex("#2A0B4E"), Palette.Hex("#FF8AE2"),
                        Palette.Hex("#120626"), Palette.Hex("#FF5CE1"), Palette.Hex("#140B22"),
                        Palette.Hex("#2A0E48"), Palette.Hex("#7C3AED"), 1.00f, 1.00f);
                case ThemeId.Storm:
                    return new ThemePalette(
                        Palette.Hex("#05070D"), Palette.Hex("#1A2B3A"), Palette.Hex("#9FC7E8"),
                        Palette.Hex("#0D1722"), Palette.Hex("#7DD3FC"), Palette.Hex("#0E1520"),
                        Palette.Hex("#26384A"), Palette.Hex("#3B5B78"), 0.42f, 0.65f);
                default:
                    return new ThemePalette(
                        Palette.Hex("#2E7FD6"), Palette.Hex("#BDEBFF"), Palette.Hex("#FFF6D8"),
                        Palette.Hex("#2D5A8E"), Palette.Hex("#D9F5FF"), Palette.Hex("#26364A"),
                        Palette.Hex("#B7D9EE"), Palette.Hex("#D8ECFF"), 0.00f, 0.12f);
            }
        }
    }
}
