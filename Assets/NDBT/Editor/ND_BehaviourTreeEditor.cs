using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace ND_BehaviourTrees.Editor
{   
    [CustomEditor(typeof(BehaviourTree))]
    public class ND_BehaviourTreeEditor : UnityEditor.Editor
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int index)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset.GetType() == typeof(BehaviourTree))
            {
                ND_BehaviorTreesEditorWindow.Open((BehaviourTree)asset);
                return true;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open"))
            {
                ND_BehaviorTreesEditorWindow.Open((BehaviourTree)target);

            }
        }
    }
}
