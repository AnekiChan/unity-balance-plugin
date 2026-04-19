using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class BalancingData : ScriptableObject
    {
        public List<BalancingNode> Nodes = new List<BalancingNode>();
        public List<NodeConnection> Connections = new List<NodeConnection>();
    }

    [System.Serializable]
    public class NodeConnection
    {
        public string FromNodeId;
        public string ToNodeId;
    }
}