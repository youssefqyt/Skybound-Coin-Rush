using System;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Accumulates score from distance, coins, near misses, combo and power-ups.
    /// Kept separate from GameManager so scoring rules can be tuned in one place.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int DistanceScore { get; private set; }
        public int CoinScore { get; private set; }
        public int BonusScore { get; private set; }
        public int NearMisses { get; private set; }

        public event Action<int> ScoreChanged;

        private float accumulator;
        private int lastPublished;

        public static ScoreManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("ScoreManager", parent);
            return go.AddComponent<ScoreManager>();
        }

        public void ResetRun()
        {
            Score = 0;
            DistanceScore = 0;
            CoinScore = 0;
            BonusScore = 0;
            NearMisses = 0;
            accumulator = 0f;
            lastPublished = 0;
            Publish();
        }

        public void AddDistance(float meters, float speed01, float scoreMultiplier)
        {
            if (meters <= 0f) return;
            float value = meters * (10f + speed01 * 8f) * Mathf.Max(1f, scoreMultiplier);
            accumulator += value;
            if (accumulator >= 1f)
            {
                int add = Mathf.FloorToInt(accumulator);
                accumulator -= add;
                DistanceScore += add;
                Score += add;
                Publish();
            }
        }

        public void AddCoin(int value, float comboMultiplier, int scoreMultiplier)
        {
            if (value <= 0) return;
            int add = Mathf.RoundToInt(50f * value * Mathf.Max(1f, comboMultiplier) *
                Mathf.Max(1, scoreMultiplier));
            CoinScore += add;
            Score += add;
            Publish();
        }

        public void AddBonus(int amount, bool countsAsNearMiss)
        {
            if (amount <= 0) return;
            if (countsAsNearMiss) NearMisses++;
            BonusScore += amount;
            Score += amount;
            Publish();
        }

        public float Score01(float target)
        {
            if (target <= 0f) return 0f;
            return Mathf.Clamp01(Score / target);
        }

        private void Publish()
        {
            if (Score == lastPublished) return;
            lastPublished = Score;
            if (ScoreChanged != null) ScoreChanged(Score);
        }
    }
}
