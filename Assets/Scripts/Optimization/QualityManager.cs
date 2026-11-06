using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Applies performance settings and gently adjusts quality if the device cannot
    /// hold the target frame rate. Keeps mobile builds smooth on low-end hardware.
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        public int CurrentLevel { get; private set; } = 2;

        private float sampleTimer;
        private int sampleFrames;
        private float goodTimer;
        private int badSeconds;

        public static QualityManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("QualityManager", parent);
            return go.AddComponent<QualityManager>();
        }

        public void Apply()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            SettingsData settings = SaveManager.Instance.Data.Settings;
            CurrentLevel = Mathf.Clamp(settings.QualityLevel, 0, 3);
            int targetFps = settings.FpsTarget <= 30 ? 30 : 60;
            if (settings.LowEndMode) targetFps = 30;
            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = CurrentLevel >= 2 ? 2 : 0;
            QualitySettings.softParticles = false;
            if (VFXManager.Instance != null)
                VFXManager.Instance.SetBudget(BudgetFor(CurrentLevel));
        }

        public static int BudgetFor(int level)
        {
            switch (level)
            {
                case 0: return 70;
                case 1: return 120;
                case 2: return 190;
                default: return 260;
            }
        }

        public void SetLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, 3);
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.Settings.QualityLevel = CurrentLevel;
                SaveManager.Instance.MarkDirty();
            }
            Apply();
        }

        public void Tick(float deltaTime)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            SettingsData settings = SaveManager.Instance.Data.Settings;
            if (!settings.AutoQuality) return;

            sampleTimer += deltaTime;
            sampleFrames++;
            if (sampleTimer < 1f) return;

            float fps = sampleFrames / sampleTimer;
            sampleTimer = 0f;
            sampleFrames = 0;

            int target = settings.FpsTarget <= 30 ? 30 : 60;
            if (fps < target * 0.72f)
            {
                badSeconds++;
                goodTimer = 0f;
                if (badSeconds >= 3 && CurrentLevel > 0)
                {
                    badSeconds = 0;
                    SetLevel(CurrentLevel - 1);
                }
            }
            else if (fps > target * 0.95f)
            {
                badSeconds = 0;
                goodTimer += 1f;
                if (goodTimer >= 8f && CurrentLevel < 3)
                {
                    goodTimer = 0f;
                    SetLevel(CurrentLevel + 1);
                }
            }
            else
            {
                badSeconds = 0;
                goodTimer = 0f;
            }
        }
    }
}
