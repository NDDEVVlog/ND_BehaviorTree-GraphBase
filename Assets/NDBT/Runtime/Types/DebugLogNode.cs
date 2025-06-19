using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_DrawTrello
{
    [NodeInfo("Debug_Log", "Debug/DebugLog")]
    
    public class DebugLogNode : Node
    {   
        [ExposeProperty()]
        public string Log;

        public int number;
        public override string OnProcess(DrawTrello tree)
        {   
            
            Debug.Log("Debug Log Hello :" + Log);
            return base.OnProcess(tree);
        }
    }
}
