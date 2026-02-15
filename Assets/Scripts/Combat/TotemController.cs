using System.Collections.Generic;
using UnityEngine;

namespace TotemClash.Combat
{
    /// <summary>
    /// Local (non-networked) totem controller for single-player mode.
    /// Manages totem pickup, carrying, dropping, and score multiplier.
    /// </summary>
    public class TotemController : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [Tooltip("Range within which the totem can be picked up")]
        [SerializeField] private float pickUpRange = 2f;

        [Header("Drop Settings")]
        [Tooltip("Force applied when dropping the totem")]
        [SerializeField] private float dropForce = 5f;

        [Header("Score Settings")]
        [Tooltip("How much the multiplier increases per second while carried")]
        [SerializeField] private float scoreMultiplierIncrease = 0.1f;

        [Tooltip("Maximum score multiplier cap")]
        [SerializeField] private float maxMultiplier = 3f;

        [Header("Visual Settings")]
        [Tooltip("Height offset when carried")]
        [SerializeField] private float carryHeightOffset = 1.5f;

        [Tooltip("Smooth follow speed when carried")]
        [SerializeField] private float smoothFollowSpeed = 10f;

        [Tooltip("Rotation speed when carried")]
        [SerializeField] private float rotationSpeed = 180f;

        // Internal state
        private GameObject currentCarrier;
        private bool isBeingCarried;
        private float carryTime;
        private float currentMultiplier;

        // Static registry for finding closest totem
        private static readonly List<TotemController> allTotems = new();

        // Cached references
        private Rigidbody rb;
        private Collider totemCollider;

        // Events
        public System.Action<GameObject> OnPickedUp;
        public System.Action<bool> OnDropped;
        public System.Action OnReset;

        /// <summary>
        /// Gets all registered totems in the scene.
        /// </summary>
        public static IReadOnlyList<TotemController> AllTotems => allTotems;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            totemCollider = GetComponent<Collider>();

            if (rb == null)
            {
                Debug.LogWarning("[TotemController] No Rigidbody found. Adding one automatically.");
                rb = gameObject.AddComponent<Rigidbody>();
            }

