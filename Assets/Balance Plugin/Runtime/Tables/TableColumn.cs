using System;

namespace BalancePlugin
{
    [Serializable]
    public class TableColumn
    {
        public string Name = "Column";
        public ColumnType Type = ColumnType.Integer;
    }
}
