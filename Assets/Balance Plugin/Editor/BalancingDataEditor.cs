using UnityEngine;
using UnityEditor;
using System.IO;

namespace BalancePlugin
{
    [CustomEditor(typeof(BalancingData))]
    public class BalancingDataEditor : Editor
    {
        [MenuItem("Assets/Create/Balance/Balancing Data")]
        public static void CreateBalancingData()
        {
            string folder = GetSelectedProjectFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "BalancingData.asset").Replace("\\", "/"));
            BalancingData data = CreateInstance<BalancingData>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Balance Window"))
            {
                BalancingWindow.ShowWindow();
            }
        }

        private static string GetSelectedProjectFolder()
        {
            string path = "Assets";
            Object selected = Selection.activeObject;
            if (selected == null)
                return path;

            string selectedPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(selectedPath))
                return path;

            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            string directory = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrEmpty(directory) ? path : directory.Replace("\\", "/");
        }
    }
}
