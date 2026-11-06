using UnityEngine;

namespace Skybound
{
    /// <summary>Level progression: unlocking, star ratings and completion rewards.</summary>
    public class LevelManager : MonoBehaviour
    {
        public int CurrentIndex { get; private set; } = -1;

        public bool IsEndless { get { return CurrentIndex < 0; } }
        public LevelDef Current { get { return IsEndless ? null : GameConfig.GetLevel(CurrentIndex); } }

        public static LevelManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("LevelManager", parent);
            return go.AddComponent<LevelManager>();
        }

        public void StartEndless()
        {
            CurrentIndex = -1;
        }

        public void StartLevel(int index)
        {
            CurrentIndex = Mathf.Clamp(index, 0, GameConfig.Levels.Length - 1);
        }

        public bool IsUnlocked(int index)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return index == 0;
            SaveData data = SaveManager.Instance.Data;
            if (index <= data.UnlockedLevel) return true;
            return false;
        }

        public bool MeetsPlayerLevel(int index)
        {
            if (index < 0 || index >= GameConfig.Levels.Length) return false;
            if (SaveManager.Instance == null) return true;
            return SaveManager.Instance.Data.PlayerLevel >= GameConfig.Levels[index].RequiredPlayerLevel;
        }

        public int StarsFor(int index)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data.LevelStars == null) return 0;
            if (index < 0 || index >= SaveManager.Instance.Data.LevelStars.Length) return 0;
            return SaveManager.Instance.Data.LevelStars[index];
        }

        public int TotalStars()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data.LevelStars == null) return 0;
            int total = 0;
            for (int i = 0; i < SaveManager.Instance.Data.LevelStars.Length; i++)
                total += SaveManager.Instance.Data.LevelStars[i];
            return total;
        }

        public int EvaluateStars(int index, int score, bool completed)
        {
            if (!completed) return 0;
            LevelDef def = GameConfig.GetLevel(index);
            if (score >= def.ThreeStarScore) return 3;
            if (score >= def.TwoStarScore) return 2;
            return 1;
        }

        /// <summary>Applies completion rewards and unlocks the next level. Returns stars earned.</summary>
        public int CompleteLevel(int index, int score)
        {
            if (SaveManager.Instance == null) return 0;
            int stars = EvaluateStars(index, score, true);
            SaveData data = SaveManager.Instance.Data;
            if (data.LevelStars == null || data.LevelStars.Length != GameConfig.Levels.Length)
                data.LevelStars = new int[GameConfig.Levels.Length];
            if (index >= 0 && index < data.LevelStars.Length && stars > data.LevelStars[index])
                data.LevelStars[index] = stars;

            if (index + 1 < GameConfig.Levels.Length && data.UnlockedLevel < index + 1)
                data.UnlockedLevel = index + 1;

            int coinReward = 180 + stars * 140 + index * 60;
            int xpReward = 120 + stars * 90 + index * 40;
            data.Coins += coinReward;
            SaveManager.Instance.AddXp(xpReward);
            SaveManager.Instance.MarkDirty();
            SaveManager.Instance.Save();
            return stars;
        }

        public int LevelCount { get { return GameConfig.Levels.Length; } }
    }
}
