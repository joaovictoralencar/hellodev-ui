using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggle : UISelectable
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [FoldoutGroup("Toggle Settings")]
#endif
        [SerializeField] protected Toggle _toggle;

#if ODIN_INSPECTOR
        [FoldoutGroup("Toggle Settings")]
#endif
        [SerializeField] protected bool DeselectOnClick;

        #endregion

        #region Properties

        public override bool IsInteractable => _toggle && _toggle.interactable;

        public bool IsOn
        {
            get => _toggle ? _toggle.isOn : false;
            set
            {
                if (_toggle)
                    _toggle.isOn = value;
            }
        }

        public Toggle.ToggleEvent OnValueChanged
        {
            get
            {
                if (_toggle == null) _toggle = GetComponent<Toggle>();
                return _toggle ? _toggle.onValueChanged : null;
            }
        }

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            if (_toggle == null)
                _toggle = GetComponent<Toggle>();

            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(HandleToggleValueChanged);
            }

            EndPressing.AddListener(OnEndPressing);

            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);

            EndPressing.RemoveListener(OnEndPressing);
        }

        #endregion

        #region Private Methods

        private void HandleToggleValueChanged(bool isOn)
        {
            // Optionally update visual state or other logic when toggle value changes.
            // The visual state is already managed by UISelectable's state machine.
        }

        private void OnEndPressing()
        {
            if (!DeselectOnClick) return;
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        #endregion

        #region Overrides

        public override void SetInteractable(bool interactable)
        {
            if (_toggle == null) return;

            _toggle.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(IsHighlighted ? SelectableState.Highlighted : SelectableState.Normal);
        }

        protected override void OnSelectedState()
        {
            // Toggle might not need special selected state visual; but we can adjust if needed.
        }

        protected override void UpdateDebugText() { }

        protected override void OnNormalState()      { }
        protected override void OnHighlightedState() { }
        protected override void OnPressedState()     { }
        protected override void OnDisabledState()    { }

        #endregion
    }
}