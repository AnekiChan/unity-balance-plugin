using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BalancePlugin
{
    public class TableWindow : EditorWindow
    {
        private TableData _currentTable;
        private Vector2 _scrollPos;
        private float[] _rowHeights;
        private float _totalRowHeight;

        private const float ColumnMinWidth = 150f;
        private const float SpriteColumnWidth = 64f;
        private const float ColumnRowIdWidth = 50f;
        private const float HeaderHeight = 24f;
        private const float MinRowHeight = 22f;

        private float GetColumnWidth(int colIndex)
        {
            return _currentTable.GetColumnType(colIndex) == ColumnType.Sprite ? SpriteColumnWidth : ColumnMinWidth;
        }

        private float GetColumnX(int colIndex)
        {
            float x = 0f;
            for (int c = 0; c < colIndex; c++)
                x += GetColumnWidth(c);
            return x;
        }

        private float GetTotalColumnsWidth(int colCount)
        {
            return GetColumnX(colCount);
        }

        [MenuItem("Tools/Balance/Table Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<TableWindow>("Balance Table");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (_currentTable == null)
            {
                EditorGUILayout.HelpBox("Select a TableData asset or create a new one.", MessageType.Info);
                return;
            }

            DrawTable();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var prevTable = _currentTable;
            _currentTable = (TableData)EditorGUILayout.ObjectField(
                _currentTable, typeof(TableData), false,
                GUILayout.MinWidth(200));

            if (_currentTable != prevTable)
                OnTableChanged();

            if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateNewTable();

            if (_currentTable != null)
            {
                if (GUILayout.Button("+ Col", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    ShowAddColumnMenu();

                if (GUILayout.Button("+ Row", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    AddRow();

                if (GUILayout.Button("Delete Table", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    DeleteTable();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnTableChanged()
        {
            _scrollPos = Vector2.zero;
            _rowHeights = null;
            if (_currentTable != null)
                _currentTable.PruneNullRows();
        }

        private void CreateNewTable()
        {
            string folder = EditorUtility.OpenFolderPanel("Select folder for table", "Assets", "");
            if (string.IsNullOrEmpty(folder))
                return;

            string dataPath = folder.Replace(Application.dataPath, "Assets");

            string tableName = "NewTable";
            string baseDataPath = Path.Combine(dataPath, tableName + ".asset").Replace("\\", "/");
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(baseDataPath);

            TableData data = CreateInstance<TableData>();
            AssetDatabase.CreateAsset(data, uniquePath);
            AssetDatabase.SaveAssets();

            _currentTable = data;
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log($"[BalancePlugin] Table created at: {uniquePath}");
        }

        private void DeleteTable()
        {
            if (!EditorUtility.DisplayDialog("Delete Table",
                $"Delete '{_currentTable.name}'? Row assets will also be removed.",
                "Delete", "Cancel"))
                return;

            var table = _currentTable;
            _currentTable = null;

            string tablePath = AssetDatabase.GetAssetPath(table);

            foreach (var row in table.Rows.ToList())
            {
                if (row != null)
                {
                    string rowPath = AssetDatabase.GetAssetPath(row);
                    if (!string.IsNullOrEmpty(rowPath))
                        AssetDatabase.DeleteAsset(rowPath);
                }
            }

            AssetDatabase.DeleteAsset(tablePath);
            AssetDatabase.SaveAssets();
        }

        private void ShowAddColumnMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Integer"), false, () => AddColumn("NewColumn", ColumnType.Integer));
            menu.AddItem(new GUIContent("Float"), false, () => AddColumn("NewColumn", ColumnType.Float));
            menu.AddItem(new GUIContent("String"), false, () => AddColumn("NewColumn", ColumnType.String));
            menu.AddItem(new GUIContent("Sprite"), false, () => AddColumn("NewColumn", ColumnType.Sprite));
            menu.AddItem(new GUIContent("Formula"), false, () => AddColumn("NewColumn", ColumnType.Formula));
            menu.ShowAsContext();
        }

        private void AddColumn(string name, ColumnType type)
        {
            _currentTable.AddColumn(name, type);
            _currentTable.SyncAllRowCellCounts();
            MarkDirty();
        }

        private void AddRow()
        {
            var row = _currentTable.AddRow();
            SaveRowAsset(row, _currentTable.RowCount - 1);
            MarkDirty();
        }

        private void SaveRowAsset(TableRow row, int index)
        {
            string tablePath = AssetDatabase.GetAssetPath(_currentTable);
            string directory = Path.GetDirectoryName(tablePath);
            string tableName = Path.GetFileNameWithoutExtension(tablePath);
            string rowsFolder = Path.Combine(directory, tableName + "_Rows").Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(rowsFolder))
            {
                string parent = Path.GetDirectoryName(rowsFolder);
                string folderName = Path.GetFileName(rowsFolder);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            string rowPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(rowsFolder, $"Row_{index:000}.asset").Replace("\\", "/"));

            row.name = Path.GetFileNameWithoutExtension(rowPath);
            AssetDatabase.CreateAsset(row, rowPath);
        }

        private void DrawTable()
        {
            if (_currentTable == null) return;

            _currentTable.PruneNullRows();

            int colCount = _currentTable.ColumnCount;
            int rowCount = _currentTable.RowCount;

            if (_currentTable.Columns == null || _currentTable.Rows == null)
                return;

            PrecomputeRowHeights(colCount, rowCount);

            if (_rowHeights == null || _rowHeights.Length < rowCount)
                return;

            float totalWidth = ColumnRowIdWidth + GetTotalColumnsWidth(colCount);
            float totalHeight = HeaderHeight + _totalRowHeight;

            Rect viewRect = GUILayoutUtility.GetRect(position.width, position.height - EditorGUIUtility.singleLineHeight - 6);
            Rect contentRect = new Rect(0, 0, Mathf.Max(totalWidth, viewRect.width - 16), totalHeight);

            _scrollPos = GUI.BeginScrollView(viewRect, _scrollPos, contentRect);

            DrawHeaderRow(colCount);

            float yPos = HeaderHeight;
            for (int r = 0; r < rowCount; r++)
            {
                float rowHeight = _rowHeights[r];
                Rect rowRect = new Rect(0, yPos, contentRect.width, rowHeight);
                bool isOdd = r % 2 == 1;
                EditorGUI.DrawRect(rowRect, isOdd ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.22f, 0.22f, 0.22f, 0.5f));

                DrawDataRow(r, colCount, yPos, rowHeight);
                yPos += rowHeight;
            }

            GUI.EndScrollView();
        }

        private void PrecomputeRowHeights(int colCount, int rowCount)
        {
            if (_rowHeights == null || _rowHeights.Length != rowCount)
                _rowHeights = new float[rowCount];

            _totalRowHeight = 0f;
            for (int r = 0; r < rowCount; r++)
            {
                _rowHeights[r] = GetRowHeight(_currentTable.GetRow(r), colCount);
                _totalRowHeight += _rowHeights[r];
            }
        }

        private float GetRowHeight(TableRow row, int colCount)
        {
            float maxHeight = MinRowHeight;
            if (row == null) return maxHeight;

            for (int c = 0; c < colCount; c++)
            {
                var colType = _currentTable.GetColumnType(c);
                if (colType == ColumnType.Sprite)
                {
                    maxHeight = Mathf.Max(maxHeight, SpriteColumnWidth);
                    continue;
                }
                if (colType == ColumnType.Formula)
                {
                    var formulaCell = row?.GetCell(c);
                    float formulaH = MinRowHeight * 2f + 4;
                    if (formulaCell != null && !string.IsNullOrEmpty(formulaCell.formulaString))
                    {
                        float extraH = EditorStyles.textField.CalcHeight(
                            new GUIContent(formulaCell.formulaString), GetColumnWidth(c) - 4);
                        if (extraH > MinRowHeight)
                            formulaH = extraH + MinRowHeight + 6;
                    }
                    maxHeight = Mathf.Max(maxHeight, formulaH);
                    continue;
                }
                if (colType != ColumnType.String)
                    continue;
                var cell = row.GetCell(c);
                if (cell == null) continue;

                float textHeight = EditorStyles.textArea.CalcHeight(
                    new GUIContent(cell.stringValue), GetColumnWidth(c) - 4) + 4;
                maxHeight = Mathf.Max(maxHeight, textHeight);
            }
            return maxHeight;
        }

        private void DrawHeaderRow(int colCount)
        {
            Rect headerRect = new Rect(0, 0, ColumnRowIdWidth, HeaderHeight);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUI.LabelField(headerRect, "#", EditorStyles.boldLabel);

            for (int c = 0; c < colCount; c++)
            {
                float colW = GetColumnWidth(c);
                float colX = ColumnRowIdWidth + GetColumnX(c);
                Rect colRect = new Rect(colX, 0, colW, HeaderHeight);
                EditorGUI.DrawRect(colRect, new Color(0.15f, 0.15f, 0.15f, 1f));

                var col = _currentTable.GetColumn(c);
                Rect nameRect = new Rect(colRect.x + 2, colRect.y + 2, colRect.width - 22, colRect.height - 4);
                Rect typeRect = new Rect(colRect.x + colRect.width - 20, colRect.y + 4, 18, 16);

                string newName = EditorGUI.TextField(nameRect, col.Name);
                if (newName != col.Name)
                {
                    _currentTable.SetColumnName(c, newName);
                    MarkDirty();
                }

                ColumnType newType = (ColumnType)EditorGUI.EnumPopup(typeRect, col.Type, EditorStyles.miniLabel);
                if (newType != col.Type)
                {
                    _currentTable.SetColumnType(c, newType);
                    MarkDirty();
                }

                if (Event.current.type == EventType.ContextClick && colRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    ShowColumnContextMenu(c);
                }
            }
        }

        private void DrawDataRow(int rowIndex, int colCount, float yPos, float rowHeight)
        {
            var row = _currentTable.GetRow(rowIndex);
            if (row == null) return;

            Rect idRect = new Rect(0, yPos, ColumnRowIdWidth, rowHeight);
            EditorGUI.LabelField(idRect, rowIndex.ToString());

            if (Event.current.type == EventType.ContextClick && idRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                ShowRowContextMenu(rowIndex);
            }

            EditorGUI.BeginChangeCheck();

            for (int c = 0; c < colCount; c++)
            {
                float colW = GetColumnWidth(c);
                float colX = ColumnRowIdWidth + GetColumnX(c);
                Rect cellRect = new Rect(colX + 1, yPos + 1, colW - 2, rowHeight - 2);
                DrawCell(cellRect, row, c);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(row);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawCell(Rect rect, TableRow row, int columnIndex)
        {
            var colType = _currentTable.GetColumnType(columnIndex);
            var cell = row.GetCell(columnIndex);
            if (cell == null) return;

            switch (colType)
            {
                case ColumnType.Integer:
                    int intVal = EditorGUI.IntField(rect, cell.intValue);
                    if (intVal != cell.intValue)
                        cell.SetValue(ColumnType.Integer, intVal);
                    break;

                case ColumnType.Float:
                    float floatVal = EditorGUI.FloatField(rect, cell.floatValue);
                    if (!Mathf.Approximately(floatVal, cell.floatValue))
                        cell.SetValue(ColumnType.Float, floatVal);
                    break;

                case ColumnType.String:
                    string strVal = EditorGUI.TextArea(rect, cell.stringValue);
                    if (strVal != cell.stringValue)
                        cell.SetValue(ColumnType.String, strVal);
                    break;

                case ColumnType.Sprite:
                    {
                        float size = Mathf.Min(rect.width, rect.height);
                        Rect spriteRect = new Rect(
                            rect.x + (rect.width - size) * 0.5f,
                            rect.y + (rect.height - size) * 0.5f,
                            size, size);
                        Sprite sprite = (Sprite)EditorGUI.ObjectField(spriteRect, cell.spriteValue, typeof(Sprite), false);
                        if (sprite != cell.spriteValue)
                            cell.SetValue(ColumnType.Sprite, sprite);
                    }
                    break;

                case ColumnType.Formula:
                    {
                        float halfH = (rect.height - 2) * 0.5f;
                        Rect formulaRect = new Rect(rect.x, rect.y, rect.width, halfH);
                        Rect resultRect = new Rect(rect.x, rect.y + halfH + 2, rect.width, halfH);

                        string newFormula = EditorGUI.TextField(formulaRect, cell.formulaString);
                        if (newFormula != cell.formulaString)
                            cell.formulaString = newFormula;

                        var (success, res) = TableFormulaEvaluator.Evaluate(cell.formulaString, row);
                        cell.formulaResult = success ? res : "ERR: " + res;

                        Color prevColor = GUI.color;
                        GUI.color = success ? Color.white : Color.red;
                        EditorGUI.LabelField(resultRect, cell.formulaResult, EditorStyles.miniLabel);
                        GUI.color = prevColor;
                    }
                    break;
            }
        }

        private void ShowColumnContextMenu(int columnIndex)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Column"), false, () =>
            {
                _currentTable.RemoveColumn(columnIndex);
                _currentTable.SyncAllRowCellCounts();
                MarkDirty();
            });
            menu.ShowAsContext();
        }

        private void ShowRowContextMenu(int rowIndex)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select Row Asset"), false, () =>
            {
                var row = _currentTable.GetRow(rowIndex);
                if (row != null)
                {
                    Selection.activeObject = row;
                    EditorGUIUtility.PingObject(row);
                }
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Row"), false, () =>
            {
                var row = _currentTable.GetRow(rowIndex);
                string rowPath = AssetDatabase.GetAssetPath(row);
                _currentTable.RemoveRow(rowIndex);
                if (!string.IsNullOrEmpty(rowPath))
                    AssetDatabase.DeleteAsset(rowPath);
                MarkDirty();
            });
            menu.AddItem(new GUIContent("Duplicate Row"), false, () =>
            {
                var source = _currentTable.GetRow(rowIndex);
                if (source == null) return;
                var newRow = _currentTable.AddRow();
                for (int c = 0; c < _currentTable.ColumnCount; c++)
                {
                    var srcCell = source.GetCell(c);
                    var dstCell = newRow.GetCell(c);
                    if (srcCell != null && dstCell != null)
                    {
                        dstCell.SetValue(_currentTable.GetColumnType(c), srcCell.GetValue(_currentTable.GetColumnType(c)));
                    }
                }
                newRow.RecalculateFormulas();
                SaveRowAsset(newRow, _currentTable.RowCount - 1);
                EditorUtility.SetDirty(newRow);
                MarkDirty();
            });
            menu.ShowAsContext();
        }

        private void MarkDirty()
        {
            if (_currentTable == null) return;
            EditorUtility.SetDirty(_currentTable);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void OnSelectionChange()
        {
            var selected = Selection.activeObject as TableData;
            if (selected != null)
                _currentTable = selected;
        }
    }
}
