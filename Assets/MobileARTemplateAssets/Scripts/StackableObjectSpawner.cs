using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Enhanced ObjectSpawner that automatically adds SpawnedObjectMarker component
    /// to spawned objects, enabling them to be used as stacking surfaces.
    /// </summary>
    public class StackableObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The base ObjectSpawner component to extend.")]
        private ObjectSpawner m_BaseSpawner;

        /// <summary>
        /// The base ObjectSpawner component.
        /// </summary>
        public ObjectSpawner baseSpawner
        {
            get => m_BaseSpawner;
            set => m_BaseSpawner = value;
        }

        [SerializeField]
        [Tooltip("Automatically add SpawnedObjectMarker to spawned objects.")]
        private bool m_AutoAddMarker = true;

        /// <summary>
        /// Automatically add SpawnedObjectMarker to spawned objects.
        /// </summary>
        public bool autoAddMarker
        {
            get => m_AutoAddMarker;
            set => m_AutoAddMarker = value;
        }

        [SerializeField]
        [Tooltip("Automatically add colliders to spawned objects if they don't have one.")]
        private bool m_AutoAddCollider = true;

        /// <summary>
        /// Automatically add colliders to spawned objects if they don't have one.
        /// </summary>
        public bool autoAddCollider
        {
            get => m_AutoAddCollider;
            set => m_AutoAddCollider = value;
        }

        void Start()
        {
            if (m_BaseSpawner == null)
            {
                m_BaseSpawner = GetComponent<ObjectSpawner>();
                if (m_BaseSpawner == null)
                {
                    Debug.LogError("StackableObjectSpawner requires an ObjectSpawner component on the same GameObject.", this);
                    enabled = false;
                    return;
                }
            }

            // Subscribe to spawn events
            m_BaseSpawner.objectSpawned += OnObjectSpawned;
        }

        void OnDestroy()
        {
            if (m_BaseSpawner != null)
            {
                m_BaseSpawner.objectSpawned -= OnObjectSpawned;
            }
        }

        void OnObjectSpawned(GameObject spawnedObject)
        {
            if (spawnedObject == null)
                return;

            // Add collider if needed
            if (m_AutoAddCollider && !spawnedObject.GetComponentInChildren<Collider>())
            {
                // Try to add appropriate collider based on mesh
                var meshFilter = spawnedObject.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    var meshCollider = spawnedObject.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                    Debug.Log($"Added MeshCollider to {spawnedObject.name}", spawnedObject);
                }
                else
                {
                    // Fallback to box collider
                    spawnedObject.AddComponent<BoxCollider>();
                    Debug.Log($"Added BoxCollider to {spawnedObject.name}", spawnedObject);
                }
            }

            // Add marker component
            if (m_AutoAddMarker && !spawnedObject.GetComponent<SpawnedObjectMarker>())
            {
                spawnedObject.AddComponent<SpawnedObjectMarker>();
                Debug.Log($"Added SpawnedObjectMarker to {spawnedObject.name}", spawnedObject);
            }
        }
    }
}
