using System;
using System.Collections.Generic;
using HelloDev.Logging;
using HelloDev.UI.Default;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Individual popup instance. Uses UIContainer for show/hide.
    /// </summary>
    [RequireComponent(typeof(UIContainer))]
    public class UIPopup : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("LocalizeStringEvent for the title text.")]
        [SerializeField] private LocalizeStringEvent titleText;

        [Tooltip("LocalizeStringEvent for the message text.")]
        [SerializeField] private LocalizeStringEvent messageText;

        [Tooltip("Image for the popup icon.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Container for spawned buttons.")]
        [SerializeField] private Transform buttonContainer;

        [Tooltip("Prefab for popup buttons.")]
        [SerializeField] private UIButton buttonPrefab;

        [Header("Modal")]
        [Tooltip("CanvasGroup that blocks input behind the popup.")]
        [SerializeField] private CanvasGroup modalBlocker;

        [Header("Debug")]
        [SerializeField] private bool debug;

        #region Private Fields

        private UIContainer _container;
        private readonly List<UIButton> _spawnedButtons = new();
        private Action<int> _onResult;
        private int _cancelButtonIndex = -1;
        private int _defaultButtonIndex;

        #endregion

        #region Properties

        /// <summary>Gets the UIContainer component.</summary>
        public UIContainer Container => _container;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _container = GetComponent<UIContainer>();
        }

        #endregion

        #region Public API

        /// <summary>Sets up the popup from a ScriptableObject configuration.</summary>
        public void Setup(Popup_SO config, Action<int> onResult)
        {
            if (config == null)
            {
                Logger.LogError(HelloDev.Logging.UIConstants.System, "Cannot setup popup: config is null");
                return;
            }

            _onResult           = onResult;
            _defaultButtonIndex = config.defaultButtonIndex;
            _cancelButtonIndex  = config.cancelButtonIndex;

            if (titleText != null && config.title != null && !config.title.IsEmpty)
            {
                titleText.gameObject.SetActive(true);
                titleText.StringReference = config.title;
            }
            else if (titleText != null) titleText.gameObject.SetActive(false);

            if (messageText != null && config.message != null && !config.message.IsEmpty)
            {
                messageText.gameObject.SetActive(true);
                messageText.StringReference = config.message;
            }
            else if (messageText != null) messageText.gameObject.SetActive(false);

            if (iconImage != null)
            {
                if (config.icon != null) { iconImage.gameObject.SetActive(true); iconImage.sprite = config.icon; }
                else                       iconImage.gameObject.SetActive(false);
            }

            ClearButtons();

            if (config.buttons != null && config.buttons.Length > 0)
            {
                for (int i = 0; i < config.buttons.Length; i++)
                    CreateButton(i, config.buttons[i].label.GetLocalizedString());

                if (_cancelButtonIndex < 0)
                    _cancelButtonIndex = config.buttons.Length - 1;
            }

            SetDefaultSelection();

            if (debug) Logger.Log(HelloDev.Logging.UIConstants.System, "Popup setup from config: " + (config != null ? config.name : "null"));
        }

        /// <summary>Sets up the popup with runtime-provided strings.</summary>
        public void Setup(string title, string message, string[] buttonLabels, Action<int> onResult, int defaultIndex = 0, int cancelIndex = -1)
        {
            _onResult           = onResult;
            _defaultButtonIndex = defaultIndex;
            _cancelButtonIndex  = cancelIndex;

            if (titleText != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(title);
                titleText.gameObject.SetActive(hasTitle);
                if (hasTitle)
                {
                    titleText.StringReference.Clear();
                    titleText.GetComponent<TMP_Text>()?.SetText(title);
                }
            }

            if (messageText != null)
            {
                bool hasMsg = !string.IsNullOrEmpty(message);
                messageText.gameObject.SetActive(hasMsg);
                if (hasMsg)
                {
                    messageText.StringReference.Clear();
                    messageText.GetComponent<TMP_Text>()?.SetText(message);
                }
            }

            if (iconImage != null) iconImage.gameObject.SetActive(false);

            ClearButtons();

            if (buttonLabels != null && buttonLabels.Length > 0)
            {
                for (int i = 0; i < buttonLabels.Length; i++)
                    CreateButton(i, buttonLabels[i]);

                if (_cancelButtonIndex < 0)
                    _cancelButtonIndex = buttonLabels.Length - 1;
            }

            SetDefaultSelection();

            if (debug) Logger.Log(HelloDev.Logging.UIConstants.System, "Popup setup with title: " + (title ?? string.Empty));
        }

        /// <summary>Closes the popup with the specified button index result.</summary>
        public void Close(int buttonIndex)
        {
            if (debug) Logger.Log(HelloDev.Logging.UIConstants.System, "Popup closed with button index: " + buttonIndex);
            _container.Hide();
            _onResult?.Invoke(buttonIndex);
        }

        /// <summary>Handles cancel input by triggering the cancel button.</summary>
        public void HandleCancel()
        {
            if (_cancelButtonIndex >= 0 && _cancelButtonIndex < _spawnedButtons.Count)
            {
                if (debug) Logger.Log(HelloDev.Logging.UIConstants.System, "Popup HandleCancel -> button index: " + _cancelButtonIndex);
                Close(_cancelButtonIndex);
            }
            else if (_spawnedButtons.Count > 0)
            {
                int lastIndex = _spawnedButtons.Count - 1;
                if (debug) Logger.Log(HelloDev.Logging.UIConstants.System, "Popup HandleCancel -> fallback to last button: " + lastIndex);
                Close(lastIndex);
            }
        }

        /// <summary>Shows the popup.</summary>
        public void Show()
        {
            _container.Show();
            SetDefaultSelection();
        }

        #endregion

        #region Button Management

        private void CreateButton(int index, string label)
        {
            if (buttonPrefab == null || buttonContainer == null) return;

            var buttonGO = Instantiate(buttonPrefab.gameObject, buttonContainer);
            var button   = buttonGO.GetComponent<UIButton>();

            if (button != null)
            {
                var tmpText = buttonGO.GetComponentInChildren<TMP_Text>();
                if (tmpText != null) tmpText.text = label;

                int capturedIndex = index;
                button.OnClick.AddListener(() => Close(capturedIndex));
                _spawnedButtons.Add(button);

                if (debug) Logger.LogVerbose(HelloDev.Logging.UIConstants.System, "Created button [" + index + "]: " + (label ?? string.Empty));
            }
        }

        private void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                {
                    // Remove listeners by destroying the GameObject to avoid leaks.
                    Destroy(button.gameObject);
                }
            }
            _spawnedButtons.Clear();
        }

        private void SetDefaultSelection()
        {
            if (_defaultButtonIndex >= 0 && _defaultButtonIndex < _spawnedButtons.Count)
                _container.autoSelectable = _spawnedButtons[_defaultButtonIndex].GetComponent<Selectable>();
            else if (_spawnedButtons.Count > 0)
                _container.autoSelectable = _spawnedButtons[0].GetComponent<Selectable>();
        }

        #endregion

        /// <summary>
        /// Prepare popup for reuse by pooling: clear spawned buttons, callbacks, and modal state.
        /// </summary>
        public void ResetForReuse()
        {
            _onResult = null;
            _defaultButtonIndex = -1;
            _cancelButtonIndex = -1;
            ClearButtons();

            if (modalBlocker != null)
            {
                modalBlocker.alpha = 0f;
                modalBlocker.blocksRaycasts = false;
                modalBlocker.interactable = false;
                modalBlocker.gameObject.SetActive(false);
            }

            if (titleText != null) titleText.gameObject.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            if (iconImage != null) iconImage.gameObject.SetActive(false);
        }
    }
}

