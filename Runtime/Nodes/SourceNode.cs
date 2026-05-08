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

        public OutputAmountType OutputAmountType = OutputAmountType.Number;
        public int OutputAmount = 1;
        public string OutputFormula = "x";
        public int OutputRandomAmount = 1;
        [Range(0, 100)] public float OutputRandomChance = 50f;
        [Min(1)] public int SendInterval = 1;

        public override void Initialize()
        {
        }

        public int GetOutputAmount(int tick)
        {
            switch (OutputAmountType)
            {
                case OutputAmountType.Number:
                    return OutputAmount;

                case OutputAmountType.Formula:
                    return FormulaEvaluator.EvaluateSingle(OutputFormula, tick);

                case OutputAmountType.Random:
                    if (UnityEngine.Random.value * 100f < OutputRandomChance)
                        return OutputRandomAmount;
                    return 0;

                default:
                    return OutputAmount;
            }
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (tick % SendInterval != 0) return;

            int amount = GetOutputAmount(tick);
            if (amount <= 0) return;

            foreach (string nodeName in OutputNodeIds)
            {
                BalancingNode node = data.GetNode(nodeName);
                node.ProcessResources(data, tick, CurrencyIndex, amount);
            }
        }
    }
}
