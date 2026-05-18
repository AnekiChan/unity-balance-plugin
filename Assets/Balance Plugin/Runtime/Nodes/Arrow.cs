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
        [Min(1)] public int RepeatCount = 1;
        public bool SubtractResource = true;
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

            int totalSent = 0;

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
                    int gateAmount = gate.TotalInputThisTick;
                    if (gateAmount <= 0)
                        return 0;

                    if (!from.CanSend(data, tick, CurrencyIndex, gateAmount))
                        return 0;

                    if (SubtractResource && from is PoolNode p)
                        p.Withdraw(CurrencyIndex, gateAmount);

                    from.BeforeSend(data, tick, CurrencyIndex, gateAmount);
                    to.ReceiveResource(data, tick, CurrencyIndex, gateAmount);
                    totalSent = gateAmount;
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
                    int amount = totalRatio > 0
                        ? gate.TotalInputThisTick * GateRatio / totalRatio
                        : 0;
                    if (amount <= 0)
                        return 0;

                    if (!from.CanSend(data, tick, CurrencyIndex, amount))
                        return 0;

                    if (SubtractResource && from is PoolNode pp)
                        pp.Withdraw(CurrencyIndex, amount);

                    from.BeforeSend(data, tick, CurrencyIndex, amount);
                    to.ReceiveResource(data, tick, CurrencyIndex, amount);
                    totalSent = amount;
                }
            }
            else
            {
                for (int n = 0; n < RepeatCount; n++)
                {
                    int amount;
                    if (from is ConverterNode conv)
                        amount = Output.GetAmount(tick, conv.TotalReceived, n);
                    else if (Output.AmountType == OutputAmountType.All && from is PoolNode poolAll)
                        amount = poolAll.GetStored(CurrencyIndex);
                    else
                        amount = Output.GetAmount(tick, 0, n);

                    if (amount <= 0)
                        break;

                    if (!from.CanSend(data, tick, CurrencyIndex, amount))
                        break;

                    if (SubtractResource && from is PoolNode pool)
                        pool.Withdraw(CurrencyIndex, amount);

                    from.BeforeSend(data, tick, CurrencyIndex, amount);
                    to.ReceiveResource(data, tick, CurrencyIndex, amount);
                    totalSent += amount;
                }
            }

            FiredThisTick = true;
            return totalSent;
        }
    }
}
