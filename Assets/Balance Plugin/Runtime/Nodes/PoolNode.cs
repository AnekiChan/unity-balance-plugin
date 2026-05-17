using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class PoolNode : BalancingNode
    {
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public List<NodeOutput> Outputs = new List<NodeOutput>();

        public int StartAmount = 0;
        public int StoredAmount = 0;
        [Min(0)] public int SendInterval = 0;

        private int prevTick = 0;

        public override void Initialize()
        {
            StoredAmount = StartAmount;
            prevTick = 0;
        }

        private int GetOutputAmount(int index, int tick, int s = 0)
        {
            if (index < Outputs.Count)
                return Outputs[index].GetAmount(tick, s);
            return 1;
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (CurrencyIndex == SendCurrencyIndex)
                StoredAmount += SendAmount;

            bool shouldFire = SendInterval <= 0 || tick % SendInterval == 0;
            if (prevTick != tick && shouldFire && OutputNodeIds.Count > 0)
            {
                for (int i = 0; i < OutputNodeIds.Count; i++)
                {
                    BalancingNode target = data.GetNode(OutputNodeIds[i]);
                    if (target == null) continue;

                    if (target is DrainNode drain)
                    {
                        int drainAmount = Mathf.Min(StoredAmount, drain.DrainAmount);
                        if (drainAmount > 0)
                        {
                            StoredAmount -= drainAmount;
                            drain.ProcessResources(data, tick, CurrencyIndex, drainAmount);
                        }
                    }
                    else
                    {
                        int outputAmount = GetOutputAmount(i, tick, SendAmount);
                        if (outputAmount > 0 && StoredAmount >= outputAmount)
                        {
                            StoredAmount -= outputAmount;
                            target.ProcessResources(data, tick, CurrencyIndex, outputAmount);
                        }
                    }
                }
                prevTick = tick;
            }
        }
    }
}
