using UnityEngine;

namespace Skybound
{
    public enum CameraMode
    {
        Menu,
        Gameplay,
        GameOver,
        LevelComplete
    }

    public class CameraRig : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public CameraMode Mode { get; private set; } = CameraMode.Menu;

        private Transform cam;
        private Vector3 basePosition;
        private float targetSize = 5.4f;
        private float currentSize = 5.4f;
        private float shake;
        private float shakeDuration;
        private float shakeTimer;
        private float speed01;
        private float menuTime;
        private float extraZoom;
        private Vector3 velocity;
        private Vector3 shakeOffset;

        public static CameraRig Create(Transform parent)
        {
            GameObject go = new GameObject("CameraRig");
            if (parent != null) go.transform.SetParent(parent, false);
            CameraRig rig = go.AddComponent<CameraRig>();
            rig.Build();
            return rig;
        }

        private void Build()
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";
            Camera = camGo.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = 5.4f;
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 100f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Palette.Background;
            Camera.allowHDR = false;
            Camera.allowMSAA = false;
            camGo.AddComponent<AudioListener>();
            cam = camGo.transform;
            cam.position = new Vector3(-1.4f, 0f, -10f);
            basePosition = new Vector3(-1.4f, 0f, -10f);
        }

        public void SetMode(CameraMode mode)
        {
            Mode = mode;
            menuTime = 0f;
            switch (mode)
            {
                case CameraMode.Menu:
                    targetSize = 5.35f;
                    extraZoom = 0f;
                    break;
                case CameraMode.Gameplay:
                    targetSize = 5.4f;
                    extraZoom = 0f;
                    break;
                case CameraMode.GameOver:
                    targetSize = 4.65f;
                    extraZoom = 0f;
                    AddShake(0.22f, 0.5f);
                    break;
                case CameraMode.LevelComplete:
                    targetSize = 6.1f;
                    extraZoom = 0f;
                    break;
            }
        }

        public void AddShake(float strength, float duration)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null &&
                !SaveManager.Instance.Data.Settings.ScreenShake) return;
            shake = Mathf.Max(shake, strength);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = Mathf.Max(shakeTimer, duration);
        }

        public void SetSpeed01(float value)
        {
            speed01 = Mathf.Clamp01(value);
        }

        public void Pulse(float amount)
        {
            extraZoom += amount;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            menuTime += dt;

            Vector3 desired = Vector3.zero;
            float desiredSize = targetSize;
            switch (Mode)
            {
                case CameraMode.Menu:
                    desired = new Vector3(-1.5f + Mathf.Sin(menuTime * 0.22f) * 0.45f,
                        0.05f + Mathf.Sin(menuTime * 0.31f) * 0.12f, -10f);
                    desiredSize = 5.05f + Mathf.Sin(menuTime * 0.18f) * 0.08f;
                    break;
                case CameraMode.Gameplay:
                {
                    PlayerController player = GameManager.Instance != null ? GameManager.Instance.Player : null;
                    float laneOffset = player != null ? player.VisualX * 0.10f : 0f;
                    float jumpOffset = player != null ? player.VisualHeight * 0.05f : 0f;
                    desired = new Vector3(laneOffset, -0.12f + jumpOffset, -10f);
                    desiredSize = 5.4f - speed01 * 0.18f;
                    break;
                }
                case CameraMode.GameOver:
                    desired = new Vector3(0f, -0.45f, -10f);
                    break;
                case CameraMode.LevelComplete:
                    desired = new Vector3(0f, 0.1f, -10f);
                    break;
            }

            float smooth = Mode == CameraMode.Gameplay ? 0.10f : 0.22f;
            basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smooth, Mathf.Infinity, dt);
            currentSize = Mathf.Lerp(currentSize, desiredSize, 1f - Mathf.Exp(-6f * dt));

            shakeTimer -= dt;
            if (shakeTimer <= 0f)
            {
                shake = 0f;
                shakeOffset = Vector3.zero;
            }
            else
            {
                float decay = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                float t = Time.unscaledTime * 37f;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shake * decay,
                    (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shake * decay,
                    0f);
            }

            extraZoom = Mathf.Lerp(extraZoom, 0f, 1f - Mathf.Exp(-4f * dt));
            cam.position = basePosition + shakeOffset;
            Camera.orthographicSize = Mathf.Max(3f, currentSize - extraZoom);
        }

        public void SnapToMenu()
        {
            basePosition = new Vector3(-1.5f, 0.05f, -10f);
            currentSize = 5.05f;
            targetSize = 5.05f;
            cam.position = basePosition;
            Camera.orthographicSize = currentSize;
        }
    }
}
