using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Default implementation of a popup with a title, message, and a dynamic list of buttons.
    /// </summary>
    public class UIPopup : BaseUIPopup
    {
        [Header("UI")]
        [SerializeField] private LocalizeStringEvent titleText;
        [SerializeField] private LocalizeStringEvent messageText;
        [SerializeField] private Transform buttonContainer;

        [FormerlySerializedAs("buttonPrefab")]
        [SerializeField] private UIButton defaultButtonPrefab;
        [SerializeField] private AssetReference defaultButtonReference;

        private readonly List<UIButton> _buttons = new();
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _buttonHandles = new();

        #region Lifecycle

        /// <summary>
        /// Initialises the popup: builds content and spawns buttons.
        /// </summary>
        public override async Task Initialize(PopupRequest request)
        {
            Logging.Logger.LogVerbose("UI.PopUp", $"Initialising popup '{request.Title ?? request.LocalizedTitle?.GetLocalizedString()}'", this);
            Container.InstaHide();
            await base.Initialize(request);

            BuildContent();
            await BuildButtons();

            Logging.Logger.Log("UI.PopUp", $"Popup initialised with {_buttons.Count} button(s)", this);
        }

        /// <summary>
        /// Shows the popup and sets the default selected button.
        /// </summary>
        public override void Show()
        {
            base.Show();
            SetDefaultSelection();
            Logging.Logger.LogVerbose("UI.PopUp", "Popup shown", this);
        }

        /// <summary>
        /// Releases all addressable button assets when the popup is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            foreach (var kvp in _buttonHandles)
            {
                kvp.Value.Release();
                Logging.Logger.LogVerbose("UI.PopUp", "Released button asset handle", this);
            }
            _buttonHandles.Clear();
            Logging.Logger.LogVerbose("UI.PopUp", "Popup destroyed, cleaned up button handles", this);
        }

        #endregion

        #region Content Builders

        /// <summary>
        /// Fills the title and message fields from the request, using either
        /// static text or localized strings.
        /// </summary>
        private void BuildContent()
        {
            if (Request.LocalizedTitle != null)
            {
                titleText.StringReference = Request.LocalizedTitle;
                titleText.gameObject.SetActive(true);
            }
            else if (!string.IsNullOrEmpty(Request.Title))
            {
                titleText.GetComponent<TextMeshProUGUI>().text = Request.Title;
                titleText.gameObject.SetActive(true);
            }
            else
            {
                titleText.gameObject.SetActive(false);
            }

            if (Request.LocalizedMessage != null)
            {
                messageText.StringReference = Request.LocalizedMessage;
                messageText.gameObject.SetActive(true);
            }
            else if (!string.IsNullOrEmpty(Request.Message))
            {
                messageText.GetComponent<TextMeshProUGUI>().text = Request.Message;
                messageText.gameObject.SetActive(true);
            }
            else
            {
                messageText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Creates all buttons defined in the request, using prefabs or addressable assets.
        /// If no custom button is provided, falls back to the default button.
        /// </summary>
        private async Task BuildButtons()
        {
            try
            {
                buttonContainer.DestroyAllChildren();
                _buttons.Clear();

                if (Request.Buttons == null) return;

                for (int i = 0; i < Request.Buttons.Count; i++)
                {
                    int index = i;
                    var buttonData = Request.Buttons[i];

                    // Get the prefab (either direct or loaded via Addressables)
                    GameObject prefab = await GetButtonPrefabAsync(buttonData);
                    if (prefab == null) continue;

                    UIButton buttonInstance = Instantiate(prefab, buttonContainer).GetComponent<UIButton>();
                    if (buttonInstance == null)
                    {
                        Logging.Logger.LogWarning("UI.PopUp", "Instantiated button has no UIButton component", this);
                        continue;
                    }

                    // Set the label (localized or fallback)
                    SetButtonLabel(buttonInstance, buttonData);

                    // Hook up callback and optional auto-close
                    buttonInstance.OnClick.AddListener(() =>
                    {
                        buttonData.Callback?.Invoke(this);
                        if (buttonData.ClosesPopup) ClosePopUp();
                    });

                    _buttons.Add(buttonInstance);
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.LogError("UI.PopUp", $"Failed to build buttons: {ex.Message}", this);
                throw new Exception($"Failed to build buttons. Message: {ex.Message} \nStacktrace: {ex.StackTrace}", ex);
            }
        }

        #endregion

        #region Button Helpers

        /// <summary>
        /// Determines which prefab to use for a button and loads it (if addressable).
        /// Handles fallback to default button prefab/reference.
        /// </summary>
        private async Task<GameObject> GetButtonPrefabAsync(PopupButtonData buttonData)
        {
            // 1. Try the button's own addressable reference
            if (buttonData.PrefabReference.RuntimeKeyIsValid())
                return await LoadButtonAssetAsync(buttonData.PrefabReference);

            // 2. Try the button's own direct prefab
            if (buttonData.Prefab != null)
                return buttonData.Prefab.gameObject;

            // 3. Fallback to default addressable reference
            if (defaultButtonReference.RuntimeKeyIsValid())
                return await LoadButtonAssetAsync(defaultButtonReference);

            // 4. Final fallback to default direct prefab
            return defaultButtonPrefab != null ? defaultButtonPrefab.gameObject : null;
        }

        /// <summary>
        /// Loads a button asset via Addressables and caches the handle so we can release it later.
        /// </summary>
        private async Task<GameObject> LoadButtonAssetAsync(AssetReference reference)
        {
            string key = reference.RuntimeKey.ToString();

            Logging.Logger.LogVerbose("UI.PopUp", "Loading button asset from addressables", this);

            if (!_buttonHandles.TryGetValue(key, out var handle))
            {
                handle = Addressables.LoadAssetAsync<GameObject>(reference);
                await handle.Task;
                _buttonHandles[key] = handle;
                Logging.Logger.LogVerbose("UI.PopUp", "Button asset loaded and cached", this);
            }

            return handle.Result;
        }

        /// <summary>
        /// Sets the button's text using either the localized string or the fallback label.
        /// Retrieves both possible components once to avoid repeated GetComponent calls.
        /// </summary>
        private void SetButtonLabel(UIButton btn, PopupButtonData buttonData)
        {
            var buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
            var localizeScript = btn.GetComponentInChildren<LocalizeStringEvent>();

            if (buttonData.LocalizedLabel != null && localizeScript != null)
                localizeScript.StringReference = buttonData.LocalizedLabel;
            else if (buttonText != null)
                buttonText.text = buttonData.Label;
            else
                Logging.Logger.LogWarning("UI.PopUp", "Button has no TextMeshProUGUI or LocalizeStringEvent to set label", this);
        }

        /// <summary>
        /// Sets the first button as the default selectable for gamepad/keyboard navigation.
        /// </summary>
        private void SetDefaultSelection()
        {
            if (_buttons.Count == 0) return;
            Container.autoSelectable = _buttons[0].GetComponent<Selectable>();
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handles cancellation input – closes the popup.
        /// </summary>
        public override void HandleCancel()
        {
            Logging.Logger.LogVerbose("UI.PopUp", "Popup received cancel, closing", this);
            ClosePopUp();
        }

        #endregion
    }
}