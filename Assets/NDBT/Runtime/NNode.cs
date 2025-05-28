using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_BehaviorTrees
{
    public abstract class Node : ScriptableObject
    {
        public enum Status { Success, Failure, Running }

        public string name = string.Empty;
        private int _priority;
        public int priority => _priority; // Expose priority but keep setter private

        private int order = -1;
        private BehaviorTree tree;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        private Vector2 nodePosition = Vector2.zero;

        [SerializeField]
        [HideInInspector]
        private bool breakpoint = false;

        [SerializeField]
        [HideInInspector]
        private bool arrangeable = true;

#endif
#if UNITY_EDITOR
        /// <summary>
        /// Editor only method to initialize node in behaviour tree.
        /// </summary>
        /// <param name="tree">Behaviour tree reference. (Owner of this node.)</param>
        /// <param name="order">Node order in behaviour tree.</param>
        public void Initialize(BehaviorTree tree, int order)
        {
            this.tree = tree;
            this.order = order;

            SetupParentReference();
            OnInspectorChanged();
        }
#endif


        public virtual void Traverse(Action<Node> visiter)
        {
            visiter(this);
        }

        // Make sure this is not readonly if you need to reassign it
        public List<Node> children = new List<Node>();
        private Node parent;
        protected int currentChild;

        /// <summary>
        /// Sets a reference to the parent node.
        /// </summary>
        internal virtual void SetupParentReference() { }

        protected internal virtual void SetParent(Node parent)
        {
            this.parent = parent;
        }

        public Node(string name = "Node", int priority = 0)
        {
            this.name = name;
            _priority = priority;
        }


        public virtual void AddChild(Node child) => children.Add(child);

        public virtual Status Process() => children[currentChild].Process();

        public virtual void Reset()
        {
            currentChild = 0;
            foreach (var child in children)
            {
                if (child != null)
                    child.Reset();
            }
        }


        // Update priority and trigger recalculation in RandomRateSelector
        public virtual void SetPriority(int priority)
        {
            _priority = priority;
            // Notify any listeners that the priority has changed (e.g., RandomRateSelector)
            OnPriorityChanged?.Invoke(this);
        }

        // Event to notify when priority changes
        public event Action<Node> OnPriorityChanged;

#if UNITY_EDITOR
        /// <summary>
        /// Editor only callback called when the value in the inspector changes.
        /// </summary>
        public event Action InspectorChanged;
#endif


#if UNITY_EDITOR
        /// <summary>
        /// Editor only method called when the value in the inspector or changes.
        /// </summary>
        //[OnObjectChanged(DelayCall = true)]
        protected virtual void OnInspectorChanged()
        {
            InspectorChanged?.Invoke();
        }

        public void UpdateNodePosition(Node node, Vector2 newPosition)
        {
            // Set new position to node's property if it has one, or store positions in a dictionary
            Debug.Log($"Node {node.name} moved to {newPosition}");
        }
                
        public void SetPosition(Vector2 pos)
        {
            nodePosition = pos;
        }
        public Vector2 GetNodePosition() => nodePosition;

#endif

    }

}