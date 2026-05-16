using UnityEditor;
using UnityEngine;

namespace BalancePlugin
{
    [CustomEditor(typeof(TableData))]
    public class TableDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Table Window"))
            {
                TableWindow.ShowWindow();
            }
        }
    }
}
