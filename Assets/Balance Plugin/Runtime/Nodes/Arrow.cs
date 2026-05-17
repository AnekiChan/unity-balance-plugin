using UnityEngine;

namespace BalancePlugin
{
    public class Arrow : ScriptableObject
    {
        public string ArrowId = "";
        public string FromNodeId;
        public string ToNodeId;
        public int CurrencyIndex = 0;
        [Min(0)] public int SendInterval = 0;
        public NodeOutput Output = new NodeOutput();

        [Range(0, 100)] public int GateRatio = 10;
        [Range(0, 100)] public float GateChance = 50f;

        [System.NonSerialized] public bool FiredThisTick;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(ArrowId))
                ArrowId = System.Guid.NewGuid().ToString();
        }

        public int Process(BalancingData data, int tick)
        {
            if (FiredThisTick)
                return 0;
            if (SendInterval > 0 && tick % SendInterval != 0)
                return 0;

            BalancingNode from = data.GetNode(FromNodeId);
            BalancingNode to = data.GetNode(ToNodeId);
            if (from == null || to == null)
                return 0;

            int amount;

            if (from is GateNode gate)
            {
                if (gate.TotalInputThisTick <= 0)
                    return 0;

                if (gate.Mode == GateMode.RandomPath)
                {
                    if (!gate.HasResolvedThisTick)
                        gate.ResolveRandomPath(data);
                    if (gate.SelectedArrowId != ArrowId)
                        return 0;
                    amount = gate.TotalInputThisTick;
                }
                else
                {
                    var outgoing = data.GetOutgoingArrows(gate.NodeId);
                    int totalRatio = 0;
                    foreach (Arrow a in outgoing)
                    {
                        if (a != null)
                            totalRatio += a.GateRatio;
                    }
                    amount = totalRatio > 0
                        ? gate.TotalInputThisTick * GateRatio / totalRatio
                        : 0;
                }
            }
            else if (from is ConverterNode conv)
            {
                amount = Output.GetAmount(tick, conv.TotalReceived);
            }
            else
            {
                if (Output.AmountType == OutputAmountType.All && from is PoolNode pool)
                    amount = pool.GetStored(CurrencyIndex);
                else
                    amount = Output.GetAmount(tick, 0);
            }

            if (amount <= 0)
                return 0;

            if (!from.CanSend(data, tick, CurrencyIndex, amount))
                return 0;

            from.BeforeSend(data, tick, CurrencyIndex, amount);
            to.ReceiveResource(data, tick, CurrencyIndex, amount);

            FiredThisTick = true;
            return amount;
        }
    }
}
