using System;
using System.Collections.Generic;
using HelloDev.Logging;
using HelloDev.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Base container for UI panels with show/hide animations and navigation support.
    ///
    /// Navigation:
    /// - Unity's built-in navigation handles Up/Down/Left/Right between selectables
    /// - This class handles panel-level navigation (Cancel/Back to parent panel)
    /// - Set up Navigation mode on selectables (Automatic or Explicit) for within-panel nav
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UIContainer : MonoBehaviour
    {
        #region Static Panel Registry

        private static readonly List<UIContainer> _activeContainers = new();
        private static readonly Dictionary<Transform, int> _depthCache = new();

        /// <summary>
        /// All currently active (visible) containers.
        /// </summary>
        public static IReadOnlyList<UIContainer> ActiveContainers => _activeContainers;

        /// <summary>
        /// Finds the most specific (innermost) container that owns the currently selected GameObject.
        /// When containers are nested, returns the deepest one containing the selection.
        /// </summary>
        public static UIContainer GetContainerForSelection()
        {
            if (EventSystem.current == null) return null;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return null;

            UIContainer bestMatch = null;
            int bestDepth = -1;

            foreach (var container in _activeContainers)
            {
                if (selected.transform.IsChildOf(container.transform))
                {
                    // Count depth - deeper containers have more parents (cached)
                    int depth = GetTransformDepthCached(container.transform);
                    if (depth > bestDepth)
                    {
                        bestDepth = depth;
                        bestMatch = container;
                    }
                }
            }
            return bestMatch;
        }

        private static int GetTransformDepthCached(Transform t)
        {
            if (_depthCache.TryGetValue(t, out int cachedDepth))
                return cachedDepth;

            int depth = 0;
            Transform current = t;
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            _depthCache[t] = depth;
            return depth;
        }

        /// <summary>
        /// Clears the transform depth cache. Called when hierarchy might have changed.
        /// </summary>
        public static void ClearDepthCache()
        {
            _depthCache.Clear();
        }

        #endregion

        public string ID;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ID)) ID = gameObject.name;
        }
