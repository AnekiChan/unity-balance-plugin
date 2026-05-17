using UnityEngine;
using UnityEditor;
using System;
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
            if (_currentNode is SourceNode sourceNode)
            {
                DrawCurrencySelector("Output Currency");
                sourceNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", sourceNode.SendInterval);
                DrawOutputsList(sourceNode.Outputs, sourceNode.OutputNodeIds, "Output");
            }
            else if (_currentNode is PoolNode poolNode)
            {
                DrawCurrencySelector("Stored Currency");
                poolNode.StartAmount = EditorGUILayout.IntField("Initial Amount", poolNode.StartAmount);
                poolNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", poolNode.SendInterval);
                DrawOutputsList(poolNode.Outputs, poolNode.OutputNodeIds, "Output");
            }
            else if (_currentNode is DrainNode drainNode)
            {
                drainNode.DrainAmount = EditorGUILayout.IntField("Drain Amount", drainNode.DrainAmount);
            }
            else if (_currentNode is ConverterNode converterNode)
            {
                DrawCurrencySelector("Output Currency");
                converterNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", converterNode.SendInterval);
                DrawOutputsList(converterNode.Outputs, converterNode.OutputNodeIds, "Output");
            }
            else if (_currentNode is GateNode gateNode)
            {
                DrawCurrencySelector("Output Currency");
                gateNode.SendInterval = EditorGUILayout.IntField("Send Interval (ticks)", gateNode.SendInterval);
                DrawOutputsList(gateNode.Outputs, gateNode.OutputNodeIds, "Output");
                DrawGateChancesList(gateNode);
            }
        }

        private void DrawOutputsList(List<NodeOutput> outputs, List<string> outputNodeIds, string prefix)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(prefix + "s", EditorStyles.boldLabel);

            if (_data == null)
                return;

            int count = outputNodeIds.Count;

            while (outputs.Count < count)
                outputs.Add(new NodeOutput());
            while (outputs.Count > count)
                outputs.RemoveAt(outputs.Count - 1);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Connect outputs to configure amounts.", MessageType.Info);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                string nodeId = outputNodeIds[i];
                BalancingNode targetNode = _data.GetNode(nodeId);
                string label = targetNode != null
                    ? $"{targetNode.DisplayName} ({targetNode.NodeType})"
                    : nodeId;

                NodeOutput output = outputs[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                output.AmountType = (OutputAmountType)EditorGUILayout.EnumPopup("Type", output.AmountType);

                switch (output.AmountType)
                {
                    case OutputAmountType.Number:
                        output.Amount = EditorGUILayout.IntField("Amount", output.Amount);
                        break;

                    case OutputAmountType.Formula:
                        output.Formula = EditorGUILayout.TextField("Formula", output.Formula);
                        EditorGUI.indentLevel++;
                        var (success, result, preview) = FormulaEvaluator.Evaluate(output.Formula, _currentTick, 0);
                        if (success)
                            EditorGUILayout.LabelField("Preview: " + preview + " ... " + result);
                        else
                        {
                            GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
                            errorStyle.normal.textColor = Color.red;
                            EditorGUILayout.LabelField("Error: " + result, errorStyle);
                        }
                        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
                        hintStyle.normal.textColor = Color.gray;
                        hintStyle.fontSize = 10;
                        EditorGUILayout.LabelField("x = tick, s = input amount", hintStyle);
                        EditorGUI.indentLevel--;
                        break;

                    case OutputAmountType.Random:
                        output.RandomAmount = EditorGUILayout.IntField("Amount", output.RandomAmount);
                        output.RandomChance = EditorGUILayout.Slider("Chance (%)", output.RandomChance, 0f, 100f);
                        break;

                    case OutputAmountType.RandomRange:
                        output.RandomRangeMin = EditorGUILayout.IntField("Min", output.RandomRangeMin);
                        output.RandomRangeMax = EditorGUILayout.IntField("Max", output.RandomRangeMax);
                        break;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
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
