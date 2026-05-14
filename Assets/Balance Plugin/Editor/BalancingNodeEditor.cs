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

            if (node is SourceNode)
            {
                DrawProperty("SendInterval");
                DrawOutputFields("Output");
            }
            else if (node is PoolNode)
            {
                DrawProperty("StartAmount");
                DrawProperty("StoredAmount");
                DrawProperty("SendInterval");
                DrawOutputFields("Output");
            }
            else if (node is DrainNode)
            {
                DrawProperty("DrainAmount");
            }
            else if (node is ConverterNode)
            {
                DrawOutputFields("Output");
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("InputNodeIds");
                DrawProperty("OutputNodeIds");
            }

            serializedObject.ApplyModifiedProperties();
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

        private void DrawOutputFields(string prefix)
        {
            SerializedProperty amountType = serializedObject.FindProperty(prefix + "AmountType");
            if (amountType == null)
                return;

            EditorGUILayout.PropertyField(amountType);
            OutputAmountType type = (OutputAmountType)amountType.enumValueIndex;

            switch (type)
            {
                case OutputAmountType.Number:
                    DrawProperty(prefix + "Amount");
                    break;
                case OutputAmountType.Formula:
                    DrawProperty(prefix + "Formula");
                    break;
                case OutputAmountType.Random:
                    DrawProperty(prefix + "RandomAmount");
                    DrawProperty(prefix + "RandomChance");
                    break;
            }
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
