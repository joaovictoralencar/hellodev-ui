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

        /// <summary>
        /// Gets the UIContainer component.
        /// </summary>
        public UIContainer Container => _container;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _container = GetComponent<UIContainer>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets up the popup from a ScriptableObject configuration.
        /// </summary>
        public void Setup(Popup_SO config, Action<int> onResult)
        {
            if (config == null)
            {
                Logger.LogError(LogSystems.UIPopup, "Cannot setup popup: config is null");
                return;
            }

            _onResult = onResult;
            _defaultButtonIndex = config.defaultButtonIndex;
            _cancelButtonIndex = config.cancelButtonIndex;

            // Set title
            if (titleText != null && config.title != null && !config.title.IsEmpty)
            {
                titleText.gameObject.SetActive(true);
                titleText.StringReference = config.title;
            }
            else if (titleText != null)
            {
                titleText.gameObject.SetActive(false);
            }

            // Set message
            if (messageText != null && config.message != null && !config.message.IsEmpty)
            {
                messageText.gameObject.SetActive(true);
                messageText.StringReference = config.message;
            }
            else if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }

            // Set icon
            if (iconImage != null)
            {
                if (config.icon != null)
                {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = config.icon;
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            // Create buttons
            ClearButtons();

            if (config.buttons != null && config.buttons.Length > 0)
            {
                for (int i = 0; i < config.buttons.Length; i++)
                {
                    var buttonConfig = config.buttons[i];
                    CreateButton(i, buttonConfig.label.GetLocalizedString());
                }

                // If cancelButtonIndex is -1, use last button
                if (_cancelButtonIndex < 0)
                {
                    _cancelButtonIndex = config.buttons.Length - 1;
                }
            }

            // Focus default button
            SetDefaultSelection();

            if (debug)
            {
                Logger.Log(LogSystems.UIPopup, $"Popup setup from config: {config.name}");
            }
        }

        /// <summary>
        /// Sets up the popup with runtime-provided strings.
        /// </summary>
        public void Setup(string title, string message, string[] buttonLabels, Action<int> onResult, int defaultIndex = 0, int cancelIndex = -1)
        {
            _onResult = onResult;
            _defaultButtonIndex = defaultIndex;
            _cancelButtonIndex = cancelIndex;

            // Set title
            if (titleText != null)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    titleText.gameObject.SetActive(true);
                    // Clear localization and set raw text
                    titleText.StringReference.Clear();
                    var tmpText = titleText.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.text = title;
                    }
                }
                else
                {
                    titleText.gameObject.SetActive(false);
                }
            }

            // Set message
            if (messageText != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    messageText.gameObject.SetActive(true);
                    messageText.StringReference.Clear();
                    var tmpText = messageText.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.text = message;
                    }
                }
                else
                {
                    messageText.gameObject.SetActive(false);
                }
            }

            // Hide icon for runtime popups
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            // Create buttons
            ClearButtons();

            if (buttonLabels != null && buttonLabels.Length > 0)
            {
                for (int i = 0; i < buttonLabels.Length; i++)
                {
                    CreateButton(i, buttonLabels[i]);
                }

                // If cancelIndex is -1, use last button
                if (_cancelButtonIndex < 0)
                {
                    _cancelButtonIndex = buttonLabels.Length - 1;
                }
            }

            // Focus default button
            SetDefaultSelection();

            if (debug)
            {
                Logger.Log(LogSystems.UIPopup, $"Popup setup with title: {title}");
            }
        }

        /// <summary>
        /// Closes the popup with the specified button index result.
        /// </summary>
        public void Close(int buttonIndex)
        {
            if (debug)
            {
                Logger.Log(LogSystems.UIPopup, $"Popup closed with button index: {buttonIndex}");
            }

            // Hide the container
            _container.Hide();

            // Invoke callback
            _onResult?.Invoke(buttonIndex);
        }

        /// <summary>
        /// Handles cancel input by triggering the cancel button.
        /// </summary>
        public void HandleCancel()
        {
            if (_cancelButtonIndex >= 0 && _cancelButtonIndex < _spawnedButtons.Count)
            {
                if (debug)
                {
                    Logger.Log(LogSystems.UIPopup, $"Popup HandleCancel → button index: {_cancelButtonIndex}");
                }
                Close(_cancelButtonIndex);
            }
            else if (_spawnedButtons.Count > 0)
            {
                // Fall back to last button
                int lastIndex = _spawnedButtons.Count - 1;
                if (debug)
                {
                    Logger.Log(LogSystems.UIPopup, $"Popup HandleCancel → fallback to last button: {lastIndex}");
                }
                Close(lastIndex);
            }
        }

        /// <summary>
        /// Shows the popup.
        /// </summary>
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
            var button = buttonGO.GetComponent<UIButton>();

            if (button != null)
            {
                // Set button label
                var tmpText = buttonGO.GetComponentInChildren<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = label;
                }

                // Wire up click handler
                int capturedIndex = index;
                button.OnClick.AddListener(() => Close(capturedIndex));

                _spawnedButtons.Add(button);

                if (debug)
                {
                    Logger.LogVerbose(LogSystems.UIPopup, $"Created button [{index}]: {label}");
                }
            }
        }

        private void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _spawnedButtons.Clear();
        }

        private void SetDefaultSelection()
        {
            if (_defaultButtonIndex >= 0 && _defaultButtonIndex < _spawnedButtons.Count)
            {
                var defaultButton = _spawnedButtons[_defaultButtonIndex];
                if (defaultButton != null)
                {
                    _container.autoSelectable = defaultButton.GetComponent<Selectable>();
                }
            }
            else if (_spawnedButtons.Count > 0)
            {
                _container.autoSelectable = _spawnedButtons[0].GetComponent<Selectable>();
            }
        }

        #endregion
    }
}
