using System;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Timed pickup chain. Collecting coins raises the combo, which boosts score,
    /// and any collision (or a lull in pickups) breaks it.
    /// </summary>
    public class ComboManager : MonoBehaviour
    {
        public int Count { get; private set; }
        public float Timer { get; private set; }
        public int Best { get; private set; }
        public float Window = 2.5f;

        public event Action<int> Changed;
        public event Action<int> Broken;

        public float Multiplier
        {
            get { return 1f + Mathf.Min(Count, 30) * 0.1f; }
        }

        public float Timer01
        {
            get
            {
                if (Count <= 0) return 0f;
                return Mathf.Clamp01(Timer / Window);
            }
        }

        public static ComboManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("ComboManager", parent);
            return go.AddComponent<ComboManager>();
        }

        public void ResetRun()
        {
            Count = 0;
            Timer = 0f;
            Best = 0;
            if (Changed != null) Changed(Count);
        }

        public int RegisterPickup()
        {
            Count++;
            Timer = Window;
            if (Count > Best) Best = Count;
            if (Changed != null) Changed(Count);
            return Count;
        }

        public int RegisterNearMiss()
        {
            return RegisterPickup();
        }

        public void Tick(float deltaTime)
        {
            if (Count <= 0) return;
            Timer -= deltaTime;
            if (Timer <= 0f)
            {
                Break(true);
            }
        }

        public void Break(bool notify)
        {
            if (Count <= 0) return;
            int previous = Count;
            Count = 0;
            Timer = 0f;
            if (Changed != null) Changed(Count);
            if (notify && Broken != null) Broken(previous);
        }

        public void Restore(int count)
        {
            Count = count;
            Timer = Window;
            if (Changed != null) Changed(Count);
        }
    }
}
