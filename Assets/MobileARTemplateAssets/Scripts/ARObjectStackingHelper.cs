using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Helper component that extends AR raycast functionality to detect spawned objects
    /// in addition to AR planes, enabling object stacking.
    /// </summary>
    public class ARObjectStackingHelper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The AR ray interactor used for raycasting.")]
        private XRRayInteractor m_ARInteractor;

        /// <summary>
        /// The AR ray interactor used for raycasting.
        /// </summary>
        public XRRayInteractor arInteractor
        {
            get => m_ARInteractor;
            set => m_ARInteractor = value;
        }

        [SerializeField]
        [Tooltip("Layer mask for spawned AR objects. Should match the layer set in SpawnedObjectMarker.")]
        private LayerMask m_ARObjectsLayerMask;

        /// <summary>
        /// Layer mask for spawned AR objects.
        /// </summary>
        public LayerMask arObjectsLayerMask
        {
            get => m_ARObjectsLayerMask;
            set => m_ARObjectsLayerMask = value;
        }

        [SerializeField]
        [Tooltip("Maximum raycast distance for detecting spawned objects.")]
        private float m_MaxRaycastDistance = 10f;

        /// <summary>
        /// Maximum raycast distance for detecting spawned objects.
        /// </summary>
        public float maxRaycastDistance
        {
            get => m_MaxRaycastDistance;
            set => m_MaxRaycastDistance = value;
        }

        private Camera m_MainCamera;

        void Start()
        {
            m_MainCamera = Camera.main;
            
            // Auto-configure layer mask if not set
            if (m_ARObjectsLayerMask == 0)
            {
                int layer = LayerMask.NameToLayer("ARObjects");
                if (layer != -1)
                {
                    m_ARObjectsLayerMask = 1 << layer;
                    Debug.Log($"ARObjectStackingHelper: Auto-configured layer mask to 'ARObjects' layer.", this);
                }
                else
                {
                    Debug.LogWarning("ARObjectStackingHelper: 'ARObjects' layer not found. Please create it in Project Settings.", this);
                }
            }
        }

        /// <summary>
        /// Attempts to raycast against spawned AR objects at the given screen position.
        /// </summary>
        /// <param name="screenPosition">Screen position to raycast from.</param>
        /// <param name="hitPosition">Output: World position of the hit point.</param>
        /// <param name="hitNormal">Output: Normal vector of the hit surface.</param>
        /// <param name="hitObject">Output: The GameObject that was hit.</param>
        /// <returns>True if a spawned object was hit, false otherwise.</returns>
        public bool TryRaycastSpawnedObject(Vector2 screenPosition, out Vector3 hitPosition, out Vector3 hitNormal, out GameObject hitObject)
        {
            hitPosition = Vector3.zero;
            hitNormal = Vector3.up;
            hitObject = null;

            if (m_MainCamera == null)
            {
                Debug.LogWarning("ARObjectStackingHelper: Main camera not found.", this);
                return false;
            }

            // Create ray from screen position
            Ray ray = m_MainCamera.ScreenPointToRay(screenPosition);

            // Raycast against spawned objects
            if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_ARObjectsLayerMask))
            {
                // Verify the hit object has the SpawnedObjectMarker component
                if (hit.collider.GetComponentInParent<SpawnedObjectMarker>() != null)
                {
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    hitObject = hit.collider.gameObject;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to get a hit position for spawning, checking both AR planes and spawned objects.
        /// </summary>
        /// <param name="screenPosition">Screen position to check.</param>
        /// <param name="hitPosition">Output: World position for spawning.</param>
        /// <param name="hitNormal">Output: Surface normal at hit point.</param>
        /// <param name="isARPlane">Output: True if hit was an AR plane, false if it was a spawned object.</param>
        /// <returns>True if a valid spawn position was found.</returns>
        public bool TryGetSpawnPosition(Vector2 screenPosition, out Vector3 hitPosition, out Vector3 hitNormal, out bool isARPlane)
        {
            hitPosition = Vector3.zero;
            hitNormal = Vector3.up;
            isARPlane = false;

            // First, try AR plane raycast (existing functionality)
            if (m_ARInteractor != null && m_ARInteractor.TryGetCurrentARRaycastHit(out ARRaycastHit arHit))
            {
                if (arHit.trackable is ARPlane arPlane)
                {
                    hitPosition = arHit.pose.position;
                    hitNormal = arPlane.normal;
                    isARPlane = true;
                    return true;
                }
            }

            // If no AR plane hit, try spawned objects
            if (TryRaycastSpawnedObject(screenPosition, out hitPosition, out hitNormal, out _))
            {
                isARPlane = false;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Visualize raycast in editor
            if (m_MainCamera != null && UnityEditor.Selection.activeGameObject == gameObject)
            {
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Ray ray = m_MainCamera.ScreenPointToRay(screenCenter);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(ray.origin, ray.direction * m_MaxRaycastDistance);
            }
        }
#endif
    }
}
