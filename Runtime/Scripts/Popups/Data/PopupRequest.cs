using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Data container that describes what a popup should display and how it should behave.
    /// </summary>
    [Serializable]
    public class PopupRequest
    {
        [Tooltip("Static title text (fallback).")]
        public string Title;

        [Tooltip("Static message text (fallback).")]
        public string Message;

        [Tooltip("Localized title (overrides static Title).")]
        public LocalizedString LocalizedTitle;

        [Tooltip("Localized message (overrides static Message).")]
        public LocalizedString LocalizedMessage;

        [Tooltip("Prefab to use for this popup (optional).")]
        public GameObject Prefab;

        [Tooltip("Addressable reference to a popup prefab (optional).")]
        public AssetReferenceGameObject PrefabReference;

        [Tooltip("List of buttons to display on the popup.")]
        public List<PopupButtonData> Buttons = new();
    }
}