using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class GateNode : BalancingNode
    {
        public override string NodeType => "Gate";
        public override Color NodeColor => new Color(0.6f, 0.3f, 0.9f);
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

        public List<float> OutputChances = new List<float>();

        public override void Initialize()
        {
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
            if (SendInterval > 0 && tick % SendInterval != 0) return;

            int amount = GetOutputAmount(tick, SendAmount);
            if (amount <= 0) return;
            if (OutputNodeIds == null || OutputNodeIds.Count == 0) return;

            if (OutputChances == null || OutputChances.Count == 0)
            {
                foreach (string nodeName in OutputNodeIds)
                    data.GetNode(nodeName)?.ProcessResources(data, tick, CurrencyIndex, amount);
                return;
            }

            int count = Mathf.Min(OutputChances.Count, OutputNodeIds.Count);

            float totalChance = 0f;
            for (int i = 0; i < count; i++)
                totalChance += Mathf.Max(0f, OutputChances[i]);

            if (totalChance <= 0f)
            {
                foreach (string nodeName in OutputNodeIds)
                    data.GetNode(nodeName)?.ProcessResources(data, tick, CurrencyIndex, amount);
                return;
            }

            float roll = Random.value * totalChance;
            float cumulative = 0f;
            for (int i = 0; i < count; i++)
            {
                cumulative += Mathf.Max(0f, OutputChances[i]);
                if (roll <= cumulative)
                {
                    data.GetNode(OutputNodeIds[i])?.ProcessResources(data, tick, CurrencyIndex, amount);
                    return;
                }
            }

            int lastIndex = count - 1;
            data.GetNode(OutputNodeIds[lastIndex])?.ProcessResources(data, tick, CurrencyIndex, amount);
        }
    }
}
