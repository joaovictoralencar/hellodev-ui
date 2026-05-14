using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/UI Database", fileName = "UIDatabase")]
    public class UIDatabase_SO : ScriptableObject
    {
        [SerializeField] private List<ColourSlot_SO> colourSlots = new List<ColourSlot_SO>();
        [SerializeField] private List<FontSlot_SO> fontSlots = new List<FontSlot_SO>();
        [SerializeField] private List<UITheme_SO> themes = new List<UITheme_SO>();
        [SerializeField] private UITheme_SO activeTheme;

        public IReadOnlyList<ColourSlot_SO> ColourSlots => colourSlots;
        public IReadOnlyList<FontSlot_SO> FontSlots => fontSlots;
        public IReadOnlyList<UITheme_SO> Themes => themes;

        public UITheme_SO ActiveTheme
        {
            get => activeTheme != null ? activeTheme : (themes.Count > 0 ? themes[0] : null);
            set => activeTheme = value;
        }

        public Color GetColourForSlot(ColourSlot_SO slot, UITheme_SO themeOverride = null)
        {
            if (slot == null) return Color.white;

            var theme = themeOverride != null ? themeOverride : ActiveTheme;
            if (theme != null)
            {
                var colour = theme.GetColour(slot);
                if (colour.HasValue) return colour.Value;
            }

            if (slot.DefaultColour != null)
            {
                Logger.LogVerbose("UI", $"[UIDatabase_SO '<color=#80C0F0>{name}</color>'] " +
                    $"Slot '<color=#F0D080>{slot.DisplayName}</color>' not in theme '<color=#F0D080>{theme?.name}</color>' — using DefaultColour.");
                return slot.DefaultColour.Colour;
            }

            return Color.white;
        }

        public TMP_FontAsset GetFontForSlot(FontSlot_SO slot, UITheme_SO themeOverride = null)
        {
            if (slot == null) return null;

            var theme = themeOverride != null ? themeOverride : ActiveTheme;
            if (theme != null)
            {
                var font = theme.GetFont(slot);
                if (font != null) return font;
            }

            return slot.DefaultFont;
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Constants")]
        public void GenerateConstants()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    System.Type genType = null;
                    try { genType = asm.GetType("HelloDev.UI.Default.Editor.UIIdGenerator"); } catch { }
                    if (genType == null) continue;
                    var method = genType.GetMethod("GenerateForDatabase",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { this });
                        return;
                    }
                }

                UnityEngine.Debug.LogWarning("[UIDatabase_SO] UIIdGenerator not found.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[UIDatabase_SO] Failed to invoke UIIdGenerator: {ex}");
            }
        }

        public void EditorSetup(List<ColourSlot_SO> newColourSlots, List<FontSlot_SO> newFontSlots,
            List<UITheme_SO> newThemes, UITheme_SO newActiveTheme = null)
        {
            colourSlots = newColourSlots ?? new List<ColourSlot_SO>();
            fontSlots = newFontSlots ?? new List<FontSlot_SO>();
            themes = newThemes ?? new List<UITheme_SO>();
            activeTheme = newActiveTheme;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorAddColourSlots(List<ColourSlot_SO> additionalSlots)
        {
            if (additionalSlots == null) return;
            colourSlots.AddRange(additionalSlots);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorAddFontSlots(List<FontSlot_SO> additionalSlots)
        {
            if (additionalSlots == null) return;
            fontSlots.AddRange(additionalSlots);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Returns the most complete UIDatabase_SO in the project (by colour slot count).</summary>
        public static UIDatabase_SO FindBestInProject()
        {
            UIDatabase_SO best = null;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:UIDatabase_SO"))
            {
                var candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<UIDatabase_SO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && (best == null || candidate.ColourSlots.Count > best.ColourSlots.Count))
                    best = candidate;
            }

            return best;
        }
#endif
    }
}
