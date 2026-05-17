using UnityEngine;
using System.Collections.Generic;

namespace BalancePlugin
{
    public class PoolNode : BalancingNode
    {
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public int StartAmount = 0;
        public int StoredAmount = 0;
        public Dictionary<int, int> StoredByCurrency = new Dictionary<int, int>();

        private int _lastWithdrawTick;

        public override void Initialize()
        {
            StoredAmount = StartAmount;
            StoredByCurrency.Clear();
            if (StartAmount != 0 && CurrencyIndex >= 0)
                StoredByCurrency[CurrencyIndex] = StartAmount;
            _lastWithdrawTick = -1;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            return GetStored(currencyIndex) >= amount;
        }

        public override void BeforeSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_lastWithdrawTick != tick)
            {
                _lastWithdrawTick = tick;
            }

            int stored = GetStored(currencyIndex);
            StoredByCurrency[currencyIndex] = Mathf.Max(0, stored - amount);
            UpdateTotalStored();
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (StoredByCurrency.ContainsKey(currencyIndex))
                StoredByCurrency[currencyIndex] += amount;
            else
                StoredByCurrency[currencyIndex] = amount;
            UpdateTotalStored();
        }

        public int GetStored(int currencyIndex)
        {
            StoredByCurrency.TryGetValue(currencyIndex, out int val);
            return val;
        }

        private void UpdateTotalStored()
        {
            StoredAmount = 0;
            foreach (var kv in StoredByCurrency)
                StoredAmount += kv.Value;
        }
    }
}
