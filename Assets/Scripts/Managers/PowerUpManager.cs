using System;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Owns active power-up timers and derived gameplay multipliers.
    /// Shield is charge-based; everything else is a countdown.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        private readonly float[] timers = new float[(int)PowerUpType.Count];
        private int shieldCharges;

        public event Action Changed;

        public int ShieldCharges { get { return shieldCharges; } }

        public static PowerUpManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("PowerUpManager", parent);
            return go.AddComponent<PowerUpManager>();
        }

        public void ResetAll()
        {
            for (int i = 0; i < timers.Length; i++) timers[i] = 0f;
            shieldCharges = 0;
            Notify();
        }

        public void Tick(float deltaTime)
        {
            bool changed = false;
            for (int i = 0; i < timers.Length; i++)
            {
                if (timers[i] <= 0f) continue;
                timers[i] -= deltaTime;
                if (timers[i] <= 0f)
                {
                    timers[i] = 0f;
                    changed = true;
                }
            }
            if (changed) Notify();
        }

        public void Activate(PowerUpType type)
        {
            float duration = GameConfig.GetPowerUp(type).Duration;
            if (IsActive(type)) duration += GameConfig.GetPowerUp(type).Duration * 0.35f;
            timers[(int)type] = duration;
            if (type == PowerUpType.Shield && shieldCharges < 3) shieldCharges++;
            Notify();
        }

        public bool IsActive(PowerUpType type)
        {
            return timers[(int)type] > 0f;
        }

        public float Remaining(PowerUpType type)
        {
            return Mathf.Max(0f, timers[(int)type]);
        }

        public float Remaining01(PowerUpType type)
        {
            float duration = GameConfig.GetPowerUp(type).Duration;
            if (duration <= 0f) return 0f;
            return Mathf.Clamp01(timers[(int)type] / duration);
        }

        public int ActiveTimerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < timers.Length; i++)
                    if (timers[i] > 0f) count++;
                return count;
            }
        }

        public bool ConsumeShield()
        {
            if (shieldCharges <= 0) return false;
            shieldCharges--;
            timers[(int)PowerUpType.Shield] = 0f;
            Notify();
            return true;
        }

        public float MagnetRadius
        {
            get { return IsActive(PowerUpType.Magnet) ? 7.5f : 0f; }
        }

        public int CoinMultiplier
        {
            get { return IsActive(PowerUpType.DoubleCoins) ? 2 : 1; }
        }

        public int ScoreMultiplier
        {
            get { return IsActive(PowerUpType.ScoreMultiplier) ? 2 : 1; }
        }

        public float SpeedMultiplier
        {
            get { return IsActive(PowerUpType.Boost) ? 1.32f : 1f; }
        }

        public float WorldTimeScale
        {
            get { return IsActive(PowerUpType.SlowMotion) ? 0.55f : 1f; }
        }

        public bool IsPowerUpActive
        {
            get { return ActiveTimerCount > 0 || shieldCharges > 0; }
        }

        private void Notify()
        {
            if (Changed != null) Changed();
        }

        /// <summary>Duration for UI progress bars, adjusted for stacking.</summary>
        public float DisplayDuration(PowerUpType type)
        {
            float duration = GameConfig.GetPowerUp(type).Duration;
            if (timers[(int)type] > duration) return timers[(int)type];
            return duration;
        }
    }
}
