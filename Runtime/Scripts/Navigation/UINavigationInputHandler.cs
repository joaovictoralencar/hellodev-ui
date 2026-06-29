using HelloDev.UI.Default;
using HelloDev.UI.Popups;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Navigation
{
    /// <summary>
    /// Routes global Cancel input to the appropriate container.
    /// Single responsibility: Cancel → HandleBack()
    /// </summary>
    public class UINavigationInputHandler : MonoBehaviour
    {
        [Header("Input Bindings")]
        [Tooltip("Unique name for the cancel input action.")]
        [SerializeField] private string cancelActionName = "UI_Cancel";

        [Tooltip("Keyboard binding path for cancel.")]
        [SerializeField] private string keyboardCancel = "<Keyboard>/escape";

        [Tooltip("Gamepad binding path for cancel.")]
        [SerializeField] private string gamepadCancel = "<Gamepad>/buttonEast";

        [Header("References")]
        [Tooltip("Optional: UIPopupService to check for active popups.")]
        [SerializeField] private IUIPopupService popupService;

        [Header("Debug")]
        [SerializeField] private bool debug;

        #region Private Fields

#if ENABLE_INPUT_SYSTEM
        private InputAction _cancelAction;
#endif

        #endregion

        #region Unity Lifecycle

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            CreateCancelAction();
        }

        private void OnDisable()
        {
            DisposeCancelAction();
        }
#endif

        #endregion

        #region Input Action Management

#if ENABLE_INPUT_SYSTEM
        private void CreateCancelAction()
        {
            if (_cancelAction != null)
            {
                return;
            }

            // Create runtime action with both bindings
            _cancelAction = new InputAction(
                name: cancelActionName,
                type: InputActionType.Button
            );

            // Add keyboard binding
            if (!string.IsNullOrEmpty(keyboardCancel))
            {
                _cancelAction.AddBinding(keyboardCancel)
                    .WithGroup("Keyboard and Mouse");
            }

            // Add gamepad binding
            if (!string.IsNullOrEmpty(gamepadCancel))
            {
                _cancelAction.AddBinding(gamepadCancel)
                    .WithGroup("Gamepad");
            }

            // Subscribe to performed event
            _cancelAction.performed += OnCancelPerformed;

            // Enable the action
            _cancelAction.Enable();

            if (debug)
            {
                Logger.Log("UI", $"Cancel action created: keyboard={keyboardCancel}, gamepad={gamepadCancel}");
            }
        }

        private void DisposeCancelAction()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancelPerformed;
                _cancelAction.Disable();
                _cancelAction.Dispose();
                _cancelAction = null;

                if (debug)
                {
                    Logger.Log("UI", "Cancel action disposed");
                }
            }
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (debug)
            {
                Logger.Log("UI", "Cancel input performed");
            }

            // Check if popup service wants to handle it first (if assigned)
            if (popupService != null && popupService.HasActivePopup)
            {
                if (debug)
                {
                    Logger.Log("UI", "→ Routing to popup service");
                }
                popupService.HandleCancelInput();
                return;
            }

            // Route to current container
            var container = UIContainer.GetContainerForSelection();
            if (container != null)
            {
                if (debug)
                {
                    Logger.Log("UI", $"→ Routing to container: {container.gameObject.name}");
                }
                container.HandleBack();
            }
            else if (debug)
            {
                Logger.Log("UI", "→ No container found for current selection");
            }
        }
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually triggers the cancel action.
        /// Useful for testing or alternative input methods.
        /// </summary>
        public void TriggerCancel()
        {
#if ENABLE_INPUT_SYSTEM
            OnCancelPerformed(default);
#else
            OnCancelPerformed();
#endif
        }

        private void OnCancelPerformed()
        {
            if (debug)
            {
                Logger.Log("UI", "Cancel input performed (fallback)");
            }

            // Check if popup service wants to handle it first (if assigned)
            if (popupService != null && popupService.HasActivePopup)
            {
                if (debug)
                {
                    Logger.Log("UI", "→ Routing to popup service");
                }
                popupService.HandleCancelInput();
                return;
            }

            // Route to current container
            var container = UIContainer.GetContainerForSelection();
            if (container != null)
            {
                if (debug)
                {
                    Logger.Log("UI", $"→ Routing to container: {container.gameObject.name}");
                }
                container.HandleBack();
            }
            else if (debug)
            {
                Logger.Log("UI", "→ No container found for current selection");
            }
        }

        /// <summary>
        /// Sets the popup service reference at runtime.
        /// </summary>
        public void SetPopupService(UIPopupService service)
        {
            popupService = service;
        }

        #endregion
    }
}
