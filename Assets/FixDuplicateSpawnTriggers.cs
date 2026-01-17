using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Diagnostic script to find and remove duplicate ARInteractorSpawnTrigger components in the scene.
/// This fixes the issue where multiple spawns occur on each tap.
/// </summary>
public class FixDuplicateSpawnTriggers : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Fix Duplicate Spawn Triggers")]
    public static void FindAndRemoveDuplicates()
    {
        // Find all ARInteractorSpawnTrigger components in the scene
#if UNITY_2023_1_OR_NEWER
        var allTriggers = FindObjectsByType<ARInteractorSpawnTrigger>(FindObjectsSortMode.None);
#else
        var allTriggers = FindObjectsOfType<ARInteractorSpawnTrigger>();
#endif
        
        Debug.Log($"Found {allTriggers.Length} ARInteractorSpawnTrigger components in the scene.");
        
        if (allTriggers.Length <= 1)
        {
            Debug.Log("No duplicates found. Only one or zero ARInteractorSpawnTrigger exists.");
            return;
        }
        
        // List all instances
        Debug.Log("=== All ARInteractorSpawnTrigger instances ===");
        for (int i = 0; i < allTriggers.Length; i++)
        {
            var trigger = allTriggers[i];
            Debug.Log($"[{i}] GameObject: {trigger.gameObject.name}, " +
                     $"Hierarchy Path: {GetGameObjectPath(trigger.gameObject)}, " +
                     $"Enabled: {trigger.enabled}");
        }
        
        // Ask user to confirm deletion
        bool shouldDelete = EditorUtility.DisplayDialog(
            "Duplicate Spawn Triggers Found",
            $"Found {allTriggers.Length} ARInteractorSpawnTrigger components.\n\n" +
            "This is causing multiple spawns on each tap.\n\n" +
            "Do you want to keep only the first one and remove the rest?",
            "Yes, Remove Duplicates",
            "No, Cancel"
        );
        
        if (!shouldDelete)
        {
            Debug.Log("Operation cancelled by user.");
            return;
        }
        
        // Remove duplicates (keep the first one)
        int removedCount = 0;
        for (int i = 1; i < allTriggers.Length; i++)
        {
            var trigger = allTriggers[i];
            Debug.Log($"Removing ARInteractorSpawnTrigger from: {GetGameObjectPath(trigger.gameObject)}");
            DestroyImmediate(trigger);
            removedCount++;
        }
        
        Debug.Log($"<color=green>Successfully removed {removedCount} duplicate ARInteractorSpawnTrigger components!</color>");
        EditorUtility.DisplayDialog(
            "Success",
            $"Removed {removedCount} duplicate components.\n\n" +
            "The spawn issue should now be fixed!",
            "OK"
        );
    }
    
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
#endif
}
