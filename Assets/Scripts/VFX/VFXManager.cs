using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Lightweight pooled particle system for dust, sparks, confetti and speed lines.
    /// Avoids ParticleSystem overhead and keeps the build fully procedural.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        private class Particle
        {
            public SpriteRenderer Renderer;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Life;
            public float MaxLife;
            public float StartScale;
            public float EndScale;
            public Color StartColor;
            public Color EndColor;
            public float Gravity;
            public float Drag;
            public float Rotation;
            public float Spin;
            public float Wobble;
            public float Phase;
        }

        public static VFXManager Instance { get; private set; }

        private readonly List<Particle> active = new List<Particle>();
        private readonly Stack<Particle> free = new Stack<Particle>();
        private Transform root;
        private int maxParticles = 190;
        private float speedLinesTimer;

        public static VFXManager Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("Effects", parent);
            VFXManager manager = go.AddComponent<VFXManager>();
            manager.Build();
            return manager;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Build()
        {
            root = RuntimeUtil.Create("Particles", transform).transform;
        }

        public void Tick(float deltaTime, float speed)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Particle p = active[i];
                p.Life -= deltaTime;
                if (p.Life <= 0f)
                {
                    active.RemoveAt(i);
                    p.Renderer.gameObject.SetActive(false);
                    free.Push(p);
                    continue;
                }
                float t = 1f - Mathf.Clamp01(p.Life / p.MaxLife);
                p.Velocity.y -= p.Gravity * deltaTime;
                p.Velocity *= 1f / (1f + p.Drag * deltaTime);
                p.Position += p.Velocity * deltaTime;
                p.Position.x += Mathf.Sin((t + p.Phase) * 8f) * p.Wobble * deltaTime;
                float scale = Mathf.Lerp(p.StartScale, p.EndScale, t);
                p.Renderer.transform.localPosition = p.Position;
                p.Renderer.transform.localScale = new Vector3(scale, scale, 1f);
                p.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, p.Rotation + p.Spin * t);
                Color c = Color.Lerp(p.StartColor, p.EndColor, t);
                c.a = p.StartColor.a * (1f - t * t);
                p.Renderer.color = c;
            }

            if (speed > 0f)
            {
                speedLinesTimer -= deltaTime;
                if (speedLinesTimer <= 0f && speed > 16f)
                {
                    speedLinesTimer = Mathf.Lerp(0.24f, 0.06f, Mathf.InverseLerp(16f, 32f, speed));
                    SpawnSpeedLine(speed);
                }
            }
        }

        public void SetBudget(int max)
        {
            maxParticles = Mathf.Max(24, max);
        }

        public void Clear()
        {
            for (int i = 0; i < active.Count; i++)
            {
                active[i].Renderer.gameObject.SetActive(false);
                free.Push(active[i]);
            }
            active.Clear();
        }

        private Particle Get()
        {
            Particle p;
            if (free.Count > 0)
            {
                p = free.Pop();
            }
            else
            {
                if (active.Count >= maxParticles) return null;
                GameObject go = RuntimeUtil.Create("Particle", root);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                p = new Particle();
                p.Renderer = sr;
            }
            p.Renderer.gameObject.SetActive(true);
            active.Add(p);
            return p;
        }

        private void Spawn(Sprite sprite, Vector3 position, Vector3 velocity, float life, float startScale,
            float endScale, Color startColor, Color endColor, bool additive, float gravity = 0f,
            float drag = 1.2f, float rotation = 0f, float spin = 0f, float wobble = 0f, int order = 900)
        {
            Particle p = Get();
            if (p == null) return;
            p.Renderer.sprite = sprite;
            p.Renderer.sharedMaterial = additive ? Art.AdditiveMaterial : Art.SpriteMaterial;
            p.Renderer.sortingOrder = order;
            p.Renderer.color = startColor;
            p.Position = position;
            p.Velocity = velocity;
            p.Life = life;
            p.MaxLife = life;
            p.StartScale = startScale;
            p.EndScale = endScale;
            p.StartColor = startColor;
            p.EndColor = endColor;
            p.Gravity = gravity;
            p.Drag = drag;
            p.Rotation = rotation;
            p.Spin = spin;
            p.Wobble = wobble;
            p.Phase = Random.value * 6f;
            p.Renderer.transform.localPosition = position;
            p.Renderer.transform.localScale = new Vector3(startScale, startScale, 1f);
            p.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static Sprite Pick(params Sprite[] options)
        {
            return options[Random.Range(0, options.Length)];
        }

        public void JumpDust(Vector3 position)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 pos = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.02f, 0f);
                Vector3 vel = new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(0.4f, 1.8f), 0f);
                Spawn(Art.SoftCircle(), pos, vel, Random.Range(0.22f, 0.42f), 0.22f, 0.05f,
                    new Color(0.7f, 0.95f, 1f, 0.55f), new Color(0.5f, 0.7f, 1f, 0f), true, 1.2f, 3f);
            }
        }

        public void LandDust(Vector3 position)
        {
            for (int i = 0; i < 10; i++)
            {
                float dir = i % 2 == 0 ? -1f : 1f;
                Vector3 vel = new Vector3(dir * Random.Range(1f, 3.4f), Random.Range(0.3f, 1.4f), 0f);
                Spawn(Art.SoftCircle(), position + new Vector3(dir * 0.25f, 0.02f, 0f), vel,
                    Random.Range(0.28f, 0.5f), 0.26f, 0.04f,
                    new Color(0.8f, 0.95f, 1f, 0.5f), new Color(0.4f, 0.6f, 0.9f, 0f), true, 2.2f, 3.5f);
            }
        }

        public void SlideDust(Vector3 position)
        {
            for (int i = 0; i < 9; i++)
            {
                Vector3 vel = new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 2.4f), 0f);
                Spawn(Art.SoftCircle(), position + new Vector3(0f, 0.08f, 0f), vel,
                    Random.Range(0.3f, 0.55f), 0.3f, 0.05f,
                    new Color(1f, 0.85f, 0.5f, 0.4f), new Color(0.9f, 0.4f, 0.2f, 0f), true, 1.4f, 2.4f);
            }
        }

        public void CoinCollect(Vector3 position, Color color, bool big)
        {
            int count = big ? 14 : 7;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(1.2f, big ? 4.6f : 3f);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 0.8f, 0f);
                Spawn(Pick(Art.StarSmall(), Art.Spark(), Art.Circle(32)), position, vel,
                    Random.Range(0.25f, 0.55f), Random.Range(0.1f, 0.2f), 0.02f,
                    new Color(color.r, color.g, color.b, 1f), new Color(1f, 1f, 1f, 0f), true, 2.5f, 2.2f,
                    Random.Range(0f, 360f), Random.Range(-180f, 180f));
            }
        }

        public void HitBurst(Vector3 position)
        {
            for (int i = 0; i < 18; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(2.4f, 7.5f);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1.5f, 0f);
                Spawn(Pick(Art.Spark(), Art.StarSmall(), Art.Streak()), position + Vector3.up * 0.9f, vel,
                    Random.Range(0.3f, 0.65f), Random.Range(0.16f, 0.34f), 0.02f,
                    new Color(1f, 0.55f, 0.15f, 1f), new Color(1f, 0.1f, 0.1f, 0f), true, 6f, 1.6f,
                    Random.Range(0f, 360f), Random.Range(-420f, 420f));
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 vel = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2.2f), 0f);
                Spawn(Art.SoftCircle(), position + Vector3.up * 0.7f, vel, 0.6f, 0.7f, 1.6f,
                    new Color(0.5f, 0.1f, 0.1f, 0.5f), new Color(0.2f, 0.05f, 0.08f, 0f), false, 0.4f, 1f);
            }
        }

        public void ShieldBreak(Vector3 position)
        {
            for (int i = 0; i < 22; i++)
            {
                float angle = i / 22f * Mathf.PI * 2f;
                Vector3 vel = new Vector3(Mathf.Cos(angle) * 4.2f, Mathf.Sin(angle) * 4.2f + 0.5f, 0f);
                Spawn(Pick(Art.Spark(), Art.Circle(32)), position + Vector3.up * 0.95f, vel, 0.45f,
                    0.22f, 0.03f, new Color(0.7f, 0.45f, 1f, 1f), new Color(0.3f, 0.1f, 0.8f, 0f), true, 1f, 2f,
                    Random.Range(0f, 360f), Random.Range(-200f, 200f));
            }
            Spawn(Art.Ring(), position + Vector3.up * 0.95f, Vector3.zero, 0.4f, 0.5f, 3.2f,
                new Color(0.8f, 0.6f, 1f, 0.9f), new Color(0.4f, 0.2f, 1f, 0f), true, 0f, 0f);
        }

        public void PowerUpBurst(Vector3 position, Color color)
        {
            for (int i = 0; i < 18; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(1.6f, 5.2f);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
                Spawn(Pick(Art.Spark(), Art.StarSmall(), Art.Circle(32)), position, vel,
                    Random.Range(0.3f, 0.7f), Random.Range(0.12f, 0.28f), 0.02f,
                    new Color(color.r, color.g, color.b, 1f), new Color(1f, 1f, 1f, 0f), true, 1.5f, 2.2f,
                    Random.Range(0f, 360f), Random.Range(-220f, 220f));
            }
            Spawn(Art.Ring(), position, Vector3.zero, 0.5f, 0.4f, 3.4f,
                new Color(color.r, color.g, color.b, 0.9f), new Color(color.r, color.g, color.b, 0f), true);
        }

        public void NearMiss(Vector3 position, int direction)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 vel = new Vector3(-direction * Random.Range(2f, 5f), Random.Range(-0.4f, 0.9f), 0f);
                Spawn(Art.Streak(), position + new Vector3(direction * 0.5f, 0.9f, 0f), vel, 0.3f,
                    0.5f, 0.05f, new Color(1f, 0.9f, 0.5f, 0.9f), new Color(1f, 0.4f, 0.2f, 0f), true);
            }
        }

        public void Confetti(Vector3 position)
        {
            for (int i = 0; i < 26; i++)
            {
                Color c = i % 4 == 0 ? Palette.Accent : (i % 4 == 1 ? Palette.Primary :
                    (i % 4 == 2 ? Palette.Secondary : Palette.Success));
                Vector3 vel = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(2f, 6f), 0f);
                Spawn(Pick(Art.RoundedSprite(16, 10, 3), Art.StarSmall()), position + Vector3.up * 0.6f, vel,
                    Random.Range(0.6f, 1.3f), 0.2f, 0.05f, c, new Color(c.r, c.g, c.b, 0f), false, 5.5f, 0.6f,
                    Random.Range(0f, 360f), Random.Range(-360f, 360f), 0.8f);
            }
        }

        public void LevelUpBurst(Vector3 position)
        {
            for (int i = 0; i < 30; i++)
            {
                float angle = i / 30f * Mathf.PI * 2f;
                float speed = Random.Range(2f, 5f);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1.5f, 0f);
                Spawn(Art.StarSmall(), position, vel, Random.Range(0.5f, 1.1f), 0.24f, 0.03f,
                    new Color(1f, 0.9f, 0.3f, 1f), new Color(1f, 0.5f, 0.1f, 0f), true, 2f, 1.2f,
                    Random.Range(0f, 360f), Random.Range(-200f, 200f));
            }
        }

        public void Exclamation(Vector3 position, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 vel = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(1.5f, 3f), 0f);
                Spawn(Art.Spark(), position, vel, 0.4f, 0.35f, 0.02f,
                    new Color(color.r, color.g, color.b, 1f), new Color(color.r, color.g, color.b, 0f), true, 2f, 1.5f);
            }
        }

        public void Trail(Vector3 position, Color color)
        {
            Spawn(Art.SoftCircle(), position, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.6f), 0f),
                0.35f, 0.3f, 0.02f, new Color(color.r, color.g, color.b, 0.5f),
                new Color(color.r, color.g, color.b, 0f), true, 0f, 1.5f);
        }

        private void SpawnSpeedLine(float speed)
        {
            float t = Mathf.InverseLerp(16f, 32f, speed);
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = side * Random.Range(4.2f, 8.2f);
            float y = Random.Range(-4.2f, 2.2f);
            Vector3 vel = new Vector3(0f, -speed * 0.55f, 0f);
            Spawn(Art.Streak(), new Vector3(x, y, 0f), vel, Random.Range(0.18f, 0.35f),
                Mathf.Lerp(0.5f, 1.4f, t), 0.1f, new Color(0.7f, 0.95f, 1f, 0.12f + t * 0.18f),
                new Color(0.6f, 0.9f, 1f, 0f), true, 0f, 0.2f, 0f, 0f, 880);
        }
    }
}
