using System;
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Data for a single button inside a popup.
    /// </summary>
    [Serializable]
    public struct PopupButtonData
    {
        [Header("Localization")]
        [Tooltip("Fallback text if LocalizedLabel is not set.")]
        public string Label;
        [Tooltip("Localized label that overrides the fallback text.")]
        public LocalizedString LocalizedLabel;

        [Header("Button Settings")]
        [Tooltip("If true, the popup will close when this button is clicked.")]
        public bool ClosesPopup;
        [Tooltip("If true, the button will show an input prompt (e.g. for gamepad navigation).")]
        public bool ShowInputPrompt;

        [Header("Button Prefab")]
        [Tooltip("Optional prefab to use for this button.")]
        public UIButton Prefab;
        [Tooltip("Optional addressable reference to a button prefab.")]
        public AssetReference PrefabReference;

        [Header("Button Callback")]
        [Tooltip("Action invoked when the button is clicked.")]
        public Action<IUIPopup> Callback;

        public PopupButtonData(
            LocalizedString localizedLabel,
            string label,
            bool showInputPrompt,
            UIButton prefab,
            AssetReference prefabReference,
            bool closesPopup,
            Action<IUIPopup> callback)
        {
            LocalizedLabel = localizedLabel;
            Label = label;
            ShowInputPrompt = showInputPrompt;
            Callback = callback;
            Prefab = prefab;
            ClosesPopup = closesPopup;
            PrefabReference = prefabReference;
        }
    }
}