using UnityEngine;

namespace BalancePlugin
{
    public class PoolNode : BalancingNode
    {
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public OutputAmountType OutputAmountType = OutputAmountType.Number;
        public int OutputAmount = 1;
        public string OutputFormula = "x";
        public int OutputRandomAmount = 1;
        [Range(0, 100)] public float OutputRandomChance = 50f;
        public int OutputRandomRangeMin = 1;
        public int OutputRandomRangeMax = 10;

        public int StartAmount = 0;
        public int StoredAmount = 0;
        [Min(1)] public int SendInterval = 1;

        private int prevTick = 0;

        public override void Initialize()
        {
            StoredAmount = StartAmount;
            prevTick = 0;
        }

        public int GetOutputAmount(int tick, int s = 0)
        {
            switch (OutputAmountType)
            {
                case OutputAmountType.Number:
                    return OutputAmount;

                case OutputAmountType.Formula:
                    return FormulaEvaluator.EvaluateSingle(OutputFormula, tick, s);

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
            int outputAmount = GetOutputAmount(tick, SendAmount);

            if (prevTick != tick && (tick % SendInterval == 0) && StoredAmount >= outputAmount && outputAmount > 0)
            {
                foreach (string nodeName in OutputNodeIds)
                {
                    BalancingNode node = data.GetNode(nodeName);
                    node?.ProcessResources(data, tick, CurrencyIndex, outputAmount);
                    StoredAmount -= outputAmount;
                    prevTick = tick;
                }
            }
            if (CurrencyIndex == SendCurrencyIndex)
            {
                StoredAmount += SendAmount;
            }
        }
    }
}
