using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    [CustomEditor(typeof(TableRow))]
    public class TableRowEditor : Editor
    {
        private TableRow _row;
        private TableData _parentTable;

        private void OnEnable()
        {
            _row = (TableRow)target;
            _parentTable = _row.ParentTable;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_parentTable == null)
            {
                EditorGUILayout.HelpBox("Row is not linked to a table. ParentTable is null.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.LabelField($"Row: {_row.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Table: {_parentTable.name}");
            EditorGUILayout.Space();

            if (_parentTable.ColumnCount == 0)
            {
                EditorGUILayout.HelpBox("Table has no columns.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < _parentTable.ColumnCount; i++)
            {
                var column = _parentTable.GetColumn(i);
                var cell = _row.GetCell(i);
                if (cell == null) continue;

                DrawCellField(column.Name, column.Type, cell, i);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_row);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawCellField(string columnName, ColumnType type, CellValue cell, int index)
        {
            switch (type)
            {
                case ColumnType.Integer:
                    cell.intValue = EditorGUILayout.IntField(columnName, cell.intValue);
                    break;
                case ColumnType.Float:
                    cell.floatValue = EditorGUILayout.FloatField(columnName, cell.floatValue);
                    break;
                case ColumnType.String:
                    EditorGUILayout.LabelField(columnName);
                    cell.stringValue = EditorGUILayout.TextArea(cell.stringValue, GUILayout.MinHeight(40));
                    break;
                case ColumnType.Sprite:
                    cell.spriteValue = (Sprite)EditorGUILayout.ObjectField(columnName, cell.spriteValue, typeof(Sprite), false);
                    break;
                case ColumnType.Formula:
                    EditorGUILayout.LabelField(columnName);
                    string newFormula = EditorGUILayout.TextArea(cell.formulaString, GUILayout.MinHeight(40));
                    if (newFormula != cell.formulaString)
                    {
                        cell.formulaString = newFormula;
                        var (success, res) = TableFormulaEvaluator.Evaluate(newFormula, _row);
                        cell.formulaResult = success ? res : "ERR: " + res;
                    }
                    {
                        Color prev = GUI.color;
                        bool isError = cell.formulaResult.StartsWith("ERR:");
                        GUI.color = isError ? Color.red : Color.green;
                        EditorGUILayout.LabelField("  Result: " + cell.formulaResult, EditorStyles.miniLabel);
                        GUI.color = prev;
                    }
                    break;
            }
        }
    }
}
