using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    [CreateAssetMenu(fileName = "NewBalanceGraph", menuName = "Balance Plugin/Balance Graph")]
    public class BalanceGraph : ScriptableObject
    {
        public List<BalanceNode> Nodes = new List<BalanceNode>();
        public List<NodeConnection> Connections = new List<NodeConnection>();

        public BalanceNode AddNode(Vector2 position)
        {
            var node = new BalanceNode(position);
            Nodes.Add(node);
            return node;
        }

        public void RemoveNode(string nodeId)
        {
            Nodes.RemoveAll(n => n.Id == nodeId);
            Connections.RemoveAll(c => c.FromNodeId == nodeId || c.ToNodeId == nodeId);
        }

        public void AddConnection(string fromId, string toId)
        {
            if (fromId == toId) return;
            if (Connections.Exists(c => c.FromNodeId == fromId && c.ToNodeId == toId)) return;
            
            Connections.Add(new NodeConnection(fromId, toId));
        }

        public void RemoveConnection(string fromId, string toId)
        {
            Connections.RemoveAll(c => c.FromNodeId == fromId && c.ToNodeId == toId);
        }

        public BalanceNode GetNode(string nodeId)
        {
            return Nodes.Find(n => n.Id == nodeId);
        }

        public NodeConnection GetConnection(string fromId, string toId)
        {
            return Connections.Find(c => c.FromNodeId == fromId && c.ToNodeId == toId);
        }
    }
}
