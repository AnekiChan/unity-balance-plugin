using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    [CustomEditor(typeof(TableRow))]
    public class TableRowEditor : Editor
    {
        private TableRow _row;
        private TableData _parentTable;
        private bool _showRadarChart = true;

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

            EditorGUILayout.Space();
            _showRadarChart = EditorGUILayout.Foldout(_showRadarChart, "Spider Chart", true);
            if (_showRadarChart)
            {
                Rect chartRect = EditorGUILayout.GetControlRect(false, 260f);
                DrawRadarChart(chartRect);
            }
        }

        private void DrawRadarChart(Rect chartRect)
        {
            var numericCols = new List<(string name, double value)>();
            for (int i = 0; i < _parentTable.ColumnCount; i++)
            {
                var col = _parentTable.GetColumn(i);
                if (col.Type == ColumnType.Integer || col.Type == ColumnType.Float || col.Type == ColumnType.Formula)
                {
                    numericCols.Add((col.Name, _row.GetDouble(i)));
                }
            }

            if (numericCols.Count < 3)
            {
                EditorGUI.HelpBox(chartRect, "Need at least 3 numeric columns (Integer, Float, or Formula) to display a spider chart.", MessageType.Info);
                return;
            }

            int n = numericCols.Count;
            float padding = 50f;
            float radius = Mathf.Min(chartRect.width, chartRect.height) / 2f - padding;
            Vector2 center = chartRect.center;

            float maxVal = 1f;
            foreach (var col in numericCols)
                if (col.value > maxVal) maxVal = (float)col.value;

            // Background
            EditorGUI.DrawRect(chartRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));

            Handles.BeginGUI();

            // Grid polygons
            float[] levelScales = { 0.25f, 0.5f, 0.75f, 1f };
            for (int li = 0; li < levelScales.Length; li++)
            {
                Vector3[] gridPoints = new Vector3[n + 1];
                for (int i = 0; i < n; i++)
                {
                    float angle = i * 2f * Mathf.PI / n - Mathf.PI / 2f;
                    float r = radius * levelScales[li];
                    gridPoints[i] = new Vector3(center.x + Mathf.Cos(angle) * r, center.y + Mathf.Sin(angle) * r, 0);
                }
                gridPoints[n] = gridPoints[0];
                Handles.color = new Color(0.4f, 0.4f, 0.4f, 0.3f);
                Handles.DrawAAPolyLine(1f, gridPoints);
            }

            // Axes
            Handles.color = new Color(0.35f, 0.35f, 0.35f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float angle = i * 2f * Mathf.PI / n - Mathf.PI / 2f;
                Vector3 endPt = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0);
                Handles.DrawAAPolyLine(new Vector3(center.x, center.y, 0), endPt);
            }

            // Data polygon (filled)
            Vector3[] dataPoints = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float angle = i * 2f * Mathf.PI / n - Mathf.PI / 2f;
                float r = radius * ((float)numericCols[i].value / maxVal);
                dataPoints[i] = new Vector3(center.x + Mathf.Cos(angle) * r, center.y + Mathf.Sin(angle) * r, 0);
            }
            Handles.color = new Color(0f, 0.7f, 1f, 0.2f);
            Handles.DrawAAConvexPolygon(dataPoints);

            // Data outline
            Handles.color = new Color(0f, 0.75f, 1f, 0.85f);
            Vector3[] outline = new Vector3[n + 1];
            System.Array.Copy(dataPoints, outline, n);
            outline[n] = dataPoints[0];
            Handles.DrawAAPolyLine(2f, outline);

            // Data point dots
            for (int i = 0; i < n; i++)
            {
                Handles.color = Color.white;
                Handles.DrawSolidDisc(dataPoints[i], Vector3.forward, 3.5f);
                Handles.color = new Color(0f, 0.7f, 1f);
                Handles.DrawSolidDisc(dataPoints[i], Vector3.forward, 2f);
            }

            Handles.EndGUI();

            // Axis labels
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            for (int i = 0; i < n; i++)
            {
                float angle = i * 2f * Mathf.PI / n - Mathf.PI / 2f;
                float ax = Mathf.Cos(angle);
                float ay = Mathf.Sin(angle);

                float labelDist = radius + 24f;
                float lx = center.x + ax * labelDist;
                float ly = center.y + ay * labelDist;

                string labelText = numericCols[i].name;
                GUIContent labelContent = new GUIContent(labelText);
                Vector2 size = labelStyle.CalcSize(labelContent);

                if (ax < -0.1f) labelStyle.alignment = TextAnchor.MiddleRight;
                else if (ax > 0.1f) labelStyle.alignment = TextAnchor.MiddleLeft;
                else if (ay < 0) labelStyle.alignment = TextAnchor.UpperCenter;
                else labelStyle.alignment = TextAnchor.LowerCenter;

                Rect labelRect = new Rect(lx - size.x / 2f, ly - size.y / 2f, size.x, size.y);
                // Clamp label within chartRect
                labelRect.x = Mathf.Clamp(labelRect.x, chartRect.x, chartRect.xMax - size.x);
                labelRect.y = Mathf.Clamp(labelRect.y, chartRect.y, chartRect.yMax - size.y);
                GUI.Label(labelRect, labelContent, labelStyle);
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
