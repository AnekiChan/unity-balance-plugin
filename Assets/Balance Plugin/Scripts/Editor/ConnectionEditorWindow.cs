using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class ConnectionEditorWindow : EditorWindow
    {
        private NodeConnection _connection;
        private BalanceGraph _graph;

        public static void ShowWindow(NodeConnection connection, BalanceGraph graph)
        {
            var window = GetWindow<ConnectionEditorWindow>("Connection Editor");
            window._connection = connection;
            window._graph = graph;
            window.minSize = new Vector2(250, 100);
        }

        private void OnGUI()
        {
            if (_connection == null)
            {
                EditorGUILayout.HelpBox("No connection selected", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Connection Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var fromNode = _graph != null ? _graph.GetNode(_connection.FromNodeId) : null;
            var toNode = _graph != null ? _graph.GetNode(_connection.ToNodeId) : null;

            EditorGUILayout.LabelField($"From: {fromNode?.Label ?? "Unknown"}");
            EditorGUILayout.LabelField($"To: {toNode?.Label ?? "Unknown"}");

            EditorGUI.BeginChangeCheck();

            _connection.FlowRate = EditorGUILayout.FloatField("Flow Rate", _connection.FlowRate);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graph);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Delete Connection", GUILayout.Height(30)))
            {
                if (_graph != null)
                {
                    _graph.RemoveConnection(_connection.FromNodeId, _connection.ToNodeId);
                    Close();
                }
            }
        }
    }
}
