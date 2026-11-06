using UnityEngine;

namespace Skybound
{
    /// <summary>Floating power-up crate pooled by EndlessWorld.</summary>
    public class PowerUpPickup : MonoBehaviour
    {
        public PowerUpType Type { get; private set; }
        public int Lane { get; private set; }
        public float Z { get; private set; }
        public float WorldX { get; private set; }
        public float Height { get; private set; }
        public bool Collected { get; private set; }

        private Transform visual;
        private SpriteRenderer glow;
        private SpriteRenderer ring;
        private SpriteRenderer badge;
        private float spin;
        private float bob;
        private float collectTimer;

        public static PowerUpPickup Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("PowerUp", parent);
            PowerUpPickup pickup = go.AddComponent<PowerUpPickup>();
            pickup.Build();
            return pickup;
        }

        private void Build()
        {
            visual = RuntimeUtil.Create("Visual", transform).transform;
            glow = RuntimeUtil.CreateGlow("Glow", visual, 330, new Color(0f, 0.9f, 1f, 0.45f), Vector3.zero, 2.0f);
            ring = RuntimeUtil.CreateSprite("Ring", visual, Art.Ring(), 331, new Color(1f, 1f, 1f, 0.7f),
                Vector3.zero, new Vector3(1f, 1f, 1f), true);
            badge = RuntimeUtil.CreateSprite("Badge", visual, Art.PowerUpBadge(PowerUpType.Magnet), 332,
                Color.white, Vector3.zero, Vector3.one);
            gameObject.SetActive(false);
        }

        public void Setup(PowerUpType type, int lane, float z)
        {
            Type = type;
            Lane = lane;
            Z = z;
            Height = 1.15f;
            WorldX = GameConfig.LaneX[lane];
            Collected = false;
            spin = Random.value * 6.28f;
            bob = Random.value * 6.28f;
            collectTimer = 0f;
            badge.sprite = Art.PowerUpBadge(type);
            Color c = GameConfig.GetPowerUp(type).Color;
            glow.color = new Color(c.r, c.g, c.b, 0.5f);
            ring.color = new Color(c.r, c.g, c.b, 0.75f);
            visual.localScale = Vector3.one;
            gameObject.SetActive(true);
            UpdateTransform(0f, true);
        }

        public void Tick(float deltaTime, float speed, bool playing)
        {
            if (Collected)
            {
                collectTimer += deltaTime;
                float t = Mathf.Clamp01(collectTimer / 0.35f);
                visual.localScale = Vector3.one * (1f + t * 2.2f) * Mathf.Lerp(1f, 0.2f, t);
                Color gc = glow.color;
                gc.a = (1f - t) * 0.8f;
                glow.color = gc;
                if (t >= 1f) gameObject.SetActive(false);
                return;
            }

            if (playing) Z -= speed * deltaTime;
            spin += deltaTime * 2.6f;
            bob += deltaTime * 2.4f;
            UpdateTransform(deltaTime, false);
        }

        private void UpdateTransform(float dt, bool snap)
        {
            float k = Projection.Scale(Z);
            Vector3 p = Projection.Position(Lane, Z, Height);
            p.y += Mathf.Sin(bob) * 0.12f * k;
            transform.localPosition = p;
            transform.localScale = new Vector3(k, k, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(spin) * 10f);
            badge.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(spin * 2f) * 12f, 0f);
            ring.transform.localScale = new Vector3(1.05f + Mathf.Sin(spin * 3f) * 0.08f,
                1.05f + Mathf.Sin(spin * 3f) * 0.08f, 1f);
            int order = Projection.SortingOrder(Z, 330);
            glow.sortingOrder = order;
            ring.sortingOrder = order + 1;
            badge.sortingOrder = order + 2;
            glow.transform.localScale = new Vector3(2f, 2f, 1f);
        }

        public void Collect()
        {
            Collected = true;
            collectTimer = 0f;
        }

        /// <summary>Moves the pickup toward the player by a world-space distance.</summary>
        public void Advance(float meters)
        {
            if (meters == 0f) return;
            Z -= meters;
        }

        public void ResetPickup()
        {
            Collected = false;
            collectTimer = 0f;
            visual.localScale = Vector3.one;
            gameObject.SetActive(true);
        }
    }
}
