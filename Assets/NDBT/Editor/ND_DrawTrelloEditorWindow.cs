using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ND_DrawTrello;
using System.Security;
using UnityEditor.Experimental.GraphView;
using System;

namespace ND_DrawTrello.Editor
{
    public class ND_DrawTrelloEditorWindow : EditorWindow
    {
        public static void Open(DrawTrello target)
        {
            ND_DrawTrelloEditorWindow[] windows = Resources.FindObjectsOfTypeAll<ND_DrawTrelloEditorWindow>();
            foreach (var n in windows)
            {
                if (n.currentGraph == target)
                {
                    n.Focus();
                    return;
                }
            }
            ND_DrawTrelloEditorWindow window = CreateWindow<ND_DrawTrelloEditorWindow>(typeof(ND_DrawTrelloEditorWindow), typeof(SceneView));
            window.titleContent = new GUIContent($"{target.name}", EditorGUIUtility.ObjectContent(null, typeof(DrawTrello)).image);
            window.Load(target);
        }

        [SerializeField] private DrawTrello m_currentGraph;
        [SerializeField] private SerializedObject m_serializeObject;
        [SerializeField] private ND_DrawTrelloView m_currentView;

        public DrawTrello currentGraph => m_currentGraph;

        private void OnEnable()
        {
            if (m_currentGraph != null)
            {
                DrawGraph();
            }
        }

        public void Load(DrawTrello target)
        {   
            //LoadGraph
            m_currentGraph = target;
            DrawGraph();
            

        }
        private void DrawGraph()
        {
            m_serializeObject = new SerializedObject(m_currentGraph);
            m_currentView = new ND_DrawTrelloView(m_serializeObject, this);
            m_currentView.graphViewChanged += OnChange;
            rootVisualElement.Add(m_currentView);

        }

        private GraphViewChange OnChange(GraphViewChange graphViewChange)
        {
            this.hasUnsavedChanges = true;
            EditorUtility.SetDirty(m_currentGraph);
            return graphViewChange;
        }

        
    }
}
