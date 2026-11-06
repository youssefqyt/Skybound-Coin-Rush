using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// The Sky Hunter: an original airborne pursuit drone that creeps closer with
    /// every crash and lunges when the run ends.
    /// </summary>
    public class SkyHunter : MonoBehaviour
    {
        private Transform visual;
        private SpriteRenderer body;
        private SpriteRenderer bodyEdge;
        private SpriteRenderer eye;
        private SpriteRenderer eyeGlow;
        private SpriteRenderer leftClaw;
        private SpriteRenderer rightClaw;
        private SpriteRenderer thruster;
        private SpriteRenderer thrusterGlow;
        private SpriteRenderer lightBar;
        private SpriteRenderer shadow;

        private float threat;
        private float lunge;
        private float phase;
        private float time;
        private float playerX;
        private bool active;

        public static SkyHunter Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("SkyHunter", parent);
            SkyHunter hunter = go.AddComponent<SkyHunter>();
            hunter.Build();
            return hunter;
        }

        private void Build()
        {
            visual = RuntimeUtil.Create("Visual", transform).transform;

            shadow = RuntimeUtil.CreateSprite("Shadow", visual, Art.Shadow, 180,
                new Color(0f, 0f, 0f, 0.5f), new Vector3(0f, -0.2f, 0f), new Vector3(3.6f, 0.8f, 1f));

            thrusterGlow = RuntimeUtil.CreateGlow("ThrusterGlow", visual, 190,
                new Color(1f, 0.3f, 0.15f, 0.55f), new Vector3(0f, 0.05f, 0f), 2.6f);
            thruster = RuntimeUtil.CreateSprite("Thruster", visual, Art.Tapered(64, 128), 191,
                new Color(1f, 0.55f, 0.2f, 0.85f), new Vector3(0f, 0.15f, 0f),
                new Vector3(0.9f, 1.4f, 1f), true);

            body = RuntimeUtil.CreateSprite("Hull", visual, Art.RoundedSprite(128, 64, 26), 200,
                new Color(0.09f, 0.12f, 0.18f, 1f), new Vector3(0f, 0.85f, 0f),
                new Vector3(2.1f, 0.85f, 1f));
            bodyEdge = RuntimeUtil.CreateSprite("HullEdge", visual, Art.RoundedSprite(128, 64, 26), 201,
                new Color(0.35f, 0.18f, 0.5f, 0.9f), new Vector3(0f, 0.85f, 0f),
                new Vector3(2.0f, 0.75f, 1f));
            body.sprite = Art.RoundedSprite(128, 64, 26);

            lightBar = RuntimeUtil.CreateSprite("LightBar", visual, Art.Streak(), 203,
                new Color(1f, 0.25f, 0.2f, 0.8f), new Vector3(0f, 0.62f, 0f),
                new Vector3(1.4f, 0.18f, 1f), true);

            leftClaw = RuntimeUtil.CreateSprite("LeftClaw", visual, Art.Tapered(64, 128), 205,
                new Color(0.18f, 0.2f, 0.28f, 1f), new Vector3(-1.05f, 0.85f, 0f),
                new Vector3(0.55f, 0.95f, 1f));
            leftClaw.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            rightClaw = RuntimeUtil.CreateSprite("RightClaw", visual, Art.Tapered(64, 128), 205,
                new Color(0.18f, 0.2f, 0.28f, 1f), new Vector3(1.05f, 0.85f, 0f),
                new Vector3(0.55f, 0.95f, 1f));
            rightClaw.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);

            eyeGlow = RuntimeUtil.CreateGlow("EyeGlow", visual, 206,
                new Color(1f, 0.2f, 0.25f, 0.6f), new Vector3(0f, 0.95f, 0f), 1.5f);
            eye = RuntimeUtil.CreateSprite("Eye", visual, Art.Circle(64), 207,
                new Color(1f, 0.25f, 0.3f, 0.95f), new Vector3(0f, 0.95f, 0f),
                new Vector3(0.34f, 0.34f, 1f), true);

            gameObject.SetActive(false);
        }

        public void Show()
        {
            active = true;
            gameObject.SetActive(true);
            threat = 0f;
            lunge = 0f;
            phase = 0f;
            SetThreat(0f);
        }

        public void Hide()
        {
            active = false;
            gameObject.SetActive(false);
        }

        public void SetThreat(float value)
        {
            threat = Mathf.Clamp01(value);
            if (!active) return;
            Apply();
        }

        public void Lunge()
        {
            lunge = 1f;
        }

        public void Tick(float deltaTime, float playerWorldX)
        {
            if (!active) return;
            time += deltaTime;
            phase += deltaTime;
            playerX = playerWorldX;

            lunge = Mathf.MoveTowards(lunge, 0f, deltaTime * 0.6f);
            Apply();

            // Menacing pulse
            float pulse = 0.55f + 0.45f * Mathf.Sin(time * 7f);
            eyeGlow.color = new Color(1f, 0.18f, 0.22f, 0.35f + pulse * (0.35f + threat * 0.4f));
            float eyeScale = 0.30f + 0.05f * pulse + threat * 0.08f;
            eye.transform.localScale = new Vector3(eyeScale, eyeScale, 1f);
            thrusterGlow.color = new Color(1f, 0.35f, 0.15f, 0.35f + threat * 0.35f + lunge * 0.3f);
            thruster.transform.localScale = new Vector3(0.9f, 1.1f + pulse * 0.3f + lunge * 0.9f, 1f);
            lightBar.color = new Color(1f, 0.25f, 0.2f, 0.5f + pulse * 0.4f);
            float clawAngle = 28f + pulse * 6f - lunge * 26f;
            leftClaw.transform.localRotation = Quaternion.Euler(0f, 0f, -clawAngle);
            rightClaw.transform.localRotation = Quaternion.Euler(0f, 0f, clawAngle);
            visual.localPosition = new Vector3(0f, Mathf.Sin(phase * 2.2f) * 0.05f, 0f);
        }

        private void Apply()
        {
            float y = Mathf.Lerp(-6.1f, -4.55f, threat);
            y += lunge * 0.75f;
            float x = Mathf.Clamp(playerX * 0.55f, -1.8f, 1.8f);
            float scale = Mathf.Lerp(0.85f, 1.32f, threat) + lunge * 0.35f;
            transform.localPosition = new Vector3(x, y, 0f);
            transform.localScale = new Vector3(scale, scale, 1f);
            int order = 200;
            SpriteRenderer[] all = { shadow, thrusterGlow, thruster, body, bodyEdge, lightBar, leftClaw,
                rightClaw, eyeGlow, eye };
            for (int i = 0; i < all.Length; i++) all[i].sortingOrder = order + i;
            shadow.transform.localScale = new Vector3(3.6f, 0.8f, 1f);
            body.transform.localScale = new Vector3(2.1f, 0.85f, 1f);
            bodyEdge.transform.localScale = new Vector3(2.0f, 0.75f, 1f);
            Color hull = Color.Lerp(new Color(0.09f, 0.12f, 0.18f, 1f),
                new Color(0.28f, 0.08f, 0.12f, 1f), threat * 0.65f + lunge * 0.35f);
            body.color = hull;
            bodyEdge.color = new Color(0.35f, 0.18f, 0.5f, 0.6f + threat * 0.35f);
        }
    }
}
