using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Three rotating daily missions. Progress is stored in SaveData so it survives
    /// app restarts and resets automatically on a new calendar day.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public event Action Changed;

        private const int DailyCount = 3;

        public static MissionManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("MissionManager", parent);
            MissionManager manager = go.AddComponent<MissionManager>();
            manager.RefreshDaily();
            return manager;
        }

        public MissionState[] Missions
        {
            get
            {
                if (SaveManager.Instance == null) return new MissionState[0];
                return SaveManager.Instance.Data.DailyMissions;
            }
        }

        public void RefreshDaily()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            SaveData data = SaveManager.Instance.Data;
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (data.DailyDate == today && data.DailyMissions != null && data.DailyMissions.Length == DailyCount)
                return;

            data.DailyDate = today;
            List<MissionDef> pool = new List<MissionDef>(GameConfig.MissionPool);
            int seed = 0;
            for (int i = 0; i < today.Length; i++) seed = seed * 31 + today[i];
            System.Random rng = new System.Random(seed);
            MissionState[] missions = new MissionState[DailyCount];
            for (int i = 0; i < DailyCount; i++)
            {
                int index = rng.Next(0, pool.Count);
                MissionDef def = pool[index];
                pool.RemoveAt(index);
                missions[i] = new MissionState(def.Id);
            }
            data.DailyMissions = missions;
            SaveManager.Instance.MarkDirty();
            SaveManager.Instance.Save();
            if (Changed != null) Changed();
        }

        public MissionDef GetDef(string id)
        {
            for (int i = 0; i < GameConfig.MissionPool.Length; i++)
                if (GameConfig.MissionPool[i].Id == id) return GameConfig.MissionPool[i];
            return null;
        }

        public MissionState GetState(string id)
        {
            MissionState[] missions = Missions;
            for (int i = 0; i < missions.Length; i++)
                if (missions[i].Id == id) return missions[i];
            return null;
        }

        public float Progress01(string id)
        {
            MissionDef def = GetDef(id);
            MissionState state = GetState(id);
            if (def == null || state == null || def.Target <= 0f) return 0f;
            return Mathf.Clamp01(state.Progress / def.Target);
        }

        public bool IsComplete(string id)
        {
            MissionDef def = GetDef(id);
            MissionState state = GetState(id);
            if (def == null || state == null) return false;
            return state.Progress >= def.Target;
        }

        public bool IsClaimed(string id)
        {
            MissionState state = GetState(id);
            return state != null && state.Claimed;
        }

        public void AddMetric(MissionMetric metric, float amount)
        {
            if (amount <= 0f) return;
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            bool changed = false;
            MissionState[] missions = Missions;
            for (int i = 0; i < missions.Length; i++)
            {
                MissionDef def = GetDef(missions[i].Id);
                if (def == null || def.Metric != metric) continue;
                if (missions[i].Claimed) continue;
                if (IsMaxMetric(metric))
                {
                    if (amount > missions[i].Progress)
                    {
                        missions[i].Progress = amount;
                        changed = true;
                    }
                }
                else
                {
                    missions[i].Progress = Mathf.Min(def.Target, missions[i].Progress + amount);
                    changed = true;
                }
            }
            if (changed)
            {
                SaveManager.Instance.MarkDirty();
                if (Changed != null) Changed();
            }
        }

        public void SetBest(MissionMetric metric, float value)
        {
            AddMetric(metric, value);
        }

        private static bool IsMaxMetric(MissionMetric metric)
        {
            return metric == MissionMetric.RunDistance || metric == MissionMetric.ReachCombo ||
                   metric == MissionMetric.ScoreInRun || metric == MissionMetric.SurviveTime;
        }

        public bool Claim(string id)
        {
            if (SaveManager.Instance == null) return false;
            MissionDef def = GetDef(id);
            MissionState state = GetState(id);
            if (def == null || state == null) return false;
            if (state.Claimed || state.Progress < def.Target) return false;
            state.Claimed = true;
            SaveManager.Instance.AddCoins(def.RewardCoins);
            SaveManager.Instance.AddXp(def.RewardXp);
            SaveManager.Instance.Save();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAchievement();
                AudioManager.Instance.PlayPurchase();
            }
            if (Changed != null) Changed();
            return true;
        }

        public int CompletedCount()
        {
            int count = 0;
            MissionState[] missions = Missions;
            for (int i = 0; i < missions.Length; i++)
                if (IsComplete(missions[i].Id) && !missions[i].Claimed) count++;
            return count;
        }

        public int ClaimableCount()
        {
            return CompletedCount();
        }

        /// <summary>Push end-of-run statistics into the matching metrics.</summary>
        public void ReportRun(RunSummary run)
        {
            AddMetric(MissionMetric.CollectCoins, run.CoinCount);
            AddMetric(MissionMetric.CollectGoldCoins, run.GoldCoins);
            AddMetric(MissionMetric.CollectSilverCoins, run.SilverCoins);
            AddMetric(MissionMetric.PerformJumps, run.Jumps);
            AddMetric(MissionMetric.PerformSlides, run.Slides);
            AddMetric(MissionMetric.NearMisses, run.NearMisses);
            AddMetric(MissionMetric.UsePowerUps, run.PowerUps);
            AddMetric(MissionMetric.RunDistance, run.Distance);
            AddMetric(MissionMetric.ScoreInRun, run.Score);
            AddMetric(MissionMetric.ReachCombo, run.BestCombo);
            AddMetric(MissionMetric.SurviveTime, run.Time);
            AddMetric(MissionMetric.CompleteRuns, 1f);
        }
    }
}
