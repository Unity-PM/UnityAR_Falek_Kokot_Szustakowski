using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Enhanced spawn trigger that supports spawning objects on both AR planes and other spawned objects.
    /// This enables object stacking functionality.
    /// </summary>
    public class ARObjectStackingSpawnTrigger : MonoBehaviour
    {
        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public enum SpawnTriggerType
        {
            /// <summary>
            /// Spawn an object when the interactor activates its select input
            /// but no selection actually occurs.
            /// </summary>
            SelectAttempt,

            /// <summary>
            /// Spawn an object when an input is performed.
            /// </summary>
            InputAction,
        }

        [SerializeField]
        [Tooltip("The AR ray interactor that determines where to spawn the object.")]
        XRRayInteractor m_ARInteractor;

        /// <summary>
        /// The AR ray interactor that determines where to spawn the object.
        /// </summary>
        public XRRayInteractor arInteractor
        {
            get => m_ARInteractor;
            set => m_ARInteractor = value;
        }

        [SerializeField]
        [Tooltip("The behavior to use to spawn objects.")]
        ObjectSpawner m_ObjectSpawner;

        /// <summary>
        /// The behavior to use to spawn objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        [Tooltip("The stacking helper component for detecting spawned objects.")]
        ARObjectStackingHelper m_StackingHelper;

        /// <summary>
        /// The stacking helper component for detecting spawned objects.
        /// </summary>
        public ARObjectStackingHelper stackingHelper
        {
            get => m_StackingHelper;
            set => m_StackingHelper = value;
        }

        [SerializeField]
        [Tooltip("Whether to require that the AR Interactor hits an AR Plane with a horizontal up alignment in order to spawn anything.")]
        bool m_RequireHorizontalUpSurface;

        /// <summary>
        /// Whether to require that the AR Interactor hits an AR Plane with an alignment of
        /// PlaneAlignment.HorizontalUp in order to spawn anything.
        /// </summary>
        public bool requireHorizontalUpSurface
        {
            get => m_RequireHorizontalUpSurface;
            set => m_RequireHorizontalUpSurface = value;
        }

        [SerializeField]
        [Tooltip("The type of trigger to use to spawn an object, either when the Interactor's select action occurs or when a button input is performed.")]
        SpawnTriggerType m_SpawnTriggerType;

        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public SpawnTriggerType spawnTriggerType
        {
            get => m_SpawnTriggerType;
            set => m_SpawnTriggerType = value;
        }

        [SerializeField]
        XRInputButtonReader m_SpawnObjectInput = new XRInputButtonReader("Spawn Object");

        /// <summary>
        /// The input used to trigger spawn, if spawnTriggerType is set to SpawnTriggerType.InputAction.
        /// </summary>
        public XRInputButtonReader spawnObjectInput
        {
            get => m_SpawnObjectInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_SpawnObjectInput, value, this);
        }

        [SerializeField]
        [Tooltip("When enabled, spawn will not be triggered if an object is currently selected.")]
        bool m_BlockSpawnWhenInteractorHasSelection = true;

        /// <summary>
        /// When enabled, spawn will not be triggered if an object is currently selected.
        /// </summary>
        public bool blockSpawnWhenInteractorHasSelection
        {
            get => m_BlockSpawnWhenInteractorHasSelection;
            set => m_BlockSpawnWhenInteractorHasSelection = value;
        }

        [SerializeField]
        [Tooltip("Enable object stacking functionality.")]
        bool m_EnableStacking = true;

        /// <summary>
        /// Enable object stacking functionality.
        /// </summary>
        public bool enableStacking
        {
            get => m_EnableStacking;
            set => m_EnableStacking = value;
        }

        bool m_AttemptSpawn;
        bool m_EverHadSelection;
        Vector2 m_LastTapPosition;

        /// <summary>
        /// See MonoBehaviour.
        /// </summary>
        void OnEnable()
        {
            m_SpawnObjectInput.EnableDirectActionIfModeUsed();
        }

        /// <summary>
        /// See MonoBehaviour.
        /// </summary>
        void OnDisable()
        {
            m_SpawnObjectInput.DisableDirectActionIfModeUsed();
        }

        /// <summary>
        /// See MonoBehaviour.
        /// </summary>
        void Start()
        {
            if (m_ObjectSpawner == null)
#if UNITY_2023_1_OR_NEWER
                m_ObjectSpawner = FindFirstObjectByType<ObjectSpawner>();
#else
                m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
#endif

            if (m_ARInteractor == null)
            {
                Debug.LogError("Missing AR Interactor reference, disabling component.", this);
                enabled = false;
                return;
            }

            // Auto-find or create stacking helper
            if (m_StackingHelper == null && m_EnableStacking)
            {
#if UNITY_2023_1_OR_NEWER
                m_StackingHelper = FindFirstObjectByType<ARObjectStackingHelper>();
#else
                m_StackingHelper = FindObjectOfType<ARObjectStackingHelper>();
#endif
                
                if (m_StackingHelper == null)
                {
                    Debug.Log("ARObjectStackingHelper not found. Creating one automatically.", this);
                    var helperObj = new GameObject("AR Object Stacking Helper");
                    m_StackingHelper = helperObj.AddComponent<ARObjectStackingHelper>();
                    m_StackingHelper.arInteractor = m_ARInteractor;
                }
            }
        }

        /// <summary>
        /// See MonoBehaviour.
        /// </summary>
        void Update()
        {
            // Wait a frame after the Spawn Object input is triggered to actually cast against AR planes and spawn
            // in order to ensure the touchscreen gestures have finished processing to allow the ray pose driver
            // to update the pose based on the touch position of the gestures.
            if (m_AttemptSpawn)
            {
                m_AttemptSpawn = false;

                // Cancel the spawn if the select was delayed until the frame after the spawn trigger.
                // This can happen if the select action uses a different input source than the spawn trigger.
                if (m_ARInteractor.hasSelection)
                    return;

                // Don't spawn the object if the tap was over screen space UI.
                var isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
                if (isPointerOverUI)
                    return;

                // Try to spawn on AR plane or spawned object
                TrySpawnObject(m_LastTapPosition);
                return;
            }

            var selectState = m_ARInteractor.logicalSelectState;

            if (m_BlockSpawnWhenInteractorHasSelection)
            {
                if (selectState.wasPerformedThisFrame)
                    m_EverHadSelection = m_ARInteractor.hasSelection;
                else if (selectState.active)
                    m_EverHadSelection |= m_ARInteractor.hasSelection;
            }

            m_AttemptSpawn = false;
            switch (m_SpawnTriggerType)
            {
                case SpawnTriggerType.SelectAttempt:
                    if (selectState.wasCompletedThisFrame)
                    {
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                        if (m_AttemptSpawn)
                            m_LastTapPosition = GetTapPosition();
                    }
                    break;

                case SpawnTriggerType.InputAction:
                    if (m_SpawnObjectInput.ReadWasPerformedThisFrame())
                    {
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                        if (m_AttemptSpawn)
                            m_LastTapPosition = GetTapPosition();
                    }
                    break;
            }
        }

        Vector2 GetTapPosition()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
                return touchscreen.primaryTouch.position.ReadValue();
            
            var mouse = Mouse.current;
            if (mouse != null)
                return mouse.position.ReadValue();
            
            return Vector2.zero;
        }

        void TrySpawnObject(Vector2 screenPosition)
        {
            Vector3 spawnPosition;
            Vector3 spawnNormal;
            bool isARPlane = false;

            // First try AR plane raycast
            if (m_ARInteractor.TryGetCurrentARRaycastHit(out ARRaycastHit arRaycastHit))
            {
                if (arRaycastHit.trackable is ARPlane arPlane)
                {
                    if (m_RequireHorizontalUpSurface && arPlane.alignment != PlaneAlignment.HorizontalUp)
                        return;

                    spawnPosition = arRaycastHit.pose.position;
                    spawnNormal = arPlane.normal;
                    isARPlane = true;
                }
                else
                {
                    return;
                }
            }
            // If no AR plane hit and stacking is enabled, try spawned objects
            else if (m_EnableStacking && m_StackingHelper != null && 
                     m_StackingHelper.TryRaycastSpawnedObject(screenPosition, out spawnPosition, out spawnNormal, out _))
            {
                isARPlane = false;
            }
            else
            {
                // No valid surface found
                return;
            }

            // Spawn the object
            if (m_ObjectSpawner.TrySpawnObject(spawnPosition, spawnNormal))
            {
                // Success - object spawned
                Debug.Log($"Object spawned on {(isARPlane ? "AR Plane" : "Spawned Object")} at {spawnPosition}");
            }
        }
    }
}
