using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// A single collectible coin. Runs are fully pooled through EndlessWorld.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        public CoinType Type { get; private set; }
        public int Lane { get; private set; }
        public float Z { get; private set; }
        public float WorldX { get; private set; }
        public float Height { get; private set; }
        public bool Collected { get; private set; }

        private SpriteRenderer glow;
        private SpriteRenderer disc;
        private SpriteRenderer shine;
        private Transform visual;
        private float spin;
        private float bob;
        private float collectTimer;
        private float baseScale = 1f;

        public static Coin Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("Coin", parent);
            Coin coin = go.AddComponent<Coin>();
            coin.Build();
            return coin;
        }

        private void Build()
        {
            visual = RuntimeUtil.Create("Visual", transform).transform;
            glow = RuntimeUtil.CreateGlow("Glow", visual, 300, new Color(1f, 0.85f, 0.3f, 0.55f),
                Vector3.zero, 1.35f);
            disc = RuntimeUtil.CreateSprite("Disc", visual, Art.Coin(CoinType.Gold), 301, Color.white,
                Vector3.zero, Vector3.one);
            shine = RuntimeUtil.CreateSprite("Shine", visual, Art.Glow(), 302,
                new Color(1f, 1f, 1f, 0.35f), new Vector3(-0.05f, 0.16f, 0f), new Vector3(0.45f, 0.28f, 1f), true);
        }

        public void Setup(CoinType type, int lane, float z, float height)
        {
            Type = type;
            Lane = lane;
            Z = z;
            Height = height;
            WorldX = GameConfig.LaneX[lane];
            Collected = false;
            collectTimer = 0f;
            spin = Random.value * 6.28f;
            bob = Random.value * 6.28f;
            baseScale = type == CoinType.Gold ? 1.05f : (type == CoinType.Special ? 1.15f : 0.95f);
            Sprite sprite = Art.Coin(type);
            disc.sprite = sprite;
            Color c = GameConfig.CoinColors[(int)type];
            glow.color = new Color(c.r, c.g, c.b, 0.5f);
            transform.localScale = Vector3.one;
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
            UpdateTransform(0f, 0f, false);
        }

        public void Tick(float deltaTime, float speed, bool playing)
        {
            if (Collected)
            {
                collectTimer += deltaTime;
                float t = Mathf.Clamp01(collectTimer / 0.28f);
                visual.localScale = Vector3.one * (1f + t * 1.6f) * Mathf.Lerp(1f, 0.25f, t);
                Color gc = glow.color;
                gc.a = (1f - t) * 0.7f;
                glow.color = gc;
                if (t >= 1f) gameObject.SetActive(false);
                return;
            }

            if (playing) Z -= speed * deltaTime;
            spin += deltaTime * 4.2f;
            bob += deltaTime * 3.1f;
            UpdateTransform(deltaTime, bob, true);
        }

        private void UpdateTransform(float dt, float bobValue, bool animate)
        {
            float k = Projection.Scale(Z);
            Vector3 p = Projection.PositionAtX(WorldX, Z, Height);
            if (animate) p.y += Mathf.Sin(bobValue) * 0.09f * k;
            transform.localPosition = p;
            float scale = k * baseScale;
            transform.localScale = new Vector3(scale, scale, 1f);
            if (animate)
            {
                float squeeze = Mathf.Abs(Mathf.Cos(spin));
                visual.localScale = new Vector3(Mathf.Lerp(0.22f, 1f, squeeze), 1f, 1f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(spin * 0.5f) * 8f);
            }
            int order = Projection.SortingOrder(Z, 320);
            glow.sortingOrder = order;
            disc.sortingOrder = order + 1;
            shine.sortingOrder = order + 2;
            glow.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            collectTimer = 0f;
            if (glow != null) glow.gameObject.SetActive(true);
        }

        public void MagnetPull(float targetX, float targetHeight, float strength, float deltaTime)
        {
            if (Collected) return;
            float t = Mathf.Clamp01(strength * deltaTime);
            WorldX = Mathf.Lerp(WorldX, targetX, t);
            Height = Mathf.Lerp(Height, targetHeight, t);
        }

        /// <summary>Moves the coin toward the player by a world-space distance.</summary>
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
