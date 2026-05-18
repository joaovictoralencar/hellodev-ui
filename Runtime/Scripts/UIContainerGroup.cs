using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Logging;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(UIContainer))]
    public class UIContainerGroup : MonoBehaviour
    {
        [Header("Navigation")] [SerializeField]
        UIButton backButton;

        [Header("Settings")] public string GroupID; // Unique identifier for this group
        public ContainerFetchMode fetchMode = ContainerFetchMode.Manual;
        public bool keepOneOpen = false;
        [SerializeField] bool ShowFirstEnabledContainer = true;
        [SerializeField] private UIContainer FirstActiveContainer;

        [Header("Debug")] public bool debug = false;

        [Header("Read Only")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField]
        private Stack<UIContainer> backStack = new Stack<UIContainer>();

        [SerializeField] private List<UIContainer> containers = new List<UIContainer>();

        private UIContainer currentContainer;
        private bool firstShowHandled = false;
        private Canvas canvas;
        private CanvasGroup canvasGroup;

        public UIContainer Container { get; private set; }
        public List<UIContainer> Containers => containers;
        public UIContainer CurrentContainer => currentContainer;
        public Canvas Canvas => canvas;
        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null) TryGetComponent(out canvasGroup);
                return canvasGroup;
            }
        }

        public UIContainerGroupManager Manager { get; set; }

        public UIButton BackButton => backButton;

        private void Awake()
        {
            Container = GetComponent<UIContainer>();
            FetchEssentialComponents();
            if (fetchMode == ContainerFetchMode.FindOnAwake)
            {
                FetchContainers();
            }

            foreach (var uiContainer in containers)
            {
                uiContainer.SetGroup(this);
                uiContainer.debug = debug;
            }
        }

        private void OnEnable()
        {
            Container.onShow.SafeSubscribe(OnShow);
            //if (debug) Debug.Log($"<color=cyan>UIContainerGroup {gameObject.name} enabled</color>", gameObject);
        }

        private void OnDisable()
        {
            Container.onShow.SafeUnsubscribe(OnShow);
            //if (debug) Debug.Log($"<color=orange>UIContainerGroup {gameObject.name} DISABLED</color>", gameObject);
        }

        private void Start()
        {
            if (ShowFirstEnabledContainer && gameObject.activeSelf)
            {
                ShowFirstEnabledContainerHandler(true, null);
            }
        }

        private void OnShow()
        {
            if (ShowFirstEnabledContainer && gameObject.activeSelf)
            {
                ShowFirstEnabledContainerHandler(true, null);
            }
        }

        private void FetchContainers()
        {
            containers.Clear();
            containers.AddRange(GetComponentsInChildren<UIContainer>(true));

            // Remove self from containers list if it's there
            containers.RemoveAll(c => c == Container);
        }

        public void ShowFirstEnabledContainerHandler(bool instant, Action onShow)
        {
            if (firstShowHandled)
            {
                onShow?.Invoke();
                return;
            }

            if (currentContainer != null && currentContainer.IsVisible())
            {
                onShow?.Invoke();
                return;
            }

            if (containers.Count == 0)
            {
                Logger.LogWarning("UI", $"No UIContainers found in group {GroupID}.");
                onShow?.Invoke();
                return;
            }

            UIContainer tempFirstActiveContainer;

            if (FirstActiveContainer == null)
            {
                tempFirstActiveContainer = containers.Find(c => c.gameObject.activeSelf);
                if (tempFirstActiveContainer == null)
                {
                    // No active container found, use the first one in the list
                    if (containers.Count > 0)
                    {
                        tempFirstActiveContainer = containers[0];
                    }
                    else
                    {
                        Logger.LogWarning("UI", $"No enabled UIContainer found in group {GroupID}.");
                        onShow?.Invoke();
                        return;
                    }
                }
            }
            else
            {
                tempFirstActiveContainer = FirstActiveContainer;
            }

            ShowContainer(tempFirstActiveContainer.ID, instant, onShow);
            firstShowHandled = true;
        }

        public void ShowContainer(string id, bool instant = false, Action onShow = null, bool alsoHideChildren = false)
        {
            UIContainer containerToShow = containers.Find(c => c.ID == id);

            if (containerToShow == null)
            {
                Logger.LogError("UI", $"UIContainer with ID {id} not found in group {GroupID}");
                onShow?.Invoke();
                return;
            }

            // Make sure the group container is shown
            if (!Container.IsVisible()) Container.InstaShow(false, false);

            if (keepOneOpen)
            {
                //If there is a container opened, hide it first
                if (currentContainer != null && currentContainer != containerToShow)
                {
                    // Add current to back stack before hiding
                    if (currentContainer.IsVisible() && !backStack.Contains(currentContainer))
                    {
                        backStack.Push(currentContainer);
                    }
                }

                // Hide all containers
                HideAll(true, containerToShow, () =>
                {
                    // After all are hidden, show the requested container
                    ShowContainerInternal(containerToShow, instant, onShow);
                }, alsoHideChildren);

                // HideAll(instant, containerToShow, () =>
                // {
                //     // After all are hidden, show the requested container
                //     ShowContainerInternal(containerToShow, instant, onShow);
                // }, alsoHideChildren);
                return;
            }

            // If we don't need to keep one open or current container is null,
            // directly show the requested container
            ShowContainerInternal(containerToShow, instant, onShow);
        }

        private void ShowContainerInternal(UIContainer containerToShow, bool instant, Action onShow)
        {
            // Ensure group container is visible
            if (!Container.IsVisible()) Container.InstaShow();

            // Show the requested container
            if (instant)
            {
                containerToShow.InstaShow(true, true, onShow);
            }
            else
            {
                containerToShow.Show(true, true, onShow);
            }

            // Update current container
            currentContainer = containerToShow;

            if (debug)
            {
                //Debug.Log($"<color=cyan>[UIContainerGroup] ({gameObject.name}) current container {containerToShow.ID}</color>", gameObject);
            }
        }

        public void ShowContainer(UIContainer container, bool instant = false, Action onShow = null)
        {
            if (container == null)
            {
                Logger.LogError("UI", "Cannot show null container");
                onShow?.Invoke();
                return;
            }

            if (!containers.Contains(container))
            {
                Logger.LogError("UI", $"Container {container.ID} is not part of group {GroupID}");
                onShow?.Invoke();
                return;
            }

            ShowContainer(container.ID, instant, onShow);
        }

        public void Back(bool instant = false)
        {
            if (backStack.Count > 0)
            {
                UIContainer previousContainer = backStack.Pop();
                if (currentContainer != null)
                {
                    if (instant)
                    {
                        currentContainer.InstaHide(true);
                    }
                    else
                    {
                        currentContainer.Hide(true);
                    }
                }

                if (instant)
                {
                    previousContainer.InstaShow(true);
                }
                else
                {
                    previousContainer.Show(true);
                }

                currentContainer = previousContainer;
            }
        }

        public void HideAll(bool instant = true, UIContainer containerToShow = null, Action onComplete = null, bool alsoHideChildren = false)
        {
            // If there are no containers or none are active, just call the completion callback
            if (containers.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // For instant hide, just hide them all immediately
            if (instant)
            {
                foreach (var container in containers.Where(container => container.IsVisible() && container != containerToShow))
                {
                    container.InstaHide(true);
                    if (!alsoHideChildren) continue;
                    if (container.gameObject.TryGetComponent(out UIContainerGroup group))
                    {
                        group.HideAll(true, null, null, true);
                    }
                }

                currentContainer = null;
                onComplete?.Invoke();
                return;
            }

            // For animated hide, we need to track completion of all animations
            List<UIContainer> visibleContainers = new List<UIContainer>();

            // Find all visible containers
            foreach (var container in containers)
            {
                if (container.IsVisible() && container != containerToShow)
                {
                    visibleContainers.Add(container);
                    if (!alsoHideChildren) continue;
                    if (container.gameObject.TryGetComponent(out UIContainerGroup group))
                    {
                        group.HideAll(true, null, null, true);
                    }
                }
            }

            // If no visible containers, just call the completion callback
            if (visibleContainers.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // Track how many containers we're waiting on
            int remainingContainers = visibleContainers.Count;

            // Create a callback that will be invoked after each container is hidden
            void onContainerHidden()
            {
                remainingContainers--;

                // When all containers are hidden, invoke the completion callback
                if (remainingContainers <= 0)
                {
                    currentContainer = null;
                    onComplete?.Invoke();
                }
            }

            // Hide each container and attach our callback
            foreach (var container in visibleContainers)
            {
                // Create a one-time event listener for this specific hide operation
                UnityAction hideListener = null;
                hideListener = () =>
                {
                    // Remove this listener to prevent memory leaks
                    container.onHide.SafeUnsubscribe(hideListener);
                    onContainerHidden();
                };

                // Add the listener
                container.onHide.AddListener(hideListener);

                // Hide the container
                container.Hide(true);
            }
        }

        public void HideAllChildrenAndShow(string id, bool instant, Action onComplete = null, bool alsoHideChildren = false)
        {
            ShowContainer(id, instant, onComplete, alsoHideChildren);
        }

        private void FetchEssentialComponents()
        {
            if (canvas == null) TryGetComponent(out canvas);
            if (canvasGroup == null) TryGetComponent(out canvasGroup);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(GroupID)) GroupID = gameObject.name;

            if (fetchMode == ContainerFetchMode.FindOnValidate)
            {
                FetchContainers();
            }

            FetchEssentialComponents();
        }
#endif
    }

    public enum ContainerFetchMode
    {
        Manual,
        FindOnValidate,
        FindOnAwake
    }
}