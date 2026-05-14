using TMPro;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(fileName = "TextStyle", menuName = "HelloDev/UI/Text Style")]
    public class TextStyle_SO : ScriptableObject
    {
        [Header("Font")]
        [Tooltip("Font slot from the active theme. Leave null to keep the component's current font.")]
        [SerializeField] private FontSlot_SO fontSlot;

        [Tooltip("When enabled, ignores the font slot and uses a direct TMP_FontAsset reference.")]
        [SerializeField] private bool useDirectFont;

        [Tooltip("Font asset used when 'Use Direct Font' is on.")]
        [SerializeField] private TMP_FontAsset directFont;

        [Header("Size")]
        [Tooltip("Named size preset. Used when 'Use Custom Size' is off.")]
        [SerializeField] private FontSize_SO sizePreset;

        [Tooltip("When enabled, ignores the size preset and applies 'Custom Size' directly.")]
        [SerializeField] private bool useCustomSize;

        [SerializeField] private float customSize = 14f;

        [Header("Style & Spacing")]
        [SerializeField] private FontStyles fontStyle = FontStyles.Normal;
        [SerializeField] private float characterSpacing = 0f;
        [SerializeField] private float wordSpacing = 0f;
        [SerializeField] private float lineSpacing = 0f;

        /// <summary>
        /// Applies this style to a TextMeshProUGUI. Pass a UIDatabase_SO to resolve
        /// the font slot from the active theme; pass null to fall back to the slot's DefaultFont.
        /// </summary>
        public void ApplyTo(TextMeshProUGUI text, UIDatabase_SO database = null)
        {
            if (text == null) return;

            TMP_FontAsset resolvedFont = null;
            if (useDirectFont)
                resolvedFont = directFont;
            else if (fontSlot != null)
                resolvedFont = database != null ? database.GetFontForSlot(fontSlot) : fontSlot.DefaultFont;

            if (resolvedFont != null)
                text.font = resolvedFont;

            if (useCustomSize)
                text.fontSize = customSize;
            else if (sizePreset != null)
                text.fontSize = sizePreset.Size;

            text.fontStyle = fontStyle;
            text.characterSpacing = characterSpacing;
            text.wordSpacing = wordSpacing;
            text.lineSpacing = lineSpacing;
        }
    }
}
