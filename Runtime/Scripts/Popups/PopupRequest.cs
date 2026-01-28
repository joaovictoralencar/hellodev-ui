using System;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Data for a popup request. Can be created anywhere and raised via event.
    /// </summary>
    [Serializable]
    public struct PopupRequest
    {
        /// <summary>
        /// ScriptableObject configuration for the popup.
        /// </summary>
        public Popup_SO Config;

        /// <summary>
        /// Optional: override prefab from Config.
        /// </summary>
        public UIPopup CustomPrefab;

        /// <summary>
        /// Optional: override title from Config.
        /// </summary>
        public string TitleOverride;

        /// <summary>
        /// Optional: override message from Config.
        /// </summary>
        public string MessageOverride;

        /// <summary>
        /// For quick runtime popups without Config.
        /// </summary>
        public string[] ButtonLabels;

        /// <summary>
        /// Callback with button index when popup closes.
        /// </summary>
        public Action<int> OnResult;

        /// <summary>
        /// Creates a request from a ScriptableObject configuration.
        /// </summary>
        public static PopupRequest FromConfig(Popup_SO config, Action<int> onResult = null)
        {
            return new PopupRequest
            {
                Config = config,
                OnResult = onResult
            };
        }

        /// <summary>
        /// Creates a quick runtime popup without ScriptableObject configuration.
        /// </summary>
        public static PopupRequest Quick(string title, string message, string[] buttons, Action<int> onResult = null)
        {
            return new PopupRequest
            {
                TitleOverride = title,
                MessageOverride = message,
                ButtonLabels = buttons,
                OnResult = onResult
            };
        }
    }
}
