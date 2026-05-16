using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    public class TableRow : ScriptableObject
    {
        [HideInInspector]
        public TableData ParentTable;

        [HideInInspector]
        [SerializeField]
        private List<CellValue> cells = new List<CellValue>();

        public int CellCount => cells.Count;

        private void OnEnable()
        {
            if (cells == null) cells = new List<CellValue>();
        }

        public CellValue GetCell(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= cells.Count)
                return null;
            return cells[columnIndex];
        }

        public CellValue GetCell(string columnName)
        {
            if (ParentTable == null)
                return null;
            int index = ParentTable.GetColumnIndex(columnName);
            return index >= 0 ? GetCell(index) : null;
        }

        public object GetValue(int columnIndex)
        {
            if (ParentTable == null || columnIndex < 0 || columnIndex >= ParentTable.ColumnCount)
                return null;
            return GetCell(columnIndex)?.GetValue(ParentTable.GetColumnType(columnIndex));
        }

        public object GetValue(string columnName)
        {
            if (ParentTable == null)
                return null;
            int index = ParentTable.GetColumnIndex(columnName);
            return index >= 0 ? GetValue(index) : null;
        }

        public int GetInt(int columnIndex)
        {
            return GetCell(columnIndex)?.intValue ?? 0;
        }

        public int GetInt(string columnName)
        {
            return GetCell(columnName)?.intValue ?? 0;
        }

        public float GetFloat(int columnIndex)
        {
            return GetCell(columnIndex)?.floatValue ?? 0f;
        }

        public float GetFloat(string columnName)
        {
            return GetCell(columnName)?.floatValue ?? 0f;
        }

        public string GetString(int columnIndex)
        {
            return GetCell(columnIndex)?.stringValue ?? "";
        }

        public string GetString(string columnName)
        {
            return GetCell(columnName)?.stringValue ?? "";
        }

        public Sprite GetSprite(int columnIndex)
        {
            return GetCell(columnIndex)?.spriteValue;
        }

        public Sprite GetSprite(string columnName)
        {
            return GetCell(columnName)?.spriteValue;
        }

        public double GetDouble(int columnIndex)
        {
            if (ParentTable == null || columnIndex < 0 || columnIndex >= ParentTable.ColumnCount)
                return 0;
            var cell = GetCell(columnIndex);
            if (cell == null) return 0;
            var type = ParentTable.GetColumnType(columnIndex);

            return type switch
            {
                ColumnType.Integer => cell.intValue,
                ColumnType.Float => cell.floatValue,
                ColumnType.String => double.TryParse(cell.stringValue,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double dv) ? dv : 0,
                ColumnType.Formula => EvaluateFormula(columnIndex),
                _ => 0
            };
        }

        public double GetDouble(string columnName)
        {
            if (ParentTable == null) return 0;
            int index = ParentTable.GetColumnIndex(columnName);
            return index >= 0 ? GetDouble(index) : 0;
        }

        public double EvaluateFormula(int columnIndex)
        {
            var cell = GetCell(columnIndex);
            if (cell == null || string.IsNullOrEmpty(cell.formulaString))
                return 0;
            var (success, resultStr) = TableFormulaEvaluator.Evaluate(cell.formulaString, this);
            if (success && double.TryParse(resultStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
                return val;
            return 0;
        }

        public void RecalculateFormulas()
        {
            if (ParentTable == null) return;
            for (int i = 0; i < ParentTable.ColumnCount; i++)
            {
                if (ParentTable.GetColumnType(i) != ColumnType.Formula) continue;
                var cell = GetCell(i);
                if (cell == null) continue;
                var (success, resultStr) = TableFormulaEvaluator.Evaluate(cell.formulaString, this);
                cell.formulaResult = success ? resultStr : "ERR: " + resultStr;
            }
        }

        public void SetCellValue(int columnIndex, object value)
        {
            if (ParentTable == null || columnIndex < 0 || columnIndex >= ParentTable.ColumnCount)
                return;
            EnsureCellCount(ParentTable.ColumnCount);
            cells[columnIndex].SetValue(ParentTable.GetColumnType(columnIndex), value);
        }

        public List<CellValue> CellList => cells;

        public void EnsureCellCount(int count)
        {
            while (cells.Count < count)
                cells.Add(new CellValue());
        }
    }
}
