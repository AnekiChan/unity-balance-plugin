using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BalancePlugin
{
    public class NodeInspectorWindow : EditorWindow
    {
        private BalancingNode _currentNode;
        private BalancingWindow _parentWindow;
        private BalancingData _data;
        private int _currentTick = 1;

        public void SetParentWindow(BalancingWindow parent)
        {
            _parentWindow = parent;
        }

        public void SetData(BalancingData data)
        {
            _data = data;
        }

        public void SetNode(BalancingNode node)
        {
            _currentNode = node;
        }

        public void SetCurrentTick(int tick)
        {
            _currentTick = tick;
        }

        [MenuItem("Tools/Balance/Balance Inspector")]
        public static void ShowWindow()
        {
            GetWindow<NodeInspectorWindow>("Node Inspector");
        }

        private void OnGUI()
        {
            if (_currentNode == null)
            {
                EditorGUILayout.LabelField("Select a node to configure");
                return;
            }

            EditorGUILayout.LabelField("Type: " + _currentNode.NodeType);
            _currentNode.DisplayName = EditorGUILayout.TextField("Name", _currentNode.DisplayName);

            EditorGUILayout.Space();
            DrawNodeSpecificFields();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_currentNode);
            }
        }

        private void DrawNodeSpecificFields()
        {
            if (_currentNode is DrainNode drainNode)
            {
                drainNode.DrainAmount = EditorGUILayout.IntField("Drain Amount", drainNode.DrainAmount);
            }
            else if (_currentNode is PoolNode poolNode)
            {
                DrawCurrencySelector("Stored Currency");
                poolNode.StartAmount = EditorGUILayout.IntField("Initial Amount", poolNode.StartAmount);
                DrawPoolStoredInfo(poolNode);
            }
            else
            {
                DrawCurrencySelector("Currency");
            }
        }

        private void DrawPoolStoredInfo(PoolNode pool)
        {
            if (_data == null || _data.TickInfos == null || _data.TickInfos.Count <= 1)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stored Resources (last tick)", EditorStyles.boldLabel);

            if (pool.StoredByCurrency.Count == 0)
            {
                EditorGUILayout.LabelField("  (empty)");
                return;
            }

            foreach (var kv in pool.StoredByCurrency)
            {
                string currencyName = kv.Key < _data.Currencies.Count ? _data.Currencies[kv.Key].Name : $"Currency {kv.Key}";
                EditorGUILayout.LabelField($"  {currencyName}", kv.Value.ToString());
            }
        }

        private void DrawCurrencySelector(string label)
        {
            if (_data != null && _data.Currencies != null && _data.Currencies.Count > 0)
            {
                if (_currentNode.CurrencyIndex >= _data.Currencies.Count)
                    _currentNode.CurrencyIndex = 0;
                string[] currencyNames = new string[_data.Currencies.Count];
                for (int i = 0; i < _data.Currencies.Count; i++)
                    currencyNames[i] = _data.Currencies[i].Name;
                _currentNode.CurrencyIndex = EditorGUILayout.Popup(label, _currentNode.CurrencyIndex, currencyNames);
            }
            else
            {
                EditorGUILayout.LabelField(label, "No currencies available");
            }
        }
    }
}
