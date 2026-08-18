using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using HelloDev.Tweening;
using UnityEngine.UI;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    public abstract class UISelectable : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerUpHandler,
        IPointerDownHandler,
        ISubmitHandler
    {
        public enum SelectableState
        {
            Normal,
            Selected,
            Highlighted,
            Pressed,
            Disabled
        }

        #region Serialized Fields

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Settings")]
#endif
        [SerializeField] protected SelectableState currentState;

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Settings")]
#endif
        [SerializeField] protected bool debugMode = false;

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Settings")]
#endif
        [Tooltip("For scale on hover animation, don't touch if not used")]
        [SerializeField] protected Vector3 originalScale = Vector3.one;

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Settings")]
#endif
        [Tooltip("If true, the selectable will be selected (receive focus) when the pointer hovers over it.")]
        [SerializeField] private bool selectOnHighlight = false;

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Settings")]
#endif
        [Tooltip("Optional reference to the underlying Unity Selectable. If not set, will be auto-detected.")]
        [SerializeField] private Selectable targetSelectable;

        #endregion

        #region Events

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent NormalStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent SelectedStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent HighlightedStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent PressedStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent DisabledStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent<SelectableState> ChangedStateEvent = new();

#if ODIN_INSPECTOR
        [FoldoutGroup("Selectable Events")]
#endif
        public UnityEvent EndPressing = new UnityEvent();

        #endregion

        #region Properties

        public SelectableState CurrentState => currentState;

        /// <summary>
        /// Cached reference to the underlying Unity Selectable component, if any.
        /// </summary>
        protected Selectable TargetSelectable => targetSelectable;

        /// <summary>
        /// Convenience property for child classes – true if the selectable is currently highlighted.
        /// </summary>
        protected bool IsHighlighted => targetSelectable != null && targetSelectable.IsHighlighted();

        protected bool selected;

        #endregion

        #region Private Fields

        private UIColourStyle _colourStyle;
        private bool _lastInteractable;
        private ITweenHandle _scaleTween;
        private Coroutine _scaleCoroutine;
        private Coroutine _pressStateCoroutine;

        #endregion

        #region Lifecycle

        public abstract bool IsInteractable { get; }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void ToggleSetInteractable()
        {
            if (!Application.isPlaying) return;
            if (_lastInteractable == IsInteractable) return;
            SetInteractable(IsInteractable);
            _lastInteractable = IsInteractable;
        }

        protected virtual void Awake()
        {
            _colourStyle = GetComponent<UIColourStyle>();
            if (targetSelectable == null)
                targetSelectable = GetComponent<Selectable>();
            InitializeState();
        }

        protected virtual void OnDestroy() { }

        protected virtual void OnEnable()
        {
            UpdateState();
        }

        protected virtual void OnDisable()
        {
            if (_pressStateCoroutine != null)
            {
                StopCoroutine(_pressStateCoroutine);
                _pressStateCoroutine = null;
            }
            KillScaleTween();
        }

        protected virtual void InitializeState()
        {
            currentState = (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
                ? SelectableState.Selected
                : IsInteractable ? SelectableState.Normal : SelectableState.Disabled;
            UpdateState();
        }

        #endregion

        #region State Machine

        protected void ChangeState(SelectableState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            UpdateState();
        }

        protected virtual void UpdateState()
        {
            switch (currentState)
            {
                case SelectableState.Normal:
                    OnNormalState();
                    NormalStateEvent?.Invoke();
                    break;
                case SelectableState.Selected:
                    OnSelectedState();
                    SelectedStateEvent?.Invoke();
                    break;
                case SelectableState.Highlighted:
                    OnHighlightedState();
                    HighlightedStateEvent?.Invoke();
                    break;
                case SelectableState.Pressed:
                    OnPressedState();
                    PressedStateEvent?.Invoke();
                    break;
                case SelectableState.Disabled:
                    OnDisabledState();
                    DisabledStateEvent?.Invoke();
                    break;
            }

            _colourStyle?.Apply(currentState);
            ApplyScaleAnimation(currentState);
            ChangedStateEvent?.Invoke(currentState);
            if (debugMode) UpdateDebugText();
        }

        #endregion

        #region Scale Animation

        private void ApplyScaleAnimation(SelectableState state)
        {
            if (_colourStyle == null || _colourStyle.Style == null) return;
            var style = _colourStyle.Style;
            if (!style.ScaleOnSelect) return;

            bool enlarged = state == SelectableState.Selected || state == SelectableState.Highlighted;
            var target = enlarged ? Vector3.one * style.ScaledSize : originalScale;

            if (transform.localScale == target) return;

            float duration = style.ScaleTime;
            KillScaleTween();

            if (TweenService.IsConfigured)
            {
                _scaleTween = TweenService.Provider.Scale(transform, target, duration);
            }
            else
            {
                _scaleCoroutine = StartCoroutine(LerpScale(target, duration));
            }
        }

        private void KillScaleTween()
        {
            if (_scaleTween != null)
            {
                _scaleTween.Kill();
                _scaleTween = null;
            }
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
            if (TweenService.IsConfigured)
                TweenService.Provider.Kill(transform);
        }

        private IEnumerator LerpScale(Vector3 target, float duration)
        {
            var start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.localScale = target;
            _scaleCoroutine = null;
        }

        #endregion

        #region Event System Handlers

        public virtual void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ChangeState(SelectableState.Selected);
            UIScroll.NotifySelected(gameObject);
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            // If the pointer is still over the selectable and it's interactable, highlight it.
            if (IsHighlighted && IsInteractable)
                ChangeState(SelectableState.Highlighted);
            else
                ChangeState(SelectableState.Normal);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable) return;

            ChangeState(SelectableState.Highlighted);

            // Optionally select the object when hovered.
            if (selectOnHighlight && !selected)
                Select();
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!IsInteractable) return;

            // If still being pressed (e.g., pointer dragged outside), wait for pointer up.
            if (targetSelectable != null && targetSelectable.IsPressed()) return;

            if (selected)
                ChangeState(SelectableState.Selected);
            else
                ChangeState(SelectableState.Normal);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            ChangeState(SelectableState.Pressed);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable) return;

            if (selected)
                ChangeState(SelectableState.Selected);
            else if (IsHighlighted)
                ChangeState(SelectableState.Highlighted);
            else
                ChangeState(SelectableState.Normal);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            // Ignore submit if already pressed via pointer.
            if (targetSelectable != null && targetSelectable.IsPressed() && targetSelectable.IsHighlighted())
                return;

            if (_pressStateCoroutine != null)
            {
                StopCoroutine(_pressStateCoroutine);
                _pressStateCoroutine = null;
            }

            ChangeState(SelectableState.Pressed);
            _pressStateCoroutine = StartCoroutine(ReturnToSelectedState());
        }

        private IEnumerator ReturnToSelectedState()
        {
            try
            {
                yield return new WaitForSeconds(0.1f);

                if (gameObject.activeInHierarchy)
                {
                    if (selected)
                        ChangeState(SelectableState.Selected);
                    else if (IsHighlighted)
                        ChangeState(SelectableState.Highlighted);
                    else
                        ChangeState(SelectableState.Normal);
                }
                EndPressing?.Invoke();
            }
            finally
            {
                _pressStateCoroutine = null;
            }
        }

        public void Select()
        {
            EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        #endregion

        #region Abstract and Virtual Methods

        public abstract void SetInteractable(bool interactable);

        protected virtual void UpdateDebugText() { }

        protected virtual void OnNormalState()      { }
        protected virtual void OnSelectedState()    { }
        protected virtual void OnHighlightedState() { }
        protected virtual void OnPressedState()     { }
        protected virtual void OnDisabledState()    { }

        #endregion

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying) return;
            _colourStyle = GetComponent<UIColourStyle>();
            _colourStyle?.Apply(currentState);
        }
#endif
    }
}