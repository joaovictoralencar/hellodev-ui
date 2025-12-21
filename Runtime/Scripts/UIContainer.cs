using System;
using HelloDev.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UIContainer : MonoBehaviour
    {
        public string ID; // Unique identifier for this container

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

        [Header("Navigation")] [SerializeField]
        UIButton backButton;

        [Header("Start Action")] public StartAction onStartAction = StartAction.DoNothing;

        [Header("Animation Settings")] [SerializeField] private float openDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.3f;
        public EaseType openEase = EaseType.OutQuad;
        public EaseType hideEase = EaseType.InQuad;
        [SerializeField] bool unscaledTime = false;

        [Header("Interaction Settings")] public bool disableInteractionsWhenHidden = true;

        [Header("Auto-Select Settings")] public Selectable autoSelectable;

        [Header("Close")] [SerializeField] private UIButton[] closeButtons;
        [Header("Debug")] [SerializeField] internal bool debug = false;

        [Header("Callbacks")] public UnityEvent onShow;
        public UnityEvent onHide;
        public UnityEvent onStartHide;
        public UnityEvent onStartShow;

        public float OpenDuration => openDuration;
        public float HideDuration => hideDuration;
        public UIContainerGroup Group { get; private set; }
        public Canvas Canvas => canvas ??= GetComponent<Canvas>();
        private Canvas canvas;
        public CanvasGroup CanvasGroup => canvasGroup ??= GetComponent<CanvasGroup>();
        private CanvasGroup canvasGroup;
        private ITweenHandle fadeTween;

        // Flag to track animation in progress
        private bool animationInProgress = false;

        // Flag for pending actions
        private bool hasPendingShow = false;
        private bool hasPendingHide = false;
        private bool pendingInstant = false;
        private Action pendingShowCallback = null;

        protected virtual void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();

            foreach (var btn in closeButtons)
            {
                btn.OnClick.AddListener(() => Hide());
            }
        }

        protected virtual void Start()
        {
            // Apply initial state
            switch (onStartAction)
            {
                case StartAction.InstaHide: InstaHide(); break;
                case StartAction.InstaShow: InstaShow(); break;
                case StartAction.Hide: Hide(); break;
                case StartAction.Show: Show(); break;
            }
        }

        public void InstaHide(bool fromGroup = false, bool invokeCallbacks = true)
        {
            // Cancel any pending animations
            KillAnimation();

            // If we're already hidden and callbacks aren't needed, skip
            if (!gameObject.activeSelf && !invokeCallbacks)
                return;

            if (Group && !fromGroup)
            {
                Debug.LogWarning("This container has a group. Use group methods to hide it instead.");
                return;
            }

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

            // Clear pending actions
            hasPendingShow = false;
            hasPendingHide = false;
            pendingShowCallback = null;
        }

        public void InstaShow(bool fromGroup = false, bool invokeCallbacks = true, Action onShowCallback = null)
        {
            // Cancel any pending animations
            KillAnimation();

            // If we're already visible and callbacks aren't needed, skip
            if (gameObject.activeSelf && CanvasGroup.alpha == 1f && !invokeCallbacks && onShowCallback == null)
                return;

            if (Group && !fromGroup)
            {
                Debug.LogWarning("This container has a group. Use group methods to show it instead.");
                return;
            }

            // Apply visible state
            gameObject.SetActive(true);
            Canvas.enabled = true;
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;

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

        private void OnEnable()
        {
            //if (debug) Debug.Log($"<color=cyan>UIContainer {gameObject.name} enabled</color>", gameObject);

            // Check if we have a pending show
            if (hasPendingShow)
            {
                Show(true, true, pendingShowCallback);
                hasPendingShow = false;
                pendingShowCallback = null;
            }
        }

        private void OnDisable()
        {
            //if (debug) Debug.Log($"<color=orange>UIContainer {gameObject.name} DISABLED</color>", gameObject);

            // If we have an animation in progress, kill it
            KillAnimation();
        }

        public void ShowContainer(bool invokeCallbacks = true)
        {
            Show(false, invokeCallbacks, null);
        }

        public void HideContainer(bool invokeCallbacks = true)
        {
            Hide(false, invokeCallbacks);
        }

        public void Show(bool fromGroup = false, bool invokeCallbacks = true, Action onShowCallback = null)
        {
            // If gameobject is disabled, store as pending and exit
            if (!gameObject.activeInHierarchy)
            {
                hasPendingShow = true;
                pendingInstant = false;
                pendingShowCallback = onShowCallback;

                // Enable the gameObject to trigger OnEnable where we'll resume
                gameObject.SetActive(true);
                if (invokeCallbacks) onStartShow?.Invoke();
                return;
            }

            if (Group && !fromGroup)
            {
                Debug.LogWarning("This container has a group. Use group methods to show it instead.");
                return;
            }

            // Kill any existing animation
            KillAnimation();

            // Set animation in progress flag
            animationInProgress = true;

            // Make container visible
            gameObject.SetActive(true);
            Canvas.enabled = true;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.alpha = 0;

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
                        Hide(true, invokeCallbacks);
                    }
                });
        }

        public void Hide(bool fromGroup = false, bool invokeCallbacks = true)
        {
            // If not active, just call callbacks if needed
            if (!gameObject.activeInHierarchy)
            {
                if (invokeCallbacks) onHide?.Invoke();
                return;
            }

            if (Group && !fromGroup)
            {
                Debug.LogWarning("This container has a group. Use group methods to hide it instead.");
                return;
            }

            // If animation is in progress, mark as pending and return
            if (animationInProgress)
            {
                hasPendingHide = true;
                return;
            }

            // Kill any existing animation
            KillAnimation();

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
                        if (pendingInstant)
                        {
                            InstaShow(true, true, pendingShowCallback);
                        }
                        else
                        {
                            Show(true, true, pendingShowCallback);
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
            if (autoSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(autoSelectable.gameObject);
            }
        }

        public bool IsVisible()
        {
            return gameObject.activeSelf && Canvas.enabled && CanvasGroup.alpha > 0;
        }

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
            if (backButton && Group != null)
            {
                backButton.OnClick.AddListener(() => Group.Back());
            }
        }
    }
}