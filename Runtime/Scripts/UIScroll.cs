using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Enhanced scroll view that automatically scrolls to bring selected items into view.
    /// Works seamlessly with keyboard/controller navigation.
    /// Uses event-driven architecture — no Update loop.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class UIScroll : MonoBehaviour
    {
        #region Static Events

        public static event System.Action<GameObject> OnSelectableSelected;

        public static void NotifySelected(GameObject selected) =>
            OnSelectableSelected?.Invoke(selected);

        #endregion

        #region Serialized Fields

        [Header("Auto-Scroll to Selection")]
        [Tooltip("When enabled, the scroll view will automatically scroll to show selected items.")]
        [SerializeField] private bool autoScrollToSelection = true;

        [Header("Animation")]
        [Tooltip("Duration of the scroll animation in seconds.")]
        [SerializeField] private float scrollDuration = 0.15f;

        [Tooltip("Animation curve for the scroll. Leave default for smooth ease.")]
        [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Positioning")]
        [Tooltip("Extra padding around the selected item to ensure it is comfortably visible.")]
        [SerializeField] private float viewPadding = 10f;

        [Tooltip("When enabled, centers the selected item in the viewport.")]
        [SerializeField] private bool centerOnSelection = false;

        #endregion

        #region Private Fields

        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private Coroutine _currentTween;

        #endregion

        #region Properties

        public bool AutoScrollToSelection
        {
            get => autoScrollToSelection;
            set => autoScrollToSelection = value;
        }

        public ScrollRect ScrollRect => _scrollRect;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _viewport   = _scrollRect.viewport ?? GetComponent<RectTransform>();
            _content    = _scrollRect.content;
        }

        private void OnEnable()  => OnSelectableSelected += HandleSelectionChanged;
        private void OnDisable()
        {
            OnSelectableSelected -= HandleSelectionChanged;
            StopCurrentTween();
        }

        #endregion

        #region Event Handlers

        private void HandleSelectionChanged(GameObject selected)
        {
            if (!autoScrollToSelection || selected == null || _content == null) return;
            if (!selected.transform.IsChildOf(_content)) return;

            var itemRect = selected.GetComponent<RectTransform>();
            if (itemRect != null) ScrollToItem(itemRect);
        }

        #endregion

        #region Public Methods

        public void ScrollToItem(RectTransform item)
        {
            if (item == null || _content == null || _viewport == null) return;

            if (_scrollRect.vertical)       ScrollToItemVertical(item);
            else if (_scrollRect.horizontal) ScrollToItemHorizontal(item);
        }

        public void ScrollToItemImmediate(RectTransform item)
        {
            if (item == null || _content == null || _viewport == null) return;
            StopCurrentTween();

            if (_scrollRect.vertical)
                _scrollRect.verticalNormalizedPosition = CalculateVerticalScrollPosition(item);
            else if (_scrollRect.horizontal)
                _scrollRect.horizontalNormalizedPosition = CalculateHorizontalScrollPosition(item);
        }

        public void ScrollToTop()    => AnimateTo(v => _scrollRect.verticalNormalizedPosition   = v, _scrollRect.verticalNormalizedPosition,   1f);
        public void ScrollToBottom() => AnimateTo(v => _scrollRect.verticalNormalizedPosition   = v, _scrollRect.verticalNormalizedPosition,   0f);
        public void ScrollToLeft()   => AnimateTo(v => _scrollRect.horizontalNormalizedPosition = v, _scrollRect.horizontalNormalizedPosition, 0f);
        public void ScrollToRight()  => AnimateTo(v => _scrollRect.horizontalNormalizedPosition = v, _scrollRect.horizontalNormalizedPosition, 1f);

        #endregion

        #region Private Methods

        private void ScrollToItemVertical(RectTransform item)
        {
            float target = CalculateVerticalScrollPosition(item);
            if (!Mathf.Approximately(target, _scrollRect.verticalNormalizedPosition))
                AnimateTo(v => _scrollRect.verticalNormalizedPosition = v, _scrollRect.verticalNormalizedPosition, target);
        }

        private void ScrollToItemHorizontal(RectTransform item)
        {
            float target = CalculateHorizontalScrollPosition(item);
            if (!Mathf.Approximately(target, _scrollRect.horizontalNormalizedPosition))
                AnimateTo(v => _scrollRect.horizontalNormalizedPosition = v, _scrollRect.horizontalNormalizedPosition, target);
        }

        private void AnimateTo(System.Action<float> setter, float from, float to)
        {
            StopCurrentTween();
            _currentTween = StartCoroutine(LerpRoutine(setter, from, to));
        }

        private IEnumerator LerpRoutine(System.Action<float> setter, float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < scrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scrollDuration);
                setter(Mathf.LerpUnclamped(from, to, scrollCurve.Evaluate(t)));
                yield return null;
            }
            setter(to);
            _currentTween = null;
        }

        private void StopCurrentTween()
        {
            if (_currentTween != null) { StopCoroutine(_currentTween); _currentTween = null; }
        }

        private float CalculateVerticalScrollPosition(RectTransform item)
        {
            Canvas.ForceUpdateCanvases();

            float contentHeight  = _content.rect.height;
            float viewportHeight = _viewport.rect.height;

            if (contentHeight <= viewportHeight)
                return _scrollRect.verticalNormalizedPosition;

            Vector3[] itemCorners     = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            item.GetWorldCorners(itemCorners);
            _viewport.GetWorldCorners(viewportCorners);

            float itemTop      = itemCorners[1].y;
            float itemBottom   = itemCorners[0].y;
            float viewportTop  = viewportCorners[1].y;
            float viewportBot  = viewportCorners[0].y;
            float scrollable   = contentHeight - viewportHeight;

            if (centerOnSelection)
            {
                float offset = (itemTop + itemBottom) / 2f - (viewportTop + viewportBot) / 2f;
                return Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + offset / scrollable);
            }

            float paddedTop = viewportTop - viewPadding;
            float paddedBot = viewportBot + viewPadding;
            if (itemTop <= paddedTop && itemBottom >= paddedBot)
                return _scrollRect.verticalNormalizedPosition;

            float delta = itemTop > paddedTop ? itemTop - paddedTop : itemBottom - paddedBot;
            return Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + delta / scrollable);
        }

        private float CalculateHorizontalScrollPosition(RectTransform item)
        {
            Canvas.ForceUpdateCanvases();

            float contentWidth  = _content.rect.width;
            float viewportWidth = _viewport.rect.width;

            if (contentWidth <= viewportWidth)
                return _scrollRect.horizontalNormalizedPosition;

            Vector3[] itemCorners     = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            item.GetWorldCorners(itemCorners);
            _viewport.GetWorldCorners(viewportCorners);

            float itemLeft    = itemCorners[0].x;
            float itemRight   = itemCorners[2].x;
            float viewportLeft  = viewportCorners[0].x;
            float viewportRight = viewportCorners[2].x;
            float scrollable  = contentWidth - viewportWidth;

            if (centerOnSelection)
            {
                float offset = (itemLeft + itemRight) / 2f - (viewportLeft + viewportRight) / 2f;
                return Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + offset / scrollable);
            }

            float paddedLeft  = viewportLeft  + viewPadding;
            float paddedRight = viewportRight - viewPadding;
            if (itemLeft >= paddedLeft && itemRight <= paddedRight)
                return _scrollRect.horizontalNormalizedPosition;

            float delta = itemLeft < paddedLeft ? itemLeft - paddedLeft : itemRight - paddedRight;
            return Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + delta / scrollable);
        }

        #endregion
    }
}
