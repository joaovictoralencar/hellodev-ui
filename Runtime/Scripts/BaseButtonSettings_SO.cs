using UnityEngine;
using UnityEngine.Serialization;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Button Setting", fileName = "Button Setting")]
    public class BaseButtonSettings_SO : ScriptableObject
    {
        [Header("Colours")] [SerializeField] private Colour_SO normalTextColour;
        [SerializeField] private bool normalTextColourOverrideValue;

        [Space(5)] [SerializeField] private Colour_SO selectedTextColour;
        [SerializeField] private bool selectedTextColourOverrideValue;

        [Space(5)] [SerializeField] private Colour_SO disabledTextColour;
        [SerializeField] private bool disabledTextColourOverrideValue;


        [Header("Animation")] [SerializeField] bool _scaleOnSelect;
        [FormerlySerializedAs("_scaleDiff")] [SerializeField] float _scaledSize = 1.15f;
        [SerializeField] float _scaleTime = .15f;

        public Colour_SO NormalTextColour => normalTextColour;
        public Colour_SO SelectedTextColour => selectedTextColour;
        public Colour_SO DisabledTextColour => disabledTextColour;
        public bool ScaleOnSelect => _scaleOnSelect;
        public float ScaledSize => _scaledSize;
        public float ScaleTime => _scaleTime;
        public bool SelectedTextColourOverrideValue => selectedTextColourOverrideValue;
        public bool NormalTextColourOverrideValue => normalTextColourOverrideValue;
        public bool DisabledTextColourOverrideValue => disabledTextColourOverrideValue;
    }
}