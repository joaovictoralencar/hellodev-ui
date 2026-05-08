using System;
using System.Collections.Generic;
using HelloDev.Logging;
using UnityEngine;
using Logger = HelloDev.Logging.Logger;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.UI.Default
{
    public class UIContainerGroupManager : MonoBehaviour
    {
        [SerializeField] private bool ShowFirstActiveGroup;
        [SerializeField] private UIContainerGroup FirstActiveGroup;

        [Header("Groups")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField]
        public List<UIContainerGroup> containerGroups = new List<UIContainerGroup>();

        public enum ContainerFetchMode
        {
            Manual,
            FindOnValidate,
            FindOnAwake
        }

        [Header("Settings")] public ContainerFetchMode fetchMode = ContainerFetchMode.Manual;
        private UIContainerGroup currentGroup;
        
        [SerializeField] private bool debug = false;
        
        /// <summary>
        /// Navigates back to the previous container group.
        /// Maintains a stack of previously active groups to enable back navigation.
        /// </summary>
        [SerializeField]
        private Stack<UIContainerGroup> groupBackStack = new Stack<UIContainerGroup>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fetchMode == ContainerFetchMode.FindOnValidate)
            {
                FetchContainers();
            }
        }
#endif
        
        private void Awake()
        {
            if (fetchMode == ContainerFetchMode.FindOnAwake)
            {
                FetchContainers();
            }

            // Ensure all groups are initialized
            foreach (var group in containerGroups)
            {
                if (group == null)
                {
                    Logger.LogError("UI", "A UIContainerGroup is missing in the UIContainerGroupManager.");
                }
                else 
                {
                    OnRegisterGroup(group);
                }
            }
        }
        
        private void OnEnable()
        {
            if (debug) Debug.Log($"<color=cyan>UIContainerGroupManager {gameObject.name} enabled</color>", gameObject);
        }
        
        private void OnDisable()
        {
            if (debug) Debug.Log($"<color=orange>UIContainerGroupManager {gameObject.name} DISABLED</color>", gameObject);
        }
        
        private void Start()
        {
           if (ShowFirstActiveGroup) ShowFirstEnabledContainer();
        }
        
        private void FetchContainers()
        {
            containerGroups.Clear();
            containerGroups.AddRange(GetComponentsInChildren<UIContainerGroup>(true));
        }
        
        private void ShowFirstEnabledContainer()
        {
            UIContainerGroup tempFirstActiveContainer;

            if (FirstActiveGroup == null)
            {
                tempFirstActiveContainer = containerGroups.Find(c => c.gameObject.activeSelf);
                if (tempFirstActiveContainer == null)
                {
                    Logger.LogWarning("UI", $"No enabled UIContainerGroup found in group manager {gameObject.name}.");
                    return;
                }
            }
            else
            {
                tempFirstActiveContainer = FirstActiveGroup;
            }

            ShowContainerGroup(tempFirstActiveContainer, true);
        }

        /// <summary>
        /// Registers a group to the manager at runtime.
        /// </summary>
        /// <param name="group">The UIContainerGroup to register.</param>
        public void RegisterGroup(UIContainerGroup group)
        {
            if (!containerGroups.Contains(group))
            {
                containerGroups.Add(group);
                OnRegisterGroup(group);
            }
        }

        private void OnRegisterGroup(UIContainerGroup group)
        {
            group.Manager = this;
            group.debug = debug;
            if (group.BackButton) group.BackButton.OnClick.AddListener(() => group.Back());
        }

        /// <summary>
        /// Unregisters a group from the manager at runtime.
        /// </summary>
        /// <param name="group">The UIContainerGroup to unregister.</param>
        public void UnregisterGroup(UIContainerGroup group)
        {
            if (containerGroups.Contains(group))
            {
                containerGroups.Remove(group);
            }
        }

        /// <summary>
        /// Shows a container in the specified group.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="containerId">The ID of the container.</param>
        /// <param name="instant">Whether to show instantly or with animation.</param>
        /// <param name="onShow">Callback after the container is shown.</param>
        public void ShowContainer(string groupId, string containerId, bool instant = false, Action onShow = null)
        {
            UIContainerGroup group = containerGroups.Find(g => g.GroupID == groupId);

            if (group == null)
            {
                Logger.LogError("UI", $"UIContainerGroup with ID {groupId} not found.");
                return;
            }

            // First, handle hiding current group if necessary
            if (currentGroup != group)
            {
                // Store reference to new group
                UIContainerGroup newGroup = group;

                // Handle hiding the current group first
                if (currentGroup != null)
                {
                    // Hide current group
                    currentGroup.HideAll(instant,  null, () => {
                        // After current group is hidden, activate the new group
                        ActivateNewGroup(newGroup, containerId, instant, onShow);
                    });
                }
                else
                {
                    // No current group to hide, directly activate new group
                    ActivateNewGroup(newGroup, containerId, instant, onShow);
                }
            }
            else
            {
                // Same group, just show the container
                group.ShowContainer(containerId, instant, onShow);
            }
        }

        private void ActivateNewGroup(UIContainerGroup newGroup, string containerId, bool instant, Action onShow)
        {
            // Update current group
            currentGroup = newGroup;
            
            // Hide all other groups
            foreach (var otherGroup in containerGroups)
            {
                if (otherGroup != newGroup)
                {
                    otherGroup.gameObject.SetActive(false);
                }
            }
            
            // Ensure the group is active
            newGroup.gameObject.SetActive(true);
            
            // Show the container
            newGroup.ShowContainer(containerId, instant, onShow);
        }

        public void ShowContainer(UIContainer container, bool instant = false, Action onShow = null)
        {
            if (container == null)
            {
                Logger.LogError("UI", "Cannot show null container");
                return;
            }

            if (container.Group == null)
            {
                Logger.LogError("UI", $"Container {container.ID} does not have a group assigned");
                return;
            }

            ShowContainer(container.Group.GroupID, container.ID, instant, onShow);
        }

        public void ShowContainerGroup(UIContainerGroup group, bool instant = false, Action onShow = null)
        {
            if (!containerGroups.Contains(group))
            {
                Logger.LogError("UI", $"Group {group.name} is not registered with this manager");
                return;
            }
            
            // If we have a current group and it's different from the new group
            if (currentGroup != null && currentGroup != group)
            {
                // Push the current group to the back stack
                groupBackStack.Push(currentGroup);
            }

            // Handle hiding current group if necessary
            if (currentGroup != group)
            {
                // Store reference to new group
                UIContainerGroup newGroup = group;

                // Handle hiding the current group first
                if (currentGroup != null)
                {
                    // Hide current group
                    currentGroup.HideAll(instant, null, () => {
                        // After current group is hidden, activate the new group
                        ActivateNewGroupAndShowFirst(newGroup, instant, onShow);
                    });
                }
                else
                {
                    // No current group to hide, directly activate new group
                    ActivateNewGroupAndShowFirst(newGroup, instant, onShow);
                }
            }
            else if (onShow != null)
            {
                // Group is already active, just invoke the callback
                onShow.Invoke();
            }
        }

        private void ActivateNewGroupAndShowFirst(UIContainerGroup newGroup, bool instant, Action onShow)
        {
            // Update current group
            currentGroup = newGroup;
            
            // Hide all other groups
            foreach (var otherGroup in containerGroups)
            {
                if (otherGroup != newGroup)
                {
                    otherGroup.gameObject.SetActive(false);
                }
            }
            
            // Ensure the group is active
            newGroup.gameObject.SetActive(true);
            
            // Show the first container
            newGroup.Container.InstaShow(true);
            newGroup.ShowFirstEnabledContainerHandler(instant, onShow);
        }

        public void Back()
        {
            if (groupBackStack.Count > 0)
            {
                // Get the previous group from the back stack
                UIContainerGroup previousGroup = groupBackStack.Pop();

                // Show the previous group
                ShowContainerGroup(previousGroup, true);
            }
            else
            {
                Logger.LogWarning("UI", "Cannot navigate back: No previous groups in the stack.");
            }
        }
        
        /// <summary>
        /// Hide all container groups
        /// </summary>
        /// <param name="instant">Whether to hide instantly or with animation</param>
        /// <param name="onComplete">Callback after all groups are hidden</param>
        public void HideAll(bool instant = true, Action onComplete = null)
        {
            if (containerGroups.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }
            
            int remainingGroups = containerGroups.Count;
            
            foreach (var group in containerGroups)
            {
                group.HideAll(instant, null, () => {
                    remainingGroups--;
                    if (remainingGroups <= 0)
                    {
                        currentGroup = null;
                        onComplete?.Invoke();
                    }
                });
            }
        }
    }
}