using UnityEditor;
using UnityEngine;
using HelloDev.UI.Default;

namespace HelloDev.UI.Default.Editor
{
    [CustomEditor(typeof(ColorDatabase_SO))]
    public class ColorDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(6);
            if (GUILayout.Button("Generate Color IDs"))
            {
                var db = (ColorDatabase_SO)target;
                ColorIdGenerator.GenerateForDatabase(db);
            }
        }
    }
}
