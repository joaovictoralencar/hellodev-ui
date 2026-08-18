using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(InputField))]
    public class UIInput : UISelectable
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [FoldoutGroup("Input Settings")]
#endif
        [SerializeField] protected InputField _inputField;

#if ODIN_INSPECTOR
        [FoldoutGroup("Input Settings")]
#endif
        [SerializeField] protected bool DeselectOnSubmit;

        #endregion

        #region Properties

        public override bool IsInteractable => _inputField && _inputField.interactable;

        public string Text
        {
            get => _inputField ? _inputField.text : string.Empty;
            set
            {
                if (_inputField)
                    _inputField.text = value;
            }
        }

        public InputField.OnChangeEvent OnValueChanged
        {
            get
            {
                if (_inputField == null) _inputField = GetComponent<InputField>();
                return _inputField ? _inputField.onValueChanged : null;
            }
        }

        // Correct type: EndEditEvent, not SubmitEvent
        public InputField.EndEditEvent OnEndEdit
        {
            get
            {
                if (_inputField == null) _inputField = GetComponent<InputField>();
                return _inputField ? _inputField.onEndEdit : null;
            }
        }

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            if (_inputField == null)
                _inputField = GetComponent<InputField>();

            if (_inputField != null)
            {
                _inputField.onEndEdit.AddListener(HandleEndEdit);
            }

            EndPressing.AddListener(OnEndPressing);

            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_inputField != null)
                _inputField.onEndEdit.RemoveListener(HandleEndEdit);

            EndPressing.RemoveListener(OnEndPressing);
        }

        #endregion

        #region Private Methods

        private void HandleEndEdit(string value)
        {
            // Optionally handle submit logic (e.g., deselect, call custom events).
        }

        private void OnEndPressing()
        {
            if (!DeselectOnSubmit) return;
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        #endregion

        #region Overrides

        public override void SetInteractable(bool interactable)
        {
            if (_inputField == null) return;

            _inputField.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(IsHighlighted ? SelectableState.Highlighted : SelectableState.Normal);
        }

        // Input fields may want to stay highlighted while focused.
        protected override void OnSelectedState()
        {
            // Keep the selected state but also allow visual distinction if needed.
        }

        protected override void UpdateDebugText() { }

        protected override void OnNormalState()      { }
        protected override void OnHighlightedState() { }
        protected override void OnPressedState()     { }
        protected override void OnDisabledState()    { }

        #endregion
    }
}