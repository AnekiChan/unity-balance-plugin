using UnityEngine;

namespace BalancePlugin
{
    public enum GateMode
    {
        Distribution,
        RandomPath
    }

    public class GateNode : BalancingNode
    {
        public override string NodeType => "Gate";
        public override Color NodeColor => new Color(0.6f, 0.3f, 0.9f);
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => true;

        public GateMode Mode = GateMode.Distribution;

        [System.NonSerialized] public int TotalInputThisTick;
        [System.NonSerialized] public bool HasResolvedThisTick;
        [System.NonSerialized] public string SelectedArrowId = "";

        private int _lastReceiveTick;

        public override void Initialize()
        {
            TotalInputThisTick = 0;
            HasResolvedThisTick = false;
            SelectedArrowId = "";
            _lastReceiveTick = -1;
        }

        public override bool CanSend(BalancingData data, int tick, int currencyIndex, int amount)
        {
            return TotalInputThisTick > 0;
        }

        public override void ReceiveResource(BalancingData data, int tick, int currencyIndex, int amount)
        {
            if (_lastReceiveTick != tick)
            {
                _lastReceiveTick = tick;
                TotalInputThisTick = 0;
                HasResolvedThisTick = false;
                SelectedArrowId = "";
            }
            TotalInputThisTick += amount;
        }

        public void ResolveRandomPath(BalancingData data)
        {
            HasResolvedThisTick = true;
            var outgoing = data.GetOutgoingArrows(NodeId);
            if (outgoing.Count == 0)
                return;

            float totalChance = 0f;
            foreach (Arrow a in outgoing)
            {
                if (a != null)
                    totalChance += Mathf.Max(0f, a.GateChance);
            }

            if (totalChance <= 0f)
            {
                SelectedArrowId = outgoing[Random.Range(0, outgoing.Count)].ArrowId;
                return;
            }

            float roll = Random.value * totalChance;
            float cumulative = 0f;
            foreach (Arrow a in outgoing)
            {
                if (a == null) continue;
                cumulative += Mathf.Max(0f, a.GateChance);
                if (roll <= cumulative)
                {
                    SelectedArrowId = a.ArrowId;
                    return;
                }
            }
            int lastIndex = outgoing.Count - 1;
            if (lastIndex >= 0 && outgoing[lastIndex] != null)
                SelectedArrowId = outgoing[lastIndex].ArrowId;
        }
    }
}
