using UnityEngine;
using UnityEditor;
using System;

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
            if (_currentNode is SourceNode sourceNode)
            {
                DrawCurrencySelector("Output Currency");
                sourceNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", sourceNode.SendInterval);
                DrawOutputAmountFields(sourceNode.OutputAmountType, sourceNode, "Output");
            }
            else if (_currentNode is PoolNode poolNode)
            {
                DrawCurrencySelector("Stored Currency");
                poolNode.StartAmount = EditorGUILayout.IntField("Initial Amount", poolNode.StartAmount);
                poolNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", poolNode.SendInterval);
                DrawOutputAmountFields(poolNode.OutputAmountType, poolNode, "Output");
            }
            else if (_currentNode is DrainNode drainNode)
            {
                drainNode.DrainAmount = EditorGUILayout.IntField("Drain Amount", drainNode.DrainAmount);
            }
            else if (_currentNode is ConverterNode converterNode)
            {
                DrawCurrencySelector("Output Currency");
                converterNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", converterNode.SendInterval);
                DrawOutputAmountFields(converterNode.OutputAmountType, converterNode, "Output");
            }
            else if (_currentNode is GateNode gateNode)
            {
                DrawCurrencySelector("Output Currency");
                gateNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", gateNode.SendInterval);
                DrawOutputAmountFields(gateNode.OutputAmountType, gateNode, "Output");
                DrawGateChancesList(gateNode);
            }
        }

        private void DrawOutputAmountFields(OutputAmountType amountType, BalancingNode node, string prefix)
        {
            Type nodeType = node.GetType();

            System.Reflection.FieldInfo typeField = nodeType.GetField(prefix + "AmountType");
            if (typeField != null)
            {
                OutputAmountType currentType = (OutputAmountType)typeField.GetValue(node);
                OutputAmountType newType = (OutputAmountType)EditorGUILayout.EnumPopup(prefix + " Type", currentType);
                if (newType != currentType)
                {
                    typeField.SetValue(node, newType);
                    EditorUtility.SetDirty(node);
                }

                switch (newType)
                {
                    case OutputAmountType.Number:
                        DrawNumberField(nodeType, node, prefix);
                        break;

                    case OutputAmountType.Formula:
                        DrawFormulaField(nodeType, node, prefix);
                        break;

                    case OutputAmountType.Random:
                        DrawRandomFields(nodeType, node, prefix);
                        break;

                    case OutputAmountType.RandomRange:
                        DrawRandomRangeFields(nodeType, node, prefix);
                        break;
                }
            }
        }

        private void DrawNumberField(Type nodeType, BalancingNode node, string prefix)
        {
            System.Reflection.FieldInfo amountField = nodeType.GetField(prefix + "Amount");
            if (amountField != null)
            {
                int currentValue = (int)amountField.GetValue(node);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(prefix + " Amount");
                int newValue = EditorGUILayout.IntField(currentValue);
                if (newValue != currentValue)
                {
                    amountField.SetValue(node, newValue);
                    EditorUtility.SetDirty(node);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFormulaField(Type nodeType, BalancingNode node, string prefix)
        {
            System.Reflection.FieldInfo formulaField = nodeType.GetField(prefix + "Formula");
            if (formulaField != null)
            {
                string currentFormula = (string)formulaField.GetValue(node);
                string newFormula = EditorGUILayout.TextField(prefix + " Formula", currentFormula);
                if (newFormula != currentFormula)
                {
                    formulaField.SetValue(node, newFormula);
                    EditorUtility.SetDirty(node);
                }

                EditorGUI.indentLevel++;
                var (success, result, preview) = FormulaEvaluator.Evaluate(newFormula, _currentTick, 0);
                if (success)
                {
                    GUIStyle normalStyle = new GUIStyle(GUI.skin.label);
                    normalStyle.normal.textColor = Color.black;
                    EditorGUILayout.LabelField("Preview:", "[" + preview + ", ..., " + result + "]");
                }
                else
                {
                    GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
                    errorStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField("Error:", result, errorStyle);
                }
                GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
                hintStyle.normal.textColor = Color.gray;
                hintStyle.fontSize = 10;
                EditorGUILayout.LabelField("x = tick number, s = input amount", hintStyle);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRandomFields(Type nodeType, BalancingNode node, string prefix)
        {
            System.Reflection.FieldInfo amountField = nodeType.GetField(prefix + "RandomAmount");
            System.Reflection.FieldInfo chanceField = nodeType.GetField(prefix + "RandomChance");

            if (amountField != null)
            {
                int currentAmount = (int)amountField.GetValue(node);
                int newAmount = EditorGUILayout.IntField(prefix + " Amount", currentAmount);
                if (newAmount != currentAmount)
                {
                    amountField.SetValue(node, newAmount);
                    EditorUtility.SetDirty(node);
                }
            }

            if (chanceField != null)
            {
                float currentChance = (float)chanceField.GetValue(node);
                float newChance = EditorGUILayout.Slider(prefix + " Chance (%)", currentChance, 0f, 100f);
                if (Math.Abs(newChance - currentChance) > 0.01f)
                {
                    chanceField.SetValue(node, newChance);
                    EditorUtility.SetDirty(node);
                }
            }
        }

        private void DrawRandomRangeFields(Type nodeType, BalancingNode node, string prefix)
        {
            System.Reflection.FieldInfo minField = nodeType.GetField(prefix + "RandomRangeMin");
            System.Reflection.FieldInfo maxField = nodeType.GetField(prefix + "RandomRangeMax");

            if (minField == null || maxField == null)
                return;

            int currentMin = (int)minField.GetValue(node);
            int currentMax = (int)maxField.GetValue(node);

            int newMin = EditorGUILayout.IntField(prefix + " Min", currentMin);
            int newMax = EditorGUILayout.IntField(prefix + " Max", currentMax);

            if (newMin != currentMin || newMax != currentMax)
            {
                minField.SetValue(node, newMin);
                maxField.SetValue(node, newMax);
                EditorUtility.SetDirty(node);
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

        private void DrawGateChancesList(GateNode gate)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output Chances", EditorStyles.boldLabel);

            if (_data == null)
                return;

            int count = gate.OutputNodeIds.Count;

            while (gate.OutputChances.Count < count)
                gate.OutputChances.Add(0f);
            while (gate.OutputChances.Count > count)
                gate.OutputChances.RemoveAt(gate.OutputChances.Count - 1);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Connect outputs to configure chances.", MessageType.Info);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                string nodeId = gate.OutputNodeIds[i];
                BalancingNode targetNode = _data.GetNode(nodeId);
                string label = targetNode != null
                    ? $"{targetNode.DisplayName} ({targetNode.NodeType})"
                    : nodeId;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                gate.OutputChances[i] = EditorGUILayout.Slider(gate.OutputChances[i], 0f, 100f);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
