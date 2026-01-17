using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Spawns objects on top of the currently selected object when the user taps the screen.
    /// Workflow: 1) Tap to select an object, 2) Tap again to spawn on top of it.
    /// </summary>
    public class SelectionBasedSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The interaction group that tracks the selected object.")]
        private XRInteractionGroup m_InteractionGroup;

        /// <summary>
        /// The interaction group that tracks the selected object.
        /// </summary>
        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        [SerializeField]
        [Tooltip("The object spawner to use for creating new objects.")]
        private ObjectSpawner m_ObjectSpawner;

        /// <summary>
        /// The object spawner to use for creating new objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        [Tooltip("Vertical offset above the selected object where the new object will spawn.")]
        private float m_SpawnHeightOffset = 0.1f;

        /// <summary>
        /// Vertical offset above the selected object where the new object will spawn.
        /// </summary>
        public float spawnHeightOffset
        {
            get => m_SpawnHeightOffset;
            set => m_SpawnHeightOffset = value;
        }

        [SerializeField]
        [Tooltip("Enable visual feedback showing where the object will spawn.")]
        private bool m_ShowSpawnPreview = true;

        /// <summary>
        /// Enable visual feedback showing where the object will spawn.
        /// </summary>
        public bool showSpawnPreview
        {
            get => m_ShowSpawnPreview;
            set => m_ShowSpawnPreview = value;
        }

        [SerializeField]
        [Tooltip("Color of the spawn preview indicator.")]
        private Color m_PreviewColor = new Color(0f, 1f, 0f, 0.5f);

        /// <summary>
        /// Color of the spawn preview indicator.
        /// </summary>
        public Color previewColor
        {
            get => m_PreviewColor;
            set => m_PreviewColor = value;
        }

        private GameObject m_PreviewIndicator;
        private bool m_WasSelectedLastFrame;

        void Start()
        {
            // Auto-find components if not set
            if (m_InteractionGroup == null)
            {
#if UNITY_2023_1_OR_NEWER
                m_InteractionGroup = FindFirstObjectByType<XRInteractionGroup>();
#else
                m_InteractionGroup = FindObjectOfType<XRInteractionGroup>();
#endif
                if (m_InteractionGroup == null)
                {
                    Debug.LogWarning("SelectionBasedSpawner: No XRInteractionGroup found. This component requires an interaction group to track selected objects.", this);
                }
            }

            if (m_ObjectSpawner == null)
            {
#if UNITY_2023_1_OR_NEWER
                m_ObjectSpawner = FindFirstObjectByType<ObjectSpawner>();
#else
                m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
#endif
                if (m_ObjectSpawner == null)
                {
                    Debug.LogError("SelectionBasedSpawner: No ObjectSpawner found. Disabling component.", this);
                    enabled = false;
                    return;
                }
            }

            // Create preview indicator
            if (m_ShowSpawnPreview)
            {
                CreatePreviewIndicator();
            }
        }

        void Update()
        {
            var selectedObject = GetSelectedObject();

            // Update preview indicator
            if (m_ShowSpawnPreview && m_PreviewIndicator != null)
            {
                if (selectedObject != null)
                {
                    UpdatePreviewPosition(selectedObject);
                    m_PreviewIndicator.SetActive(true);
                }
                else
                {
                    m_PreviewIndicator.SetActive(false);
                }
            }

            // Detect tap to spawn
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasReleasedThisFrame && selectedObject != null)
            {
                // Don't spawn if this tap just selected the object
                if (m_WasSelectedLastFrame)
                {
                    SpawnOnSelectedObject(selectedObject);
                }
            }
            // Mouse input for editor testing
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && selectedObject != null && m_WasSelectedLastFrame)
            {
                SpawnOnSelectedObject(selectedObject);
            }

            m_WasSelectedLastFrame = selectedObject != null;
        }

        GameObject GetSelectedObject()
        {
            if (m_InteractionGroup == null || m_InteractionGroup.focusInteractable == null)
                return null;

            return m_InteractionGroup.focusInteractable.transform.gameObject;
        }

        void SpawnOnSelectedObject(GameObject selectedObject)
        {
            // Get the top surface of the selected object
            Bounds bounds = GetObjectBounds(selectedObject);
            Vector3 spawnPosition = bounds.center + Vector3.up * (bounds.extents.y + m_SpawnHeightOffset);
            Vector3 spawnNormal = Vector3.up;

            // Spawn the object
            if (m_ObjectSpawner.TrySpawnObject(spawnPosition, spawnNormal))
            {
                Debug.Log($"Spawned object on top of {selectedObject.name} at {spawnPosition}", this);
            }
        }

        Bounds GetObjectBounds(GameObject obj)
        {
            // Try to get bounds from renderer
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            // Fallback: try collider
            var collider = obj.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            // Last resort: use transform position with small bounds
            return new Bounds(obj.transform.position, Vector3.one * 0.1f);
        }

        void CreatePreviewIndicator()
        {
            m_PreviewIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_PreviewIndicator.name = "Spawn Preview Indicator";
            m_PreviewIndicator.transform.localScale = Vector3.one * 0.05f;

            // Remove collider so it doesn't interfere
            Destroy(m_PreviewIndicator.GetComponent<Collider>());

            // Set material
            var renderer = m_PreviewIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = m_PreviewColor;
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                renderer.material = material;
            }

            m_PreviewIndicator.SetActive(false);
        }

        void UpdatePreviewPosition(GameObject selectedObject)
        {
            if (m_PreviewIndicator == null)
                return;

            Bounds bounds = GetObjectBounds(selectedObject);
            Vector3 previewPosition = bounds.center + Vector3.up * (bounds.extents.y + m_SpawnHeightOffset);
            m_PreviewIndicator.transform.position = previewPosition;
        }

        void OnDestroy()
        {
            if (m_PreviewIndicator != null)
            {
                Destroy(m_PreviewIndicator);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!enabled || m_InteractionGroup == null)
                return;

            var selectedObject = GetSelectedObject();
            if (selectedObject != null)
            {
                Bounds bounds = GetObjectBounds(selectedObject);
                Vector3 spawnPosition = bounds.center + Vector3.up * (bounds.extents.y + m_SpawnHeightOffset);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPosition, 0.05f);
                Gizmos.DrawLine(bounds.center, spawnPosition);
            }
        }
#endif
    }
}
