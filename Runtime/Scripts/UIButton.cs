using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Button))]
    public class UIButton : UISelectable
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [FoldoutGroup("Button Settings")]
#endif
        [SerializeField] protected bool DeselectOnClick;

#if ODIN_INSPECTOR
        [FoldoutGroup("Button Settings")]
#endif
        [SerializeField] protected Button _button;

        #endregion

        #region Properties

        public override bool IsInteractable => _button && _button.interactable;

        public Button.ButtonClickedEvent OnClick
        {
            get
            {
                if (_button == null) _button = GetComponent<Button>();
                return _button ? _button.onClick : null;
            }
        }

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            EndPressing.AddListener(OnEndPressing);

            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EndPressing.RemoveListener(OnEndPressing);
        }

        #endregion

        #region Private Methods

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
            if (_button == null) return;

            _button.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(IsHighlighted ? SelectableState.Highlighted : SelectableState.Normal);
        }

        protected override void UpdateDebugText() { }

        protected override void OnNormalState()      { }
        protected override void OnHighlightedState() { }
        protected override void OnPressedState()     { }
        protected override void OnDisabledState()    { }

        #endregion
    }
}