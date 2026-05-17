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
        public List<Arrow> Arrows = new List<Arrow>();

        [Header("Simulation")]
        [Min(1)] public int TickCount = 1;

        private List<TickInfo> _tickInfos = new List<TickInfo>();

        public List<TickInfo> TickInfos => _tickInfos;

        private void OnEnable()
        {
            if (Nodes == null)
                Nodes = new List<BalancingNode>();
            if (Arrows == null)
                Arrows = new List<Arrow>();
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
                foreach (Arrow arrow in Arrows)
                {
                    if (arrow != null)
                        arrow.FiredThisTick = false;
                }

                bool changed;
                do
                {
                    changed = false;
                    foreach (Arrow arrow in Arrows)
                    {
                        if (arrow == null || arrow.FiredThisTick)
                            continue;
                        int result = arrow.Process(this, i);
                        if (result > 0)
                            changed = true;
                    }
                } while (changed);

                CalculateTick(i);
            }
            if (debugValues)
                DebugTicks();
        }

        private void CalculateTick(int tick)
        {
            List<BalancingNode> poolNodes = Nodes.FindAll(x => x != null && x.NodeType == "Pool");
            Dictionary<int, int> resources = new Dictionary<int, int>();
            foreach (BalancingNode node in poolNodes)
            {
                PoolNode pool = node as PoolNode;
                foreach (var kv in pool.StoredByCurrency)
                {
                    if (resources.ContainsKey(kv.Key))
                        resources[kv.Key] += kv.Value;
                    else
                        resources[kv.Key] = kv.Value;
                }
            }
            _tickInfos.Add(new TickInfo() { Tick = tick, Resources = resources });
        }

        private void DebugTicks()
        {
            string debugString = "";
            foreach (TickInfo info in _tickInfos)
            {
                debugString += "Tick: " + info.Tick + " Resources: ";
                foreach (var res in info.Resources)
                {
                    string currencyName = res.Key < Currencies.Count ? Currencies[res.Key].Name : "???";
                    debugString += currencyName + ": " + res.Value + " ";
                }
                debugString += "\n";
            }
            Debug.Log(debugString);
        }

        public BalancingNode GetNode(string id)
        {
            return Nodes.Find(n => n != null && n.NodeId == id);
        }

        public Arrow GetArrowByEndpoints(string fromNodeId, string toNodeId)
        {
            return Arrows.Find(a => a != null && a.FromNodeId == fromNodeId && a.ToNodeId == toNodeId);
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
