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

        public List<NodeOutput> Outputs = new List<NodeOutput>();
        [Min(0)] public int SendInterval = 0;
        public List<float> OutputChances = new List<float>();

        public override void Initialize()
        {
        }

        private int GetOutputAmount(int index, int tick, int s)
        {
            if (index < Outputs.Count)
                return Outputs[index].GetAmount(tick, s);
            return s;
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {
            if (SendInterval > 0 && tick % SendInterval != 0) return;
            if (OutputNodeIds == null || OutputNodeIds.Count == 0) return;

            if (OutputChances == null || OutputChances.Count == 0)
            {
                for (int i = 0; i < OutputNodeIds.Count; i++)
                {
                    int amount = GetOutputAmount(i, tick, SendAmount);
                    if (amount <= 0) continue;
                    data.GetNode(OutputNodeIds[i])?.ProcessResources(data, tick, CurrencyIndex, amount);
                }
                return;
            }

            int count = Mathf.Min(OutputChances.Count, OutputNodeIds.Count);

            float totalChance = 0f;
            for (int i = 0; i < count; i++)
                totalChance += Mathf.Max(0f, OutputChances[i]);

            if (totalChance <= 0f)
            {
                for (int i = 0; i < OutputNodeIds.Count; i++)
                {
                    int amount = GetOutputAmount(i, tick, SendAmount);
                    if (amount <= 0) continue;
                    data.GetNode(OutputNodeIds[i])?.ProcessResources(data, tick, CurrencyIndex, amount);
                }
                return;
            }

            float roll = Random.value * totalChance;
            float cumulative = 0f;
            for (int i = 0; i < count; i++)
            {
                cumulative += Mathf.Max(0f, OutputChances[i]);
                if (roll <= cumulative)
                {
                    int amount = GetOutputAmount(i, tick, SendAmount);
                    if (amount > 0)
                        data.GetNode(OutputNodeIds[i])?.ProcessResources(data, tick, CurrencyIndex, amount);
                    return;
                }
            }

            int lastIndex = count - 1;
            int lastAmount = GetOutputAmount(lastIndex, tick, SendAmount);
            if (lastAmount > 0)
                data.GetNode(OutputNodeIds[lastIndex])?.ProcessResources(data, tick, CurrencyIndex, lastAmount);
        }
    }
}
