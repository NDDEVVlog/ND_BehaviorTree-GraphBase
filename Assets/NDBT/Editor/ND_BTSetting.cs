using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ND_BehaviourTrees.Editor
{
    // Add a menuName for easier manual creation if needed, though the singleton handles it.
    [CreateAssetMenu(fileName = "ND_BTSettings", menuName = "ND_BehaviourTrees/Settings Asset")]
    public sealed class ND_BTSetting : ScriptableObject
    {

        private const string SettingsAssetPath = "Assets/NDBT/Editor/Resources/ND_BTSettings.asset";

        [Tooltip("The UXML file to use for the default appearance of nodes in the Behavior Tree editor.")]
        [SerializeField]
        private VisualTreeAsset defaultNodeUXML;

        private static ND_BTSetting _instance;

        public static ND_BTSetting Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<ND_BTSetting>(SettingsAssetPath);

                    if (_instance == null)
                    {
                        // Ensure the directory exists before creating the asset
                        string directoryPath = Path.GetDirectoryName(SettingsAssetPath);
                        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        _instance = CreateInstance<ND_BTSetting>();
                        AssetDatabase.CreateAsset(_instance, SettingsAssetPath);
                        AssetDatabase.SaveAssets(); // Save the newly created asset
                        AssetDatabase.Refresh();    // Make sure Unity's asset database is aware of the new file
                        Debug.LogWarning("Created new ND_BTSetting at: " + SettingsAssetPath +
                                       ". Please select it and assign the 'Default Node UXML' in the Inspector.");
                    }
                }
                return _instance;
            }
        }

        public VisualTreeAsset GetNodeDefaultUXML()
        {
            if (defaultNodeUXML == null)
            {
                // Make this an error as it's critical
                Debug.LogError($"CRITICAL: 'Default Node UXML' is not assigned in the ND_BTSetting asset. " +
                               $"Please select '{SettingsAssetPath}' in the Project window and assign a VisualTreeAsset to it in the Inspector.");
            }
            return defaultNodeUXML;
        }

        public string GetNodeDefaultUXMLPath()
        {
            if (defaultNodeUXML == null)
            {
                Debug.LogError($"CRITICAL: 'Default Node UXML' is not assigned in the ND_BTSetting asset. Cannot get path. " +
                               $"Please select '{SettingsAssetPath}' and assign it.");
                return null; // Explicitly return null
            }

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(defaultNodeUXML);
            // Debug.Log($"Default Node UXML Path: {path}"); // Optional: Uncomment for debugging
            return path;
#else
            // This case should ideally not be hit if this is an editor-only settings class
            Debug.LogError("GetNodeDefaultUXMLPath() called outside of UNITY_EDITOR, but AssetDatabase is editor-only. " +
                           "defaultNodeUXML path cannot be retrieved at runtime this way.");
            return null;
#endif
        }

        // Helper to easily find and select the settings asset
        [MenuItem("Tools/ND_BehaviourTrees/Select Settings Asset", false, 100)]
        public static void SelectSettingsAsset()
        {
            // This will create the asset if it doesn't exist and then select it.
            Selection.activeObject = Instance;
            if (Instance != null && Instance.defaultNodeUXML == null)
            {
                EditorUtility.DisplayDialog("ND_BTSetting",
                    "The ND_BTSetting asset has been selected (or created).\n\nPlease assign the 'Default Node UXML' field in the Inspector.", "OK");
            }
        }
    }
}