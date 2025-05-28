using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_BehaviourTrees
{
    [CreateAssetMenu(menuName = "BehaviourTree/Trees")]
    public class BehaviourTree : ScriptableObject
    {
        [SerializeReference] private List<Node> m_nodes;

        [SerializeField] public List<ND_BTConnection> m_connection;

        public List<ND_BTConnection> conections;

        public List<Node> nodes => m_nodes;
        public BehaviourTree()
        {
            m_nodes = new List<Node>();
            m_connection = new List<ND_BTConnection>();
        }
    }
}
