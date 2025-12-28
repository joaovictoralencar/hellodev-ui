using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Enhanced scroll view that automatically scrolls to bring selected items into view.
    /// Works seamlessly with keyboard/controller navigation.
    /// Uses event-driven architecture - no Update loop.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class UIScroll : MonoBehaviour
    {
        #region Static Events

        /// <summary>
        /// Global event fired when any selectable is selected.
        /// UIScroll instances subscribe to this to handle auto-scrolling.
        /// </summary>
        public static event Action<GameObject> OnSelectableSelected;

        /// <summary>
        /// Call this from selectables when they receive selection.
        /// </summary>
        public static void NotifySelected(GameObject selected)
        {
            OnSelectableSelected?.Invoke(selected);
        }

        #endregion

        #region Serialized Fields

        [Header("Auto-Scroll to Selection")]
        [Tooltip("When enabled, the scroll view will automatically scroll to show selected items.")]
        [SerializeField] private bool autoScrollToSelection = true;

        [Header("Animation")]
        [Tooltip("Duration of the scroll animation in seconds.")]
        [SerializeField] private float scrollDuration = 0.15f;

        [Tooltip("Easing function for the scroll animation.")]
        [SerializeField] private Ease scrollEase = Ease.OutCubic;

        [Header("Positioning")]
        [Tooltip("Extra padding around the selected item to ensure it's comfortably visible.")]
        [SerializeField] private float viewPadding = 10f;

        [Tooltip("When enabled, centers the selected item in the viewport. When disabled, scrolls minimum distance to make item visible.")]
        [SerializeField] private bool centerOnSelection = false;

        #endregion

        #region Private Fields

        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private Tween _currentTween;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether auto-scroll to selection is enabled.
        /// </summary>
        public bool AutoScrollToSelection
        {
            get => autoScrollToSelection;
            set => autoScrollToSelection = value;
        }

        /// <summary>
        /// The underlying ScrollRect component.
        /// </summary>
        public ScrollRect ScrollRect => _scrollRect;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport ?? GetComponent<RectTransform>();
            _content = _scrollRect.content;
        }

        private void OnEnable()
        {
            OnSelectableSelected += HandleSelectionChanged;
        }

        private void OnDisable()
        {
            OnSelectableSelected -= HandleSelectionChanged;
            _currentTween.Stop();
        }

        #endregion

        #region Event Handlers

        private void HandleSelectionChanged(GameObject selected)
        {
            if (!autoScrollToSelection) return;
            if (selected == null || _content == null) return;

            // Only scroll if the selected object is within our content
            if (!selected.transform.IsChildOf(_content))
                return;

            var itemRect = selected.GetComponent<RectTransform>();
            if (itemRect != null)
                ScrollToItem(itemRect);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Scrolls to bring the specified item into view.
        /// </summary>
        /// <param name="item">The RectTransform of the item to scroll to.</param>
        public void ScrollToItem(RectTransform item)
        {
            if (item == null || _content == null || _viewport == null) return;

            if (_scrollRect.vertical)
                ScrollToItemVertical(item);
            else if (_scrollRect.horizontal)
                ScrollToItemHorizontal(item);
        }

        /// <summary>
        /// Scrolls to bring the specified item into view immediately without animation.
        /// </summary>
        /// <param name="item">The RectTransform of the item to scroll to.</param>
        public void ScrollToItemImmediate(RectTransform item)
        {
            if (item == null || _content == null || _viewport == null) return;

            _currentTween.Stop();

            if (_scrollRect.vertical)
            {
                float targetPosition = CalculateVerticalScrollPosition(item);
                _scrollRect.verticalNormalizedPosition = targetPosition;
            }
            else if (_scrollRect.horizontal)
            {
                float targetPosition = CalculateHorizontalScrollPosition(item);
                _scrollRect.horizontalNormalizedPosition = targetPosition;
            }
        }

        /// <summary>
        /// Scrolls to the top of the content.
        /// </summary>
        public void ScrollToTop()
        {
            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.verticalNormalizedPosition, 1f, scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value, scrollEase);
        }

        /// <summary>
        /// Scrolls to the bottom of the content.
        /// </summary>
        public void ScrollToBottom()
        {
            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.verticalNormalizedPosition, 0f, scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value, scrollEase);
        }

        /// <summary>
        /// Scrolls to the left of the content.
        /// </summary>
        public void ScrollToLeft()
        {
            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.horizontalNormalizedPosition, 0f, scrollDuration,
                value => _scrollRect.horizontalNormalizedPosition = value, scrollEase);
        }

        /// <summary>
        /// Scrolls to the right of the content.
        /// </summary>
        public void ScrollToRight()
        {
            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.horizontalNormalizedPosition, 1f, scrollDuration,
                value => _scrollRect.horizontalNormalizedPosition = value, scrollEase);
        }

        #endregion

        #region Private Methods

        private void ScrollToItemVertical(RectTransform item)
        {
            float targetPosition = CalculateVerticalScrollPosition(item);

            // Check if we actually need to scroll
            if (Mathf.Approximately(targetPosition, _scrollRect.verticalNormalizedPosition))
                return;

            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.verticalNormalizedPosition, targetPosition, scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value, scrollEase);
        }

        private void ScrollToItemHorizontal(RectTransform item)
        {
            float targetPosition = CalculateHorizontalScrollPosition(item);

            // Check if we actually need to scroll
            if (Mathf.Approximately(targetPosition, _scrollRect.horizontalNormalizedPosition))
                return;

            _currentTween.Stop();
            _currentTween = Tween.Custom(_scrollRect.horizontalNormalizedPosition, targetPosition, scrollDuration,
                value => _scrollRect.horizontalNormalizedPosition = value, scrollEase);
        }

        private float CalculateVerticalScrollPosition(RectTransform item)
        {
            // Force layout rebuild to ensure accurate positions
            Canvas.ForceUpdateCanvases();

            float contentHeight = _content.rect.height;
            float viewportHeight = _viewport.rect.height;

            // If content fits in viewport, no scrolling needed
            if (contentHeight <= viewportHeight)
                return _scrollRect.verticalNormalizedPosition;

            // Get item's position relative to content
            Vector3[] itemCorners = new Vector3[4];
            item.GetWorldCorners(itemCorners);

            Vector3[] viewportCorners = new Vector3[4];
            _viewport.GetWorldCorners(viewportCorners);

            // Calculate item bounds in world space
            float itemTop = itemCorners[1].y; // Top-left corner Y
            float itemBottom = itemCorners[0].y; // Bottom-left corner Y

            float viewportTop = viewportCorners[1].y;
            float viewportBottom = viewportCorners[0].y;

            // Scrollable range
            float scrollableHeight = contentHeight - viewportHeight;

            if (centerOnSelection)
            {
                // Center the item in the viewport
                float itemCenter = (itemTop + itemBottom) / 2f;
                float viewportCenter = (viewportTop + viewportBottom) / 2f;
                float offset = itemCenter - viewportCenter;

                // Convert offset to normalized position change
                // Positive offset (item above center) means we need to scroll UP (increase normalizedPosition)
                float normalizedOffset = offset / scrollableHeight;
                return Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + normalizedOffset);
            }
            else
            {
                // Scroll minimum distance to make item visible
                float paddedViewportTop = viewportTop - viewPadding;
                float paddedViewportBottom = viewportBottom + viewPadding;

                // Check if item is already fully visible
                if (itemTop <= paddedViewportTop && itemBottom >= paddedViewportBottom)
                    return _scrollRect.verticalNormalizedPosition;

                float offset = 0f;

                if (itemTop > paddedViewportTop)
                {
                    // Item is above viewport - need to scroll UP (increase normalizedPosition)
                    offset = itemTop - paddedViewportTop;
                }
                else if (itemBottom < paddedViewportBottom)
                {
                    // Item is below viewport - need to scroll DOWN (decrease normalizedPosition)
                    offset = itemBottom - paddedViewportBottom;
                }

                // Convert offset to normalized position change
                float normalizedOffset = offset / scrollableHeight;
                return Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + normalizedOffset);
            }
        }

        private float CalculateHorizontalScrollPosition(RectTransform item)
        {
            // Force layout rebuild to ensure accurate positions
            Canvas.ForceUpdateCanvases();

            float contentWidth = _content.rect.width;
            float viewportWidth = _viewport.rect.width;

            // If content fits in viewport, no scrolling needed
            if (contentWidth <= viewportWidth)
                return _scrollRect.horizontalNormalizedPosition;

            // Get item's position relative to content
            Vector3[] itemCorners = new Vector3[4];
            item.GetWorldCorners(itemCorners);

            Vector3[] viewportCorners = new Vector3[4];
            _viewport.GetWorldCorners(viewportCorners);

            // Calculate item bounds in world space
            float itemLeft = itemCorners[0].x; // Bottom-left corner X
            float itemRight = itemCorners[2].x; // Top-right corner X

            float viewportLeft = viewportCorners[0].x;
            float viewportRight = viewportCorners[2].x;

            // Scrollable range
            float scrollableWidth = contentWidth - viewportWidth;

            if (centerOnSelection)
            {
                // Center the item in the viewport
                float itemCenter = (itemLeft + itemRight) / 2f;
                float viewportCenter = (viewportLeft + viewportRight) / 2f;
                float offset = itemCenter - viewportCenter;

                // Convert offset to normalized position change
                // Positive offset (item to the right) means we need to scroll RIGHT (increase normalizedPosition)
                float normalizedOffset = offset / scrollableWidth;
                return Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + normalizedOffset);
            }
            else
            {
                // Scroll minimum distance to make item visible
                float paddedViewportLeft = viewportLeft + viewPadding;
                float paddedViewportRight = viewportRight - viewPadding;

                // Check if item is already fully visible
                if (itemLeft >= paddedViewportLeft && itemRight <= paddedViewportRight)
                    return _scrollRect.horizontalNormalizedPosition;

                float offset = 0f;

                if (itemLeft < paddedViewportLeft)
                {
                    // Item is to the left of viewport - need to scroll LEFT (decrease normalizedPosition)
                    offset = itemLeft - paddedViewportLeft;
                }
                else if (itemRight > paddedViewportRight)
                {
                    // Item is to the right of viewport - need to scroll RIGHT (increase normalizedPosition)
                    offset = itemRight - paddedViewportRight;
                }

                // Convert offset to normalized position change
                float normalizedOffset = offset / scrollableWidth;
                return Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + normalizedOffset);
            }
        }

        #endregion
    }
}
