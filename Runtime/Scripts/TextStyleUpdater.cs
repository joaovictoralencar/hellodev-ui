using TMPro;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextStyleUpdater : MonoBehaviour
    {
        [SerializeField] private TextStyle_SO textStyle;
        [SerializeField] private Colour_SO textColour;

        private TextMeshProUGUI textComponent;

        private void OnEnable()
        {
            ValidateStyle();
        }

        private void OnValidate()
        {
            ValidateStyle();
        }

        private void Awake()
        {
            ValidateStyle();
        }

        private void ValidateStyle()
        {
            ValidateTMP();
            ApplyTextStyle();
            ApplyTextColour();
        }

        private void ValidateTMP()
        {
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent == null) textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        }

        public Colour_SO TextColourSO
        {
            get => textColour;
            set
            {
                textColour = value;
                ApplyTextColour();
            }
        }

        public TextStyle_SO TextStyleSO
        {
            get => textStyle;
            set
            {
                textStyle = value;
                ApplyTextStyle();
            }
        }

        private void ApplyTextStyle()
        {
            if (textComponent == null || textStyle == null) return;
            textStyle.ApplyTo(textComponent);
        }

        private void ApplyTextColour()
        {
            if (textComponent == null || textColour == null) return;
            textComponent.color = textColour.Colour;
        }
    }
}