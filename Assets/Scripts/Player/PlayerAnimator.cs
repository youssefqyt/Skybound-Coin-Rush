using UnityEngine;

namespace Skybound
{
    public enum PlayerAnimState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Slide,
        Hit,
        Dead,
        Celebrate
    }

    public class PlayerAnimator : MonoBehaviour
    {
        private Transform visualRoot;
        private Transform body;
        private Transform shadow;
        private Transform aura;
        private Transform torso;
        private Transform head;
        private Transform hood;
        private Transform backpack;
        private Transform core;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftShoe;
        private Transform rightShoe;
        private Transform stripe;
        private Transform visor;
        private readonly Transform[] scarf = new Transform[3];
        private readonly SpriteRenderer[] scarfRenderers = new SpriteRenderer[3];

        private SpriteRenderer shadowRenderer;
        private SpriteRenderer auraRenderer;
        private SpriteRenderer torsoRenderer;
        private SpriteRenderer headRenderer;
        private SpriteRenderer hoodRenderer;
        private SpriteRenderer backpackRenderer;
        private SpriteRenderer coreRenderer;
        private SpriteRenderer leftArmRenderer;
        private SpriteRenderer rightArmRenderer;
        private SpriteRenderer leftLegRenderer;
        private SpriteRenderer rightLegRenderer;
        private SpriteRenderer leftShoeRenderer;
        private SpriteRenderer rightShoeRenderer;
        private SpriteRenderer stripeRenderer;
        private SpriteRenderer visorRenderer;

        private Color primary;
        private Color secondary;
        private Color accent;
        private PlayerAnimState state = PlayerAnimState.Idle;
        private float phase;
        private float stateTime;
        private float flash;
        private float tilt;
        private float slideAmount;
        private float deathRotation;
        private float deathFade = 1f;
        private bool built;

        public void Build(CharacterDef character, SkinDef skin)
        {
            primary = skin.Primary;
            secondary = skin.Secondary;
            accent = skin.Accent;

            Sprite capsule = Art.Capsule(32, 96);
            Sprite rounded = Art.RoundedSprite(64, 96, 18);
            Sprite smallRound = Art.RoundedSprite(64, 64, 16);
            Sprite circle = Art.Circle();
            Sprite tapered = Art.Tapered(64, 128);
            Sprite shoeSprite = Art.RoundedSprite(64, 32, 14);
            Sprite stripeSprite = Art.RoundedSprite(64, 12, 6);

            visualRoot = RuntimeUtil.Create("Visual", transform).transform;
            aura = RuntimeUtil.CreateSprite("Aura", visualRoot, Art.Glow(), 470,
                new Color(accent.r, accent.g, accent.b, 0.18f), new Vector3(0f, 1.05f, 0f),
                new Vector3(2.2f, 2.6f, 1f), true).transform;
            auraRenderer = aura.GetComponent<SpriteRenderer>();
            shadow = RuntimeUtil.CreateSprite("Shadow", visualRoot, Art.Shadow, 410,
                new Color(0f, 0f, 0f, 0.55f), new Vector3(0f, -0.04f, 0f),
                new Vector3(1.6f, 0.55f, 1f)).transform;
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();

            body = RuntimeUtil.Create("Body", visualRoot).transform;

            // Back scarf (drawn behind the body).
            for (int i = 0; i < 3; i++)
            {
                scarf[i] = RuntimeUtil.Create("Scarf" + i, body).transform;
                scarfRenderers[i] = scarf[i].gameObject.AddComponent<SpriteRenderer>();
                scarfRenderers[i].sprite = tapered;
                scarfRenderers[i].sharedMaterial = Art.SpriteMaterial;
                scarfRenderers[i].color = accent;
                scarfRenderers[i].sortingOrder = 500 + i;
                scarf[i].localScale = new Vector3(0.38f - i * 0.08f, 0.62f - i * 0.08f, 1f);
            }
            scarf[0].localPosition = new Vector3(-0.28f, 1.72f, 0f);
            scarf[1].localPosition = new Vector3(-0.58f, 1.64f, 0f);
            scarf[2].localPosition = new Vector3(-0.84f, 1.58f, 0f);
            scarf[0].localRotation = Quaternion.Euler(0f, 0f, 102f);
            scarf[1].localRotation = Quaternion.Euler(0f, 0f, 112f);
            scarf[2].localRotation = Quaternion.Euler(0f, 0f, 122f);

            // Back leg and arm.
            leftLeg = CreateLimb("LegBack", new Vector3(-0.17f, 0.88f, 0f), capsule, secondary * 0.82f,
                new Vector3(0.82f, 0.78f, 1f), 505, out leftLegRenderer, out leftShoe, out leftShoeRenderer, shoeSprite);
            leftArm = CreateArm("ArmBack", new Vector3(-0.42f, 1.56f, 0f), capsule, secondary * 0.88f, 504,
                out leftArmRenderer);

            // Torso.
            torso = RuntimeUtil.Create("Torso", body).transform;
            torsoRenderer = RuntimeUtil.CreateSprite("TorsoBody", torso, rounded, 512, primary,
                new Vector3(0f, 1.30f, 0f), new Vector3(1.05f, 0.96f, 1f)).GetComponent<SpriteRenderer>();
            stripe = RuntimeUtil.CreateSprite("Stripe", torso, stripeSprite, 513,
                new Color(0.95f, 1f, 1f, 0.85f), new Vector3(0f, 1.44f, 0f), new Vector3(0.82f, 0.9f, 1f)).transform;
            stripeRenderer = stripe.GetComponent<SpriteRenderer>();
            backpack = RuntimeUtil.Create("Backpack", torso).transform;
            backpackRenderer = RuntimeUtil.CreateSprite("PackBody", backpack, smallRound, 514, secondary,
                new Vector3(0f, 1.34f, 0f), new Vector3(0.58f, 0.56f, 1f)).GetComponent<SpriteRenderer>();
            core = RuntimeUtil.CreateGlow("PackCore", backpack, 516,
                new Color(accent.r, accent.g, accent.b, 0.95f), new Vector3(0f, 1.34f, 0f), 0.5f).transform;
            coreRenderer = core.GetComponent<SpriteRenderer>();

            // Head and hood.
            hood = RuntimeUtil.Create("Hood", body).transform;
            hoodRenderer = RuntimeUtil.CreateSprite("HoodShape", hood, circle, 510,
                new Color(secondary.r * 0.8f, secondary.g * 0.8f, secondary.b * 0.8f, 1f),
                new Vector3(0f, 1.97f, 0f), new Vector3(0.74f, 0.78f, 1f)).GetComponent<SpriteRenderer>();
            head = RuntimeUtil.Create("Head", body).transform;
            headRenderer = RuntimeUtil.CreateSprite("HeadShape", head, circle, 520, primary,
                new Vector3(0f, 1.98f, 0f), new Vector3(0.56f, 0.60f, 1f)).GetComponent<SpriteRenderer>();
            visor = RuntimeUtil.CreateSprite("Visor", head, stripeSprite, 521,
                new Color(accent.r, accent.g, accent.b, 0.95f), new Vector3(0f, 2.01f, 0f),
                new Vector3(0.44f, 0.55f, 1f), true).transform;
            visorRenderer = visor.GetComponent<SpriteRenderer>();
            // Ear discs
            RuntimeUtil.CreateGlow("EarL", head, 522, new Color(accent.r, accent.g, accent.b, 0.8f),
                new Vector3(-0.20f, 1.99f, 0f), 0.22f);
            RuntimeUtil.CreateGlow("EarR", head, 522, new Color(accent.r, accent.g, accent.b, 0.8f),
                new Vector3(0.20f, 1.99f, 0f), 0.22f);

            // Front leg and arm.
            rightLeg = CreateLimb("LegFront", new Vector3(0.17f, 0.88f, 0f), capsule, secondary,
                new Vector3(0.82f, 0.78f, 1f), 525, out rightLegRenderer, out rightShoe, out rightShoeRenderer, shoeSprite);
            rightArm = CreateArm("ArmFront", new Vector3(0.42f, 1.56f, 0f), capsule, secondary, 530,
                out rightArmRenderer);

            ApplyLook(character, skin);
            built = true;
        }

        private Transform CreateLimb(string name, Vector3 hip, Sprite capsule, Color color, Vector3 scale,
            int order, out SpriteRenderer legRenderer, out Transform shoe, out SpriteRenderer shoeRenderer,
            Sprite shoeSprite)
        {
            Transform pivot = RuntimeUtil.Create(name, body).transform;
            pivot.localPosition = hip;
            legRenderer = RuntimeUtil.CreateSprite(name + "Sprite", pivot, capsule, order, color,
                new Vector3(0f, -0.36f, 0f), scale).GetComponent<SpriteRenderer>();
            shoe = RuntimeUtil.CreateSprite(name + "Shoe", pivot, shoeSprite, order + 1, accent,
                new Vector3(0f, -0.72f, 0f), new Vector3(0.72f, 0.72f, 1f), true).transform;
            shoeRenderer = shoe.GetComponent<SpriteRenderer>();
            return pivot;
        }

        private Transform CreateArm(string name, Vector3 shoulder, Sprite capsule, Color color, int order,
            out SpriteRenderer armRenderer)
        {
            Transform pivot = RuntimeUtil.Create(name, body).transform;
            pivot.localPosition = shoulder;
            armRenderer = RuntimeUtil.CreateSprite(name + "Sprite", pivot, capsule, order, color,
                new Vector3(0f, -0.25f, 0f), new Vector3(0.72f, 0.62f, 1f)).GetComponent<SpriteRenderer>();
            NetworkGlow(name + "Glove", pivot, order + 1);
            return pivot;
        }

        private void NetworkGlow(string name, Transform parent, int order)
        {
            RuntimeUtil.CreateGlow(name, parent, order, new Color(accent.r, accent.g, accent.b, 0.9f),
                new Vector3(0f, -0.52f, 0f), 0.28f);
        }

        public void ApplyLook(CharacterDef character, SkinDef skin)
        {
            primary = skin.Primary;
            secondary = skin.Secondary;
            accent = skin.Accent;

            Set(torsoRenderer, primary);
            Set(stripeRenderer, Color.Lerp(primary, Color.white, 0.65f));
            Set(backpackRenderer, secondary);
            Set(hoodRenderer, Color.Lerp(secondary, Color.black, 0.15f));
            Set(headRenderer, primary);
            Set(leftArmRenderer, Color.Lerp(secondary, Color.black, 0.18f));
            Set(rightArmRenderer, secondary);
            Set(leftLegRenderer, Color.Lerp(secondary, Color.black, 0.25f));
            Set(rightLegRenderer, Color.Lerp(secondary, Color.black, 0.08f));
            Set(leftShoeRenderer, accent);
            Set(rightShoeRenderer, accent);
            Set(scarfRenderers[0], accent);
            Set(scarfRenderers[1], Color.Lerp(accent, secondary, 0.25f));
            Set(scarfRenderers[2], Color.Lerp(accent, secondary, 0.45f));
            if (auraRenderer != null) auraRenderer.color = new Color(accent.r, accent.g, accent.b, 0.16f);
            if (coreRenderer != null) coreRenderer.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            if (visorRenderer != null) visorRenderer.color = new Color(accent.r, accent.g, accent.b, 0.95f);
        }

        private static void Set(SpriteRenderer renderer, Color color)
        {
            if (renderer != null) renderer.color = color;
        }

        public void SetState(PlayerAnimState next)
        {
            if (state == next) return;
            state = next;
            stateTime = 0f;
            if (next == PlayerAnimState.Dead) deathRotation = 0f;
        }

        public void FlashHit()
        {
            flash = 1f;
        }

        public void SetDead()
        {
            state = PlayerAnimState.Dead;
            stateTime = 0f;
            deathRotation = 0f;
            deathFade = 1f;
        }

        public void ResetVisual()
        {
            if (!built || visualRoot == null || body == null) return;
            state = PlayerAnimState.Idle;
            stateTime = 0f;
            phase = 0f;
            flash = 0f;
            tilt = 0f;
            slideAmount = 0f;
            deathRotation = 0f;
            deathFade = 1f;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one;
            visualRoot.gameObject.SetActive(true);
        }

        public void Tick(float deltaTime, float speed01, float laneTilt, bool grounded, float verticalVelocity,
            bool sliding, bool invulnerable)
        {
            if (!built || visualRoot == null || body == null) return;
            stateTime += deltaTime;
            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 4f);
            tilt = Mathf.Lerp(tilt, laneTilt, 1f - Mathf.Exp(-12f * deltaTime));

            if (state == PlayerAnimState.Dead)
            {
                deathRotation += deltaTime * 330f;
                deathFade = Mathf.MoveTowards(deathFade, 0.25f, deltaTime * 0.55f);
                body.localRotation = Quaternion.Euler(0f, 0f, deathRotation);
                body.localPosition = new Vector3(0f, Mathf.Sin(stateTime * 5f) * 0.05f, 0f);
                ApplyColors(0.35f);
                if (shadowRenderer != null) shadowRenderer.color = new Color(0f, 0f, 0f, 0.2f);
                return;
            }

            bool airborne = !grounded;
            if (state == PlayerAnimState.Idle)
            {
                phase += deltaTime * 1.7f;
                float breathe = Mathf.Sin(phase) * 0.018f;
                body.localPosition = new Vector3(0f, breathe, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.6f) * 1.2f);
                SetLeg(leftLeg, 2f);
                SetLeg(rightLeg, -2f);
                SetArm(leftArm, 6f);
                SetArm(rightArm, -6f);
                if (shadowRenderer != null)
                {
                    shadowRenderer.transform.localScale = new Vector3(1.6f, 0.55f, 1f);
                    shadowRenderer.color = new Color(0f, 0f, 0f, 0.5f);
                }
                AnimateScarf(deltaTime, 0.1f);
                ApplyColors(1f);
                return;
            }
            if (state == PlayerAnimState.Celebrate)
            {
                phase += deltaTime * 7f;
                body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * 0.18f, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * 5f);
                SetArm(leftArm, -155f);
                SetArm(rightArm, 155f);
                SetLeg(leftLeg, 5f);
                SetLeg(rightLeg, -5f);
                if (shadowRenderer != null) shadowRenderer.transform.localScale = new Vector3(1.6f, 0.55f, 1f);
                AnimateScarf(deltaTime, 3.5f);
                ApplyColors(1f);
                return;
            }

            if (sliding)
            {
                slideAmount = Mathf.MoveTowards(slideAmount, 1f, deltaTime * 8f);
            }
            else
            {
                slideAmount = Mathf.MoveTowards(slideAmount, 0f, deltaTime * 7f);
            }

            if (airborne)
            {
                phase += deltaTime * 4f;
                float tuck = Mathf.Clamp01(verticalVelocity * 0.12f);
                if (verticalVelocity > 0f)
                {
                    SetLeg(leftLeg, Mathf.Lerp(8f, 58f, tuck));
                    SetLeg(rightLeg, Mathf.Lerp(-5f, -42f, tuck));
                    SetArm(leftArm, -30f - tuck * 40f);
                    SetArm(rightArm, 30f + tuck * 40f);
                    body.localRotation = Quaternion.Euler(0f, 0f, tilt - 3f);
                }
                else
                {
                    SetLeg(leftLeg, 28f);
                    SetLeg(rightLeg, -22f);
                    SetArm(leftArm, -55f);
                    SetArm(rightArm, 55f);
                    body.localRotation = Quaternion.Euler(0f, 0f, tilt + 3f);
                }
                body.localPosition = new Vector3(0f, 0f, 0f);
                if (shadowRenderer != null)
                {
                    shadowRenderer.transform.localScale = new Vector3(1.3f, 0.43f, 1f);
                    shadowRenderer.color = new Color(0f, 0f, 0f, 0.28f);
                }
                if (auraRenderer != null) auraRenderer.color = new Color(accent.r, accent.g, accent.b, 0.22f);
            }
            else
            {
                float runSpeed = 7f + speed01 * 9f;
                phase += deltaTime * runSpeed;
                float swing = Mathf.Sin(phase) * (34f + speed01 * 10f);
                float swing2 = Mathf.Sin(phase + Mathf.PI) * (30f + speed01 * 10f);
                SetLeg(leftLeg, swing);
                SetLeg(rightLeg, swing2);
                SetArm(leftArm, swing2 * 0.75f);
                SetArm(rightArm, swing * 0.75f);
                float bob = Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.045f;
                body.localPosition = new Vector3(0f, bob, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(phase * 2f) * 1.6f);
                if (shadowRenderer != null)
                {
                    shadowRenderer.transform.localScale = new Vector3(1.6f, 0.55f, 1f);
                    shadowRenderer.color = new Color(0f, 0f, 0f, 0.5f);
                }
                if (auraRenderer != null) auraRenderer.color = new Color(accent.r, accent.g, accent.b, 0.15f);
            }

            // Slide pose: compress the body forward, keep the head visible.
            if (slideAmount > 0.001f)
            {
                body.localRotation = Quaternion.Euler(0f, 0f, tilt - 68f * slideAmount);
                body.localPosition = new Vector3(0f, -0.1f * slideAmount, 0f);
                SetLeg(leftLeg, Mathf.Lerp(0f, 14f, slideAmount));
                SetLeg(rightLeg, Mathf.Lerp(0f, -10f, slideAmount));
                SetArm(leftArm, Mathf.Lerp(0f, -55f, slideAmount));
                SetArm(rightArm, Mathf.Lerp(0f, 22f, slideAmount));
                if (shadowRenderer != null)
                {
                    shadowRenderer.transform.localScale = new Vector3(1.9f, 0.4f, 1f);
                    shadowRenderer.color = new Color(0f, 0f, 0f, 0.45f);
                }
            }

            AnimateScarf(deltaTime, speed01);
            ApplyColors(invulnerable ? (Mathf.Repeat(stateTime, 0.16f) < 0.08f ? 0.45f : 1f) : 1f);
        }

        private void AnimateScarf(float dt, float speed01)
        {
            if (scarf[0] == null || scarf[1] == null || scarf[2] == null) return;
            float wave = Mathf.Sin(phase * 0.8f + stateTime * 2f) * (6f + speed01 * 8f);
            scarf[0].localRotation = Quaternion.Euler(0f, 0f, 102f + wave);
            scarf[1].localRotation = Quaternion.Euler(0f, 0f, 112f + wave * 1.4f);
            scarf[2].localRotation = Quaternion.Euler(0f, 0f, 122f + wave * 1.8f);
            float bob = Mathf.Sin(phase * 1.2f) * 0.015f;
            scarf[0].localPosition = new Vector3(-0.28f, 1.72f + bob, 0f);
            scarf[1].localPosition = new Vector3(-0.58f, 1.64f + bob, 0f);
            scarf[2].localPosition = new Vector3(-0.84f, 1.58f + bob, 0f);
        }

        private void SetLeg(Transform leg, float angle)
        {
            if (leg == null) return;
            leg.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void SetArm(Transform arm, float angle)
        {
            if (arm == null) return;
            arm.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ApplyColors(float alpha)
        {
            float flashMix = flash;
            Color p = Color.Lerp(primary, Color.white, flashMix);
            Color s = Color.Lerp(secondary, Color.white, flashMix);
            Color a = Color.Lerp(accent, Color.white, flashMix);
            p.a = alpha;
            s.a = alpha;
            a.a = alpha;
            Set(torsoRenderer, p);
            Set(headRenderer, p);
            Set(hoodRenderer, Color.Lerp(s, Color.black, 0.15f));
            Set(backpackRenderer, s);
            Set(leftArmRenderer, Color.Lerp(s, Color.black, 0.18f));
            Set(rightArmRenderer, s);
            Set(leftLegRenderer, Color.Lerp(s, Color.black, 0.25f));
            Set(rightLegRenderer, Color.Lerp(s, Color.black, 0.08f));
            Set(leftShoeRenderer, a);
            Set(rightShoeRenderer, a);
            Set(scarfRenderers[0], a);
            Set(scarfRenderers[1], a);
            Set(scarfRenderers[2], a);
            Set(stripeRenderer, Color.Lerp(Color.white, primary, 0.3f));
            Set(visorRenderer, a);
        }

        public void Celebrate()
        {
            SetState(PlayerAnimState.Celebrate);
        }

        public void Hide()
        {
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
        }
    }
}
