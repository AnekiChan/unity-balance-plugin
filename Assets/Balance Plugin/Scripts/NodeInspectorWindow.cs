using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class NodeInspectorWindow : EditorWindow
    {
        private BalancingNode _currentNode;
        private BalancingWindow _parentWindow;
        private BalancingData _data;

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

        [MenuItem("Tools/Balancing Inspector")]
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
        }

        private void DrawNodeSpecificFields()
        {
            if (_currentNode is SourceNode sourceNode)
            {
                DrawCurrencySelector("Output Currency");
                sourceNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", sourceNode.SendInterval);
                sourceNode.OutputAmount = EditorGUILayout.IntField("Output Amount", sourceNode.OutputAmount);
            }
            else if (_currentNode is PoolNode poolNode)
            {
                DrawCurrencySelector("Stored Currency");
                poolNode.StoredAmount = EditorGUILayout.IntField("Stored Amount", poolNode.StoredAmount);
                poolNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", poolNode.SendInterval);
                poolNode.OutputAmount = EditorGUILayout.IntField("Output Amount", poolNode.OutputAmount);
            }
            else if (_currentNode is DrainNode drainNode)
            {
                drainNode.DrainAmount = EditorGUILayout.IntField("Drain Amount", drainNode.DrainAmount);
            }
            else if (_currentNode is ConverterNode converterNode)
            {
                DrawCurrencySelector("Output Currency");
                // converterNode.InputAmount = EditorGUILayout.IntField("Input Amount Required", converterNode.InputAmount);
                converterNode.OutputAmount = EditorGUILayout.IntField("Output Amount", converterNode.OutputAmount);
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