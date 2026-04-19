using UnityEngine;

namespace BalancePlugin
{
    public class PoolNode : BalancingNode
    {
        /* Pools (P) store Resources and are the building blocks.
        They can receive Resources and send them forward to other Nodes.*/
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;

        public override void ProcessResources(BalancingData data)
        {
        }
    }
}