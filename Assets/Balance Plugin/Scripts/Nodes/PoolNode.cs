using UnityEngine;

namespace BalancePlugin
{
    public class PoolNode : BalancingNode
    {
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;
        // public bool IsProcessed = false;

        public int StartAmount = 0;
        public int StoredAmount = 0;
        [Min(1)] public int SendInterval = 1;
        [Min(1)] public int OutputAmount = 1;

        private int prevTick = 0;

        public override void Initialize()
        {
            StoredAmount = StartAmount;
            // IsProcessed = false;
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (prevTick != tick && (tick % SendInterval == 0) && StoredAmount >= OutputAmount)
            {
                foreach (string nodeName in OutputNodeIds)
                {
                    BalancingNode node = data.GetNode(nodeName);
                    node.ProcessResources(data, tick, CurrencyIndex, OutputAmount);
                    StoredAmount -= OutputAmount;
                    prevTick = tick;
                }
                // IsProcessed = true;
            }
            if (CurrencyIndex == SendCurrencyIndex)
            {
                StoredAmount += SendAmount;
            }
        }
    }
}