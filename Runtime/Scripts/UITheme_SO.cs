using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// A theme maps each ColourSlot_SO to a concrete Colour_SO and each FontSlot_SO to a TMP_FontAsset.
    /// The asset name is the theme display name — rename the asset to rename the theme.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/UI/UI Theme", fileName = "UITheme")]
    public class UITheme_SO : ScriptableObject
    {
        [Serializable]
        public class ColourEntry
        {
            public ColourSlot_SO Slot;
            public Colour_SO Colour;
        }

        [Serializable]
        public class FontEntry
        {
            public FontSlot_SO Slot;
            public TMP_FontAsset Font;
        }

        [SerializeField] private List<ColourEntry> colourEntries = new List<ColourEntry>();
        [SerializeField] private List<FontEntry> fontEntries = new List<FontEntry>();

        public IReadOnlyList<ColourEntry> ColourEntries => colourEntries;
        public IReadOnlyList<FontEntry> FontEntries => fontEntries;

        public Color? GetColour(ColourSlot_SO slot)
        {
            if (slot == null) return null;
            var entry = colourEntries.Find(e => e.Slot == slot);
            if (entry == null)
            {
                if (slot.DefaultColour == null)
                    Debug.LogWarning($"[UITheme_SO '<color=#F0D080>{name}</color>'] " +
                        $"Slot '<color=#F0D080>{slot.DisplayName}</color>' not found and has no DefaultColour. " +
                        $"Add it to the theme or assign a DefaultColour on the slot asset.");
                return null;
            }

            return entry.Colour != null ? entry.Colour.Colour : (Color?)null;
        }

        public TMP_FontAsset GetFont(FontSlot_SO slot)
        {
            if (slot == null) return null;
            var entry = fontEntries.Find(e => e.Slot == slot);
            if (entry != null && entry.Font != null) return entry.Font;
            return slot.DefaultFont;
        }

#if UNITY_EDITOR
        public void EditorSetup(List<ColourEntry> newColourEntries, List<FontEntry> newFontEntries = null)
        {
            colourEntries = newColourEntries ?? new List<ColourEntry>();
            fontEntries = newFontEntries ?? new List<FontEntry>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorAddColourEntries(List<ColourEntry> additionalEntries)
        {
            if (additionalEntries == null) return;
            colourEntries.AddRange(additionalEntries);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorAddFontEntries(List<FontEntry> additionalEntries)
        {
            if (additionalEntries == null) return;
            fontEntries.AddRange(additionalEntries);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
