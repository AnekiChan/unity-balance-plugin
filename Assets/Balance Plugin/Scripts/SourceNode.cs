using UnityEngine;

namespace BalancePlugin
{
    public class SourceNode : BalancingNode
    {
        /*Sources (S) are Nodes that produce an infinite number of Resources for other Nodes to use.
        They can generate resources for an unlimited number of Nodes simultaneously and at various rates.*/
        public override string NodeType => "Source";
        public override Color NodeColor => Color.green;
        [Space]
        public int ResourceAmount = 0;

        public override void ProcessResources(BalancingData data)
        {
        }
    }
}