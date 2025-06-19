using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ND_BehaviourTrees
{   [NodeInfo("Debug_Log", "Debug/Test1")]
    public class Test1 : Node
    {   
        [System.Serializable]
        public struct CheckBox
        {   
            [Multiline]
            public string text;
            public bool Check;
        }

        [ExposeProperty()]
        public List<CheckBox> checkBoxes;

        //Node
        // Start is called before the first frame update
        void TestOne()
        {
            TestThree();
        }

        // Update is called once per frame
        void Test2()
        {
            Debug.Log("Sora the cheetah");
        }

        void Test4()
        {
            Debug.Log("Number 4");
        }

        //Node
        void TestThree()
        {
            Test2();
            Test4();
        }
    }
}
