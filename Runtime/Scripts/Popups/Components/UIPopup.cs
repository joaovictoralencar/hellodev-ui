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
        private Dictionary<string, AsyncOperationHandle<GameObject>> _buttonHandles = new();

        #region Lifecycle

        public override async Task Initialize(PopupRequest request)
        {
            Logging.Logger.LogVerbose("UI.PopUp", $"Initialising popup '{request.Title ?? request.LocalizedTitle?.GetLocalizedString()}'", this);
            Container.InstaHide();
            await base.Initialize(request);

            // Build the static content (title/message) and then the dynamic buttons
            BuildContent();
            await BuildButtons();

            Logging.Logger.Log("UI.PopUp", $"Popup initialised with {_buttons.Count} button(s)", this);
        }

        public override void Show()
        {
            base.Show();
            SetDefaultSelection();
            Logging.Logger.LogVerbose("UI.PopUp", $"Popup '{Id}' shown", this);
        }

        private void OnDestroy()
        {
            // Release any addressable button assets we loaded
            foreach (var kvp in _buttonHandles)
            {
                kvp.Value.Release();
            }
            _buttonHandles.Clear();
            Logging.Logger.LogVerbose("UI.PopUp", $"Popup '{Id}' destroyed, cleaned up button handles", this);
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
                    UIButton buttonInstance = null;

                    // Determine which prefab to use for this button
                    if (Request.Buttons[i].PrefabReference != null && Request.Buttons[i].PrefabReference.IsValid())
                    {
                        buttonInstance = await SpawnButtonAsync(Request.Buttons[i].PrefabReference);
                    }
                    else if (Request.Buttons[i].Prefab)
                    {
                        buttonInstance = Instantiate(Request.Buttons[i].Prefab, buttonContainer);
                    }
                    else if (defaultButtonReference != null && defaultButtonReference.IsValid())
                    {
                        buttonInstance = await SpawnButtonAsync(defaultButtonReference);
                    }
                    else if (defaultButtonPrefab != null)
                    {
                        buttonInstance = Instantiate(defaultButtonPrefab, buttonContainer);
                    }

                    if (buttonInstance == null) continue;

                    // Set the button's label (localized or fallback)
                    SetButtonLabel(buttonInstance, Request.Buttons[i]);

                    // Hook up the callback and optional auto-close
                    buttonInstance.OnClick.AddListener(() =>
                    {
                        Request.Buttons[index].Callback?.Invoke(this);
                        if (Request.Buttons[index].ClosesPopup) ClosePopUp();
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

        private void SetButtonLabel(UIButton btn, PopupButtonData requestButton)
        {
            var buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
            var localizeScriptEvent = btn.GetComponentInChildren<LocalizeStringEvent>();

            if (requestButton.LocalizedLabel != null)
                localizeScriptEvent.StringReference = requestButton.LocalizedLabel;
            else
                buttonText.text = requestButton.Label;
        }

        private async Task<UIButton> SpawnButtonAsync(AssetReference reference)
        {
            GameObject buttonInstance;

            // Cache the loaded asset so we can reuse the same handle for duplicates
            if (!_buttonHandles.ContainsKey(reference.RuntimeKey.ToString()))
            {
                var op = Addressables.LoadAssetAsync<GameObject>(reference);
                await op.Task;

                _buttonHandles.TryAdd(reference.RuntimeKey.ToString(), op);
                buttonInstance = Instantiate(op.Result, buttonContainer);
            }
            else
            {
                buttonInstance = Instantiate(_buttonHandles[reference.RuntimeKey.ToString()].Result, buttonContainer);
            }

            return buttonInstance.GetComponent<UIButton>();
        }

        private void SetDefaultSelection()
        {
            if (_buttons.Count == 0) return;
            Container.autoSelectable = _buttons[0].GetComponent<Selectable>();
        }

        #endregion

        #region Input Handling

        public override void HandleCancel()
        {
            Logging.Logger.LogVerbose("UI.PopUp", $"Popup '{Id}' received cancel, closing", this);
            ClosePopUp();
        }

        #endregion
    }
}