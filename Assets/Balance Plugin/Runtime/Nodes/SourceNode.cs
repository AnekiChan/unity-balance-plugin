using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class SourceNode : BalancingNode
    {
        public override string NodeType => "Source";
        public override Color NodeColor => Color.green;
public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public OutputAmountType OutputAmountType = OutputAmountType.Number;
        public int OutputAmount = 1;
        public string OutputFormula = "x";
        public int OutputRandomAmount = 1;
        [Range(0, 100)] public float OutputRandomChance = 50f;
        public int OutputRandomRangeMin = 1;
        public int OutputRandomRangeMax = 10;
        [Min(0)] public int SendInterval = 0;

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

                case OutputAmountType.RandomRange:
                    int min = Mathf.Min(OutputRandomRangeMin, OutputRandomRangeMax);
                    int max = Mathf.Max(OutputRandomRangeMin, OutputRandomRangeMax);
                    return UnityEngine.Random.Range(min, max + 1);

                default:
                    return OutputAmount;
            }
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (SendInterval > 0 && tick % SendInterval != 0) return;

            int amount = GetOutputAmount(tick);
            if (amount <= 0) return;

            foreach (string nodeName in OutputNodeIds)
            {
                BalancingNode node = data.GetNode(nodeName);
                node?.ProcessResources(data, tick, CurrencyIndex, amount);
            }
        }
    }
}
