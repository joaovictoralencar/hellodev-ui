using System;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// ScriptableObject configuration for popups.
    /// Defines content, buttons, and behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/UI/Popup")]
    public class Popup_SO : ScriptableObject
    {
        [Header("Style")]
        [Tooltip("Custom prefab for this popup. If null, uses service default.")]
        public UIPopup customPrefab;

        [Header("Content")]
        public LocalizedString title;
        public LocalizedString message;
        public Sprite icon;

        [Header("Buttons")]
        public PopupButton[] buttons;

        [Header("Behavior")]
        [Tooltip("Index of the button to focus by default when popup opens.")]
        public int defaultButtonIndex = 0;

        [Tooltip("Index of the button triggered by Cancel input. -1 = last button.")]
        public int cancelButtonIndex = -1;

        /// <summary>
        /// Configuration for a single popup button.
        /// </summary>
        [Serializable]
        public class PopupButton
        {
            public LocalizedString label;
            public PopupButtonType type = PopupButtonType.Custom;

            [Tooltip("Show input prompt (e.g., [A]/[B]) on this button.")]
            public bool showInputPrompt = true;
        }
    }
}

