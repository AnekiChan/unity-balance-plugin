using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class BalancingDataEditor : Editor
    {
        [MenuItem("Assets/Create/Balance/Balancing Data")]
        public static void CreateBalancingData()
        {
            string path = "Assets/" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".asset";
            BalancingData data = CreateInstance<BalancingData>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }
    }
}