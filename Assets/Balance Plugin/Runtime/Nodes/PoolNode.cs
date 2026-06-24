using UnityEngine;
using System.Collections.Generic;

namespace BalancePlugin
{
    public enum PoolCapacityMode
    {
        Infinite,
        Limited
    }

    public class PoolNode : BalancingNode
    {
        public override string NodeType => "Pool";
        public override Color NodeColor => Color.blue;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public int StartAmount = 0;
        public int StoredAmount = 0;
        public Dictionary<int, int> StoredByCurrency = new Dictionary<int, int>();

        public PoolCapacityMode CapacityMode = PoolCapacityMode.Infinite;
        public int MaxAmount = 0;

        [System.NonSerialized] public Dictionary<string, int> FairAllocateAmounts = new Dictionary<string, int>();
        [System.NonSerialized] public bool HasFairAllocatedThisTick;

        private int _lastWithdrawTick;

        public override void Initialize()
        {
            StoredAmount = StartAmount;
            StoredByCurrency.Clear();
            if (StartAmount != 0 && CurrencyIndex >= 0)
            {
                int amount = StartAmount;
                if (CapacityMode == PoolCapacityMode.Limited)
                    amount = Mathf.Min(amount, MaxAmount);
                StoredByCurrency[CurrencyIndex] = amount;
            }
            if (FairAllocateAmounts == null)
                FairAllocateAmounts = new Dictionary<string, int>();
            FairAllocateAmounts.Clear();
            HasFairAllocatedThisTick = false;
            _lastWithdrawTick = -1;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            return GetStored(currencyIndex) >= amount;
        }

        public override void OnTickStart(BalancingData data, int tick)
        {
            HasFairAllocatedThisTick = false;
        }

        public override void BeforeSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            _lastWithdrawTick = tick;
        }

        public void FairAllocate(BalancingData data, int tick)
        {
            HasFairAllocatedThisTick = true;
            if (FairAllocateAmounts == null)
                FairAllocateAmounts = new Dictionary<string, int>();
            FairAllocateAmounts.Clear();

            var outgoing = data.GetOutgoingArrows(NodeId);
            if (outgoing.Count == 0)
                return;

            int totalDemand = 0;
            int nonAllCount = 0;
            foreach (Arrow a in outgoing)
            {
                if (a == null || a.Output.AmountType == OutputAmountType.All)
                    continue;
                int demand = a.Output.GetAmount(tick, 0, 0);
                if (demand > 0)
                {
                    totalDemand += demand;
                    nonAllCount++;
                }
            }
            if (nonAllCount == 0 || totalDemand <= 0)
                return;

            int available = GetStored(CurrencyIndex);
            if (available <= 0)
                return;

            int totalAllocated = 0;
            foreach (Arrow a in outgoing)
            {
                if (a == null || a.Output.AmountType == OutputAmountType.All)
                    continue;
                int demand = a.Output.GetAmount(tick, 0, 0);
                if (demand <= 0)
                    continue;
                int share = totalDemand <= available
                    ? demand
                    : available * demand / totalDemand;
                FairAllocateAmounts[a.ArrowId] = share;
                totalAllocated += share;
            }

            if (totalDemand > available)
            {
                int remainder = available - totalAllocated;
                for (int i = 0; remainder > 0 && i < outgoing.Count; i++)
                {
                    Arrow a = outgoing[i];
                    if (a == null || a.Output.AmountType == OutputAmountType.All)
                        continue;
                    FairAllocateAmounts[a.ArrowId]++;
                    remainder--;
                }
            }
        }

        public void Withdraw(int currencyIndex, int amount)
        {
            int stored = GetStored(currencyIndex);
            StoredByCurrency[currencyIndex] = Mathf.Max(0, stored - amount);
            UpdateTotalStored();
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (CapacityMode == PoolCapacityMode.Limited)
            {
                int currentTotal = StoredAmount;
                int remaining = MaxAmount - currentTotal;
                if (remaining <= 0) return;
                amount = Mathf.Min(amount, remaining);
            }

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
