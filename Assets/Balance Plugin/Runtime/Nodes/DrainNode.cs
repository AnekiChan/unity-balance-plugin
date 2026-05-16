using UnityEngine;

namespace BalancePlugin
{
    public class DrainNode : BalancingNode
    {
        public override string NodeType => "Drain";
        public override Color NodeColor => Color.red;
        public override bool CanHaveInput => true;
        public override bool CanHaveOutput => false;

        [Min(1)] public int DrainAmount = 1;
        [Min(0)] public int SendInterval = 0;

        public override void Initialize()
        {
        }

        public override void ProcessResources(BalancingData data, int tick, int SendCurrencyIndex, int SendAmount)
        {

        }
    }
}