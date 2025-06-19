using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ND_DrawTrello.Editor
{
    [CustomEditor(typeof(ND_DrawTrelloSetting))]
    public class ND_DTSettingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Debug.Log("Test");


        }
    }
}
