using UnityEditor;
using UnityEngine;

namespace BalancePlugin
{
    [CustomEditor(typeof(Arrow))]
    public class ArrowEditor : Editor
    {
        private BalancingData _data;
        private SerializedProperty _amountType;
        private SerializedProperty _amount;
        private SerializedProperty _formula;
        private SerializedProperty _randomAmount;
        private SerializedProperty _randomChance;
        private SerializedProperty _randomRangeMin;
        private SerializedProperty _randomRangeMax;
        private SerializedProperty _gateRatio;
        private SerializedProperty _gateChance;
        private SerializedProperty _unlimitedRepeats;

        private void OnEnable()
        {
            _data = FindOwnerData(target as Arrow);
            _amountType = serializedObject.FindProperty("Output.AmountType");
            _amount = serializedObject.FindProperty("Output.Amount");
            _formula = serializedObject.FindProperty("Output.Formula");
            _randomAmount = serializedObject.FindProperty("Output.RandomAmount");
            _randomChance = serializedObject.FindProperty("Output.RandomChance");
            _randomRangeMin = serializedObject.FindProperty("Output.RandomRangeMin");
            _randomRangeMax = serializedObject.FindProperty("Output.RandomRangeMax");
            _gateRatio = serializedObject.FindProperty("GateRatio");
            _gateChance = serializedObject.FindProperty("GateChance");
            _unlimitedRepeats = serializedObject.FindProperty("UnlimitedRepeats");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Arrow arrow = (Arrow)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Arrow Id", arrow.ArrowId);
                EditorGUILayout.TextField("From", arrow.FromNodeId);
                EditorGUILayout.TextField("To", arrow.ToNodeId);
            }

            EditorGUILayout.Space();
            DrawCurrency(arrow);
            DrawProperty(serializedObject.FindProperty("SendInterval"), "Send Interval");
            EditorGUILayout.PropertyField(_unlimitedRepeats, new GUIContent("Unlimited Repeats"));
            DrawProperty(serializedObject.FindProperty("SubtractResource"), "Subtract Resource");

            if (IsFromGate(arrow))
            {
                DrawGateSection(arrow);
            }
            else
            {
                DrawOutputSection(arrow);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsFromGate(Arrow arrow)
        {
            if (_data == null || string.IsNullOrEmpty(arrow.FromNodeId))
                return false;
            return _data.GetNode(arrow.FromNodeId) is GateNode;
        }

        private void DrawGateSection(Arrow arrow)
        {
            GateNode gate = _data?.GetNode(arrow.FromNodeId) as GateNode;
            if (gate == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gate Output", EditorStyles.boldLabel);

            if (gate.Mode == GateMode.Distribution)
            {
                EditorGUILayout.PropertyField(_gateRatio, new GUIContent("Ratio (%)"));
                EditorGUILayout.LabelField(
                    $"Share of input: {_gateRatio.intValue}%",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(_gateChance, new GUIContent("Chance (%)"));
                EditorGUILayout.LabelField(
                    "Weight for random path selection",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawOutputSection(Arrow arrow)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_amountType, new GUIContent("Type"));

            OutputAmountType type = (OutputAmountType)_amountType.enumValueIndex;

            switch (type)
            {
                case OutputAmountType.Number:
                    EditorGUILayout.PropertyField(_amount, new GUIContent("Amount"));
                    break;

                case OutputAmountType.Formula:
                    EditorGUILayout.PropertyField(_formula, new GUIContent("Formula"));
                    DrawFormulaPreview(arrow);
                    break;

                case OutputAmountType.Random:
                    EditorGUILayout.PropertyField(_randomAmount, new GUIContent("Amount"));
                    EditorGUILayout.PropertyField(_randomChance, new GUIContent("Chance (%)"));
                    break;

                case OutputAmountType.RandomRange:
                    EditorGUILayout.PropertyField(_randomRangeMin, new GUIContent("Min"));
                    EditorGUILayout.PropertyField(_randomRangeMax, new GUIContent("Max"));
                    break;

                case OutputAmountType.All:
                    EditorGUILayout.HelpBox("Takes all resources from the source pool.", MessageType.Info);
                    break;
            }
        }

        private void DrawFormulaPreview(Arrow arrow)
        {
            string formula = _formula.stringValue;
            if (string.IsNullOrWhiteSpace(formula))
                return;

            var (success, result, preview) = FormulaEvaluator.Evaluate(formula, 1, 0);
            if (success)
            {
                EditorGUILayout.LabelField("Preview", $"tick 1,2: {preview} ... result: {result}", EditorStyles.miniLabel);
            }
            else
            {
                GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Error: " + result, errorStyle);
            }

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.normal.textColor = Color.gray;
            hintStyle.fontSize = 10;
            EditorGUILayout.LabelField("x = tick, s = input amount, n = repeat index (0-based)", hintStyle);
        }

        private void DrawCurrency(Arrow arrow)
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

            int index = Mathf.Clamp(currencyIndex.intValue, 0, currencyNames.Length - 1);
            index = EditorGUILayout.Popup("Currency", index, currencyNames);
            currencyIndex.intValue = index;
        }

        private void DrawProperty(SerializedProperty property, string label)
        {
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        private BalancingData FindOwnerData(Arrow arrow)
        {
            if (arrow == null)
                return null;

            string path = AssetDatabase.GetAssetPath(arrow);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<BalancingData>(path);
        }
    }
}
