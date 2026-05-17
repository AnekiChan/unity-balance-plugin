using UnityEngine;

namespace BalancePlugin
{
    public class SourceNode : BalancingNode
    {
        public override string NodeType => "Source";
        public override Color NodeColor => Color.green;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        [System.NonSerialized] public int TriggerCount;
        private int _lastTriggerTick;

        public override void Initialize()
        {
            TriggerCount = 0;
            _lastTriggerTick = -1;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            return true;
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_lastTriggerTick != tick)
            {
                _lastTriggerTick = tick;
                TriggerCount = 0;
            }
            TriggerCount++;
        }
    }
}
