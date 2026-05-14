#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HelloDev.UI.Default.Editor
{
    /// <summary>
    /// One-click wizard that scaffolds a minimal UIDatabase asset hierarchy.
    ///
    /// Creates:
    ///   • 5 ColourSlot_SO assets  (Primary, Secondary, Surface, OnSurface, Text)
    ///   • 2 FontSlot_SO assets    (Heading, Body)
    ///   • 10 Colour_SO assets     (one per colour slot per theme: Light + Dark)
    ///   • 2 UITheme_SO assets     (Light, Dark)
    ///   • 1 UIDatabase_SO         (wires everything together)
    ///   • 1 UISelectableStyle_SO placeholder
    ///
    /// Usage: Tools → HelloDev → UI → Create UI Database
    /// </summary>
    public static class UIThemeSetupWizard
    {
        [MenuItem("Tools/HelloDev/UI/Create UI Database")]
        public static void Run()
        {
            string root = EditorUtility.SaveFolderPanel(
                "Choose folder for UI Database assets",
                "Assets",
                "UIData");

            if (string.IsNullOrEmpty(root)) return;

            if (root.StartsWith(Application.dataPath))
                root = "Assets" + root.Substring(Application.dataPath.Length);

            if (AssetDatabase.LoadMainAssetAtPath(root + "/SO_UIDatabase.asset") != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "UI Database Already Exists",
                    $"A UI Database already exists at:\n\n{root}\n\nOverwrite it?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite) return;
            }

            CreateAssets(root);
        }

        public static UIDatabase_SO CreateAssets(string rootFolder)
        {
            EnsureFolder(rootFolder);
            string slotsFolder = rootFolder + "/Slots";
            string fontSlotsFolder = rootFolder + "/FontSlots";
            string coloursLight = rootFolder + "/Colours/Light";
            string coloursDark = rootFolder + "/Colours/Dark";
            string themesFolder = rootFolder + "/Themes";

            EnsureFolder(rootFolder + "/Colours");
            EnsureFolder(slotsFolder);
            EnsureFolder(fontSlotsFolder);
            EnsureFolder(coloursLight);
            EnsureFolder(coloursDark);
            EnsureFolder(themesFolder);

            var colPrimaryLight = CreateColour(coloursLight, "Light_Primary", new Color(0.13f, 0.53f, 0.90f));
            var colSecondaryLight = CreateColour(coloursLight, "Light_Secondary", new Color(0.56f, 0.14f, 0.67f));
            var colSurfaceLight = CreateColour(coloursLight, "Light_Surface", new Color(0.96f, 0.96f, 0.96f));
            var colOnSurfaceLight = CreateColour(coloursLight, "Light_OnSurface", new Color(0.13f, 0.13f, 0.13f));
            var colTextLight = CreateColour(coloursLight, "Light_Text", new Color(0.10f, 0.10f, 0.10f));

            var colPrimaryDark = CreateColour(coloursDark, "Dark_Primary", new Color(0.56f, 0.80f, 0.98f));
            var colSecondaryDark = CreateColour(coloursDark, "Dark_Secondary", new Color(0.85f, 0.65f, 0.95f));
            var colSurfaceDark = CreateColour(coloursDark, "Dark_Surface", new Color(0.07f, 0.07f, 0.07f));
            var colOnSurfaceDark = CreateColour(coloursDark, "Dark_OnSurface", new Color(0.88f, 0.88f, 0.88f));
            var colTextDark = CreateColour(coloursDark, "Dark_Text", new Color(0.92f, 0.92f, 0.92f));
            AssetDatabase.SaveAssets();

            var slotPrimary = CreateColourSlot(slotsFolder, "Primary", "Primary", "brand", colPrimaryLight);
            var slotSecondary = CreateColourSlot(slotsFolder, "Secondary", "Secondary", "brand", colSecondaryLight);
            var slotSurface = CreateColourSlot(slotsFolder, "Surface", "Surface", "neutral", colSurfaceLight);
            var slotOnSurface = CreateColourSlot(slotsFolder, "OnSurface", "OnSurface", "neutral", colOnSurfaceLight);
            var slotText = CreateColourSlot(slotsFolder, "Text", "Text", "neutral", colTextLight);
            AssetDatabase.SaveAssets();

            var fontSlotHeading = CreateFontSlot(fontSlotsFolder, "Heading", "Heading", "headline", null);
            var fontSlotBody = CreateFontSlot(fontSlotsFolder, "Body", "Body", "body", null);
            AssetDatabase.SaveAssets();

            var themeLight = ScriptableObject.CreateInstance<UITheme_SO>();
            themeLight.EditorSetup(
                new List<UITheme_SO.ColourEntry>
                {
                    new UITheme_SO.ColourEntry { Slot = slotPrimary, Colour = colPrimaryLight },
                    new UITheme_SO.ColourEntry { Slot = slotSecondary, Colour = colSecondaryLight },
                    new UITheme_SO.ColourEntry { Slot = slotSurface, Colour = colSurfaceLight },
                    new UITheme_SO.ColourEntry { Slot = slotOnSurface, Colour = colOnSurfaceLight },
                    new UITheme_SO.ColourEntry { Slot = slotText, Colour = colTextLight },
                });
            CreateOrReplaceAsset(themeLight, themesFolder + "/SO_UITheme_Light.asset");

            var themeDark = ScriptableObject.CreateInstance<UITheme_SO>();
            themeDark.EditorSetup(
                new List<UITheme_SO.ColourEntry>
                {
                    new UITheme_SO.ColourEntry { Slot = slotPrimary, Colour = colPrimaryDark },
                    new UITheme_SO.ColourEntry { Slot = slotSecondary, Colour = colSecondaryDark },
                    new UITheme_SO.ColourEntry { Slot = slotSurface, Colour = colSurfaceDark },
                    new UITheme_SO.ColourEntry { Slot = slotOnSurface, Colour = colOnSurfaceDark },
                    new UITheme_SO.ColourEntry { Slot = slotText, Colour = colTextDark },
                });
            CreateOrReplaceAsset(themeDark, themesFolder + "/SO_UITheme_Dark.asset");
            AssetDatabase.SaveAssets();

            var db = ScriptableObject.CreateInstance<UIDatabase_SO>();
            db.name = "SO_UIDatabase";
            db.EditorSetup(
                new List<ColourSlot_SO> { slotPrimary, slotSecondary, slotSurface, slotOnSurface, slotText },
                new List<FontSlot_SO> { fontSlotHeading, fontSlotBody },
                new List<UITheme_SO> { themeLight, themeDark },
                themeLight);
            CreateOrReplaceAsset(db, rootFolder + "/SO_UIDatabase.asset");

            var styleSO = ScriptableObject.CreateInstance<UISelectableStyle_SO>();
            var stylePath = rootFolder + "/SO_SelectableStyle_Default.asset";
            CreateOrReplaceAsset(styleSO, stylePath);
            var serialized = new SerializedObject(styleSO);
            SetStateStyle(serialized, "normal", slotPrimary, slotText);
            SetStateStyle(serialized, "highlighted", slotPrimary, slotText);
            SetStateStyle(serialized, "pressed", slotPrimary, slotText);
            SetStateStyle(serialized, "selected", slotPrimary, slotText);
            SetStateStyle(serialized, "disabled", slotSurface, slotOnSurface);
            serialized.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UIThemeSetupWizard] Done! Assets created at '{rootFolder}'.\n" +
                      $"  Colour Slots: Primary, Secondary, Surface, OnSurface, Text\n" +
                      $"  Font Slots: Heading, Body\n" +
                      $"  Themes: SO_UITheme_Light, SO_UITheme_Dark\n" +
                      $"  Database: {rootFolder}/SO_UIDatabase.asset");

            Selection.activeObject = db;
            return db;
        }

        private static Colour_SO CreateColour(string folder, string name, Color color)
        {
            var colour = ScriptableObject.CreateInstance<Colour_SO>();
            var so = new SerializedObject(colour);
            so.FindProperty("_colour").colorValue = color;
            so.ApplyModifiedProperties();
            CreateOrReplaceAsset(colour, $"{folder}/SO_Colour_{name}.asset");
            return colour;
        }

        private static ColourSlot_SO CreateColourSlot(string folder, string assetName,
            string displayName, string tag, Colour_SO defaultColour)
        {
            var slot = ScriptableObject.CreateInstance<ColourSlot_SO>();
            slot.EditorSetup(displayName, tag, tag, string.Empty, defaultColour);
            CreateOrReplaceAsset(slot, $"{folder}/SO_ColourSlot_{assetName}.asset");
            return slot;
        }

        private static FontSlot_SO CreateFontSlot(string folder, string assetName,
            string displayName, string role, TMP_FontAsset defaultFont)
        {
            var slot = ScriptableObject.CreateInstance<FontSlot_SO>();
            slot.EditorSetup(displayName, role, string.Empty, defaultFont);
            CreateOrReplaceAsset(slot, $"{folder}/SO_FontSlot_{assetName}.asset");
            return slot;
        }

        private static T CreateOrReplaceAsset<T>(T asset, string path) where T : Object
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetStateStyle(SerializedObject so, string stateName,
            ColourSlot_SO background, ColourSlot_SO text)
        {
            var stateProperty = so.FindProperty(stateName);
            if (stateProperty == null) return;
            stateProperty.FindPropertyRelative("Background").objectReferenceValue = background;
            stateProperty.FindPropertyRelative("Text").objectReferenceValue = text;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string folder = path.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
