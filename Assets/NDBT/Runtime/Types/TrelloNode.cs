using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_DrawTrello
{
            [System.Serializable]
        public struct CheckBox
        {   
            [Multiline]
            public string text;
            public bool isChecked   ;
        }
    [NodeInfo("Trello", "Trello/TrelloDefault")]
    public class TrelloNode : Node
    {   
        [SerializeReference]
        public List<TrelloChildNode> childrenNode = new List<TrelloChildNode>();
    }

    [Serializable]
    [NodeInfo("TrelloChild", "Trello/TrelloChild")]
    public class TrelloChildNode : Node
    {
        public string task;
        public bool isComplete = false;
        public string Description;
        public List<CheckBox> checkBoxes = new List<CheckBox>();
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(task) && (string.IsNullOrEmpty(name) || name != task))
            {
                name = task; // Set the SO asset name from the task field
            }
        }

    }
}
