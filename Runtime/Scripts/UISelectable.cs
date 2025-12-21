using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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

        protected Vector3 originalScale;
        protected bool selected;
        protected bool mouseOver;
        protected bool pointerDown;

        // Events for each state
        public UnityEvent NormalStateEvent = new();
        public UnityEvent SelectedStateEvent = new();
        public UnityEvent HighlightedStateEvent = new();
        public UnityEvent PressedStateEvent = new();
        public UnityEvent DisabledStateEvent = new();
        public UnityEvent<SelectableState> ChangedStateEvent = new();

        // Abstract property for interactability to be implemented by child classes
        public abstract bool IsInteractable { get; }

        bool lastInteractable = false;

#if ODIN_INSPECTOR
        [Button]
#endif
        public void ToggleSetInteractable()
        {
            if (!Application.isPlaying) return;
            if (lastInteractable == IsInteractable) return;
            SetInteractable(IsInteractable);
            lastInteractable = IsInteractable;
        }

        protected virtual void Awake()
        {
            originalScale = transform.localScale;
            InitializeState();
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnEnable()
        {
            UpdateState();
        }

        protected virtual void InitializeState()
        {
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
                currentState = SelectableState.Selected;
            else
                currentState = IsInteractable ? SelectableState.Normal : SelectableState.Disabled;

            UpdateState();
        }

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

            ChangedStateEvent?.Invoke(currentState);
            if (debugMode) UpdateDebugText();
        }

        // Abstract or virtual methods to be overridden by child classes
        protected virtual void UpdateDebugText()
        {
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ChangeState(SelectableState.Selected);
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

        private Coroutine pressStateCoroutine;

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (mouseOver && pointerDown) return;

            // Cancel any existing coroutine first
            if (pressStateCoroutine != null)
            {
                StopCoroutine(pressStateCoroutine);
                pressStateCoroutine = null;
            }

            // Change state to Pressed
            ChangeState(SelectableState.Pressed);

            // Start new coroutine and store reference
            pressStateCoroutine = StartCoroutine(ReturnToSelectedState());
        }

        public UnityEvent EndPressing { get; } = new UnityEvent();

        private IEnumerator ReturnToSelectedState()
        {
            try
            {
                // Wait for a small amount of time to show the pressed visual state
                yield return new WaitForSeconds(0.1f);

                // Check if the object is still active and selected before changing state
                if (gameObject.activeInHierarchy && selected)
                {
                    ChangeState(SelectableState.Selected);
                }
                else if (gameObject.activeInHierarchy)
                {
                    // Reset to appropriate state if conditions changed
                    ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
                }

                EndPressing?.Invoke();
            }
            finally
            {
                // Always clear the coroutine reference
                pressStateCoroutine = null;
            }
        }

        protected virtual void OnDisable()
        {
            // Clean up if the GameObject is disabled
            if (pressStateCoroutine != null)
            {
                StopCoroutine(pressStateCoroutine);
                pressStateCoroutine = null;
            }
        }

        // Abstract methods to be implemented by child classes
        public abstract void SetInteractable(bool interactable);

        #region State Handlers

        protected virtual void OnNormalState()
        {
        }

        protected virtual void OnSelectedState()
        {
        }

        protected virtual void OnHighlightedState()
        {
        }

        protected virtual void OnPressedState()
        {
        }

        protected virtual void OnDisabledState()
        {
        }

        #endregion
    }
}