using UnityEngine;

namespace BalancePlugin
{
    public class ConverterNode : BalancingNode
    {
        public override string NodeType => "Converter";
        public override Color NodeColor => Color.yellow;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        // [Min(1)] public int InputAmount = 1;
        [Min(1)] public int OutputAmount = 1;

        public override void Initialize()
        {
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            foreach (string nodeName in OutputNodeIds)
            {
                BalancingNode node = data.GetNode(nodeName);
                node.ProcessResources(data, tick, CurrencyIndex, OutputAmount);
            }
        }
    }
}