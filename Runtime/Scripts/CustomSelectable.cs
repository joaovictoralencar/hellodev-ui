using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Selectable))]
    public abstract class CustomSelectable : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerUpHandler,
        IPointerDownHandler
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
        [SerializeField] protected bool debugMode;

        [Header("Navigation")]
        [SerializeField] protected Selectable selectableComponent;

        protected bool selected;
        protected bool mouseOver;

        // Events for each state
        public UnityEvent NormalStateEvent = new();
        public UnityEvent SelectedStateEvent = new();
        public UnityEvent HighlightedStateEvent = new();
        public UnityEvent PressedStateEvent = new();
        public UnityEvent DisabledStateEvent = new();
        public UnityEvent<SelectableState> ChangedStateEvent = new();

        // Manual selection events
        public UnityEvent ManualSelectedEvent = new();
        public UnityEvent ManualDeselectedEvent = new();

        // Abstract property for interactability to be implemented by child classes
        public bool IsInteractable => selectableComponent.interactable;

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
            // Get the Selectable component
            if (selectableComponent == null)
                selectableComponent = GetComponent<Selectable>();

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

        #region Navigation Properties

        /// <summary>
        /// Get the Unity Selectable component for navigation
        /// </summary>
        public Selectable SelectableComponent => selectableComponent;
        
        #endregion

        #region Manual Selection Methods

        /// <summary>
        /// Manually select this component
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual void Select()
        {
            if (!IsInteractable) return;
            
            selected = true;
            ChangeState(SelectableState.Selected);
            
            // Also select the Unity Selectable for navigation
            if (selectableComponent != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            
            ManualSelectedEvent?.Invoke();
        }

        /// <summary>
        /// Manually deselect this component
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual void Deselect()
        {
            selected = false;
            ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
            
            // Also deselect from EventSystem if this object is currently selected
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            
            ManualDeselectedEvent?.Invoke();
        }

        /// <summary>
        /// Toggle selection state manually
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual void ToggleSelection()
        {
            if (selected)
                Deselect();
            else
                Select();
        }

        /// <summary>
        /// Check if this component is currently selected
        /// </summary>
        public bool IsSelected => selected;

        /// <summary>
        /// Get current state
        /// </summary>
        public SelectableState CurrentState => currentState;

        #endregion

        #region Pointer Events (Mouse/Touch interaction)

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            mouseOver = true;
            if (!IsInteractable) return;
            
            // Only change to highlighted if not already selected or pressed
            if (currentState != SelectableState.Selected && currentState != SelectableState.Pressed)
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
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable) return;

            // Auto-select on click if not already selected
            if (!selected)
            {
                Select();
            }
            else
            {
                ChangeState(SelectableState.Selected);
            }
        }

        #endregion

        #region Press Animation Methods

        private Coroutine pressStateCoroutine;

        /// <summary>
        /// Manually trigger press animation (useful for keyboard/gamepad input)
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual void TriggerPress()
        {
            if (!IsInteractable) return;

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

                // Check if the object is still active before changing state
                if (gameObject.activeInHierarchy && IsInteractable)
                {
                    if (selected)
                    {
                        ChangeState(SelectableState.Selected);
                    }
                    else
                    {
                        ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
                    }
                }

                EndPressing?.Invoke();
            }
            finally
            {
                // Always clear the coroutine reference
                pressStateCoroutine = null;
            }
        }

        #endregion

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
        public void SetInteractable(bool interactable)
        {
            selectableComponent.interactable = interactable;
            if (interactable)
            {
                ChangeState(mouseOver ? SelectableState.Highlighted : SelectableState.Normal);
            }
            else
                ChangeState(SelectableState.Disabled);
        }

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