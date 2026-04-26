using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class BalancingData : ScriptableObject
    {
        public List<CurrencyInfo> Currencies = new List<CurrencyInfo> { new CurrencyInfo { Name = "Coin", Color = Color.red } };
        [HideInInspector]
        public List<BalancingNode> Nodes = new List<BalancingNode>();
        [HideInInspector]
        public List<NodeConnection> Connections = new List<NodeConnection>();

        [Header("Simulation")]
        [Min(1)] public int TickCount = 1;

        private List<TickInfo> _tickInfos = new List<TickInfo>();

        public List<TickInfo> TickInfos => _tickInfos;

        private void OnEnable()
        {
            if (Nodes == null)
                Nodes = new List<BalancingNode>();
            if (Connections == null)
                Connections = new List<NodeConnection>();
        }

        public void CalculateStatistics(bool debugValues = true)
        {
            _tickInfos.Clear();
            foreach (BalancingNode node in Nodes)
            {
                node.Initialize();
            }
            CalculateTick(0);
            for (int i = 1; i <= TickCount; i++)
            {
                List<BalancingNode> startNodes = Nodes.FindAll(x => x.InputNodeIds.Count == 0);
                foreach (BalancingNode node in startNodes)
                {
                    node.ProcessResources(this, i, -1, -1);
                }
                CalculateTick(i);
            }
            if (debugValues)
                DebugTicks();
        }

        private void ClearNodes()
        {
            List<BalancingNode> poolNodes = Nodes.FindAll(x => x.NodeType == "Pool");
            foreach (BalancingNode node in poolNodes)
            {
                (node as PoolNode).StoredAmount = 0;
            }
        }

        private void CalculateTick(int tick)
        {
            List<BalancingNode> poolNodes = Nodes.FindAll(x => x.NodeType == "Pool");
            Dictionary<int, int> resourses = new Dictionary<int, int>();
            foreach (BalancingNode node in poolNodes)
            {
                if (resourses.ContainsKey(node.CurrencyIndex))
                    resourses[node.CurrencyIndex] += (node as PoolNode).StoredAmount;
                else
                    resourses.Add(node.CurrencyIndex, (node as PoolNode).StoredAmount);
            }
            _tickInfos.Add(new TickInfo() { Tick = tick, Resources = resourses });
        }

        private void DebugTicks()
        {
            string debugString = "";
            foreach (TickInfo info in _tickInfos)
            {
                debugString += "Tick: " + info.Tick + " Resources: ";
                foreach (var res in info.Resources)
                {
                    debugString += Currencies[res.Key].Name + ": " + res.Value + " ";
                }
                debugString += "\n";
            }
            Debug.Log(debugString);
        }

        public BalancingNode GetNode(string id)
        {
            return Nodes.Find(n => n.NodeId == id);
        }
    }

    [System.Serializable]
    public class NodeConnection
    {
        public string FromNodeId;
        public string ToNodeId;
    }

    [System.Serializable]
    public class TickInfo
    {
        public int Tick;
        public Dictionary<int, int> Resources = new Dictionary<int, int>();
    }

    [System.Serializable]
    public class CurrencyInfo
    {
        public string Name = "NewCurrency";
        public Color Color = Color.red;
    }
}