            if (totemCollider == null)
            {
                Debug.LogWarning("[TotemController] No Collider found. Totem may not be interactable.");
            }
        }

        private void OnEnable()
        {
            if (!allTotems.Contains(this))
            {
                allTotems.Add(this);
            }
        }

        private void OnDisable()
        {
            allTotems.Remove(this);
        }

        private void Update()
        {
            if (isBeingCarried)
            {
                UpdateCarriedPosition();
                UpdateMultiplier();
            }
            else if (rb != null && rb.isKinematic)
            {
                // Ensure physics is enabled when not carried
                rb.isKinematic = false;
            }
        }

        /// <summary>
        /// Picks up the totem by the specified carrier.
        /// </summary>
        /// <param name="carrier">The GameObject that will carry the totem (player or bot)</param>
        /// <returns>True if pickup was successful, false otherwise</returns>
        public bool PickUp(GameObject carrier)
        {
            if (carrier == null)
            {
                Debug.LogWarning("[TotemController] Cannot pick up: carrier is null");
                return false;
            }

            if (isBeingCarried)
            {
                Debug.Log("[TotemController] Cannot pick up: already being carried");
                return false;
            }

            // Check if carrier is within pickup range
            float distance = Vector3.Distance(transform.position, carrier.transform.position);
            if (distance > pickUpRange)
            {
                Debug.Log($"[TotemController] Cannot pick up: carrier too far ({distance:F2} > {pickUpRange})");
                return false;
            }

            currentCarrier = carrier;
            isBeingCarried = true;
            carryTime = 0f;
            currentMultiplier = 1f;

            // Disable physics while carried
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Disable collider to prevent physics interactions
            if (totemCollider != null)
            {
                totemCollider.enabled = false;
            }

            Debug.Log($"[TotemController] Picked up by {carrier.name}");
            OnPickedUp?.Invoke(carrier);

            return true;
        }

        /// <summary>
        /// Drops the totem.
        /// </summary>
        /// <param name="applyForce">Whether to apply a force when dropping</param>
        public void Drop(bool applyForce = false)
        {
            if (!isBeingCarried)
            {
                Debug.Log("[TotemController] Cannot drop: not being carried");
                return;
            }

            GameObject previousCarrier = currentCarrier;

            // Re-enable physics
            if (rb != null)
            {
                rb.isKinematic = false;

                if (applyForce && previousCarrier != null)
                {
                    // Apply force in the direction the carrier is facing
                    Vector3 dropDirection = previousCarrier.transform.forward;
                    rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
                }
            }

            // Re-enable collider
            if (totemCollider != null)
            {
                totemCollider.enabled = true;
            }

            currentCarrier = null;
            isBeingCarried = false;

            Debug.Log($"[TotemController] Dropped by {previousCarrier?.name}");
            OnDropped?.Invoke(applyForce);
        }

        /// <summary>
        /// Returns whether the totem is currently being carried.
        /// </summary>
        public bool IsBeingCarried()
        {
            return isBeingCarried;
        }

        /// <summary>
        /// Returns the current carrier's GameObject.
        /// </summary>
        public GameObject GetCarrier()
        {
            return currentCarrier;
        }

        /// <summary>
        /// Returns the current score multiplier based on carry time.
        /// </summary>
        public float GetCarryMultiplier()
        {
            return currentMultiplier;
        }

        /// <summary>
        /// Returns the total time the totem has been carried in current session.
        /// </summary>
        public float GetCarryTime()
        {
            return carryTime;
        }

        /// <summary>
        /// Resets the totem to its initial state.
        /// </summary>
        public void ResetTotem()
        {
            if (isBeingCarried)
            {
                Drop(false);
            }

            currentCarrier = null;
            isBeingCarried = false;
            carryTime = 0f;
            currentMultiplier = 1f;

            // Reset physics
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Re-enable collider
            if (totemCollider != null)
            {
                totemCollider.enabled = true;
            }

            Debug.Log("[TotemController] Totem reset to initial state");
            OnReset?.Invoke();
        }

        /// <summary>
        /// Gets the pickup range for this totem.
        /// </summary>
        public float GetPickupRange()
        {
            return pickUpRange;
        }

        /// <summary>
        /// Updates the position to smoothly follow the carrier.
        /// </summary>
        private void UpdateCarriedPosition()
        {
            if (currentCarrier == null)
            {
                Drop(false);
                return;
            }

            Vector3 targetPosition = currentCarrier.transform.position + Vector3.up * carryHeightOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothFollowSpeed * Time.deltaTime);

            // Rotate the totem while carried
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Updates the score multiplier based on carry time.
        /// </summary>
        private void UpdateMultiplier()
        {
            carryTime += Time.deltaTime;
            currentMultiplier = Mathf.Min(1f + carryTime * scoreMultiplierIncrease, maxMultiplier);
        }

        /// <summary>
        /// Finds the closest totem to the given position.
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <returns>Closest TotemController or null if none exist</returns>
        public static TotemController FindClosest(Vector3 position)
        {
            TotemController closest = null;
            float closestDistance = float.MaxValue;

            foreach (var totem in allTotems)
            {
                if (totem == null) continue;

                float distance = Vector3.Distance(position, totem.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = totem;
                }
            }

            return closest;
        }

        /// <summary>
        /// Finds the closest totem within pickup range of the given position.
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <returns>Closest TotemController within range or null</returns>
        public static TotemController FindClosestInRange(Vector3 position)
        {
            TotemController closest = null;
            float closestDistance = float.MaxValue;

            foreach (var totem in allTotems)
            {
                if (totem == null) continue;

                float distance = Vector3.Distance(position, totem.transform.position);
                if (distance < totem.pickUpRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = totem;
                }
            }

            return closest;
        }

        /// <summary>
        /// Gets the number of active totems in the scene.
        /// </summary>
        public static int GetTotemCount()
        {
            return allTotems.Count;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw pickup range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickUpRange);

            // Draw line to carrier if being carried
            if (isBeingCarried && currentCarrier != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentCarrier.transform.position);
            }
        }
    }
}
