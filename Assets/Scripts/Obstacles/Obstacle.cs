using UnityEngine;

namespace Skybound
{
    public enum DodgeRequirement
    {
        AvoidLane,
        Jump,
        HighJump,
        Slide
    }

    /// <summary>
    /// A pooled hazard on the skyway. All geometry is procedural and tinted per kind.
    /// Gameplay collision is resolved in lane space by EndlessWorld.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        public ObstacleKind Kind { get; private set; }
        public DodgeRequirement Requirement { get; private set; }
        public int Lane { get; private set; }
        public float Z { get; private set; }
        public float WorldX { get; private set; }
        public float Depth { get; private set; }
        public float Clearance { get; private set; }
        public bool Passed { get; private set; }
        public bool NearMissed { get; private set; }

        private Transform visual;
        private SpriteRenderer body;
        private SpriteRenderer bodyShade;
        private SpriteRenderer bodyGlow;
        private SpriteRenderer stripe;
        private SpriteRenderer leftPost;
        private SpriteRenderer rightPost;
        private SpriteRenderer topBar;
        private SpriteRenderer beam;
        private SpriteRenderer beamCore;
        private SpriteRenderer lightGlow;
        private SpriteRenderer window;
        private SpriteRenderer rotorA;
        private SpriteRenderer rotorB;
        private SpriteRenderer shadow;
        private SpriteRenderer gapHole;
        private SpriteRenderer gapEdge;

        private float phase;
        private float flash;
        private Color accent;

        private static readonly Color BarrierColor = Palette.Hex("#EF4444");
        private static readonly Color WarningColor = Palette.Hex("#FFD700");
        private static readonly Color GateColor = Palette.Hex("#00E5FF");
        private static readonly Color VehicleColor = Palette.Hex("#7C3AED");
        private static readonly Color DarkMetal = Palette.Hex("#101C2E");
        private static readonly Color LaserColor = Palette.Hex("#FF3B5C");

