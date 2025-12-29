using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Enhanced toggle component with keyboard/controller navigation support.
    /// Provides auto-selection on keyboard focus and visual feedback events
    /// that are decoupled from toggle state changes.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class UIToggle : UISelectable
    {
        #region Serialized Fields

        [Header("Toggle Settings")]
        [Space(10)]
        [SerializeField] protected Toggle _toggle;

        [Header("Keyboard Navigation")]
        [Tooltip("When true, navigating to this toggle via keyboard/controller will auto-select it.")]
        [SerializeField] private bool autoSelectOnKeyboardFocus = true;

        [Tooltip("When true, visual feedback will hide when keyboard focus moves to a non-sibling element.")]
        [SerializeField] private bool hideVisualOnFocusLostToNonSibling = true;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the toggle state changes to ON.
        /// Use for logic that should run once on selection.
        /// </summary>
        [Space(5)]
        public UnityEvent OnToggleOn = new();

        /// <summary>
        /// Fired when the toggle state changes to OFF.
        /// </summary>
        public UnityEvent OnToggleOff = new();

        /// <summary>
        /// Fired when visual feedback should be shown.
        /// This fires on toggle on AND when returning focus to an already-selected toggle.
        /// Use this for selection visuals that need to restore when refocusing.
        /// </summary>
        public UnityEvent OnShowVisualFeedback = new();

        /// <summary>
        /// Fired when visual feedback should be hidden.
        /// This fires on toggle off AND when keyboard focus leaves to a non-sibling.
        /// Use this for selection visuals that should hide when navigating away.
        /// </summary>
        public UnityEvent OnHideVisualFeedback = new();

        #endregion

        #region Properties

        public Toggle.ToggleEvent OnValueChanged => _toggle ? _toggle.onValueChanged : null;
        public override bool IsInteractable => _toggle && _toggle.interactable;
        public bool IsOn => _toggle && _toggle.isOn;
        public Toggle Toggle => _toggle;

        #endregion

        #region Private Fields

        private Coroutine _checkFocusCoroutine;
        private bool _isHandlingToggleChange;

        #endregion

        #region Unity Lifecycle

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
            if (_toggle == null)
                _toggle = GetComponent<Toggle>();

            if (_toggle)
                _toggle.onValueChanged.AddListener(HandleToggleValueChanged);

            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_toggle)
                _toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);

            if (_checkFocusCoroutine != null)
                StopCoroutine(_checkFocusCoroutine);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_checkFocusCoroutine != null)
            {
                StopCoroutine(_checkFocusCoroutine);
                _checkFocusCoroutine = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Programmatically set the toggle state.
        /// </summary>
        public void SetIsOn(bool value)
        {
            if (_toggle)
                _toggle.isOn = value;
        }

        public override void SetInteractable(bool interactable)
        {
            if (_toggle == null) return;

            _toggle.interactable = interactable;

            if (!interactable)
                ChangeState(SelectableState.Disabled);
            else
                ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
        }

        #endregion

        #region Private Methods - Toggle State

        private void HandleToggleValueChanged(bool isOn)
        {
            // Guard against re-entry to prevent stack overflow
            if (_isHandlingToggleChange) return;

            _isHandlingToggleChange = true;
            try
            {
                if (isOn)
                {
                    OnToggleOn.Invoke();
                    OnShowVisualFeedback.Invoke();
                }
                else
                {
                    OnToggleOff.Invoke();
                    OnHideVisualFeedback.Invoke();
                }

                UpdateState();
            }
            finally
            {
                _isHandlingToggleChange = false;
            }
        }

        #endregion

        #region State Handlers - Keyboard Navigation

        protected override void OnSelectedState()
        {
            HandleKeyboardFocus();
        }

        protected override void OnHighlightedState()
        {
            HandleKeyboardFocus();
        }

        protected override void OnNormalState()
        {
            HandleFocusLost();
        }

        protected override void OnPressedState()
        {
        }

        protected override void OnDisabledState()
        {
        }

        private void HandleKeyboardFocus()
        {
            if (!autoSelectOnKeyboardFocus) return;

            if (!IsOn)
            {
                // Auto-select when keyboard navigates here
                SetIsOn(true);
            }
            else
            {
                // Already selected - ensure visual feedback is shown
                // (it may have been hidden when navigating away previously)
                OnShowVisualFeedback.Invoke();
            }
        }

        private void HandleFocusLost()
        {
            if (!hideVisualOnFocusLostToNonSibling) return;
            if (!IsOn) return;

            // Wait one frame for EventSystem to update selection
            if (_checkFocusCoroutine != null)
                StopCoroutine(_checkFocusCoroutine);

            _checkFocusCoroutine = StartCoroutine(CheckFocusLostNextFrame());
        }

        private IEnumerator CheckFocusLostNextFrame()
        {
            yield return null;

            _checkFocusCoroutine = null;

            // Re-check if still toggled on
            if (!IsOn) yield break;

            var currentSelected = EventSystem.current?.currentSelectedGameObject;

            // If no selection, hide visual
            if (currentSelected == null)
            {
                OnHideVisualFeedback.Invoke();
                yield break;
            }

            // Check if new selection is a sibling (in same ToggleGroup)
            bool isSibling = IsSiblingSelection(currentSelected);

            if (!isSibling)
            {
                // Navigated to non-sibling - hide visual feedback
                // (toggle state remains on, but visual can hide)
                OnHideVisualFeedback.Invoke();
            }
        }

        /// <summary>
        /// Determines if the given selection is a sibling (should not trigger hide).
        /// Override this method for custom sibling detection logic.
        /// </summary>
        protected virtual bool IsSiblingSelection(GameObject selection)
        {
            if (selection == null) return false;

            // Check if selection is another UIToggle in the same ToggleGroup
            var otherToggle = selection.GetComponentInParent<UIToggle>();
            if (otherToggle == null) return false;

            // Same toggle group = sibling
            if (_toggle.group != null && otherToggle.Toggle?.group == _toggle.group)
                return true;

            return false;
        }

        #endregion
    }
}
