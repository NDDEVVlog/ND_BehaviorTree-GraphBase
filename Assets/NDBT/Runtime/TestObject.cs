using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_DrawTrello
{
    public class TestObject : MonoBehaviour
    {   
        
        
        public DrawTrello tree;
        public DrawTrello treeInstance;

        public void OnEnable()
        {   
            treeInstance = Instantiate(tree);
            ExecuateAccess();
        }

        private void ExecuateAccess()
        {   
            
            treeInstance.Init();
            Node rootNode = treeInstance.GetRootNode();
            ProcessAndMoveToNextNode(rootNode);

        }

        private void ProcessAndMoveToNextNode(Node rootNode)
        {
            string nextNodeID = rootNode.OnProcess(treeInstance);
            if (!string.IsNullOrEmpty(nextNodeID))
            {
                Node node = treeInstance.GetNode(nextNodeID);
                ProcessAndMoveToNextNode(node);
            }
        }

        
    }
}
