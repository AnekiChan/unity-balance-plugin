using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
                if (node == null)
                    continue;
                node.Initialize();
            }
            CalculateTick(0);
            for (int i = 1; i <= TickCount; i++)
            {
                List<BalancingNode> startNodes = Nodes.FindAll(x => x != null && x.NodeType == "Source" && x.InputNodeIds.Count == 0);
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
            List<BalancingNode> poolNodes = Nodes.FindAll(x => x != null && x.NodeType == "Pool");
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
            return Nodes.Find(n => n != null && n.NodeId == id);
        }

#if UNITY_EDITOR
        public void CreateGraphSO(string currencyName, string folderPath = "Assets")
        {
            if (_tickInfos.Count == 0)
                return;

            int currencyIndex = Currencies.FindIndex(c => c.Name == currencyName);
            if (currencyIndex < 0)
            {
                Debug.LogError($"Currency '{currencyName}' not found");
                return;
            }

            AnimationCurve graph = new AnimationCurve();
            foreach (TickInfo info in _tickInfos)
            {
                if (info.Resources.TryGetValue(currencyIndex, out int value))
                {
                    graph.AddKey(info.Tick, value);
                }
            }

            CurrencyGraph currencyGraph = CreateInstance<CurrencyGraph>();
            currencyGraph.Graph = graph;

            string absoluteDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderPath));
            System.IO.Directory.CreateDirectory(absoluteDir);

            string basePath = $"{folderPath}/{currencyName}Graph";
            string extension = ".asset";

            string path = basePath + extension;
            int counter = 1;
            while (AssetDatabase.LoadAssetAtPath<CurrencyGraph>(path) != null)
            {
                path = basePath + "_" + counter + extension;
                counter++;
            }
            AssetDatabase.CreateAsset(currencyGraph, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(currencyGraph);
        }
#endif
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
        public bool Visible = true;
    }
}
