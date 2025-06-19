using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace ND_DrawTrello.Editor
{   
    [CustomEditor(typeof(DrawTrello))]
    public class ND_DrawTrelloEditor : UnityEditor.Editor
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int index)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset.GetType() == typeof(DrawTrello))
            {
                ND_DrawTrelloEditorWindow.Open((DrawTrello)asset);
                return true;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open"))
            {
                ND_DrawTrelloEditorWindow.Open((DrawTrello)target);

            }
        }
    }
}
