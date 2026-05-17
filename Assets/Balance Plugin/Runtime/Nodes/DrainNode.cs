using UnityEngine;
using System.Collections.Generic;

namespace BalancePlugin
{
    public class DrainNode : BalancingNode
    {
        public override string NodeType => "Drain";
        public override Color NodeColor => Color.red;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => false;

        [Min(0)] public int DrainAmount = 1;
        public int DrainedAmount = 0;
        public Dictionary<int, int> DrainedByCurrency = new Dictionary<int, int>();

        private int _lastTick;

        public override void Initialize()
        {
            DrainedAmount = 0;
            DrainedByCurrency.Clear();
            _lastTick = -1;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            return false;
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_lastTick != tick)
            {
                _lastTick = tick;
                DrainedByCurrency.Clear();
            }

            DrainedAmount += amount;
            if (DrainedByCurrency.ContainsKey(currencyIndex))
                DrainedByCurrency[currencyIndex] += amount;
            else
                DrainedByCurrency[currencyIndex] = amount;
        }
    }
}
