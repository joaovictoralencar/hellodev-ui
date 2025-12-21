using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Button))]
    public class UIButton : UISelectable
    {
        [SerializeField] protected bool DeselectOnClick;
        [SerializeField] protected Button _button;

        // Implement the Interactable property
        public override bool IsInteractable => _button && _button.interactable;

        // Expose the button's onClick event
        public Button.ButtonClickedEvent OnClick
        {
            get
            {
                if (_button == null) _button = GetComponent<Button>();
                return _button ? _button.onClick : null;
            }
        }

        protected override void Awake()
        {
            // Ensure button is assigned
            if (_button == null) 
                _button = GetComponent<Button>();
            EndPressing.AddListener(OnEndPressing);
            
            // Call base Awake
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EndPressing.RemoveListener(OnEndPressing);
        }

        private void OnEndPressing()
        {
            if (!DeselectOnClick) return;
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // Implementation of SetInteractable
        public override void SetInteractable(bool interactable)
        {
            if (_button == null) return;

            _button.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
        }

        // Optional debug text update (if needed)
        protected override void UpdateDebugText()
        {
            // Debug logging disabled in production
        }

        // Optional state-specific visual or behavioral modifications
        protected override void OnNormalState()
        {
        }

        protected override void OnHighlightedState()
        {
        }

        protected override void OnPressedState()
        {
        }

        protected override void OnDisabledState()
        {
        }
    }
}