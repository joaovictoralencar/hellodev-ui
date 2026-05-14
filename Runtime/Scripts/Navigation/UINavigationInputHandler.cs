using HelloDev.Logging;
using HelloDev.UI.Default;
using HelloDev.UI.Popups;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Navigation
{
    /// <summary>
    /// Routes global Cancel input to the appropriate container.
    /// Requires Unity Input System. Only compiled when ENABLE_INPUT_SYSTEM is defined.
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
        [SerializeField] private UIPopupService popupService;

        [Header("Debug")]
        [SerializeField] private bool debug;

        private InputAction _cancelAction;

        private void OnEnable()
        {
            CreateCancelAction();
        }

        private void OnDisable()
        {
            DisposeCancelAction();
        }

        private void CreateCancelAction()
        {
            if (_cancelAction != null) return;

            _cancelAction = new InputAction(name: cancelActionName, type: InputActionType.Button);

            if (!string.IsNullOrEmpty(keyboardCancel))
                _cancelAction.AddBinding(keyboardCancel).WithGroup("Keyboard and Mouse");

            if (!string.IsNullOrEmpty(gamepadCancel))
                _cancelAction.AddBinding(gamepadCancel).WithGroup("Gamepad");

            _cancelAction.performed += OnCancelPerformed;
            _cancelAction.Enable();

            if (debug)
                Logger.Log("UI", $"Cancel action created: keyboard={keyboardCancel}, gamepad={gamepadCancel}");
        }

        private void DisposeCancelAction()
        {
            if (_cancelAction == null) return;

            _cancelAction.performed -= OnCancelPerformed;
            _cancelAction.Disable();
            _cancelAction.Dispose();
            _cancelAction = null;

            if (debug)
                Logger.Log("UI", "Cancel action disposed");
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (debug)
                Logger.Log("UI", "Cancel input performed");

            if (popupService != null && popupService.HasActivePopup)
            {
                if (debug) Logger.Log("UI", "→ Routing to popup service");
                popupService.HandleCancelInput();
                return;
            }

            var container = UIContainer.GetContainerForSelection();
            if (container != null)
            {
                if (debug) Logger.Log("UI", $"→ Routing to container: {container.gameObject.name}");
                container.HandleBack();
            }
            else if (debug)
            {
                Logger.Log("UI", "→ No container found for current selection");
            }
        }

        /// <summary>Manually triggers the cancel action. Useful for testing.</summary>
        public void TriggerCancel() => OnCancelPerformed(default);

        /// <summary>Sets the popup service reference at runtime.</summary>
        public void SetPopupService(UIPopupService service) => popupService = service;
    }
}
