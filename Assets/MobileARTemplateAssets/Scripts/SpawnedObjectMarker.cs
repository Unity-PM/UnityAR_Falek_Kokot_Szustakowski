using UnityEngine;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Marker component added to spawned AR objects to identify them as stackable surfaces.
    /// This allows other objects to be placed on top of spawned objects, not just AR planes.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpawnedObjectMarker : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The layer name for spawned AR objects. Must match the layer configured in Unity's Layer settings.")]
        private string m_ARObjectLayerName = "ARObjects";

        /// <summary>
        /// The layer name for spawned AR objects.
        /// </summary>
        public string arObjectLayerName
        {
            get => m_ARObjectLayerName;
            set => m_ARObjectLayerName = value;
        }

        [SerializeField]
        [Tooltip("Time when this object was spawned.")]
        private float m_SpawnTime;

        /// <summary>
        /// Time when this object was spawned.
        /// </summary>
        public float spawnTime => m_SpawnTime;

        void Awake()
        {
            m_SpawnTime = Time.time;
            
            // Ensure the object has a collider
            if (!TryGetComponent<Collider>(out _))
            {
                Debug.LogWarning($"SpawnedObjectMarker on {gameObject.name} requires a Collider component. Adding BoxCollider.", this);
                gameObject.AddComponent<BoxCollider>();
            }

            // Set the layer for this object and all children
            SetLayerRecursively(gameObject, m_ARObjectLayerName);
        }

        /// <summary>
        /// Sets the layer for this GameObject and all its children.
        /// Creates the layer if it doesn't exist.
        /// </summary>
        private void SetLayerRecursively(GameObject obj, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            
            if (layer == -1)
            {
                Debug.LogWarning($"Layer '{layerName}' does not exist. Please create it in Edit → Project Settings → Tags and Layers.", this);
                return;
            }

            obj.layer = layer;

            // Set layer for all children
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layerName);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Validate layer exists in editor
            if (!string.IsNullOrEmpty(m_ARObjectLayerName))
            {
                int layer = LayerMask.NameToLayer(m_ARObjectLayerName);
                if (layer == -1)
                {
                    Debug.LogWarning($"Layer '{m_ARObjectLayerName}' does not exist. Create it in Edit → Project Settings → Tags and Layers.", this);
                }
            }
        }
#endif
    }
}
