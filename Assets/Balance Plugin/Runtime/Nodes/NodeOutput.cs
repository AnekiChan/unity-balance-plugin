using System;
using UnityEngine;

namespace BalancePlugin
{
    [Serializable]
    public class NodeOutput
    {
        public OutputAmountType AmountType = OutputAmountType.Number;
        public int Amount = 1;
        public string Formula = "s";
        public int RandomAmount = 1;
        [Range(0, 100)] public float RandomChance = 50f;
        public int RandomRangeMin = 1;
        public int RandomRangeMax = 10;

        public int GetAmount(int tick, int s = 0)
        {
            switch (AmountType)
            {
                case OutputAmountType.Number:
                    return Amount;

                case OutputAmountType.Formula:
                    return FormulaEvaluator.EvaluateSingle(Formula, tick, s);

                case OutputAmountType.Random:
                    if (UnityEngine.Random.value * 100f < RandomChance)
                        return RandomAmount;
                    return 0;

                case OutputAmountType.RandomRange:
                    int min = Mathf.Min(RandomRangeMin, RandomRangeMax);
                    int max = Mathf.Max(RandomRangeMin, RandomRangeMax);
                    return UnityEngine.Random.Range(min, max + 1);

                case OutputAmountType.All:
                    return -1;

                default:
                    return Amount;
            }
        }
    }
}
