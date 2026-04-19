using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class NodeInspectorWindow : EditorWindow
    {
        private BalancingNode _currentNode;
        private BalancingWindow _parentWindow;

        public void SetParentWindow(BalancingWindow parent)
        {
            _parentWindow = parent;
        }

        public void SetNode(BalancingNode node)
        {
            _currentNode = node;
        }

        [MenuItem("Tools/Balancing Inspector")]
        public static void ShowWindow()
        {
            GetWindow<NodeInspectorWindow>("Node Inspector");
        }

        private void OnGUI()
        {
            if (_currentNode == null)
            {
                EditorGUILayout.LabelField("Select a node to configure");
                return;
            }

            EditorGUILayout.LabelField("Type: " + _currentNode.NodeType);
            _currentNode.DisplayName = EditorGUILayout.TextField("Name", _currentNode.DisplayName);

            EditorGUILayout.Space();
            DrawCustomProperties();
        }

        private void DrawCustomProperties()
        {
            SerializedObject so = new SerializedObject(_currentNode);
            SerializedProperty iterator = so.GetIterator();
            bool first = true;

            while (iterator.NextVisible(first))
            {
                first = false;
                if (iterator.name == "NodeId" || iterator.name == "Position" || iterator.name == "DisplayName")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            so.ApplyModifiedProperties();
        }
    }
}