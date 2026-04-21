using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class SourceNode : BalancingNode
    {
        public override string NodeType => "Source";
        public override Color NodeColor => Color.green;
        public override bool CanHaveInput => false;
        public override bool CanHaveOutput => true;

        [Min(1)] public int ProduceAmount = 1;
        [Min(1)] public int SendInterval = 1;

        public override void Initialize()
        {
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (tick % SendInterval != 0) return;

            foreach (string nodeName in OutputNodeIds)
            {
                BalancingNode node = data.GetNode(nodeName);
                node.ProcessResources(data, tick, CurrencyIndex, ProduceAmount);
            }
        }
    }
}