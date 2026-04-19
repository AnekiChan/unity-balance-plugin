using UnityEngine;
using System.Collections.Generic;

namespace BalancePlugin
{
    public abstract class BalancingNode : ScriptableObject
    {
        public string NodeId = System.Guid.NewGuid().ToString();
        public string DisplayName = "Node";
        public Vector2 Position;
        public List<string> InputNodeIds = new List<string>();
        public List<string> OutputNodeIds = new List<string>();

        public abstract string NodeType { get; }
        public abstract Color NodeColor { get; }

        public abstract void ProcessResources(BalancingData data);
    }
}