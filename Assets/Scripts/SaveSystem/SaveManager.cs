using System;
using UnityEngine;

namespace Skybound
{
    [Serializable]
    public class SettingsData
    {
        public float MusicVolume = 0.7f;
        public float SfxVolume = 0.9f;
        public float VoiceVolume = 0.8f;
        public bool Vibration = true;
        public bool ScreenShake = true;
        public int QualityLevel = 2;
        public bool AutoQuality = true;
        public int FpsTarget = 60;
        public int Language = 0;
        public bool OnScreenControls = false;
        public bool TutorialPrompts = true;
        public bool Notifications = true;
        public float UiScale = 1f;
        public bool LowEndMode = false;
    }

    [Serializable]
    public class MissionState
    {
        public string Id;
        public float Progress;
        public bool Claimed;

        public MissionState(string id)
        {
            Id = id;
            Progress = 0f;
            Claimed = false;
        }
    }

    [Serializable]
    public class AchievementState
    {
        public string Id;
        public bool Unlocked;
        public float Progress;

        public AchievementState(string id)
        {
            Id = id;
            Unlocked = false;
            Progress = 0f;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int Version = GameConfig.SaveVersion;
        public string PlayerId = "";

        public int Coins;
        public int Xp = 0;
        public int PlayerLevel = 1;

        public int SelectedCharacter = 0;
        public int SelectedSkin = 0;

        public int BestScore;
        public float BestDistance;
        public int BestCoins;
        public int BestCombo;
        public int BestTime;

        public int TotalRuns;
        public int TotalCoins;
        public int TotalGold;
        public float TotalDistance;
        public int TotalJumps;
        public int TotalSlides;
        public int TotalNearMisses;
        public int TotalPowerUps;
        public int PerfectRuns;
        public int TotalSpent;

        public int[] UnlockedCharacters = { 0 };
        public int[] UnlockedSkins = { 0 };
        public int[] LevelStars = new int[8];
        public int UnlockedLevel = 0;
        public int MaxThemeIndexSeen = 0;

        public bool TutorialCompleted;
        public bool HasSeenIntro;
        public string DailyDate = "";
        public MissionState[] DailyMissions = new MissionState[0];
        public AchievementState[] AchievementStates = new AchievementState[0];

        public int MagnetUpgrade;
        public int ShieldUpgrade;
        public int BoostUpgrade;
        public int CoinBoosterLevel;

        public SettingsData Settings = new SettingsData();
    }

    /// <summary>
    /// Local persistence with lightweight tamper detection.
    /// The storage is intentionally isolated so a cloud save provider can be added later.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveKey = "skybound_coinrush_save_v3";
        private const string Salt = "SKYBOUND//CITY//2026";
        private const float AutoSaveInterval = 12f;

        public SaveData Data { get; private set; }
        public event Action<SaveData> Changed;

        private bool dirty;
        private float autoSaveTimer;

        public static SaveManager Create(Transform parent)
        {
            GameObject go = new GameObject("SaveManager");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<SaveManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            Load();
        }

