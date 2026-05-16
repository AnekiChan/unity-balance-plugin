using System;
using UnityEngine;

namespace BalancePlugin
{
    [Serializable]
    public class CellValue
    {
        public int intValue;
        public float floatValue;
        public string stringValue = "";
        public Sprite spriteValue;

        public object GetValue(ColumnType type)
        {
            return type switch
            {
                ColumnType.Integer => intValue,
                ColumnType.Float => floatValue,
                ColumnType.String => stringValue,
                ColumnType.Sprite => spriteValue,
                _ => null
            };
        }

        public void SetValue(ColumnType type, object value)
        {
            switch (type)
            {
                case ColumnType.Integer:
                    intValue = value is int i ? i : 0;
                    break;
                case ColumnType.Float:
                    floatValue = value is float f ? f : 0f;
                    break;
                case ColumnType.String:
                    stringValue = value as string ?? "";
                    break;
                case ColumnType.Sprite:
                    spriteValue = value as Sprite;
                    break;
            }
        }
    }
}
