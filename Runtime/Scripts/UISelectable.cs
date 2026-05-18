using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using HelloDev.Tweening;
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

        [SerializeField] protected SelectableState currentState;
        [SerializeField] protected bool debugMode = false;

        public SelectableState CurrentState => currentState;

        protected Vector3 originalScale;
        protected bool selected;
        protected bool mouseOver;
        protected bool pointerDown;

        // ── Events ───────────────────────────────────────────────────────────────

        public UnityEvent NormalStateEvent     = new();
        public UnityEvent SelectedStateEvent   = new();
        public UnityEvent HighlightedStateEvent = new();
        public UnityEvent PressedStateEvent    = new();
        public UnityEvent DisabledStateEvent   = new();
        public UnityEvent<SelectableState> ChangedStateEvent = new();
        public UnityEvent EndPressing { get; } = new UnityEvent();

        // ── Optional components ──────────────────────────────────────────────────

        private UIColourStyle _colourStyle;

        /// <summary>Public accessor for attached UIColourStyle. Controls should rely on the central style application in UpdateState()</summary>
        public UIColourStyle ColourStyle => _colourStyle;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        public abstract bool IsInteractable { get; }

        private bool _lastInteractable;

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
            originalScale = transform.localScale;
            _colourStyle = GetComponent<UIColourStyle>();
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

        // ── State machine ────────────────────────────────────────────────────────

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

        // ── Scale animation ──────────────────────────────────────────────────────

        private ITweenHandle _scaleTween;
        private Coroutine    _scaleCoroutine;

        private void ApplyScaleAnimation(SelectableState state)
        {
            if (_colourStyle == null || _colourStyle.Style == null) return;
            var style = _colourStyle.Style;
            if (!style.ScaleOnSelect) return;

            bool enlarged = state == SelectableState.Selected || state == SelectableState.Highlighted;
            var  target   = enlarged ? Vector3.one * style.ScaledSize : originalScale;

            // Skip the tween if we're already at the target scale — avoids a PrimeTween warning.
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
            if (_scaleTween != null) { _scaleTween.Kill(); _scaleTween = null; }
            if (_scaleCoroutine != null) { StopCoroutine(_scaleCoroutine); _scaleCoroutine = null; }
            if (TweenService.IsConfigured) TweenService.Provider.Kill(transform);
        }

        private IEnumerator LerpScale(Vector3 target, float duration)
        {
            var start   = transform.localScale;
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

        // ── Event system handlers ────────────────────────────────────────────────

        public virtual void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ChangeState(SelectableState.Selected);
            UIScroll.NotifySelected(gameObject);
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
            selected = false;
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            mouseOver = true;
            if (!IsInteractable) return;
            ChangeState(SelectableState.Highlighted);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            mouseOver = false;
            if (currentState == SelectableState.Pressed || !IsInteractable) return;
            ChangeState(selected ? SelectableState.Selected : SelectableState.Normal);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            ChangeState(SelectableState.Pressed);
            pointerDown = true;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (IsInteractable)
                ChangeState(SelectableState.Selected);
            pointerDown = false;
        }

        private Coroutine _pressStateCoroutine;

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (mouseOver && pointerDown) return;
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
                if (gameObject.activeInHierarchy && selected)
                    ChangeState(SelectableState.Selected);
                else if (gameObject.activeInHierarchy)
                    ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
                EndPressing?.Invoke();
            }
            finally
            {
                _pressStateCoroutine = null;
            }
        }

        // ── Overridable ──────────────────────────────────────────────────────────

        public abstract void SetInteractable(bool interactable);

        protected virtual void UpdateDebugText() { }

        protected virtual void OnNormalState()      { }
        protected virtual void OnSelectedState()    { }
        protected virtual void OnHighlightedState() { }
        protected virtual void OnPressedState()     { }
        protected virtual void OnDisabledState()    { }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying) return;
            // Refresh the colour style preview in edit mode
            _colourStyle = GetComponent<UIColourStyle>();
            _colourStyle?.Apply(currentState);
        }
#endif
    }
}
