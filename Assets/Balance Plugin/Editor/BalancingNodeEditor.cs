using UnityEditor;
using UnityEngine;

namespace BalancePlugin
{
    [CustomEditor(typeof(BalancingNode), true)]
    public class BalancingNodeEditor : Editor
    {
        private BalancingData _data;

        private void OnEnable()
        {
            _data = FindOwnerData(target as BalancingNode);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            BalancingNode node = (BalancingNode)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Type", node.NodeType);
                EditorGUILayout.TextField("Node Id", node.NodeId);
            }

            DrawProperty("DisplayName", "Name");
            DrawCurrency(node);
            EditorGUILayout.Space();

            if (node is PoolNode)
            {
                DrawProperty("StartAmount");
                DrawMultiCurrencyInfo(node as PoolNode);
            }
            else if (node is DrainNode)
            {
                DrawProperty("DrainAmount");
            }
            else if (node is GateNode)
            {
                DrawProperty("Mode");
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("InputNodeIds");
                DrawProperty("OutputNodeIds");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMultiCurrencyInfo(PoolNode pool)
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

        private void DrawCurrency(BalancingNode node)
        {
            SerializedProperty currencyIndex = serializedObject.FindProperty("CurrencyIndex");
            if (currencyIndex == null)
                return;

            if (_data == null || _data.Currencies == null || _data.Currencies.Count == 0)
            {
                EditorGUILayout.PropertyField(currencyIndex);
                return;
            }

            string[] currencyNames = new string[_data.Currencies.Count];
            for (int i = 0; i < _data.Currencies.Count; i++)
                currencyNames[i] = _data.Currencies[i].Name;

            currencyIndex.intValue = EditorGUILayout.Popup("Currency", Mathf.Clamp(currencyIndex.intValue, 0, currencyNames.Length - 1), currencyNames);
        }

        private void DrawProperty(string propertyName, string label = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            if (string.IsNullOrEmpty(label))
                EditorGUILayout.PropertyField(property, true);
            else
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private BalancingData FindOwnerData(BalancingNode node)
        {
            if (node == null)
                return null;

            string path = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<BalancingData>(path);
        }
    }
}
