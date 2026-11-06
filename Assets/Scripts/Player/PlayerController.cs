using UnityEngine;

namespace Skybound
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerInput Input { get; private set; }
        public PlayerAnimator Animator { get; private set; }

        public int Lane { get; private set; } = 1;
        public float VisualX { get; private set; }
        public float VisualHeight { get; private set; }
        public float JumpHeight { get { return jumpY; } }
        public bool IsGrounded { get; private set; } = true;
        public bool IsSliding { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get { return invulnerableTimer > 0f; } }
        public int Hits { get; private set; }
        public CharacterDef Character { get; private set; }
        public SkinDef Skin { get; private set; }
        public float Speed01 { get; set; }

        private float currentX;
        private float targetX;
        private float xVelocity;
        private float jumpY;
        private float jumpVelocity;
        private float gravity = 27f;
        private float jumpMultiplier = 1f;
        private float recoveryMultiplier = 1f;
        private float slideTimer;
        private float slideDuration = 0.72f;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float slideBufferTimer;
        private float invulnerableTimer;
        private float laneTilt;
        private float targetLaneTilt;
        private float stumbleTimer;

        public static PlayerController Create(Transform parent, CharacterDef character, SkinDef skin)
        {
            GameObject go = RuntimeUtil.Create("Player", parent);
            PlayerController player = go.AddComponent<PlayerController>();
            player.Build(character, skin);
            return player;
        }

        private void Build(CharacterDef character, SkinDef skin)
        {
            Input = gameObject.AddComponent<PlayerInput>();
            Animator = gameObject.AddComponent<PlayerAnimator>();
            Animator.Build(character, skin);
            Character = character;
            Skin = skin;
            jumpMultiplier = character.JumpStat;
            recoveryMultiplier = character.RecoveryStat;
            transform.position = new Vector3(0f, Projection.NearY, 0f);
            Input.LaneRequested += OnLaneRequested;
            Input.JumpRequested += OnJumpRequested;
            Input.SlideRequested += OnSlideRequested;
            ResetForRun(character, skin);
        }

        public void ResetForRun(CharacterDef character, SkinDef skin)
        {
            Character = character;
            Skin = skin;
            jumpMultiplier = character.JumpStat;
            recoveryMultiplier = character.RecoveryStat;
            Lane = 1;
            currentX = 0f;
            targetX = 0f;
            xVelocity = 0f;
            jumpY = 0f;
            jumpVelocity = 0f;
            IsGrounded = true;
            IsSliding = false;
            slideTimer = 0f;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            slideBufferTimer = 0f;
            invulnerableTimer = 1.2f;
            IsDead = false;
            Hits = 0;
            stumbleTimer = 0f;
            laneTilt = 0f;
            targetLaneTilt = 0f;
            Animator.ResetVisual();
            Animator.ApplyLook(character, skin);
            Animator.SetState(PlayerAnimState.Run);
            transform.position = new Vector3(0f, Projection.NearY, 0f);
            Input.Enabled = false;
        }

        public void SetInputEnabled(bool enabled)
        {
            Input.Enabled = enabled && !IsDead;
        }

        public void SetMenuPose()
        {
            Lane = 1;
            currentX = 0f;
            targetX = 0f;
            jumpY = 0f;
            IsGrounded = true;
            IsSliding = false;
            slideTimer = 0f;
            invulnerableTimer = 0f;
            IsDead = false;
            transform.position = new Vector3(0f, Projection.NearY, 0f);
            Animator.ResetVisual();
            Animator.SetState(PlayerAnimState.Idle);
        }

        private void OnLaneRequested(int direction)
        {
            if (IsDead) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            int next = Mathf.Clamp(Lane + direction, 0, GameConfig.LaneCount - 1);
            if (next == Lane) return;
            Lane = next;
            targetX = GameConfig.LaneX[Lane];
            targetLaneTilt = -direction * 9f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySwoosh();
        }

        private void OnJumpRequested()
        {
            if (IsDead) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (IsSliding)
            {
                IsSliding = false;
                slideTimer = 0f;
            }
            if (IsGrounded || coyoteTimer > 0f)
            {
                PerformJump();
            }
            else
            {
                jumpBufferTimer = 0.16f;
            }
        }

        private void PerformJump()
        {
            float height = 2.55f * jumpMultiplier;
            jumpVelocity = Mathf.Sqrt(2f * gravity * height);
            IsGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpY = 0.02f;
            Animator.SetState(PlayerAnimState.Jump);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterJump();
                if (GameManager.Instance.Effects != null)
                    GameManager.Instance.Effects.JumpDust(transform.position);
            }
        }

        private void OnSlideRequested()
        {
            if (IsDead) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (!IsGrounded)
            {
                jumpVelocity = Mathf.Min(jumpVelocity, -9f);
                slideBufferTimer = 0.5f;
                return;
            }
            StartSlide();
        }

        private void StartSlide()
        {
            IsSliding = true;
            slideTimer = slideDuration;
            slideBufferTimer = 0f;
            Animator.SetState(PlayerAnimState.Slide);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySlide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterSlide();
                if (GameManager.Instance.Effects != null)
                    GameManager.Instance.Effects.SlideDust(transform.position);
            }
        }

        private void Update()
        {
            if (Animator == null) return;

            float dt = Time.deltaTime;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing && !IsDead)
            {
                UpdateMovement(dt);
            }
            else if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing && IsDead)
            {
                Animator.Tick(dt, Speed01, 0f, true, 0f, false, false);
            }
            else
            {
                VisualHeight = 0f;
                VisualX = currentX;
                transform.position = new Vector3(currentX, Projection.NearY, 0f);
                Animator.Tick(dt, 0.05f, 0f, true, 0f, false, false);
            }
        }

        private void UpdateMovement(float dt)
        {
            // Lateral lane movement
            currentX = Mathf.SmoothDamp(currentX, targetX, ref xVelocity, 0.075f, 60f, dt);
            VisualX = currentX;
            VisualHeight = jumpY;
            laneTilt = Mathf.MoveTowards(laneTilt, targetLaneTilt, dt * 55f);
            targetLaneTilt = Mathf.MoveTowards(targetLaneTilt, 0f, dt * 40f);

            // Jumping
            if (!IsGrounded)
            {
                jumpVelocity -= gravity * dt;
                jumpY += jumpVelocity * dt;
                coyoteTimer = 0f;
                if (jumpY <= 0f)
                {
                    jumpY = 0f;
                    jumpVelocity = 0f;
                    IsGrounded = true;
                    Animator.SetState(PlayerAnimState.Run);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
                    if (GameManager.Instance != null && GameManager.Instance.Effects != null)
                        GameManager.Instance.Effects.LandDust(transform.position);
                    if (slideBufferTimer > 0f)
                    {
                        StartSlide();
                    }
                    else if (jumpBufferTimer > 0f)
                    {
                        PerformJump();
                    }
                }
            }
            else
            {
                coyoteTimer = 0.09f;
            }

            jumpBufferTimer = Mathf.MoveTowards(jumpBufferTimer, 0f, dt);
            slideBufferTimer = Mathf.MoveTowards(slideBufferTimer, 0f, dt);

            // Sliding
            if (IsSliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f)
                {
                    IsSliding = false;
                }
            }

            invulnerableTimer = Mathf.MoveTowards(invulnerableTimer, 0f, dt);
            stumbleTimer = Mathf.MoveTowards(stumbleTimer, 0f, dt);

            transform.position = new Vector3(currentX, Projection.NearY + jumpY, 0f);
            float speed01 = Mathf.Clamp01((GameManager.Instance != null ? GameManager.Instance.WorldSpeed : 13f) / 32f);
            Animator.Tick(dt, speed01, laneTilt, IsGrounded, jumpVelocity, IsSliding, invulnerableTimer > 0f);
        }

        /// <summary>Returns true if the hit landed (no shield absorbed it).</summary>
        public bool TakeHit()
        {
            if (IsDead) return false;
            if (invulnerableTimer > 0f) return false;

            PowerUpManager powers = GameManager.Instance != null ? GameManager.Instance.PowerUps : null;
            if (powers != null && powers.ConsumeShield())
            {
                invulnerableTimer = 1.5f;
                Animator.FlashHit();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayShield();
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.Effects != null)
                        GameManager.Instance.Effects.ShieldBreak(transform.position);
                    GameManager.Instance.OnHitAbsorbed();
                }
                return false;
            }

            Hits++;
            invulnerableTimer = 1.2f * recoveryMultiplier;
            stumbleTimer = 0.7f;
            Animator.FlashHit();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerHit(Hits);
                if (GameManager.Instance.Effects != null)
                    GameManager.Instance.Effects.HitBurst(transform.position);
            }
            if (Hits >= 3)
            {
                Die();
            }
            return true;
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            IsSliding = false;
            Input.Enabled = false;
            Animator.SetDead();
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerCaught();
        }

        public void Revive()
        {
            IsDead = false;
            Hits = 0;
            invulnerableTimer = 3f;
            slideTimer = 0f;
            IsSliding = false;
            jumpY = 0f;
            jumpVelocity = 0f;
            IsGrounded = true;
            currentX = 0f;
            targetX = 0f;
            Lane = 1;
            Animator.ResetVisual();
            Animator.FlashHit();
            transform.position = new Vector3(0f, Projection.NearY, 0f);
            Input.Enabled = true;
        }

        public void Celebrate()
        {
            IsSliding = false;
            Input.Enabled = false;
            Animator.Celebrate();
        }

        public void SetInvulnerable(float seconds)
        {
            invulnerableTimer = Mathf.Max(invulnerableTimer, seconds);
        }

        public bool IsStumbling { get { return stumbleTimer > 0f; } }
    }
}
