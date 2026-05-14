using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Default.Editor
{
    [CustomEditor(typeof(BaseColourBinder), editorForChildClasses: true)]
    public class BaseColourBinderEditor : UnityEditor.Editor
    {
        private int previewThemeIndex;
        private Color? savedColour;
        private bool previewActive;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

            var dbProp = serializedObject.FindProperty("editorPreviewDatabase");
            EditorGUILayout.PropertyField(dbProp,
                new GUIContent("Preview Database", "Override which database drives preview colours. Leave null to auto-discover."));
            serializedObject.ApplyModifiedProperties();

            var db = (dbProp.objectReferenceValue as UIDatabase_SO) ?? UIDatabase_SO.FindBestInProject();
            UITheme_SO selectedTheme = null;

            if (db != null && db.Themes.Count > 0)
            {
                var themeNames = new[] { "Active Theme" }
                    .Concat(db.Themes.Select(t => t != null ? t.name : "(null)"))
                    .ToArray();

                previewThemeIndex = Mathf.Clamp(previewThemeIndex, 0, themeNames.Length - 1);
                previewThemeIndex = EditorGUILayout.Popup(
                    new GUIContent("Theme", "Theme to use for the preview."),
                    previewThemeIndex,
                    themeNames);

                selectedTheme = previewThemeIndex == 0 ? null : db.Themes[previewThemeIndex - 1];
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField("Theme", "No database found");
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview"))
            {
                var graphic = GetTargetGraphic();
                if (graphic != null)
                    savedColour = graphic.color;

                ((BaseColourBinder)target).EditorApplyPreview(selectedTheme);
                previewActive = true;
            }

            using (new EditorGUI.DisabledScope(!previewActive))
            {
                if (GUILayout.Button("Revert"))
                {
                    var graphic = GetTargetGraphic();
                    if (graphic != null && savedColour.HasValue)
                    {
                        graphic.color = savedColour.Value;
                        EditorUtility.SetDirty(graphic);
                    }

                    savedColour = null;
                    previewActive = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (previewActive)
            {
                var themeName = selectedTheme != null ? selectedTheme.name : "Active Theme";
                EditorGUILayout.HelpBox(
                    $"Previewing slot / '{themeName}'. Click Revert to undo, or save to keep.",
                    MessageType.Info);
            }
        }

        private Graphic GetTargetGraphic()
        {
            var prop = serializedObject.FindProperty("target");
            if (prop == null) return null;
            return prop.objectReferenceValue as Graphic;
        }
    }
}
