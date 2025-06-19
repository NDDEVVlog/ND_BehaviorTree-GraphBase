using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ND_BehaviourTrees.Editor
{
    [CustomEditor(typeof(ND_BTSetting))]
    public class ND_BTSettingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Debug.Log("Test");


        }
    }
}
