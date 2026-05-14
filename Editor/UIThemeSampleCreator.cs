#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default.Editor
{
    public static class UIThemeSampleCreator
    {
        private const string SampleRootFolder = "Assets/HelloDev/Samples/UIThemeSample";

        [MenuItem("Tools/HelloDev/Delete UI Theme Sample")]
        public static void DeleteSample()
        {
            if (!AssetDatabase.IsValidFolder(SampleRootFolder))
            {
                Debug.Log("[UIThemeSampleCreator] Nothing to delete — sample folder does not exist.");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete UI Theme Sample",
                $"This will permanently delete:\n\n{SampleRootFolder}\n\nAre you sure?",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            if (AssetDatabase.DeleteAsset(SampleRootFolder))
            {
                AssetDatabase.Refresh();
                Logger.Log("UI", $"[UIThemeSampleCreator] Deleted: {SampleRootFolder}");
            }
            else
            {
                Debug.LogError($"[UIThemeSampleCreator] Failed to delete {SampleRootFolder}");
            }
        }

        [MenuItem("Tools/HelloDev/Create UI Theme Sample")]
        public static void CreateSample()
        {
            if (AssetDatabase.IsValidFolder(SampleRootFolder))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "UI Theme Sample Already Exists",
                    $"The sample folder already exists:\n\n{SampleRootFolder}\n\nDelete it and recreate?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite) return;

                AssetDatabase.DeleteAsset(SampleRootFolder);
                AssetDatabase.Refresh();
            }

            try
            {
                EnsureFolder("Assets", "HelloDev");
                EnsureFolder("Assets/HelloDev", "Samples");
                EnsureFolder("Assets/HelloDev/Samples", "UIThemeSample");

                string baseFolder = SampleRootFolder;
                string scenesFolder = baseFolder + "/Scenes";
                string soFolder = baseFolder + "/ScriptableObjects";

                EnsureFolder(baseFolder, "Scenes");
                EnsureFolder(baseFolder, "ScriptableObjects");

                var db = UIThemeSetupWizard.CreateAssets(soFolder);

                string dbAssetPath = soFolder + "/SO_UIDatabase_Sample.asset";
                AssetDatabase.RenameAsset(soFolder + "/SO_UIDatabase.asset", "SO_UIDatabase_Sample");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                db = AssetDatabase.LoadAssetAtPath<UIDatabase_SO>(dbAssetPath);
                if (db == null)
                {
                    Debug.LogError("[UIThemeSampleCreator] Failed to reload database after rename.");
                    return;
                }

                string lightFolder = soFolder + "/Colours/Light";
                string darkFolder = soFolder + "/Colours/Dark";

                var colErrorLight = CreateColourAsset(lightFolder, "Light_Error", new Color(0.90f, 0.22f, 0.21f));
                var colSuccessLight = CreateColourAsset(lightFolder, "Light_Success", new Color(0.26f, 0.63f, 0.28f));
                var colWarningLight = CreateColourAsset(lightFolder, "Light_Warning", new Color(0.98f, 0.55f, 0.00f));
                var colOnPrimaryLight = CreateColourAsset(lightFolder, "Light_OnPrimary", Color.white);

                var colErrorDark = CreateColourAsset(darkFolder, "Dark_Error", new Color(0.98f, 0.73f, 0.73f));
                var colSuccessDark = CreateColourAsset(darkFolder, "Dark_Success", new Color(0.70f, 0.93f, 0.70f));
                var colWarningDark = CreateColourAsset(darkFolder, "Dark_Warning", new Color(1.00f, 0.88f, 0.45f));
                var colOnPrimaryDark = CreateColourAsset(darkFolder, "Dark_OnPrimary", new Color(0.07f, 0.07f, 0.07f));
                AssetDatabase.SaveAssets();

                string slotsFolder = soFolder + "/Slots";
                var slotError = CreateColourSlotAsset(slotsFolder, "Error", "Error", "error", "semantic", "Errors and destructive states", colErrorLight);
                var slotSuccess = CreateColourSlotAsset(slotsFolder, "Success", "Success", "success", "semantic", "Confirmations and positive states", colSuccessLight);
                var slotWarning = CreateColourSlotAsset(slotsFolder, "Warning", "Warning", "warning", "semantic", "Cautions and non-critical alerts", colWarningLight);
                var slotOnPrimary = CreateColourSlotAsset(slotsFolder, "OnPrimary", "OnPrimary", "on-primary", "brand", "Content on Primary-coloured surfaces.", colOnPrimaryLight);
                AssetDatabase.SaveAssets();

                var themeLight = db.Themes[0];
                var themeDark = db.Themes[1];

                themeLight.EditorAddColourEntries(new List<UITheme_SO.ColourEntry>
                {
                    new UITheme_SO.ColourEntry { Slot = slotError, Colour = colErrorLight },
                    new UITheme_SO.ColourEntry { Slot = slotSuccess, Colour = colSuccessLight },
                    new UITheme_SO.ColourEntry { Slot = slotWarning, Colour = colWarningLight },
                    new UITheme_SO.ColourEntry { Slot = slotOnPrimary, Colour = colOnPrimaryLight },
                });
                themeDark.EditorAddColourEntries(new List<UITheme_SO.ColourEntry>
                {
                    new UITheme_SO.ColourEntry { Slot = slotError, Colour = colErrorDark },
                    new UITheme_SO.ColourEntry { Slot = slotSuccess, Colour = colSuccessDark },
                    new UITheme_SO.ColourEntry { Slot = slotWarning, Colour = colWarningDark },
                    new UITheme_SO.ColourEntry { Slot = slotOnPrimary, Colour = colOnPrimaryDark },
                });

                db.EditorAddColourSlots(new List<ColourSlot_SO> { slotError, slotSuccess, slotWarning, slotOnPrimary });
                AssetDatabase.SaveAssets();

                string fontSlotFolder = soFolder + "/FontSlots";
                var robotoBold  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Packages/com.hellodev.ui/Runtime/Fonts/Roboto/static/Roboto-Bold SDF.asset");
                var robotoReg   = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Packages/com.hellodev.ui/Runtime/Fonts/Roboto/static/Roboto-Regular SDF.asset");
                var openSansBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Packages/com.hellodev.ui/Runtime/Fonts/Open_Sans/static/OpenSans-Bold SDF.asset");
                var openSansReg  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Packages/com.hellodev.ui/Runtime/Fonts/Open_Sans/static/OpenSans-Regular SDF.asset");

                var fontSlotHeading = AssetDatabase.LoadAssetAtPath<FontSlot_SO>(fontSlotFolder + "/SO_FontSlot_Heading.asset");
                var fontSlotBody    = AssetDatabase.LoadAssetAtPath<FontSlot_SO>(fontSlotFolder + "/SO_FontSlot_Body.asset");
                // Default fonts act as the ultimate fallback when no theme is active
                if (fontSlotHeading != null) fontSlotHeading.EditorSetup("Heading", "headline", "Used for titles and major headings.", robotoBold);
                if (fontSlotBody    != null) fontSlotBody.EditorSetup("Body", "body", "Used for body text, labels, and captions.", openSansReg);

                if (fontSlotHeading != null && fontSlotBody != null)
                {
                    // Light: Roboto Bold headings, Open Sans Regular body
                    themeLight.EditorAddFontEntries(new List<UITheme_SO.FontEntry>
                    {
                        new UITheme_SO.FontEntry { Slot = fontSlotHeading, Font = robotoBold  },
                        new UITheme_SO.FontEntry { Slot = fontSlotBody,    Font = openSansReg },
                    });
                    // Dark: Open Sans Bold headings, Roboto Regular body — visibly different on theme switch
                    themeDark.EditorAddFontEntries(new List<UITheme_SO.FontEntry>
                    {
                        new UITheme_SO.FontEntry { Slot = fontSlotHeading, Font = openSansBold },
                        new UITheme_SO.FontEntry { Slot = fontSlotBody,    Font = robotoReg    },
                    });
                }
                AssetDatabase.SaveAssets();

                EnsureFolder(soFolder, "TextStyles");
                string textStylesFolder = soFolder + "/TextStyles";

                var styleHeading = ScriptableObject.CreateInstance<TextStyle_SO>();
                var styleHeadingSO = new SerializedObject(styleHeading);
                styleHeadingSO.FindProperty("fontSlot").objectReferenceValue = fontSlotHeading;
                styleHeadingSO.FindProperty("useCustomSize").boolValue = true;
                styleHeadingSO.FindProperty("customSize").floatValue = 36f;
                styleHeadingSO.FindProperty("fontStyle").enumValueIndex = (int)FontStyles.Bold;
                styleHeadingSO.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(styleHeading, textStylesFolder + "/SO_TextStyle_Heading.asset");

                var styleBody = ScriptableObject.CreateInstance<TextStyle_SO>();
                var styleBodySO = new SerializedObject(styleBody);
                styleBodySO.FindProperty("fontSlot").objectReferenceValue = fontSlotBody;
                styleBodySO.FindProperty("useCustomSize").boolValue = true;
                styleBodySO.FindProperty("customSize").floatValue = 14f;
                styleBodySO.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(styleBody, textStylesFolder + "/SO_TextStyle_Body.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var slotPrimary = db.ColourSlots[0];
                var slotSecondary = db.ColourSlots[1];
                var slotSurface = db.ColourSlots[2];
                var slotOnSurface = db.ColourSlots[3];

                EnsureFolder(soFolder, "Styles");
                string stylesFolder = soFolder + "/Styles";
                string primaryBtnPath = stylesFolder + "/SO_SelectableStyle_ButtonPrimary.asset";
                string secondaryBtnPath = stylesFolder + "/SO_SelectableStyle_ButtonSecondary.asset";
                CreateButtonStyleAsset(primaryBtnPath, slotPrimary, slotOnPrimary, slotSecondary, slotSurface, slotOnSurface, scaleOnSelect: true);
                CreateButtonStyleAsset(secondaryBtnPath, slotSecondary, slotOnPrimary, slotPrimary, slotSurface, slotOnSurface, scaleOnSelect: true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                db = AssetDatabase.LoadAssetAtPath<UIDatabase_SO>(dbAssetPath);
                if (db == null)
                {
                    Debug.LogError($"[UIThemeSampleCreator] UIDatabase_SO unloaded after NewScene. Path: {dbAssetPath}");
                    return;
                }

                var btnPrimaryStyle = AssetDatabase.LoadAssetAtPath<UISelectableStyle_SO>(primaryBtnPath);
                var btnSecondaryStyle = AssetDatabase.LoadAssetAtPath<UISelectableStyle_SO>(secondaryBtnPath);

                slotPrimary = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Primary.asset");
                slotSecondary = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Secondary.asset");
                slotSurface = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Surface.asset");
                slotOnSurface = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_OnSurface.asset");
                slotError = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Error.asset");
                slotSuccess = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Success.asset");
                slotWarning = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_Warning.asset");
                slotOnPrimary = AssetDatabase.LoadAssetAtPath<ColourSlot_SO>(slotsFolder + "/SO_ColourSlot_OnPrimary.asset");

                var styleHeadingLoaded = AssetDatabase.LoadAssetAtPath<TextStyle_SO>(textStylesFolder + "/SO_TextStyle_Heading.asset");
                var styleBodyLoaded = AssetDatabase.LoadAssetAtPath<TextStyle_SO>(textStylesFolder + "/SO_TextStyle_Body.asset");

                var cameraGO = new GameObject("Main Camera");
                cameraGO.tag = "MainCamera";
                var cam = cameraGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cameraGO.AddComponent<AudioListener>();

                var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(800, 600);
                canvasGO.layer = LayerMask.NameToLayer("UI");

                if (Object.FindAnyObjectByType<EventSystem>() == null)
                {
                    var esGO = new GameObject("EventSystem", typeof(EventSystem));
                    var inputModuleType = FindTypeInLoadedAssemblies("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                    esGO.AddComponent(inputModuleType ?? typeof(StandaloneInputModule));
                }

                var bg = MakeRect("Background", canvasGO.transform);
                Stretch(bg);
                bg.AddComponent<Image>().color = Color.white;
                BindImage(bg, slotSurface);

                var title = MakeRect("Title", canvasGO.transform);
                SetPos(title, new Vector2(0, 240), new Vector2(720, 55));
                var titleTmp = title.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "HelloDev UI Theme System";
                titleTmp.fontSize = 36;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.alignment = TextAlignmentOptions.Center;
                BindColour(title, slotOnSurface);
                if (styleHeadingLoaded != null) BindFont(title, styleHeadingLoaded);

                var subtitle = MakeRect("Subtitle", canvasGO.transform);
                SetPos(subtitle, new Vector2(0, 195), new Vector2(720, 36));
                var subtitleTmp = subtitle.AddComponent<TextMeshProUGUI>();
                subtitleTmp.text = "Click the primary button to toggle Light / Dark themes";
                subtitleTmp.fontSize = 14;
                subtitleTmp.alignment = TextAlignmentOptions.Center;
                BindColour(subtitle, slotOnSurface);
                if (styleBodyLoaded != null) BindFont(subtitle, styleBodyLoaded);

                float swatchY = 80f;
                CreateSwatch(canvasGO.transform, "Swatch_Primary", slotPrimary, slotOnPrimary, "Primary", new Vector2(-275f, swatchY), new Vector2(95f, 85f));
                CreateSwatch(canvasGO.transform, "Swatch_Secondary", slotSecondary, slotOnPrimary, "Secondary", new Vector2(-165f, swatchY), new Vector2(95f, 85f));
                CreateSwatch(canvasGO.transform, "Swatch_Error", slotError, slotOnPrimary, "Error", new Vector2(-55f, swatchY), new Vector2(95f, 85f));
                CreateSwatch(canvasGO.transform, "Swatch_Success", slotSuccess, slotOnPrimary, "Success", new Vector2(55f, swatchY), new Vector2(95f, 85f));
                CreateSwatch(canvasGO.transform, "Swatch_Warning", slotWarning, slotOnPrimary, "Warning", new Vector2(165f, swatchY), new Vector2(95f, 85f));
                CreateSwatch(canvasGO.transform, "Swatch_Surface", slotSurface, slotOnSurface, "Surface", new Vector2(275f, swatchY), new Vector2(95f, 85f));

                var toggleBtn = CreateButton(canvasGO.transform, "ToggleThemeButton",
                    btnPrimaryStyle, "Toggle Theme",
                    new Vector2(-120f, -30f), new Vector2(215f, 52f));
                toggleBtn.AddComponent<SampleThemeSwitcher>();

                CreateButton(canvasGO.transform, "SecondaryButton",
                    btnSecondaryStyle, "Secondary Action",
                    new Vector2(120f, -30f), new Vector2(215f, 52f));

                CreateChip(canvasGO.transform, "Chip_Error", slotError, slotOnPrimary, "  Error", new Vector2(-200f, -115f), new Vector2(165f, 34f));
                CreateChip(canvasGO.transform, "Chip_Success", slotSuccess, slotOnPrimary, "  Success", new Vector2(0f, -115f), new Vector2(165f, 34f));
                CreateChip(canvasGO.transform, "Chip_Warning", slotWarning, slotOnPrimary, "  Warning", new Vector2(200f, -115f), new Vector2(165f, 34f));

                var hint = MakeRect("Hint_DirectColour", canvasGO.transform);
                SetPos(hint, new Vector2(0f, -195f), new Vector2(740f, 28f));
                var hintTmp = hint.AddComponent<TextMeshProUGUI>();
                hintTmp.text = "Swatches/Chips: ImageColourBinder  |  Buttons: UIButton + UIColourStyle  |  This text: DirectColour (fixed, ignores themes)";
                hintTmp.fontSize = 11;
                hintTmp.alignment = TextAlignmentOptions.Center;
                var hintBinder = hint.AddComponent<TMPColourBinder>();
                var hintBinderSO = new SerializedObject(hintBinder);
                hintBinderSO.FindProperty("useDirectColour").boolValue = true;
                hintBinderSO.FindProperty("directColour").objectReferenceValue = slotOnSurface.DefaultColour;
                hintBinderSO.ApplyModifiedProperties();

                var loaderGO = new GameObject("UIThemeRuntime");
                var loader = loaderGO.AddComponent<UIThemeRuntime>();
                var loaderSO = new SerializedObject(loader);
                loaderSO.FindProperty("loadMode").enumValueIndex = (int)UIThemeRuntime.LoadMode.Direct;
                loaderSO.FindProperty("database").objectReferenceValue = db;
                loaderSO.ApplyModifiedProperties();

                string scenePath = scenesFolder + "/UIThemeSample.unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Logger.Log("UI", $"[UIThemeSampleCreator] Done — {db.ColourSlots.Count} colour slots, {db.FontSlots.Count} font slots, 2 themes, scene: {scenePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[UIThemeSampleCreator] Failed: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private static void CreateSwatch(Transform parent, string name,
            ColourSlot_SO bgSlot, ColourSlot_SO labelSlot, string label,
            Vector2 pos, Vector2 size)
        {
            var go = MakeRect(name, parent);
            SetPos(go, pos, size);
            go.AddComponent<Image>().color = Color.white;
            BindImage(go, bgSlot);

            var textGO = MakeRect("Label", go.transform);
            Stretch(textGO);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            BindColour(textGO, labelSlot);
        }

        private static GameObject CreateButton(Transform parent, string name,
            UISelectableStyle_SO style, string label,
            Vector2 pos, Vector2 size)
        {
            var go = MakeRect(name, parent);
            SetPos(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = new Color32(33, 135, 230, 255);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            go.AddComponent<UIButton>();

            var textGO = MakeRect("Label", go.transform);
            Stretch(textGO);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.color = Color.white;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            var colourStyle = go.AddComponent<UIColourStyle>();
            var colourStyleSO = new SerializedObject(colourStyle);
            colourStyleSO.FindProperty("style").objectReferenceValue = style;
            colourStyleSO.FindProperty("backgroundGraphic").objectReferenceValue = img;
            colourStyleSO.FindProperty("labelText").objectReferenceValue = tmp;
            colourStyleSO.ApplyModifiedProperties();

            return go;
        }

        private static void CreateChip(Transform parent, string name,
            ColourSlot_SO bgSlot, ColourSlot_SO labelSlot, string label,
            Vector2 pos, Vector2 size)
        {
            var go = MakeRect(name, parent);
            SetPos(go, pos, size);
            go.AddComponent<Image>().color = Color.white;
            BindImage(go, bgSlot);

            var textGO = MakeRect("Label", go.transform);
            Stretch(textGO);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            BindColour(textGO, labelSlot);
        }

        private static void BindImage(GameObject go, ColourSlot_SO slot)
        {
            var binder = go.AddComponent<ImageColourBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("slot").objectReferenceValue = slot;
            so.ApplyModifiedProperties();
        }

        private static void BindColour(GameObject go, ColourSlot_SO slot)
        {
            var binder = go.AddComponent<TMPColourBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("slot").objectReferenceValue = slot;
            so.ApplyModifiedProperties();
        }

        private static void BindFont(GameObject go, TextStyle_SO style)
        {
            var binder = go.AddComponent<TMPFontBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("style").objectReferenceValue = style;
            so.ApplyModifiedProperties();
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetPos(GameObject go, Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static ColourSlot_SO CreateColourSlotAsset(string folder, string assetName,
            string displayName, string role, string tag, string description, Colour_SO defaultColour)
        {
            var slot = ScriptableObject.CreateInstance<ColourSlot_SO>();
            slot.EditorSetup(displayName, role, tag, description, defaultColour);
            AssetDatabase.CreateAsset(slot, folder + "/SO_ColourSlot_" + assetName + ".asset");
            return slot;
        }

        private static Colour_SO CreateColourAsset(string folder, string assetName, Color color)
        {
            var colour = ScriptableObject.CreateInstance<Colour_SO>();
            var so = new SerializedObject(colour);
            so.FindProperty("_colour").colorValue = color;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(colour, folder + "/SO_Colour_" + assetName + ".asset");
            return colour;
        }

        private static void CreateButtonStyleAsset(string assetPath,
            ColourSlot_SO bgSlot, ColourSlot_SO textSlot,
            ColourSlot_SO pressedBgSlot,
            ColourSlot_SO disabledBgSlot, ColourSlot_SO disabledTextSlot,
            bool scaleOnSelect = false)
        {
            var styleSO = ScriptableObject.CreateInstance<UISelectableStyle_SO>();
            AssetDatabase.CreateAsset(styleSO, assetPath);
            var so = new SerializedObject(styleSO);
            SetStateSlots(so, "normal", bgSlot, textSlot);
            SetStateSlots(so, "highlighted", bgSlot, textSlot);
            SetStateSlots(so, "pressed", pressedBgSlot, textSlot);
            SetStateSlots(so, "selected", bgSlot, textSlot);
            SetStateSlots(so, "disabled", disabledBgSlot, disabledTextSlot);
            if (scaleOnSelect)
            {
                so.FindProperty("scaleOnSelect").boolValue = true;
                so.FindProperty("scaledSize").floatValue = 1.05f;
                so.FindProperty("scaleTime").floatValue = 0.15f;
            }
            so.ApplyModifiedProperties();
        }

        private static void SetStateSlots(SerializedObject so, string stateName,
            ColourSlot_SO background, ColourSlot_SO text)
        {
            var state = so.FindProperty(stateName);
            if (state == null) return;
            state.FindPropertyRelative("Background").objectReferenceValue = background;
            state.FindPropertyRelative("Text").objectReferenceValue = text;
        }

        private static System.Type FindTypeInLoadedAssemblies(string fullTypeName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullTypeName);
                if (t != null) return t;
            }
            return null;
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string full = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
