using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class ConverterNode : BalancingNode
    {
        public override string NodeType => "Converter";
        public override Color NodeColor => Color.yellow;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public List<NodeOutput> Outputs = new List<NodeOutput>();
        [Min(0)] public int SendInterval = 0;

        public override void Initialize()
        {
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (SendInterval > 0 && tick % SendInterval != 0) return;
            if (OutputNodeIds == null || OutputNodeIds.Count == 0) return;

            for (int i = 0; i < OutputNodeIds.Count; i++)
            {
                int amount = i < Outputs.Count ? Outputs[i].GetAmount(tick, SendAmount) : SendAmount;
                if (amount <= 0) continue;
                data.GetNode(OutputNodeIds[i])?.ProcessResources(data, tick, CurrencyIndex, amount);
            }
        }
    }
}
