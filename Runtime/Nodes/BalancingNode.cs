using UnityEngine;
using System.Collections.Generic;

namespace BalancePlugin
{
    public abstract class BalancingNode : ScriptableObject
    {
        public string NodeId = System.Guid.NewGuid().ToString();
        public string DisplayName = "Node";
        public Vector2 Position;
        public List<string> InputNodeIds = new List<string>();
        public List<string> OutputNodeIds = new List<string>();
        public int CurrencyIndex = 0;

        public abstract string NodeType { get; }
        public abstract Color NodeColor { get; }
        public abstract bool CanHaveInput { get; }
        public abstract bool CanHaveOutput { get; }

        public abstract void Initialize();
        public abstract void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount);

        public string GetCurrency(BalancingData data)
        {
            if (data == null || data.Currencies == null || data.Currencies.Count == 0)
                return "Coin";
            if (CurrencyIndex >= data.Currencies.Count)
                CurrencyIndex = 0;
            return data.Currencies[CurrencyIndex].Name;
        }
    }
}
