using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggle : UISelectable
    {
        [Header("Toggle Settings"), Space(10), SerializeField] protected Toggle _toggle;

        // Expose the toggle's value changed event
        public Toggle.ToggleEvent OnValueChanged => _toggle ? _toggle.onValueChanged : null;

        // Implement the Interactable property
        public override bool IsInteractable => _toggle && _toggle.interactable;

        // Current toggle state
        public bool IsOn => _toggle && _toggle.isOn;

        public Toggle Toggle => _toggle;
        
        [Space(5)]
        public UnityEvent OnToggleOn = new();
        public UnityEvent OnToggleOff = new();
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (_toggle == null)
            {
                _toggle = GetComponentInChildren<Toggle>();
            }
        }
#endif

        protected override void Awake()
        {
            // Ensure toggle is assigned
            if (_toggle == null) 
                _toggle = GetComponent<Toggle>();

            // Add listener to track toggle state changes
            if (_toggle)
                _toggle.onValueChanged.AddListener(HandleToggleValueChanged);

            // Call base Awake
            base.Awake();
        }

        private void HandleToggleValueChanged(bool isOn)
        {
            // You can add custom logic here when toggle state changes
            // For example, changing visual state based on toggle state
            if (isOn)
                OnToggleOn.Invoke();
            else
                OnToggleOff.Invoke();
            UpdateState();
        }

        // Implementation of SetInteractable
        public override void SetInteractable(bool interactable)
        {
            if (_toggle == null) return;

            _toggle.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
        }

        protected override void OnNormalState()
        {
        }

        protected override void OnHighlightedState()
        {
        }

        protected override void OnSelectedState()
        {
        }

        protected override void OnPressedState()
        {
        }

        protected override void OnDisabledState()
        {
        }

        // Optional method to programmatically set toggle state
        public void SetIsOn(bool value)
        {
            if (_toggle)
                _toggle.isOn = value;
        }

        // Clean up listener when object is destroyed
        protected override void OnDestroy()
        {
            if (_toggle)
                _toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }
}