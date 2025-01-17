using Controllers.StateMachines;
using Enemies;
using Levels;
using Managers;
using Objects.Scriptable;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Controllers
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        public PlayerSettings playerSettings;
        
        [Header("Animation Settings")]
        public Animator animator;

        [Header("Particle Effects")]
        public ParticleSystem particleEffect;

        [Header("Audio Settings")]
        public AudioSource audioSource;

        [HideInInspector] public Rigidbody2D rb;
        [HideInInspector] public PieController pieController;
        [HideInInspector] public InputController inputController;
        [HideInInspector] public IdleState idleState;
        [HideInInspector] public WalkingState walkingState;
        [HideInInspector] public RunningState runningState;
        [HideInInspector] public JumpingState jumpingState;
        [HideInInspector] public FallingState fallingState;
        [HideInInspector] public ParachutingState parachutingState;
        
        #region Properties
        public int CurrentHealth { get; private set; }
        public float CoyoteTimeCounter { get; private set; }
        public float JumpBufferCounter { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsKnockback { get; private set; }
        public bool BeenWarped { get; set; }
        public bool HasUmbrella { get; set; }
        public bool CanParachute
        {
            get => _canParachute && HasUmbrella;
            set => _canParachute = value;
        }
        
        #endregion
        
        private BaseState _currentState;
        private float _invincibilityTimer;
        private bool _canParachute;
        
        private void Awake()
        {
            // Set the player controller global reference.
            if (GameManager.Instance.playerController == null)
                GameManager.Instance.playerController = this;
            
            // Get the rigidbody component.
            rb = GetComponent<Rigidbody2D>();
            
            // Get the pie controller component.
            pieController = GetComponent<PieController>();
            
            // Get the input controller component.
            inputController = GetComponent<InputController>();
            
            // Set up the player's states.
            idleState = new IdleState(this);
            walkingState = new WalkingState(this);
            runningState = new RunningState(this);
            // crouchingState = new CrouchingState(this);
            jumpingState = new JumpingState(this);
            fallingState = new FallingState(this);
            parachutingState = new ParachutingState(this);
            
            // Set the player's initial state.
            ChangeState(idleState);
        }

        private void Start()
        {
            // Subscribe to the jump input event.
            InputManager.Jump.started += JumpStarted;
            
            // Set the player's health.
            CurrentHealth = playerSettings.maxHealth;
        }

        private void Update()
        {
            // Restart Level if the player falls too far.
            if (rb.velocity.y < Physics2D.gravity.y && transform.position.y < -30.0f)
                LevelUtil.RestartLevel(TransitionManager.TransitionType.ZoomAndFade);
            
            if (GameManager.IsPaused) return;
            
            UpdatePlayerOrientation();
            HandleInvincibility();
            
            _currentState.UpdateState();
            _currentState.HandleInput();
            
            UpdateCoyoteTimeCounter();
            UpdateJumpBufferCounter();
        }
        
        private void LateUpdate()
        {
            if (GameManager.IsPaused || IsInvincible) return;
            
            Magnetize();
        }

        private void OnDisable() => InputManager.Jump.started -= JumpStarted;
        
        private void JumpStarted(InputAction.CallbackContext context) => ChangeState(jumpingState);

        public void ChangeState(BaseState state)
        {
            _currentState?.ExitState();
            _currentState = state;
            _currentState.EnterState();
        }
        
        public BaseState GetCurrentState() => _currentState;
        
        /// <summary>
        /// Update the player's orientation based on the direction they are facing.
        /// </summary>
        private void UpdatePlayerOrientation()
        {
            var animatorTransform = animator.transform;
            var direction = GetPlayerDirection();
            if (direction == Vector3.right)
                animatorTransform.eulerAngles = Vector3.zero;
            else if (direction == Vector3.left)
                animatorTransform.eulerAngles = new Vector3(0, 180, 0);
            else
                animatorTransform.eulerAngles = animatorTransform.eulerAngles;
        }
        
        private void HandleInvincibility()
        {
            if (!IsInvincible) return;
            _invincibilityTimer -= Time.deltaTime;
            if (_invincibilityTimer <= 0)
                IsInvincible = false;
        }
        
        private void UpdateCoyoteTimeCounter() => CoyoteTimeCounter = IsGrounded() ? 
                playerSettings.coyoteTime : CoyoteTimeCounter - Time.deltaTime;
        
        private void UpdateJumpBufferCounter() => JumpBufferCounter = InputManager.Jump.triggered ? 
                playerSettings.jumpBufferTime : JumpBufferCounter - Time.deltaTime;
        
        public bool CanJump() => JumpBufferCounter > 0.0f && CoyoteTimeCounter > 0.0f;
        
        public void ResetCoyoteTimeAndJumpBufferCounter() => CoyoteTimeCounter = JumpBufferCounter = 0.0f;

        public void Bounce(Vector2 force) => rb.AddForce(force);
        
        private void Magnetize()
        {
            var hitColliders = new Collider2D[12];
            var numColliders = Physics2D.OverlapCircleNonAlloc(transform.position, 
                playerSettings.magnetRadius, hitColliders, playerSettings.magnetLayer);
            for (var i = 0; i < numColliders; i++)
            {
                var hitCollider = hitColliders[i];
                var coin = hitCollider.GetComponent<Coin>();
                if (coin != null && coin.CanMagnetize)
                {
                    var colliderRb = hitCollider.GetComponent<Rigidbody2D>();
                    var direction = transform.position - hitCollider.transform.position;
                    colliderRb!.AddForce(direction.normalized * (playerSettings.magnetForce * Time.fixedDeltaTime),
                        ForceMode2D.Impulse);
                }
            }
        }
        
        public void TakeDamage(int damage = 1, EnemyBase enemy = null)
        {
            if (IsInvincible) return;
            
            // Play the hurt sound.
            audioSource.Configure(playerSettings.fartAudioData);
            audioSource.Play();

            // Create a particle effect.
            // Instantiate(particleEffect, transform.position, Quaternion.identity);
            
            // Reduce the player's health.
            CurrentHealth -= damage;
            
            // Get the player's direction based on the animator's orientation.
            var direction = animator.transform.eulerAngles == Vector3.zero ? Vector3.right : Vector3.left;

            // Apply the knockback force using both horizontal and vertical settings.
            var force = new Vector2(-direction.x * playerSettings.horizontalKnockback,
                playerSettings.verticalKnockback);
            rb.AddForce(force, ForceMode2D.Impulse);

            
            IsKnockback = true;
            // Reset the knockback state after a short delay.
            TimerManager.Instance.StartTimer(0.5f, () => IsKnockback = false);
            
            // Drop the currency.
            DropCurrency(enemy!);
            
            // Activate invincibility frames after taking damage.
            ActivateInvincibility();
        }

        private void DropCurrency(EnemyBase enemy)
        {
            // Get the current currency.
            var currentCurrency = CurrencyManager.Instance.Currency;
            // Calculate the loss based on the enemy's damage percentage.
            var rawLoss = currentCurrency * (enemy.damagePercentage / 100.0f);
            var currencyLoss = rawLoss > 0 ? Mathf.CeilToInt(rawLoss) : 0;
            
            // Calculate the dice drops.
            var diceDrops = CurrencyManager.CalculateDiceDrops(currencyLoss);
            
            // Get the player's direction based on the animator's orientation.
            var direction = animator.transform.eulerAngles == Vector3.zero ? Vector3.right : Vector3.left;
            
            // Drop the calculated currency.
            foreach (var (coinValue, quantity) in diceDrops)
                CurrencyManager.DropCurrency(coinValue, quantity, playerSettings.dropForce,
                    playerSettings.dropOffset, transform.position, direction);
            
            // Subtract the currency from the player.
            CurrencyManager.Instance.RemoveCurrency(currencyLoss);
            
            // Restart the level if the player has no currency.
            if (CurrencyManager.Instance.Currency <= 0)
                LevelUtil.RestartLevel(TransitionManager.TransitionType.ZoomAndFade);
        }
        
        private void ActivateInvincibility()
        {
            IsInvincible = true;
            _invincibilityTimer = playerSettings.invincibilityDuration;
        }
        
        private Vector3 GetPlayerDirection()
        {
            return inputController.MoveInput.x switch
            {
                > 0 => Vector3.right,
                < 0 => Vector3.left,
                _ => Vector3.zero
            };
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public bool IsGrounded() => !GameManager.IsPaused && Physics2D.OverlapCircle(
            GameObject.FindWithTag("GroundCheck").transform.position, 
            playerSettings.groundCheckRadius, playerSettings.groundLayerMask);
    }
}
