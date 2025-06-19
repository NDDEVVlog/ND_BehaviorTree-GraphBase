using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

namespace ND_DrawTrello
{
    [System.Serializable]
    public class Node
    {
        [SerializeField] private string m_guid;

        [SerializeField] private Rect m_position;

        public string typeName;
        public string id => m_guid;
        public Rect position => m_position;

        public Node()
        {
            NewGUID();
        }
        private void NewGUID()
        {
            m_guid = Guid.NewGuid().ToString();
        }
        public void SetPosition(Rect position)
        {
            m_position = position;
        }

        public virtual string OnProcess(DrawTrello tree)
        {
            Node nextNodeInFlow = tree.GetNodeFromOutputConnection(m_guid, 0);
            if (nextNodeInFlow != null)
            {
                return nextNodeInFlow.id;
            }
            return string.Empty;
        }

        
    }
}
