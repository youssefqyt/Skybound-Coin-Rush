using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Permanent achievement evaluation. Runs after each run and awards coins once.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public event Action<AchievementDef> Unlocked;

        private readonly List<AchievementDef> newlyUnlocked = new List<AchievementDef>();

        public static AchievementManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("AchievementManager", parent);
            return go.AddComponent<AchievementManager>();
        }

        public IReadOnlyList<AchievementDef> NewlyUnlocked { get { return newlyUnlocked; } }

        public void ClearCache()
        {
            newlyUnlocked.Clear();
        }

        public void EvaluateRun(RunSummary run)
        {
            newlyUnlocked.Clear();
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            SaveData data = SaveManager.Instance.Data;
            if (data.AchievementStates == null || data.AchievementStates.Length == 0) return;

            int levelsComplete = 0;
            if (data.LevelStars != null)
            {
                for (int i = 0; i < data.LevelStars.Length; i++)
                    if (data.LevelStars[i] > 0) levelsComplete++;
            }

            for (int i = 0; i < GameConfig.Achievements.Length; i++)
            {
                AchievementDef def = GameConfig.Achievements[i];
                AchievementState state = FindState(data, def.Id);
                if (state == null) continue;
                if (state.Unlocked) continue;

                float value = ValueFor(def.Metric, data, run, levelsComplete);
                if (value > state.Progress) state.Progress = value;
                if (state.Progress >= def.Target)
                {
                    state.Unlocked = true;
                    state.Progress = def.Target;
                    data.Coins += def.RewardCoins;
                    newlyUnlocked.Add(def);
                    if (Unlocked != null) Unlocked(def);
                }
            }

            SaveManager.Instance.MarkDirty();
            SaveManager.Instance.Save();
            if (newlyUnlocked.Count > 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayAchievement();
        }

        private static AchievementState FindState(SaveData data, string id)
        {
            for (int i = 0; i < data.AchievementStates.Length; i++)
                if (data.AchievementStates[i].Id == id) return data.AchievementStates[i];
            return null;
        }

        private static float ValueFor(AchievementMetric metric, SaveData data, RunSummary run, int levelsComplete)
        {
            switch (metric)
            {
                case AchievementMetric.FirstRun:
                    return data.TotalRuns + 1;
                case AchievementMetric.TotalCoins:
                    return data.TotalCoins + run.CoinCount;
                case AchievementMetric.BestDistance:
                    return Mathf.Max(data.BestDistance, run.Distance);
                case AchievementMetric.MaxSpeed:
                    return Mathf.Max(data.BestTime * 0f, run.TopSpeed);
                case AchievementMetric.TotalDistance:
                    return data.TotalDistance + run.Distance;
                case AchievementMetric.PerfectRun:
                    return data.PerfectRuns + (run.Perfect && run.Distance >= 500f ? 1 : 0);
                case AchievementMetric.BestCombo:
                    return Mathf.Max(data.BestCombo, run.BestCombo);
                case AchievementMetric.SurvivalTime:
                    return Mathf.Max(data.BestTime, run.Time);
                case AchievementMetric.TotalPowerUps:
                    return data.TotalPowerUps + run.PowerUps;
                case AchievementMetric.LevelCompleted:
                    return levelsComplete + (run.LevelCompleted ? 1 : 0);
                case AchievementMetric.BestScore:
                    return Mathf.Max(data.BestScore, run.Score);
                case AchievementMetric.TotalGold:
                    return data.TotalGold + run.GoldCoins;
                default:
                    return 0f;
            }
        }

        public float Progress(string id)
        {
            if (SaveManager.Instance == null) return 0f;
            AchievementState state = FindState(SaveManager.Instance.Data, id);
            return state != null ? state.Progress : 0f;
        }

        public bool IsUnlocked(string id)
        {
            if (SaveManager.Instance == null) return false;
            AchievementState state = FindState(SaveManager.Instance.Data, id);
            return state != null && state.Unlocked;
        }
    }
}
