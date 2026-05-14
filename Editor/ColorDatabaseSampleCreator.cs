#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Logger = HelloDev.Logging.Logger;
using HelloDev.UI.Default;

namespace HelloDev.UI.Default.Editor
{
    public static class ColorDatabaseSampleCreator
    {
        [MenuItem("Tools/HelloDev/Create Color Database Sample")]
        public static void CreateSample()
        {
            try
            {
                // Folders
                EnsureFolder("Assets", "HelloDev");
                EnsureFolder("Assets/HelloDev", "Samples");
                EnsureFolder("Assets/HelloDev/Samples", "ColorDatabaseSample");
                EnsureFolder("Assets/HelloDev/Samples/ColorDatabaseSample", "Scenes");
                EnsureFolder("Assets/HelloDev/Samples/ColorDatabaseSample", "ScriptableObjects");

                string baseFolder = "Assets/HelloDev/Samples/ColorDatabaseSample";
                string scenesFolder = baseFolder + "/Scenes";
                string soFolder = baseFolder + "/ScriptableObjects";

                // Create Colour_SO assets
                var primaryColour = ScriptableObject.CreateInstance<Colour_SO>();
                SetColourSOColor(primaryColour, new Color(0.11f, 0.53f, 0.89f, 1f));
                AssetDatabase.CreateAsset(primaryColour, soFolder + "/PrimaryColour.asset");

                var onSurfaceColour = ScriptableObject.CreateInstance<Colour_SO>();
                SetColourSOColor(onSurfaceColour, Color.black);
                AssetDatabase.CreateAsset(onSurfaceColour, soFolder + "/OnSurfaceColour.asset");

                AssetDatabase.SaveAssets();

                // Create ColorDatabase_SO
                var db = ScriptableObject.CreateInstance<ColorDatabase_SO>();
                db.name = "ColorDatabaseSample";

                // Create slots
                var slotPrimary = new ColorDatabase_SO.ColorSlot
                {
                    Name = "Primary",
                    Role = "primary",
                    Tag = "brand",
                    Description = "Primary brand color",
                    ColourSO = primaryColour
                };
                slotPrimary.EnsureId();

                var slotOnSurface = new ColorDatabase_SO.ColorSlot
                {
                    Name = "OnSurface",
                    Role = "on-surface",
                    Tag = "text",
                    Description = "Color used for text on surfaces",
                    ColourSO = onSurfaceColour
                };
                slotOnSurface.EnsureId();

                var slots = new List<ColorDatabase_SO.ColorSlot> { slotPrimary, slotOnSurface };

                // Create themes
                var themeLight = new ColorDatabase_SO.ColorTheme { DisplayName = "Light" };
                themeLight.EnsureId();
                var tv1 = new ColorDatabase_SO.ColorValue { SlotId = slotPrimary.Id, Color = new Color(0.11f, 0.53f, 0.89f, 1f) };
                tv1.SyncHex();
                var tv2 = new ColorDatabase_SO.ColorValue { SlotId = slotOnSurface.Id, Color = Color.black };
                tv2.SyncHex();
                themeLight.Values.Add(tv1);
                themeLight.Values.Add(tv2);

                var themeDark = new ColorDatabase_SO.ColorTheme { DisplayName = "Dark" };
                themeDark.EnsureId();
                var dv1 = new ColorDatabase_SO.ColorValue { SlotId = slotPrimary.Id, Color = new Color(0.05f, 0.3f, 0.6f, 1f) };
                dv1.SyncHex();
                var dv2 = new ColorDatabase_SO.ColorValue { SlotId = slotOnSurface.Id, Color = Color.white };
                dv2.SyncHex();
                themeDark.Values.Add(dv1);
                themeDark.Values.Add(dv2);

                var themes = new List<ColorDatabase_SO.ColorTheme> { themeLight, themeDark };

                // Assign private fields via reflection
                var slotsField = typeof(ColorDatabase_SO).GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic);
                var themesField = typeof(ColorDatabase_SO).GetField("themes", BindingFlags.Instance | BindingFlags.NonPublic);
                if (slotsField == null || themesField == null)
                {
                    Logger.LogError("UI", "Could not find internal fields on ColorDatabase_SO. Aborting sample creation.");
                    return;
                }

                slotsField.SetValue(db, slots);
                themesField.SetValue(db, themes);

                // Set active theme
                db.ActiveThemeId = themeLight.Id;

                // Create database asset
                string dbAssetPath = soFolder + "/ColorDatabaseSample.asset";
                AssetDatabase.CreateAsset(db, dbAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Create a new empty scene
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Create Canvas
                var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.layer = LayerMask.NameToLayer("UI");

                // Create EventSystem
                if (Object.FindObjectOfType<EventSystem>() == null)
                {
                    new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                }

                // Create a Button
                var buttonGO = new GameObject("SampleButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGO.transform.SetParent(canvasGO.transform, false);
                var btnRT = buttonGO.GetComponent<RectTransform>();
                btnRT.sizeDelta = new Vector2(220, 60);
                btnRT.anchoredPosition = Vector2.zero;

                // Add ImageColorBinder to button's image
                var imgBinder = buttonGO.AddComponent<ImageColorBinder>();
                var imgBinderSO = new SerializedObject(imgBinder);
                imgBinderSO.FindProperty("slotId").stringValue = slotPrimary.Id;
                imgBinderSO.ApplyModifiedProperties();

                // Add TMP text child
                var textGO = new GameObject("ButtonText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(buttonGO.transform, false);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = "Press Me";
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.Center;

                // Add TMPColorBinder to text
                var tmpBinder = textGO.AddComponent<TMPColorBinder>();
                var tmpBinderSO = new SerializedObject(tmpBinder);
                tmpBinderSO.FindProperty("slotId").stringValue = slotOnSurface.Id;
                tmpBinderSO.ApplyModifiedProperties();

                // Create loader GameObject with ColorDatabaseRuntime
                var loaderGO = new GameObject("ColorDatabaseRuntime");
                var loader = loaderGO.AddComponent<ColorDatabaseRuntime>();
                var loaderSO = new SerializedObject(loader);
                loaderSO.FindProperty("loadMode").enumValueIndex = (int)ColorDatabaseRuntime.LoadMode.Direct;
                loaderSO.FindProperty("database").objectReferenceValue = db;
                loaderSO.FindProperty("databaseKey").stringValue = db.name;
                loaderSO.ApplyModifiedProperties();

                // Register runtime locator so bindings can resolve in-editor
                ColorDatabaseLocator.Register(db.name, loader);

                // Save scene
                string scenePath = scenesFolder + "/ColorDatabaseSample.unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Logger.Log("UI", $"Created ColorDatabase sample scene at {scenePath} and database at {dbAssetPath}");
            }
            catch (System.Exception ex)
            {
                Logger.LogError("UI", "Failed to create sample: " + ex.Message);
            }
        }

        private static void EnsureFolder(string parent, string folder)
        {
            var full = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, folder);
        }

        private static void SetColourSOColor(Colour_SO col, Color color)
        {
            var so = new SerializedObject(col);
            var prop = so.FindProperty("_colour");
            if (prop != null)
            {
                prop.colorValue = color;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
