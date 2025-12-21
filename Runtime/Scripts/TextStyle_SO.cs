using UnityEngine;
using TMPro;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(fileName = "TextStyleSO", menuName = "HelloDev/UI/TextStyleSO")]
    public class TextStyle_SO : ScriptableObject
    {
        [SerializeField] private float fontSize = 36f;
        [SerializeField] private float characterSpacing = 0f;
        [SerializeField] private float wordSpacing = 0f;
        [SerializeField] private float lineSpacing = 0f;

        public void ApplyTo(TextMeshProUGUI textComponent)
        {
            if (textComponent == null) return;

            textComponent.fontSize = fontSize;
            textComponent.characterSpacing = characterSpacing;
            textComponent.wordSpacing = wordSpacing;
            textComponent.lineSpacing = lineSpacing;
        }
    }
}
