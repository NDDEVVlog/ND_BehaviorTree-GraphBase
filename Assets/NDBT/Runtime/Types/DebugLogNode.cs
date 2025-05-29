using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_BehaviourTrees
{
    [NodeInfo("Debug_Log", "Debug/DebugLog")]
    
    public class DebugLogNode : Node
    {   
        [ExposeProperty()]
        public string Log;
        [ExposeProperty()]
        public int number;
        public override string OnProcess(BehaviourTree tree)
        {
            Debug.Log("Debug Log Hello :" + Log);
            Debug.Log("Debug Log Hello :" + (number + number));
            return base.OnProcess(tree);
        }
    }
}
