using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HelloDev.UI.Default.Editor
{
    [CustomEditor(typeof(UIColourStyle))]
    public class UIColourStyleEditor : UnityEditor.Editor
    {
        private UISelectable.SelectableState previewState = UISelectable.SelectableState.Normal;
        private int previewThemeIndex;
        private Color? savedBgColour;
        private Color? savedTextColour;
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

            previewState = (UISelectable.SelectableState)EditorGUILayout.EnumPopup(
                new GUIContent("State", "Selectable state whose colour style will be previewed."),
                previewState);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview"))
            {
                var colourStyle = (UIColourStyle)target;
                colourStyle.EditorGetColours(out savedBgColour, out savedTextColour);
                colourStyle.EditorPreview(previewState, selectedTheme);
                previewActive = true;
            }

            using (new EditorGUI.DisabledScope(!previewActive))
            {
                if (GUILayout.Button("Revert"))
                {
                    var colourStyle = (UIColourStyle)target;
                    colourStyle.EditorSetColours(savedBgColour, savedTextColour);
                    savedBgColour = null;
                    savedTextColour = null;
                    previewActive = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (previewActive)
            {
                var themeName = selectedTheme != null ? selectedTheme.name : "Active Theme";
                EditorGUILayout.HelpBox(
                    $"Previewing '{previewState}' / '{themeName}'. Click Revert to undo, or save to keep.",
                    MessageType.Info);
            }
        }
    }
}
