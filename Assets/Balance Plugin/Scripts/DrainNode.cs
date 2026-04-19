using UnityEngine;

namespace BalancePlugin
{
    public class DrainNode : BalancingNode
    {
        /* Drains (D) consume Resources from Pools and other Nodes. 
        They can drain Resources from multiple Nodes at the same time and at various rates.*/

        public override string NodeType => "Drain";
        public override Color NodeColor => Color.red;

        public override void ProcessResources(BalancingData data)
        {
        }
    }
}