        private void Update()
        {
            if (!dirty) return;
            autoSaveTimer += Time.unscaledDeltaTime;
            if (autoSaveTimer >= AutoSaveInterval)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public void Load()
        {
            string raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            SaveData loaded = null;
            if (!string.IsNullOrEmpty(raw))
            {
                loaded = Decode(raw);
            }

            Data = loaded ?? CreateDefaultSave();
            Validate(Data);
            EnsureCollections(Data);
            dirty = false;
            autoSaveTimer = 0f;

            if (Changed != null) Changed(Data);
        }

        public void Save()
        {
            if (Data == null) return;
            Validate(Data);
            string encoded = Encode(Data);
            PlayerPrefs.SetString(SaveKey, encoded);
            PlayerPrefs.Save();
            dirty = false;
            autoSaveTimer = 0f;
        }

        public void MarkDirty()
        {
            dirty = true;
            if (Changed != null) Changed(Data);
        }

        public void ResetProgress()
        {
            Data = CreateDefaultSave();
            EnsureCollections(Data);
            Save();
            if (Changed != null) Changed(Data);
        }

        private SaveData CreateDefaultSave()
        {
            SaveData data = new SaveData();
            data.PlayerId = Guid.NewGuid().ToString("N");
            data.Coins = 250;
            data.PlayerLevel = 1;
            data.UnlockedCharacters = new[] { 0 };
            data.UnlockedSkins = new[] { 0 };
            data.LevelStars = new int[GameConfig.Levels.Length];
            data.Settings = new SettingsData();
            data.Settings.AutoQuality = true;
            data.Settings.QualityLevel = DetectDefaultQuality();
            data.AchievementStates = new AchievementState[GameConfig.Achievements.Length];
            for (int i = 0; i < GameConfig.Achievements.Length; i++)
            {
                data.AchievementStates[i] = new AchievementState(GameConfig.Achievements[i].Id);
            }
            data.DailyMissions = new MissionState[0];
            return data;
        }

        private static int DetectDefaultQuality()
        {
            int memory = SystemInfo.graphicsMemorySize;
            int cores = SystemInfo.processorCount;
            if (memory <= 1024 || cores <= 4) return 0;
            if (memory <= 2048) return 1;
            if (memory <= 4096) return 2;
            return 3;
        }

        private static void EnsureCollections(SaveData data)
        {
            if (data.UnlockedCharacters == null || data.UnlockedCharacters.Length == 0)
                data.UnlockedCharacters = new[] { 0 };
            if (data.UnlockedSkins == null || data.UnlockedSkins.Length == 0)
                data.UnlockedSkins = new[] { 0 };
            if (data.LevelStars == null || data.LevelStars.Length != GameConfig.Levels.Length)
            {
                int[] stars = new int[GameConfig.Levels.Length];
                if (data.LevelStars != null)
                {
                    for (int i = 0; i < stars.Length && i < data.LevelStars.Length; i++)
                        stars[i] = data.LevelStars[i];
                }
                data.LevelStars = stars;
            }
            if (data.Settings == null) data.Settings = new SettingsData();
            if (data.AchievementStates == null || data.AchievementStates.Length == 0)
            {
                data.AchievementStates = new AchievementState[GameConfig.Achievements.Length];
                for (int i = 0; i < GameConfig.Achievements.Length; i++)
                    data.AchievementStates[i] = new AchievementState(GameConfig.Achievements[i].Id);
            }
            if (data.DailyMissions == null) data.DailyMissions = new MissionState[0];
            if (string.IsNullOrEmpty(data.PlayerId)) data.PlayerId = Guid.NewGuid().ToString("N");
        }

        private static void Validate(SaveData data)
        {
            if (data.Coins < 0) data.Coins = 0;
            if (data.Xp < 0) data.Xp = 0;
            if (data.PlayerLevel < 1) data.PlayerLevel = 1;
            if (data.BestScore < 0) data.BestScore = 0;
            if (data.BestDistance < 0f) data.BestDistance = 0f;
            if (data.BestCoins < 0) data.BestCoins = 0;
            if (data.BestCombo < 0) data.BestCombo = 0;
            if (data.SelectedCharacter < 0) data.SelectedCharacter = 0;
            if (data.SelectedSkin < 0) data.SelectedSkin = 0;
            if (data.UnlockedLevel < 0) data.UnlockedLevel = 0;
            if (data.UnlockedLevel >= GameConfig.Levels.Length) data.UnlockedLevel = GameConfig.Levels.Length - 1;
            if (data.Coins > 99999999) data.Coins = 99999999;
            if (data.Xp > 99999999) data.Xp = 99999999;
            if (data.Settings.UiScale < 0.8f) data.Settings.UiScale = 0.8f;
            if (data.Settings.UiScale > 1.25f) data.Settings.UiScale = 1.25f;
            if (data.Settings.QualityLevel < 0) data.Settings.QualityLevel = 0;
            if (data.Settings.QualityLevel > 3) data.Settings.QualityLevel = 3;
        }

        private static string Encode(SaveData data)
        {
            string json = JsonUtility.ToJson(data);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            ulong hash = Fnv1a(bytes);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ (byte)((i * 31 + 97) & 0xFF));
            }
            string payload = Convert.ToBase64String(bytes);
            return payload + "|" + hash.ToString("X16");
        }

