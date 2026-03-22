using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class NodeEditorWindow : EditorWindow
    {
        private BalanceNode _node;
        private BalanceGraph _graph;

        public static void ShowWindow(BalanceNode node, BalanceGraph graph)
        {
            var window = GetWindow<NodeEditorWindow>("Node Editor");
            window._node = node;
            window._graph = graph;
            window.minSize = new Vector2(250, 200);
        }

        private void OnGUI()
        {
            if (_node == null)
            {
                EditorGUILayout.HelpBox("No node selected", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            _node.Label = EditorGUILayout.TextField("Label", _node.Label);
            _node.Type = (NodeType)EditorGUILayout.EnumPopup("Type", _node.Type);
            _node.Value = EditorGUILayout.FloatField("Value", _node.Value);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            _node.Position = EditorGUILayout.Vector2Field("", _node.Position);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graph);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Delete Node", GUILayout.Height(30)))
            {
                if (_graph != null)
                {
                    _graph.RemoveNode(_node.Id);
                    Close();
                }
            }
        }
    }
}
