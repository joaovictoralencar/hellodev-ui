using HelloDev.Utils;
using HelloDev.Logging;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(TMP_InputField))]
    public class UIInputField : UISelectable
    {
        [SerializeField] protected TMP_InputField _inputField;

        // Events for input field interactions
        public UnityEvent<string> OnTextChanged = new();
        public UnityEvent<string> OnEndEdit = new();

        // Implement the Interactable property
        public override bool IsInteractable => _inputField && _inputField.interactable;

        // Properties to access input field state
        public string Text => _inputField ? _inputField.text : string.Empty;
        public bool IsFocused => _inputField && _inputField.isFocused;

        protected override void Awake()
        {
            if (_inputField == null)
                _inputField = GetComponent<TMP_InputField>();

            // UIColourStyle.Awake() auto-discovers the background Graphic via GetComponent,
            // which finds the TMP_InputField's targetGraphic automatically.

            if (_inputField)
            {
                _inputField.onValueChanged.SafeSubscribe(HandleTextChanged);
                _inputField.onEndEdit.SafeSubscribe(HandleEndEdit);
                _inputField.onSelect.SafeSubscribe(HandleSelect);
                _inputField.onDeselect.SafeSubscribe(HandleDeselect);
            }
            else
            {
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIInputField] TMP_InputField component missing on '{name}'. Input functionality will be limited.");
            }

            base.Awake();
        }

        private void HandleTextChanged(string newText)
        {
            OnTextChanged?.Invoke(newText);
            UpdateState();
        }

        private void HandleEndEdit(string finalText)
        {
            OnEndEdit?.Invoke(finalText);
        }

        private void HandleSelect(string selectedText)
        {
            ChangeState(SelectableState.Selected);
        }

        private void HandleDeselect(string deselectedText)
        {
            ChangeState(SelectableState.Normal);
        }

        // Implementation of SetInteractable
        public override void SetInteractable(bool interactable)
        {
            if (_inputField == null) return;

            _inputField.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
        }

        // Methods to interact with input field
        public void SetText(string text)
        {
            if (_inputField)
            {
                _inputField.text = text;
            }
            else
            {
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIInputField] SetText called but TMP_InputField missing on '{name}'");
            }
        }

        public void ActivateInputField()
        {
            if (_inputField)
                _inputField.ActivateInputField();
        }

        protected override void OnNormalState()
        {
        }

        protected override void OnSelectedState()
        {
        }

        protected override void OnHighlightedState()
        {
        }

        protected override void OnDisabledState()
        {
        }

        // Clean up listeners when object is destroyed
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_inputField)
            {
                _inputField.onValueChanged.SafeUnsubscribe(HandleTextChanged);
                _inputField.onEndEdit.SafeUnsubscribe(HandleEndEdit);
                _inputField.onSelect.SafeUnsubscribe(HandleSelect);
                _inputField.onDeselect.SafeUnsubscribe(HandleDeselect);
            }
        }
    }
}