        public static Obstacle Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("Obstacle", parent);
            Obstacle obstacle = go.AddComponent<Obstacle>();
            obstacle.Build();
            return obstacle;
        }

        private void Build()
        {
            visual = RuntimeUtil.Create("Visual", transform).transform;

            shadow = RuntimeUtil.CreateSprite("Shadow", visual, Art.Shadow, 250,
                new Color(0f, 0f, 0f, 0.4f), new Vector3(0f, 0f, 0f), new Vector3(2.4f, 0.8f, 1f));

            body = RuntimeUtil.CreateSprite("Body", visual, Art.RoundedSprite(64, 96, 10), 305,
                BarrierColor, Vector3.zero, new Vector3(1f, 1.2f, 1f));
            bodyShade = RuntimeUtil.CreateSprite("BodyShade", visual, Art.RoundedSprite(64, 96, 10), 306,
                new Color(0f, 0f, 0f, 0.28f), Vector3.zero, new Vector3(1f, 0.5f, 1f));
            bodyGlow = RuntimeUtil.CreateSprite("BodyGlow", visual, Art.Glow(), 304,
                new Color(1f, 0.2f, 0.2f, 0.25f), Vector3.zero, new Vector3(2.4f, 2.6f, 1f), true);
            stripe = RuntimeUtil.CreateSprite("Stripe", visual, Art.RoundedSprite(64, 16, 7), 307,
                Color.white, Vector3.zero, new Vector3(1f, 0.14f, 1f));
            window = RuntimeUtil.CreateSprite("Window", visual, Art.RoundedSprite(64, 48, 8), 308,
                new Color(0.6f, 0.95f, 1f, 0.85f), Vector3.zero, new Vector3(0.7f, 0.35f, 1f));

            leftPost = RuntimeUtil.CreateSprite("LeftPost", visual, Art.RoundedSprite(16, 96, 7), 305,
                DarkMetal, Vector3.zero, new Vector3(0.16f, 1.5f, 1f));
            rightPost = RuntimeUtil.CreateSprite("RightPost", visual, Art.RoundedSprite(16, 96, 7), 305,
                DarkMetal, Vector3.zero, new Vector3(0.16f, 1.5f, 1f));
            topBar = RuntimeUtil.CreateSprite("TopBar", visual, Art.RoundedSprite(96, 24, 10), 306,
                GateColor, Vector3.zero, new Vector3(1.5f, 0.24f, 1f));

            beam = RuntimeUtil.CreateSprite("Beam", visual, Art.Streak(), 320,
                new Color(1f, 0.25f, 0.4f, 0.55f), Vector3.zero, new Vector3(1.6f, 0.5f, 1f), true);
            beamCore = RuntimeUtil.CreateSprite("BeamCore", visual, Art.LaserBeam(), 321,
                new Color(1f, 0.75f, 0.8f, 0.95f), Vector3.zero, new Vector3(1.6f, 0.1f, 1f), true);

            lightGlow = RuntimeUtil.CreateGlow("WarningLight", visual, 325, WarningColor, Vector3.zero, 0.7f);
            rotorA = RuntimeUtil.CreateSprite("RotorA", visual, Art.RoundedSprite(64, 10, 5), 310,
                new Color(0.6f, 0.85f, 1f, 0.8f), Vector3.zero, new Vector3(0.9f, 0.1f, 1f), true);
            rotorB = RuntimeUtil.CreateSprite("RotorB", visual, Art.RoundedSprite(64, 10, 5), 310,
                new Color(0.6f, 0.85f, 1f, 0.8f), Vector3.zero, new Vector3(0.9f, 0.1f, 1f), true);

            gapHole = RuntimeUtil.CreateSprite("GapHole", visual, Art.White, 260,
                new Color(0.01f, 0.02f, 0.04f, 0.95f), Vector3.zero, new Vector3(2.2f, 3.2f, 1f));
            gapEdge = RuntimeUtil.CreateSprite("GapEdge", visual, Art.RoundedSprite(96, 12, 6), 261,
                WarningColor, Vector3.zero, new Vector3(2.1f, 0.13f, 1f), true);

            gameObject.SetActive(false);
        }

        public void Setup(ObstacleKind kind, int lane, float z, float difficulty)
        {
            Kind = kind;
            Lane = lane;
            Z = z;
            WorldX = GameConfig.LaneX[lane];
            Passed = false;
            NearMissed = false;
            phase = Random.value * 6.28f;
            flash = 0f;
            Depth = 0.75f;
            Clearance = 1.2f;
            transform.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            ApplyVisual(kind, difficulty);
            gameObject.SetActive(true);
            UpdateTransform(0f, true);
        }

        private void ApplyVisual(ObstacleKind kind, float difficulty)
        {
            SetActiveAll(false);
            shadow.gameObject.SetActive(true);
            float sizeBoost = 1f + Mathf.Clamp01(difficulty) * 0.06f;

            switch (kind)
            {
                case ObstacleKind.JumpBarrier:
                    Requirement = DodgeRequirement.Jump;
                    Clearance = 1.0f;
                    accent = BarrierColor;
                    body.gameObject.SetActive(true);
                    bodyShade.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    stripe.gameObject.SetActive(true);
                    shadow.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 64, 10);
                    body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                    body.transform.localScale = new Vector3(1.55f * sizeBoost, 0.62f, 1f);
                    bodyShade.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                    bodyShade.transform.localScale = new Vector3(1.55f * sizeBoost, 0.22f, 1f);
                    stripe.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                    stripe.transform.localScale = new Vector3(1.5f * sizeBoost, 0.16f, 1f);
                    stripe.color = new Color(1f, 0.95f, 0.8f, 0.95f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    bodyGlow.transform.localScale = new Vector3(2.8f, 1.6f, 1f);
                    bodyGlow.color = new Color(1f, 0.25f, 0.2f, 0.24f);
                    body.color = BarrierColor;
                    break;

                case ObstacleKind.SlideGate:
                    Requirement = DodgeRequirement.Slide;
                    Clearance = 0.4f;
                    accent = GateColor;
                    leftPost.gameObject.SetActive(true);
                    rightPost.gameObject.SetActive(true);
                    topBar.gameObject.SetActive(true);
                    window.gameObject.SetActive(true);
                    shadow.gameObject.SetActive(true);
                    leftPost.transform.localPosition = new Vector3(-0.72f, 1.15f, 0f);
                    rightPost.transform.localPosition = new Vector3(0.72f, 1.15f, 0f);
                    leftPost.transform.localScale = new Vector3(0.16f, 2.3f, 1f);
                    rightPost.transform.localScale = new Vector3(0.16f, 2.3f, 1f);
                    topBar.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                    topBar.transform.localScale = new Vector3(1.65f, 0.32f, 1f);
                    topBar.color = GateColor;
                    window.transform.localPosition = new Vector3(0f, 1.30f, 0f);
                    window.transform.localScale = new Vector3(1.25f, 1.15f, 1f);
                    window.color = new Color(0.0f, 0.9f, 1f, 0.16f);
                    leftPost.color = DarkMetal;
                    rightPost.color = DarkMetal;
                    break;

                case ObstacleKind.Laser:
                    Requirement = DodgeRequirement.Slide;
                    Clearance = 0.35f;
                    accent = LaserColor;
                    leftPost.gameObject.SetActive(true);
                    rightPost.gameObject.SetActive(true);
                    beam.gameObject.SetActive(true);
                    beamCore.gameObject.SetActive(true);
                    lightGlow.gameObject.SetActive(true);
                    leftPost.transform.localPosition = new Vector3(-0.86f, 0.85f, 0f);
                    rightPost.transform.localPosition = new Vector3(0.86f, 0.85f, 0f);
                    leftPost.transform.localScale = new Vector3(0.16f, 1.7f, 1f);
                    rightPost.transform.localScale = new Vector3(0.16f, 1.7f, 1f);
                    leftPost.color = new Color(0.2f, 0.15f, 0.2f, 1f);
                    rightPost.color = leftPost.color;
                    beam.transform.localPosition = new Vector3(0f, 1.28f, 0f);
                    beam.transform.localScale = new Vector3(1.95f, 0.55f, 1f);
                    beamCore.transform.localPosition = new Vector3(0f, 1.28f, 0f);
                    beamCore.transform.localScale = new Vector3(1.9f, 0.16f, 1f);
                    beam.color = new Color(1f, 0.22f, 0.35f, 0.6f);
                    beamCore.color = new Color(1f, 0.85f, 0.9f, 1f);
                    lightGlow.transform.localPosition = new Vector3(0f, 1.28f, 0f);
                    lightGlow.transform.localScale = new Vector3(3.2f, 0.8f, 1f);
                    break;

                case ObstacleKind.Car:
                    Requirement = DodgeRequirement.AvoidLane;
                    Clearance = 1.6f;
                    Depth = 1.2f;
                    accent = VehicleColor;
                    body.gameObject.SetActive(true);
                    bodyShade.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    window.gameObject.SetActive(true);
                    stripe.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 64, 16);
                    body.transform.localPosition = new Vector3(0f, 0.62f, 0f);
                    body.transform.localScale = new Vector3(1.7f, 0.95f * sizeBoost, 1f);
                    bodyShade.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                    bodyShade.transform.localScale = new Vector3(1.75f, 0.35f, 1f);
                    body.color = new Color(0.42f, 0.22f, 0.85f, 1f);
                    window.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                    window.transform.localScale = new Vector3(1.15f, 0.36f, 1f);
                    window.color = new Color(0.55f, 0.95f, 1f, 0.85f);
                    stripe.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                    stripe.transform.localScale = new Vector3(1.5f, 0.08f, 1f);
                    stripe.color = new Color(1f, 0.85f, 0.3f, 0.9f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    bodyGlow.transform.localScale = new Vector3(2.8f, 1.9f, 1f);
                    bodyGlow.color = new Color(0.55f, 0.3f, 1f, 0.22f);
                    shadow.transform.localScale = new Vector3(2.6f, 0.85f, 1f);
                    break;

                case ObstacleKind.Train:
                    Requirement = DodgeRequirement.AvoidLane;
                    Clearance = 3f;
                    Depth = 2.6f;
                    accent = Palette.Hex("#4D8BFF");
                    body.gameObject.SetActive(true);
                    bodyShade.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    window.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 128, 12);
                    body.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                    body.transform.localScale = new Vector3(1.95f, 3.1f, 1f);
                    bodyShade.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    bodyShade.transform.localScale = new Vector3(1.95f, 0.8f, 1f);
                    body.color = new Color(0.22f, 0.36f, 0.7f, 1f);
                    window.transform.localPosition = new Vector3(0f, 2.1f, 0f);
                    window.transform.localScale = new Vector3(1.4f, 0.55f, 1f);
                    window.color = new Color(0.6f, 0.95f, 1f, 0.9f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                    bodyGlow.transform.localScale = new Vector3(3.2f, 4.2f, 1f);
                    bodyGlow.color = new Color(0.25f, 0.5f, 1f, 0.22f);
                    shadow.transform.localScale = new Vector3(3f, 1.1f, 1f);
                    break;

                case ObstacleKind.Crate:
                    Requirement = DodgeRequirement.HighJump;
                    Clearance = 1.65f;
                    accent = WarningColor;
                    body.gameObject.SetActive(true);
                    bodyShade.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    stripe.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 96, 8);
                    body.transform.localPosition = new Vector3(0f, 0.66f, 0f);
                    body.transform.localScale = new Vector3(1.25f, 1.2f * sizeBoost, 1f);
                    bodyShade.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                    bodyShade.transform.localScale = new Vector3(1.3f, 0.2f, 1f);
                    body.color = new Color(0.55f, 0.4f, 0.16f, 1f);
                    stripe.transform.localPosition = new Vector3(0f, 0.66f, 0f);
                    stripe.transform.localScale = new Vector3(1.15f, 0.16f, 1f);
                    stripe.color = new Color(1f, 0.9f, 0.5f, 0.9f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    bodyGlow.transform.localScale = new Vector3(2.3f, 2.3f, 1f);
                    bodyGlow.color = new Color(1f, 0.8f, 0.3f, 0.18f);
                    break;

                case ObstacleKind.Drone:
                    Requirement = DodgeRequirement.Slide;
                    Clearance = 0.5f;
                    Depth = 0.8f;
                    accent = Palette.Hex("#FF38D0");
                    body.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    rotorA.gameObject.SetActive(true);
                    rotorB.gameObject.SetActive(true);
                    lightGlow.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 64, 22);
                    body.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                    body.transform.localScale = new Vector3(1.25f, 0.65f, 1f);
                    body.color = new Color(0.16f, 0.18f, 0.26f, 1f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                    bodyGlow.transform.localScale = new Vector3(2.4f, 1.9f, 1f);
                    bodyGlow.color = new Color(1f, 0.22f, 0.8f, 0.22f);
                    rotorA.transform.localPosition = new Vector3(-0.62f, 1.62f, 0f);
                    rotorB.transform.localPosition = new Vector3(0.62f, 1.62f, 0f);
                    lightGlow.transform.localPosition = new Vector3(0f, 1.28f, 0f);
                    lightGlow.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                    break;

                case ObstacleKind.Block:
                    Requirement = DodgeRequirement.AvoidLane;
                    Clearance = 2.2f;
                    accent = Palette.Hex("#EF4444");
                    body.gameObject.SetActive(true);
                    bodyShade.gameObject.SetActive(true);
                    bodyGlow.gameObject.SetActive(true);
                    body.sprite = Art.RoundedSprite(96, 128, 8);
                    body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                    body.transform.localScale = new Vector3(1.7f, 2.0f * sizeBoost, 1f);
                    bodyShade.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                    bodyShade.transform.localScale = new Vector3(1.75f, 0.4f, 1f);
                    body.color = new Color(0.20f, 0.24f, 0.34f, 1f);
                    bodyGlow.transform.localPosition = new Vector3(0f, 1f, 0f);
                    bodyGlow.transform.localScale = new Vector3(3f, 3.2f, 1f);
                    bodyGlow.color = new Color(1f, 0.3f, 0.3f, 0.18f);
                    break;

                default:
                    Requirement = DodgeRequirement.Jump;
                    Clearance = 0.8f;
                    accent = WarningColor;
                    gapHole.gameObject.SetActive(true);
                    gapEdge.gameObject.SetActive(true);
                    gapHole.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    gapHole.transform.localScale = new Vector3(2.25f, 3.4f, 1f);
                    gapEdge.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                    gapEdge.transform.localScale = new Vector3(2.1f, 0.16f, 1f);
                    gapEdge.color = WarningColor;
                    shadow.gameObject.SetActive(false);
                    break;
            }
        }

        private void SetActiveAll(bool active)
        {
            body.gameObject.SetActive(active);
            bodyShade.gameObject.SetActive(active);
            bodyGlow.gameObject.SetActive(active);
            stripe.gameObject.SetActive(active);
            window.gameObject.SetActive(active);
            leftPost.gameObject.SetActive(active);
            rightPost.gameObject.SetActive(active);
            topBar.gameObject.SetActive(active);
            beam.gameObject.SetActive(active);
            beamCore.gameObject.SetActive(active);
            lightGlow.gameObject.SetActive(active);
            rotorA.gameObject.SetActive(active);
            rotorB.gameObject.SetActive(active);
            shadow.gameObject.SetActive(active);
            gapHole.gameObject.SetActive(active);
            gapEdge.gameObject.SetActive(active);
        }

        public void Tick(float deltaTime, float speed, bool playing)
        {
            if (playing) Z -= speed * deltaTime;
            phase += deltaTime;
            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 3f);
            if (Kind == ObstacleKind.Drone)
            {
                float hover = Mathf.Sin(phase * 2.6f) * 0.06f;
                visual.localPosition = new Vector3(0f, hover, 0f);
                rotorA.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 26f) * 10f);
                rotorB.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(phase * 26f) * 10f);
            }
            else if (Kind == ObstacleKind.Laser)
            {
                float pulse = 0.72f + 0.28f * Mathf.Sin(phase * 9f);
                beamCore.transform.localScale = new Vector3(1.9f, 0.10f + pulse * 0.12f, 1f);
                beam.color = new Color(1f, 0.22f, 0.35f, 0.45f + pulse * 0.3f);
            }
            UpdateTransform(deltaTime, false);
        }

        private void UpdateTransform(float dt, bool snap)
        {
            float k = Projection.Scale(Z);
            Vector3 p = Projection.Position(Lane, Z, 0f);
            transform.localPosition = p;
            transform.localScale = new Vector3(k, k, 1f);
            int order = Projection.SortingOrder(Z, 300);
            SpriteRenderer[] all = { body, bodyShade, bodyGlow, stripe, window, leftPost, rightPost, topBar,
                beam, beamCore, lightGlow, rotorA, rotorB, shadow, gapHole, gapEdge };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                all[i].sortingOrder = order + i;
            }
            if (lightGlow.gameObject.activeSelf)
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(phase * 5f);
                Color c = accent;
                c.a = 0.5f + pulse * 0.35f;
                lightGlow.color = c;
                float s = 0.55f + pulse * 0.35f;
                lightGlow.transform.localScale = new Vector3(s * 1.4f, s * 1.4f, 1f);
            }
            if (flash > 0f)
            {
                body.color = Color.Lerp(accent, Color.white, flash);
            }
        }

        public void MarkPassed(bool nearMiss)
        {
            Passed = true;
            NearMissed = nearMiss;
        }

        public void Flash()
        {
            flash = 1f;
        }

        public void ResetObstacle()
        {
            Passed = false;
            NearMissed = false;
            flash = 0f;
            gameObject.SetActive(false);
        }
    }
}
