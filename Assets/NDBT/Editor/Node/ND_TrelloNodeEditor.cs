// File: Assets/NDBT/Editor/NodeEditors/ND_TrelloNodeEditor.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace ND_DrawTrello.Editor
{
    public class ND_TrelloNodeEditor : ND_NodeEditor
    {
        private VisualElement collapseEara;
        private Button m_AddChildButton;
        
        public ND_TrelloNodeEditor(Node node, SerializedObject BTObject)
            : base(node, BTObject, ND_DrawTrelloSetting.Instance.GetTrelloUXMLPath())
        {
            this.AddToClassList("trello-main-node-editor");


            m_AddChildButton = this.Q<Button>("add-child-task-button");
            if (m_AddChildButton != null)
            {
                m_AddChildButton.clicked += AddNewChildTask;
            }
            else
            {
                Debug.LogError("ND_TrelloNodeEditor: 'add-child-task-button' not found in UXML.");
            }

            m_DragableNodeContainer = this.Q<VisualElement>("draggable-nodes-container");
            if (m_DragableNodeContainer != null)
                Debug.Log("ND_TrelloNodeEditor: 'draggable-nodes-container' found in UXML.");
            
            PopulateExistingChildTasks();
        }

        private void PopulateExistingChildTasks()
        {
            if (m_DragableNodeContainer == null)
            {
                Debug.LogError("ND_TrelloNodeEditor: m_DragableNodeContainer is null. Cannot populate child tasks.");
                return;
            }

            TrelloNode trelloNodeData = node as TrelloNode;
            if (trelloNodeData == null)
            {
                Debug.LogError("ND_TrelloNodeEditor: Node data is not of type TrelloNode.");
                return;
            }
            
            m_DragableNodeContainer.Clear(); 

            if(trelloNodeData.childrenNode.Count >0)
            foreach (TrelloChildNode childData in trelloNodeData.childrenNode)
            {
                if (childData != null)
                    CreateAndAddChildEditor(childData);
            }
        }

        private void AddNewChildTask()
        {
            TrelloNode trelloNodeData = node as TrelloNode;
            if (trelloNodeData == null) return;
            
            Undo.RecordObject(m_SerializedObject.targetObject, "Add Trello Child Task");

            TrelloChildNode newChildData = new TrelloChildNode();
            newChildData.SetNewID (System.Guid.NewGuid().ToString());
            newChildData.task = $"New Task {trelloNodeData.childrenNode.Count + 1}";
            
            trelloNodeData.childrenNode.Add(newChildData);
            
            EditorUtility.SetDirty(m_SerializedObject.targetObject);
            m_SerializedObject.ApplyModifiedProperties(); 
            m_SerializedObject.Update();


            CreateAndAddChildEditor(newChildData);
        }

        private ND_TrelloChild CreateAndAddChildEditor(TrelloChildNode childData)
        {
            if (m_DragableNodeContainer == null)
            {
                Debug.LogError("ND_TrelloNodeEditor: m_DragableNodeContainer is null. Cannot add child task editor.");
                return null;
            }

            ND_TrelloChild childEditor = new ND_TrelloChild(childData, m_SerializedObject);
            m_DragableNodeContainer.Add(childEditor);
            
            childEditor.style.position = Position.Relative;
            childEditor.style.left = StyleKeyword.Auto;
            childEditor.style.top = StyleKeyword.Auto;
            childEditor.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            childEditor.style.marginBottom = 2;

            return childEditor;
        }
        
        public override bool DragPerform(DragPerformEvent evt, System.Collections.Generic.IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (m_DragableNodeContainer == null) return false;

            var droppedNodeEditor = selection.FirstOrDefault() as ND_NodeEditor;
            if (droppedNodeEditor is ND_TrelloChild trelloChildEditor)
            {
                if (CanAcceptDrop(selection.ToList())) 
                {
                    var childData = trelloChildEditor.node as TrelloChildNode;
                    TrelloNode trelloNodeData = this.node as TrelloNode;

                    if (childData != null && trelloNodeData != null)
                    {
                        var oldParentVisual = trelloChildEditor.parent;
                        ND_TrelloNodeEditor oldParentEditor = null;
                        if(oldParentVisual is VisualElement ve) { // Check if parent is part of another TrelloNode
                            var parentNodeEditorCandidate = ve.GetFirstAncestorOfType<ND_TrelloNodeEditor>();
                            if(parentNodeEditorCandidate != null && parentNodeEditorCandidate != this) {
                                oldParentEditor = parentNodeEditorCandidate;
                            }
                        }

                        Undo.RecordObject(m_SerializedObject.targetObject, "Move Trello Child Task");

                        if(oldParentEditor != null) {
                            TrelloNode oldTrelloParentData = oldParentEditor.node as TrelloNode;
                            oldTrelloParentData?.childrenNode.Remove(childData);
                        }
                        
                        if (!trelloNodeData.childrenNode.Contains(childData))
                        {
                            trelloNodeData.childrenNode.Add(childData);
                        }
                        
                        EditorUtility.SetDirty(m_SerializedObject.targetObject);
                        m_SerializedObject.ApplyModifiedProperties();
                        m_SerializedObject.Update();

                        trelloChildEditor.RemoveFromHierarchy();
                        m_DragableNodeContainer.Add(trelloChildEditor);

                        trelloChildEditor.style.position = Position.Relative;
                        trelloChildEditor.style.left = StyleKeyword.Auto;
                        trelloChildEditor.style.top = StyleKeyword.Auto;
                        trelloChildEditor.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                        trelloChildEditor.style.marginBottom = 2;
                    
                        evt.StopPropagation();
                        return true;
                    }
                }
            }
            return base.DragPerform(evt, selection, dropTarget, dragSource);
        }

        public void RemoveChildTaskData(TrelloChildNode childDataToRemove)
        {
            TrelloNode trelloNodeData = node as TrelloNode;
            if (trelloNodeData == null || childDataToRemove == null) return;

            Undo.RecordObject(m_SerializedObject.targetObject, "Remove Trello Child Task");
            trelloNodeData.childrenNode.Remove(childDataToRemove);
            EditorUtility.SetDirty(m_SerializedObject.targetObject);
            m_SerializedObject.ApplyModifiedProperties();
            m_SerializedObject.Update();
        }
    }
}