#endif

        public enum StartAction
        {
            DoNothing,
            Show,
            Hide,
            InstaShow,
            InstaHide
        }

        [Header("Navigation")]
        [Tooltip("Button that triggers Cancel/Back behavior")]
        [SerializeField] private UIButton backButton;

        [Tooltip("Container to focus when Cancel/Back is pressed (null = hide this container)")]
        [SerializeField] private UIContainer parentContainer;

        [Header("Start Action")]
        public StartAction onStartAction = StartAction.DoNothing;

        [Header("Animation Settings")]
        [SerializeField] private float openDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.3f;
        public EaseType openEase = EaseType.OutQuad;
        public EaseType hideEase = EaseType.InQuad;
        [SerializeField] private bool unscaledTime = false;

        [Header("Interaction Settings")]
        public bool disableInteractionsWhenHidden = true;

        [Header("Auto-Select Settings")]
        [Tooltip("Selectable to focus when this container is shown")]
        public Selectable autoSelectable;

        [Tooltip("Remember last selected element and restore on re-show")]
        [SerializeField] private bool rememberSelection = true;

        [Header("Close")]
        [SerializeField] private UIButton[] closeButtons;

        [Header("Debug")]
        [SerializeField] internal bool debug = false;

        [Header("Callbacks")]
        public UnityEvent onShow;
        public UnityEvent onHide;
        public UnityEvent onStartHide;
        public UnityEvent onStartShow;

        #region Properties

        public float OpenDuration => openDuration;
        public float HideDuration => hideDuration;
        public UIContainerGroup Group { get; private set; }
        public UIContainer ParentContainer => parentContainer;

        public Canvas Canvas => canvas ??= GetComponent<Canvas>();
        private Canvas canvas;

        public CanvasGroup CanvasGroup => canvasGroup ??= GetComponent<CanvasGroup>();
        private CanvasGroup canvasGroup;

        #endregion

        #region Private Fields

        private ITweenHandle fadeTween;
        private bool animationInProgress = false;
        private bool hasPendingShow = false;
        private bool hasPendingHide = false;
        private bool pendingInstant = false;
        private Action pendingShowCallback = null;
        private GameObject _lastSelectedObject;

        // Tracks settled visibility state manually.
        // True only after a show animation completes or InstaShow runs.
        // False after hide completes, InstaHide, or external deactivation via OnDisable.
        // NOT true during animations — use IsAnimating() for that.
        private bool _isVisible = false;

        #endregion

        protected virtual void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();

            foreach (var btn in closeButtons)
            {
                if (btn != null)
                    btn.OnClick.AddListener(() => Hide());
            }

            if (backButton != null)
                backButton.OnClick.AddListener(HandleBack);
        }

        protected virtual void Start()
        {
            switch (onStartAction)
            {
                case StartAction.InstaHide: InstaHide(); break;
                case StartAction.InstaShow: InstaShow(); break;
                case StartAction.Hide: Hide(); break;
                case StartAction.Show: Show(); break;
            }
        }

        protected virtual void OnDestroy()
        {
            _activeContainers.Remove(this);
            _depthCache.Remove(transform);
            KillAnimation();
        }

        public void InstaHide(bool fromGroup = false, bool invokeCallbacks = true)
        {
            KillAnimation();

            if (Group && !fromGroup)
            {
                Logger.LogWarning("UI", "This container has a group. Use group methods to hide it instead.");
                return;
            }

            // Remember selection before hiding
            RememberCurrentSelection();

            // Remove from active containers
            _activeContainers.Remove(this);

            // Mark as hidden before SetActive(false) so OnDisable doesn't race
            _isVisible = false;

            // Apply hidden state
            CanvasGroup.alpha = 0f;
            if (disableInteractionsWhenHidden)
            {
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            Canvas.enabled = false;
            gameObject.SetActive(false);

            if (debug)
            {
                if (Group != null)
                    Debug.Log($"<color=orange>[UIContainer] Group [{Group.gameObject.name}] started INSTA hiding {gameObject.name}</color>", gameObject);
                else
                    Debug.Log($"<color=orange>[UIContainer] ({gameObject.name}) started INSTA hiding</color>", gameObject);
            }

            if (invokeCallbacks)
            {
                onStartHide?.Invoke();
                onHide?.Invoke();
            }

            hasPendingShow = false;
            hasPendingHide = false;
            pendingShowCallback = null;
        }

        public void InstaShow(bool fromGroup = false, bool invokeCallbacks = true, Action onShowCallback = null)
        {
            // Cancel any pending animations
            KillAnimation();

            // If we're already settled-visible and callbacks aren't needed, skip
            if (_isVisible && !invokeCallbacks && onShowCallback == null)
                return;

            if (Group && !fromGroup)
            {
                Logger.LogWarning("UI", "This container has a group. Use group methods to show it instead.");
                return;
            }

            // Apply visible state
            gameObject.SetActive(true);
            Canvas.enabled = true;
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;

            // Settled: fully visible, no animation
            _isVisible = true;

            // Add to active containers
            if (!_activeContainers.Contains(this))
                _activeContainers.Add(this);

            AutoSelect();
            if (debug && Group != null)
            {
                Debug.Log($"<color=cyan>[UIContainer] Group [{Group.gameObject.name}] started INSTA SHOW {gameObject.name}</color>", gameObject);
            }

            if (invokeCallbacks)
            {
                onStartShow?.Invoke();
                onShow?.Invoke();
            }

            onShowCallback?.Invoke();

            // Clear pending actions
            hasPendingShow = false;
            hasPendingHide = false;
            pendingShowCallback = null;
        }

        protected virtual void OnEnable()
        {
            // Check if we have a pending show.
            // Guard: if already visible (e.g. GO was re-enabled externally while settled),
            // skip to avoid re-triggering show callbacks unnecessarily.
            if (hasPendingShow && !_isVisible)
            {
                Show(Group != null, true, pendingShowCallback);
                hasPendingShow = false;
                pendingShowCallback = null;
            }
        }

        protected virtual void OnDisable()
        {
            // If we have an animation in progress, kill it
            KillAnimation();

            // Handle external deactivation (e.g. parent GO disabled, scene unload).
            // InstaHide sets this before calling SetActive(false), so this is a no-op
            // in the normal flow but catches any external disable.
            if (_isVisible) hasPendingShow = true;
            _isVisible = false;
        }

        public void ShowContainer(bool invokeCallbacks = true)
        {
            if (Group != null) Group.ShowContainer(this);
            else Show(Group != null, invokeCallbacks, null);
        }

        public void HideContainer(bool invokeCallbacks = true)
        {
            Hide(Group != null, invokeCallbacks);
        }

        public void Show(bool fromGroup = false, bool invokeCallbacks = true, Action onShowCallback = null)
        {   
            // Kill any existing animation
            KillAnimation();
            
            // GO is inactive or canvas disabled — can't tween yet.
            // Queue as pending: OnEnable will resume once the GO is active.
            // Note: this is intentionally NOT checking _isVisible, because a hide animation
            // interrupted by KillAnimation() leaves the GO active but _isVisible = false.
            // In that case we want to fall through and animate directly, not re-queue.
            if (!gameObject.activeSelf || !Canvas.enabled)
            {
                hasPendingShow = true;
                pendingInstant = false;
                Canvas.enabled = true;
                pendingShowCallback = onShowCallback;
                
                // Enable the gameObject to trigger OnEnable where we'll resume
                gameObject.SetActive(true);
                if (invokeCallbacks) onStartShow?.Invoke();
                return;
            }

            if (Group && !fromGroup)
            {
                Logger.LogWarning("UI", "This container has a group. Use group methods to show it instead.");
                return;
            }

            // Set animation in progress flag
            animationInProgress = true;

            // Make container visible
            gameObject.SetActive(true);
            Canvas.enabled = true;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.alpha = 0;

            // Add to active containers immediately (so Cancel works during animation)
            if (!_activeContainers.Contains(this))
                _activeContainers.Add(this);

            if (debug)
            {
                if (Group != null)
                    Debug.Log($"<color=cyan>[UIContainer] Group [{Group.gameObject.name}] started opening {gameObject.name}</color>", gameObject);
                else
                    Debug.Log($"<color=cyan>[UIContainer] ({gameObject.name}) started opening</color>", gameObject);
            }

            if (invokeCallbacks) onStartShow?.Invoke();
            // Start fade in animation
            fadeTween = TweenService.Provider.Fade(CanvasGroup, 1f, openDuration)
                .SetEase(openEase)
                .SetUpdate(unscaledTime)
                .OnComplete(() =>
                {
                    animationInProgress = false;

                    // Settled: animation done, container is fully visible
                    _isVisible = true;

                    if (debug && Group != null)
                    {
                        Debug.Log($"<color=cyan>UIContainerGroup [{Group.GroupID}] opened {ID}</color>", gameObject);
                    }

                    if (invokeCallbacks) onShow?.Invoke();
                    onShowCallback?.Invoke();
                    AutoSelect();

                    // Check if we received a hide request during animation
                    if (hasPendingHide)
                    {
                        hasPendingHide = false;
                        Hide(Group != null, invokeCallbacks);
                    }
                });
        }

        public void Hide(bool fromGroup = false, bool invokeCallbacks = true)
        {
            // Kill any existing animation
            KillAnimation();
            
            // Not in a settled-visible state — nothing animated to hide.
            // InstaHide cleans up any partial state (alpha, canvas, GO active).
            if (!_isVisible)
            {
                InstaHide(fromGroup, invokeCallbacks);
                return;
            }

            if (Group && !fromGroup)
            {
                Logger.LogWarning("UI", "This container has a group. Use group methods to hide it instead.");
                return;
            }

            // If animation is in progress, mark as pending and return
            if (animationInProgress)
            {
                hasPendingHide = true;
                return;
            }

            // Remember selection before hiding
            RememberCurrentSelection();

            // Remove from active containers
            _activeContainers.Remove(this);

            // Set animation in progress
            animationInProgress = true;

            // Disable interactions if needed
            if (disableInteractionsWhenHidden)
            {
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            if (debug)
            {
                if (Group != null)
                    Debug.Log($"<color=orange>[UIContainer] Group [{Group.gameObject.name}] started hiding {gameObject.name}</color>", gameObject);
                else
                    Debug.Log($"<color=orange>[UIContainer] ({gameObject.name}) started hiding</color>", gameObject);
            }

            if (invokeCallbacks) onStartHide?.Invoke();
            // Start fade out animation
            fadeTween = TweenService.Provider.Fade(CanvasGroup, 0f, hideDuration)
                .SetEase(hideEase)
                .SetUpdate(unscaledTime)
                .OnComplete(() =>
                {
                    animationInProgress = false;

                    // Settled: animation done, container is fully hidden
                    _isVisible = false;

                    Canvas.enabled = false;
                    gameObject.SetActive(false);

                    if (debug && Group != null)
                    {
                        Debug.Log($"<color=orange>UIContainerGroup [{Group.GroupID}] hid [{ID}]</color>", gameObject);
                    }

                    if (invokeCallbacks) onHide?.Invoke();

                    // Check if we received a show request during animation
                    if (hasPendingShow)
                    {
                        bool isFromGroup = Group != null;
                        if (pendingInstant)
                        {
                            InstaShow(isFromGroup, true, pendingShowCallback);
                        }
                        else
                        {
                            Show(isFromGroup, true, pendingShowCallback);
                        }

                        hasPendingShow = false;
                        pendingShowCallback = null;
                    }
                });
        }

        private void KillAnimation()
        {
            if (fadeTween != null)
            {
                fadeTween.Kill();
                fadeTween = null;
            }

            animationInProgress = false;
        }

        private void AutoSelect()
        {
            if (EventSystem.current == null) return;

            // First, try to restore remembered selection
            if (rememberSelection && _lastSelectedObject != null &&
                _lastSelectedObject.activeInHierarchy &&
                _lastSelectedObject.transform.IsChildOf(transform))
            {
                var selectable = _lastSelectedObject.GetComponent<Selectable>();
                if (selectable != null && selectable.interactable)
                {
                    if (debug)
                        Debug.Log($"<color=green>[UIContainer] ({gameObject.name}) AutoSelect → restoring: {_lastSelectedObject.name}</color>", gameObject);

                    EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
                    return;
                }
            }

            // Fall back to autoSelectable
            if (autoSelectable != null && autoSelectable.gameObject.activeInHierarchy && autoSelectable.interactable)
            {
                if (debug)
                    Debug.Log($"<color=green>[UIContainer] ({gameObject.name}) AutoSelect → default: {autoSelectable.gameObject.name}</color>", gameObject);

                EventSystem.current.SetSelectedGameObject(autoSelectable.gameObject);
            }
            else if (debug)
            {
                Debug.Log($"<color=red>[UIContainer] ({gameObject.name}) AutoSelect → no valid selectable found</color>", gameObject);
            }
        }

        /// <summary>
        /// Returns true only when the container has fully settled into a visible state
        /// (show animation completed or InstaShow ran). False during animations, while
        /// hidden, or while pending activation.
        /// </summary>
        public bool IsVisible() => _isVisible;

        public bool IsAnimating()
        {
            return animationInProgress;
        }

        public void SetVisibility(bool visible, bool instant = false, bool invokeCallbacks = true, Action onComplete = null)
        {
            if (visible)
            {
                if (instant) InstaShow(false, invokeCallbacks, onComplete);
                else Show(false, invokeCallbacks, onComplete);
            }
            else
            {
                if (instant) InstaHide(false, invokeCallbacks);
                else Hide(false, invokeCallbacks);
            }
        }

        public void SetGroup(UIContainerGroup uiContainerGroup)
        {
            Group = uiContainerGroup;
            // Note: Don't add Group.Back() listener here - HandleBack() already handles groups
            // via Group.Back() call if needed. Adding another listener would cause double calls.
        }

        #region Navigation

        /// <summary>
        /// Remembers the currently selected object for restoration when re-shown.
        /// </summary>
        private void RememberCurrentSelection()
        {
            if (!rememberSelection) return;
            if (EventSystem.current == null) return;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
            {
                _lastSelectedObject = selected;
                if (debug)
                    Debug.Log($"<color=magenta>[UIContainer] ({gameObject.name}) remembered selection: {selected.name}</color>", gameObject);
            }
        }

        /// <summary>
        /// Handles the back/cancel action. Uses Group.Back() if in a group,
        /// otherwise navigates to parent container or hides.
        /// </summary>
        public void HandleBack()
        {
            if (debug)
                Debug.Log($"<color=yellow>[UIContainer] ({gameObject.name}) HandleBack called</color>", gameObject);

            // If in a group, delegate to the group's back handling
            if (Group != null)
            {
                if (debug)
                    Debug.Log($"<color=yellow>[UIContainer] ({gameObject.name}) → delegating to Group.Back()</color>", gameObject);

                Group.Back();
                return;
            }

            // Check for valid parent (not null and not self-reference)
            if (parentContainer != null && parentContainer != this)
            {
                if (debug)
                    Debug.Log($"<color=yellow>[UIContainer] ({gameObject.name}) → navigating to parent: {parentContainer.gameObject.name}</color>", gameObject);

                // Hide first, then show parent (prevents focus conflicts)
                Hide();
                parentContainer.Show();
            }
            else
            {
                if (debug)
                    Debug.Log($"<color=yellow>[UIContainer] ({gameObject.name}) → no parent, hiding</color>", gameObject);

                // No parent - just hide
                Hide();
            }
        }

        /// <summary>
        /// Focuses this container programmatically, selecting the appropriate element.
        /// </summary>
        public void Focus()
        {
            if (!IsVisible())
            {
                if (debug)
                    Debug.Log($"<color=magenta>[UIContainer] ({gameObject.name}) Focus() called but not visible</color>", gameObject);
                return;
            }

            if (debug)
                Debug.Log($"<color=magenta>[UIContainer] ({gameObject.name}) Focus() called</color>", gameObject);

            AutoSelect();
        }

        #endregion
    }
}