        private static SaveData Decode(string raw)
        {
            try
            {
                int split = raw.LastIndexOf('|');
                if (split <= 0) return null;
                string payload = raw.Substring(0, split);
                string hashText = raw.Substring(split + 1);
                byte[] bytes = Convert.FromBase64String(payload);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ (byte)((i * 31 + 97) & 0xFF));
                }
                ulong expected;
                if (!ulong.TryParse(hashText, System.Globalization.NumberStyles.HexNumber, null, out expected))
                    return null;
                ulong actual = Fnv1a(bytes);
                if (actual != expected) return null;
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ulong Fnv1a(byte[] data)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= 1099511628211UL;
            }
            hash ^= (ulong)Salt.Length;
            return hash;
        }

        public bool IsCharacterUnlocked(int index)
        {
            if (index == 0) return true;
            for (int i = 0; i < Data.UnlockedCharacters.Length; i++)
                if (Data.UnlockedCharacters[i] == index) return true;
            return false;
        }

        public bool IsSkinUnlocked(int index)
        {
            if (index == 0) return true;
            for (int i = 0; i < Data.UnlockedSkins.Length; i++)
                if (Data.UnlockedSkins[i] == index) return true;
            return false;
        }

        public bool UnlockCharacter(int index)
        {
            if (IsCharacterUnlocked(index)) return true;
            if (index < 0 || index >= GameConfig.Characters.Length) return false;
            int price = GameConfig.Characters[index].Price;
            if (!SpendCoins(price)) return false;
            int[] next = new int[Data.UnlockedCharacters.Length + 1];
            Array.Copy(Data.UnlockedCharacters, next, Data.UnlockedCharacters.Length);
            next[next.Length - 1] = index;
            Data.UnlockedCharacters = next;
            MarkDirty();
            Save();
            return true;
        }

        public bool UnlockSkin(int index)
        {
            if (IsSkinUnlocked(index)) return true;
            if (index < 0 || index >= GameConfig.Skins.Length) return false;
            int price = GameConfig.Skins[index].Price;
            if (!SpendCoins(price)) return false;
            int[] next = new int[Data.UnlockedSkins.Length + 1];
            Array.Copy(Data.UnlockedSkins, next, Data.UnlockedSkins.Length);
            next[next.Length - 1] = index;
            Data.UnlockedSkins = next;
            MarkDirty();
            Save();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Data.Coins = Mathf.Clamp(Data.Coins + amount, 0, 99999999);
            MarkDirty();
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Data.Coins < amount) return false;
            Data.Coins -= amount;
            Data.TotalSpent += amount;
            MarkDirty();
            return true;
        }

        /// <summary>Adds XP and handles level-ups. Returns number of levels gained.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0) return 0;
            Data.Xp += amount;
            int levels = 0;
            int guard = 0;
            while (Data.Xp >= XpForNextLevel(Data.PlayerLevel) && guard++ < 100)
            {
                Data.Xp -= XpForNextLevel(Data.PlayerLevel);
                Data.PlayerLevel++;
                levels++;
                Data.Coins += 150 + Data.PlayerLevel * 25;
            }
            MarkDirty();
            return levels;
        }

        public static int XpForNextLevel(int level)
        {
            return 400 + (level - 1) * 300;
        }

        public float XpProgress01()
        {
            int needed = XpForNextLevel(Data.PlayerLevel);
            if (needed <= 0) return 0f;
            return Mathf.Clamp01(Data.Xp / (float)needed);
        }

        public void RecordRun(int score, float distance, int coins, int combo, float time,
            bool perfect, int gold, int jumps, int slides, int nearMisses, int powerUps, float topSpeed)
        {
            Data.TotalRuns++;
            Data.TotalCoins += coins;
            Data.TotalGold += gold;
            Data.TotalDistance += distance;
            Data.TotalJumps += jumps;
            Data.TotalSlides += slides;
            Data.TotalNearMisses += nearMisses;
            Data.TotalPowerUps += powerUps;
            if (perfect && distance >= 500f) Data.PerfectRuns++;
            if (score > Data.BestScore) Data.BestScore = score;
            if (distance > Data.BestDistance) Data.BestDistance = distance;
            if (coins > Data.BestCoins) Data.BestCoins = coins;
            if (combo > Data.BestCombo) Data.BestCombo = combo;
            if (time > Data.BestTime) Data.BestTime = Mathf.RoundToInt(time);
            Data.Coins += 0;
            MarkDirty();
            Save();
        }

        public int GetAchievementProgress(string id)
        {
            for (int i = 0; i < Data.AchievementStates.Length; i++)
                if (Data.AchievementStates[i].Id == id)
                    return Mathf.FloorToInt(Data.AchievementStates[i].Progress);
            return 0;
        }

        public AchievementState GetAchievementState(string id)
        {
            for (int i = 0; i < Data.AchievementStates.Length; i++)
                if (Data.AchievementStates[i].Id == id)
                    return Data.AchievementStates[i];
            return null;
        }
    }
}
