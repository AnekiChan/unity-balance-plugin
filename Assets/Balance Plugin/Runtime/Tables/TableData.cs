using System.Collections.Generic;
using UnityEngine;

namespace BalancePlugin
{
    [CreateAssetMenu(fileName = "NewTable", menuName = "Balance/Table", order = 1)]
    public class TableData : ScriptableObject
    {
        [SerializeField]
        private List<TableColumn> columns = new List<TableColumn>();

        [HideInInspector]
        [SerializeField]
        private List<TableRow> rows = new List<TableRow>();

        public IReadOnlyList<TableColumn> Columns => columns;
        public IReadOnlyList<TableRow> Rows => rows;
        public int ColumnCount => columns.Count;
        public int RowCount => rows.Count;

        private void OnEnable()
        {
            if (columns == null) columns = new List<TableColumn>();
            if (rows == null) rows = new List<TableRow>();
        }

        public int GetColumnIndex(string name)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Name == name)
                    return i;
            }
            return -1;
        }

        public ColumnType GetColumnType(int index)
        {
            if (index < 0 || index >= columns.Count)
                return ColumnType.Integer;
            return columns[index].Type;
        }

        public TableColumn GetColumn(int index)
        {
            if (index < 0 || index >= columns.Count)
                return null;
            return columns[index];
        }

        public TableRow GetRow(int index)
        {
            if (index < 0 || index >= rows.Count)
                return null;
            return rows[index];
        }

#if UNITY_EDITOR
        public void AddColumn(string name, ColumnType type)
        {
            columns.Add(new TableColumn { Name = name, Type = type });
        }

        public void RemoveColumn(int index)
        {
            if (index < 0 || index >= columns.Count)
                return;
            columns.RemoveAt(index);
        }

        public void SetColumnName(int index, string name)
        {
            if (index < 0 || index >= columns.Count)
                return;
            columns[index].Name = name;
        }

        public void SetColumnType(int index, ColumnType type)
        {
            if (index < 0 || index >= columns.Count)
                return;
            columns[index].Type = type;
        }

        public TableRow AddRow()
        {
            var row = CreateInstance<TableRow>();
            row.name = $"Row_{rows.Count}";
            row.ParentTable = this;
            row.EnsureCellCount(columns.Count);
            rows.Add(row);
            return row;
        }

        public void RemoveRow(int index)
        {
            if (index < 0 || index >= rows.Count)
                return;
            var row = rows[index];
            rows.RemoveAt(index);
            if (row != null)
                DestroyImmediate(row, true);
        }

        public void MoveRow(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= rows.Count || toIndex < 0 || toIndex >= rows.Count)
                return;
            var row = rows[fromIndex];
            rows.RemoveAt(fromIndex);
            rows.Insert(toIndex, row);
        }

        public void SyncAllRowCellCounts()
        {
            foreach (var row in rows)
            {
                if (row != null)
                    row.EnsureCellCount(columns.Count);
            }
        }

        public int PruneNullRows()
        {
            return rows.RemoveAll(r => r == null);
        }
#endif
    }
}
