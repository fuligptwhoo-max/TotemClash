using UnityEngine;
using TotemClash.Classes;

namespace TotemClash.Combat
{
    /// <summary>
    /// Local (non-networked) player combat controller for single-player mode.
    /// Handles combat input and delegates ability execution to MagicianClass.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MagicianClass magicianClass;
        [SerializeField] private AimingSystem aimingSystem;

        [Header("Settings")]
        [SerializeField] private float attackInputBuffer = 0.1f;

        // Input tracking
        private float lastAttackTime;
        private bool attackBuffered;

        #region Properties

        /// <summary>
        /// Gets the animator reference.
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// Gets the player controller reference.
        /// </summary>
        public PlayerController PlayerController => playerController;

        /// <summary>
        /// Gets the magician class reference.
        /// </summary>
        public MagicianClass MagicianClass => magicianClass;

        /// <summary>
        /// Gets the aiming system reference.
        /// </summary>
        public AimingSystem AimingSystem => aimingSystem;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-find references if not set in inspector
            if (animator == null)
                animator = GetComponent<Animator>();
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (magicianClass == null)
                magicianClass = GetComponent<MagicianClass>();
            if (aimingSystem == null)
                aimingSystem = GetComponent<AimingSystem>();
        }

        private void Update()
        {
            // Process buffered attack input
            if (attackBuffered)
            {
                if (Time.time - lastAttackTime <= attackInputBuffer)
                {
                    Vector3 targetPos = GetAttackTargetPosition();
                    PrimaryAttack(targetPos);
                }
                attackBuffered = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a primary attack (fireball).
        /// </summary>
        /// <param name="targetPosition">The target position to aim at.</param>
        /// <returns>True if attack was executed, false if on cooldown or invalid.</returns>
        public bool PrimaryAttack(Vector3 targetPosition)
        {
            if (magicianClass == null)
            {
                Debug.LogWarning("[PlayerCombat] Cannot attack - MagicianClass reference is missing.");
                return false;
            }

            // Check if MagicianClass can attack (cooldown check)
            if (!magicianClass.IsAbilityReady(0))
            {
                return false;
            }

            // Execute fireball cast through MagicianClass
            magicianClass.PrimaryAttack(targetPosition);

            lastAttackTime = Time.time;
            return true;
        }

        /// <summary>
        /// Uses a specific ability by index.
        /// </summary>
        /// <param name="abilityIndex">The ability index (0 = Ability1, 1 = Ability2).</param>
        /// <param name="targetPosition">The target position to aim at.</param>
        public void UseAbility(int abilityIndex, Vector3 targetPosition)
        {
            if (magicianClass == null)
            {
                Debug.LogWarning($"[PlayerCombat] Cannot use ability {abilityIndex} - MagicianClass reference is missing.");
                return;
            }

            switch (abilityIndex)
            {
                case 0:
                    UseAbility1(targetPosition);
                    break;
                case 1:
                    UseAbility2(targetPosition);
                    break;
                default:
                    Debug.LogWarning($"[PlayerCombat] Invalid ability index: {abilityIndex}");
                    break;
            }
        }

        /// <summary>
        /// Uses the ultimate ability.
        /// </summary>
        /// <param name="targetPosition">The target position to aim at.</param>
        public void UseUltimate(Vector3 targetPosition)
        {
            if (magicianClass == null)
            {
                Debug.LogWarning("[PlayerCombat] Cannot use ultimate - MagicianClass reference is missing.");
                return;
            }

            // Trigger ultimate animation if available
            if (animator != null)
            {
                animator.SetTrigger("Ultimate");
            }

            // Execute ultimate through MagicianClass
            magicianClass.UltimateAbility(targetPosition);

            Debug.Log("[PlayerCombat] Ultimate ability used.");
        }

        /// <summary>
        /// Gets the cooldown progress for a specific ability.
        /// </summary>
        /// <param name="abilityIndex">The ability index (0 = Ability1, 1 = Ability2, 2 = Ultimate).</param>
        /// <returns>Value from 0 to 1, where 1 is fully ready.</returns>
        public float GetCooldownProgress(int abilityIndex)
        {
            if (magicianClass == null)
                return 1f;

            return magicianClass.GetCooldownProgress(abilityIndex);
        }

        /// <summary>
        /// Checks if a specific ability is ready to use.
        /// </summary>
        /// <param name="abilityIndex">The ability index (0 = Ability1, 1 = Ability2, 2 = Ultimate).</param>
        /// <returns>True if the ability is ready.</returns>
        public bool IsAbilityReady(int abilityIndex)
        {
            if (magicianClass == null)
                return false;

            return magicianClass.IsAbilityReady(abilityIndex);
        }

        /// <summary>
        /// Buffers an attack input to be processed on next update.
        /// Called by PlayerController when attack input is detected.
        /// </summary>
        public void BufferAttackInput()
        {
            attackBuffered = true;
            lastAttackTime = Time.time;
        }

        /// <summary>
        /// Gets the current attack target position from the aiming system.
        /// </summary>
        /// <returns>The target position for attacks.</returns>
        public Vector3 GetAttackTargetPosition()
        {
            if (aimingSystem != null)
            {
                return aimingSystem.GetAimPosition();
            }

            // Fallback to aim direction if no target
            return transform.position + transform.forward * 50f;
        }



        #endregion

        #region Private Methods

        private void UseAbility1(Vector3 targetPosition)
        {
            if (!magicianClass.IsAbilityReady(1))
            {
                Debug.Log("[PlayerCombat] Ability 1 is on cooldown.");
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger("Ability1");
            }

            magicianClass.Ability1(targetPosition);
            Debug.Log("[PlayerCombat] Ability 1 used.");
        }

        private void UseAbility2(Vector3 targetPosition)
        {
            if (!magicianClass.IsAbilityReady(2))
            {
                Debug.Log("[PlayerCombat] Ability 2 is on cooldown.");
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger("Ability2");
            }

            magicianClass.Ability2(targetPosition);
            Debug.Log("[PlayerCombat] Ability 2 used.");
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-populate references in editor
            if (animator == null)
                animator = GetComponent<Animator>();
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (magicianClass == null)
                magicianClass = GetComponent<MagicianClass>();
            if (aimingSystem == null)
                aimingSystem = GetComponent<AimingSystem>();
        }
#endif

        #endregion
    }
}
