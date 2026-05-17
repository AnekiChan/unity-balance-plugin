using UnityEngine;

namespace BalancePlugin
{
    public class ConverterNode : BalancingNode
    {
        public override string NodeType => "Converter";
        public override Color NodeColor => Color.yellow;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        [System.NonSerialized] public int TotalReceived;
        [System.NonSerialized] public int ReceivedCount;

        private int _lastReceiveTick;
        private bool _hasSentThisTick;

        public override void Initialize()
        {
            TotalReceived = 0;
            ReceivedCount = 0;
            _lastReceiveTick = -1;
            _hasSentThisTick = false;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_hasSentThisTick)
                return false;

            int incomingCount = 0;
            int firedCount = 0;
            foreach (Arrow arrow in data.Arrows)
            {
                if (arrow == null || arrow.ToNodeId != NodeId)
                    continue;
                incomingCount++;
                if (arrow.FiredThisTick)
                    firedCount++;
            }

            return incomingCount == 0 || firedCount >= incomingCount;
        }

        public override void BeforeSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            _hasSentThisTick = true;
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_lastReceiveTick != tick)
            {
                _lastReceiveTick = tick;
                TotalReceived = 0;
                ReceivedCount = 0;
                _hasSentThisTick = false;
            }

            TotalReceived += amount;
            ReceivedCount++;
        }
    }
}
