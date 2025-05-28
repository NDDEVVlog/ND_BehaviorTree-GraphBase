using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ND_BehaviorTrees
{
    [CreateAssetMenu]
    public class BehaviorTree : ScriptableObject
    {
        [SerializeField, HideInInspector]
        public Node rootNode;

        [SerializeReference, HideInInspector]
        public List<Node> nodes = new List<Node>();

        internal bool running = false;

        protected virtual void OnEnable()
        {
        #if UNITY_EDITOR
            InitializeNodes();
            if (rootNode == null)
            {
                // Only create root node if we're already on disk
                if (AssetDatabase.Contains(this))
                {
                    EditorApplication.delayCall += () =>
                    {
                        CreateRootNode();
                    };
                }
                else
                {
                    Debug.LogWarning("BehaviourTree asset not yet saved to disk. Save it before editing.");
                }
            }
        #else
            if (rootNode == null)
            {
                CreateRootNode();
            }
        #endif
        }

        private void CreateRootNode()
        {
            rootNode = ScriptableObject.CreateInstance<Selector>(); // or other concrete class
            rootNode.name = "Root Node";
#if UNITY_EDITOR
            rootNode.SetPosition(new Vector2(100, 100)); // <- Set position so it doesn't default to (0,0)
#endif
            nodes.Add(rootNode);

#if UNITY_EDITOR
            if (!AssetDatabase.Contains(this))
            {
                Debug.LogError("BehaviourTree asset is not persistent. Cannot add root node.");
                return;
            }
            AssetDatabase.AddObjectToAsset(rootNode, this);
            AssetDatabase.SaveAssets();
#endif
        }

        protected virtual void Reset()
        {
#if UNITY_EDITOR
            InitializeNodes();
#endif
        }

        internal void InitializeNodes()
        {
            if (rootNode == null) return;

            foreach (Node node in nodes)
            {
                node?.Initialize(this, -1);
            }

            int order = -1;
            rootNode.Traverse(n => n.Initialize(this, order++));
        }
    }
}