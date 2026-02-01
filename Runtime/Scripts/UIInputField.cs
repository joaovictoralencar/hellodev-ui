using HelloDev.Utils;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

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
            // Ensure input field is assigned
            if (_inputField == null)
                _inputField = GetComponent<TMP_InputField>();

            // Add listeners for input field events
            if (_inputField)
            {
                _inputField.onValueChanged.SafeSubscribe(HandleTextChanged);
                _inputField.onEndEdit.SafeSubscribe(HandleEndEdit);
                _inputField.onSelect.SafeSubscribe(HandleSelect);
                _inputField.onDeselect.SafeSubscribe(HandleDeselect);
            }

            // Call base Awake
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
                _inputField.text = text;
        }

        public void ActivateInputField()
        {
            if (_inputField)
                _inputField.ActivateInputField();
        }

        protected override void OnNormalState()
        {
            // Default visual state
            if (_inputField != null && _inputField.targetGraphic != null)
                _inputField.targetGraphic.color = Color.white;
        }

        protected override void OnSelectedState()
        {
            // Visual feedback when selected
            if (_inputField != null && _inputField.targetGraphic != null)
                _inputField.targetGraphic.color = Color.cyan;
        }

        protected override void OnHighlightedState()
        {
            // Visual feedback when highlighted
            if (_inputField != null && _inputField.targetGraphic != null)
                _inputField.targetGraphic.color = Color.yellow;
        }

        protected override void OnDisabledState()
        {
            // Visual feedback when disabled
            if (_inputField != null && _inputField.targetGraphic != null)
                _inputField.targetGraphic.color = Color.gray;
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