using System;
using UnityEngine;

namespace BalancePlugin
{
    public enum NodeType
    {
        Source,
        Drain,
        Converter,
        Pool,
        Gate
    }

    [Serializable]
    public class BalanceNode
    {
        public string Id;
        public Vector2 Position;
        public NodeType Type;
        public string Label;
        public float Value;

        public BalanceNode(Vector2 position)
        {
            Id = Guid.NewGuid().ToString();
            Position = position;
            Type = NodeType.Pool;
            Label = "New Node";
            Value = 100f;
        }
    }

    [Serializable]
    public class NodeConnection
    {
        public string FromNodeId;
        public string ToNodeId;
        public float FlowRate;

        public NodeConnection(string fromId, string toId)
        {
            FromNodeId = fromId;
            ToNodeId = toId;
            FlowRate = 10f;
        }
    }
}
