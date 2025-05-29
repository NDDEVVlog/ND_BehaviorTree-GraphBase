using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlasticGui.WorkspaceWindow;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ND_BehaviourTrees.Editor
{   [UnityEditor.FilePath("Assets/NDBT/Editor/Resources/ND_BTSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
    public sealed class ND_BTSetting : ScriptableSingleton<ND_BTSetting>
    {
        public enum CodeTest
        {
            Debug,
            Default
        }
        public CodeTest codeTest;

        public string testing = "ND_BT";

        private VisualTreeAsset nodeDefaultUXML;
        private const string NodeUXMLPath = "Assets/NDBT/Editor/USS/VisualElement/NodeElement.uxml";
        public VisualTreeAsset GetNodeDefaultUXML()
        {
            if (nodeDefaultUXML == null)
            {
                nodeDefaultUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/NDBT/Editor/USS/VisualElement/NodeElement.uxml"
                );
            }
            return nodeDefaultUXML;
        }
        
        public string GetNodeDefaultUXMLPath()
        {
            return NodeUXMLPath;
        }
    }
}
