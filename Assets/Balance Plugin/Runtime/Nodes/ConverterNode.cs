using UnityEngine;

namespace BalancePlugin
{
    public class ConverterNode : BalancingNode
    {
        public override string NodeType => "Converter";
        public override Color NodeColor => Color.yellow;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public OutputAmountType OutputAmountType = OutputAmountType.Number;
        public int OutputAmount = 1;
        public string OutputFormula = "x";
        public int OutputRandomAmount = 1;
        [Range(0, 100)] public float OutputRandomChance = 50f;

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