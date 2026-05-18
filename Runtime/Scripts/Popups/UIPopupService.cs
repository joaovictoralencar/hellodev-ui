using System;
using System.Collections.Generic;
using HelloDev.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Manages popup queue and lifecycle.
    /// Listens to PopupRequestEvent for decoupled access.
    /// </summary>
    public class UIPopupService : MonoBehaviour
    {
        [Header("Prefabs")] [Tooltip("Default popup prefab used when no custom prefab is specified.")] [SerializeField]
        private UIPopup defaultPrefab;

#if UNITY_ADDRESSABLES
        [Tooltip("Optional addressable reference for popup prefab (used if defaultPrefab is null).")]
        [SerializeField] private UnityEngine.AddressableAssets.AssetReference popupAddressable;
#endif

        [Tooltip("Parent transform for spawned popups.")] [SerializeField]
        private Transform popupContainer;

#if HELLODEV_GAME_EVENTS
        [Header("Events")]
        [Tooltip("Subscribe to this event for decoupled popup requests.")]
        [SerializeField] private PopupRequestEvent requestEvent;
#endif

        [Header("Pooling & Debug")]
        [Tooltip("Enable simple reuse pool for popups (opt-in). ")] [SerializeField] private bool enablePooling = false;
        [SerializeField] private bool debug = false;

        #region Private Fields

        private readonly Queue<PopupRequest> _queue = new();
        private UIPopup _currentPopup;
        private GameObject _savedSelection;
        private readonly List<UIPopup> _pool = new();
#if UNITY_ADDRESSABLES
        private GameObject _addressablePrefab;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if there is an active popup being displayed.
        /// </summary>
        public bool HasActivePopup => _currentPopup != null;

        /// <summary>
        /// Gets the current active popup, if any.
        /// </summary>
        public UIPopup CurrentPopup => _currentPopup;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
#if HELLODEV_GAME_EVENTS
            if (requestEvent != null)
            {
                requestEvent.AddListener(OnPopupRequested);
            }
#endif
        }

        private void OnDisable()
        {
#if HELLODEV_GAME_EVENTS
            if (requestEvent != null)
            {
                requestEvent.SafeUnsubscribe(OnPopupRequested);
            }
#endif
        }

        private void OnDestroy()
        {
            // Clean up current popup
            if (_currentPopup != null)
            {
                if (enablePooling)
                    ReturnToPool(_currentPopup);
                else
                    Destroy(_currentPopup.gameObject);
                _currentPopup = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows a popup from a ScriptableObject configuration.
        /// </summary>
        public void ShowPopup(Popup_SO config, Action<int> onResult = null)
        {
            if (config == null)
            {
                Logger.LogError("UI", "Cannot show popup: config is null");
                return;
            }

            var request = PopupRequest.FromConfig(config, onResult);
            EnqueueAndProcess(request);
        }

        /// <summary>
        /// Shows a quick popup with runtime-provided strings.
        /// </summary>
        public void ShowPopup(string title, string message, string[] buttonLabels, Action<int> onResult = null)
        {
            var request = PopupRequest.Quick(title, message, buttonLabels, onResult);
            EnqueueAndProcess(request);
        }

        /// <summary>
        /// Shows a popup with a custom prefab.
        /// </summary>
        public void ShowPopup(UIPopup customPrefab, string title, string message, string[] buttonLabels, Action<int> onResult = null)
        {
            var request = new PopupRequest
            {
                CustomPrefab = customPrefab,
                TitleOverride = title,
                MessageOverride = message,
                ButtonLabels = buttonLabels,
                OnResult = onResult
            };
            EnqueueAndProcess(request);
        }

        /// <summary>
        /// Handles cancel input from the navigation system.
        /// </summary>
        public void HandleCancelInput()
        {
            if (_currentPopup != null)
            {
                if (debug)
                {
                    Logger.Log("UI", "UIPopupService handling cancel input");
                }

                _currentPopup.HandleCancel();
            }
        }

        /// <summary>
        /// Immediately closes the current popup without triggering any button callback.
        /// </summary>
        public void ForceCloseCurrentPopup()
        {
            if (_currentPopup != null)
            {
                if (debug)
                {
                    Logger.Log("UI", "Force closing current popup");
                }

                if (enablePooling)
                {
                    ReturnToPool(_currentPopup);
                    _currentPopup = null;
                }
                else
                {
                    Destroy(_currentPopup.gameObject);
                    _currentPopup = null;
                }

                RestoreFocus();
                ProcessQueue();
            }
        }

        #endregion

        #region Event Handler

        private void OnPopupRequested(PopupRequest request)
        {
            EnqueueAndProcess(request);
        }

        #endregion

        #region Internal Methods

        private void EnqueueAndProcess(PopupRequest request)
        {
            _queue.Enqueue(request);

            if (debug)
            {
                Logger.Log("UI", $"Popup request enqueued. Queue size: {_queue.Count}");
            }

            ProcessQueue();
        }

        private void ProcessQueue()
        {
            // Don't process if there's already an active popup
            if (_currentPopup != null)
            {
                return;
            }

            // Check if queue has items
            if (_queue.Count == 0)
            {
                return;
            }

            // Dequeue and show
            var request = _queue.Dequeue();
            ShowPopupInternal(request);
        }

        private void ShowPopupInternal(PopupRequest request)
        {
            // Save current focus
            SaveFocus();

            // Determine which prefab to use
            UIPopup prefab = GetPrefab(request);
#if UNITY_ADDRESSABLESn            bool usingAddressable = prefab == null && _addressablePrefab != null;
#else
            const bool usingAddressable = false;
#endif
            if (prefab == null && !usingAddressable)
            {
                Logger.LogError("UI", "Cannot show popup: no prefab available");
                RestoreFocus();
                ProcessQueue();
                return;
            }

            // Get container parent
            Transform parent = popupContainer != null ? popupContainer : transform;

            // Instantiate or reuse popup
            UIPopup instance = null;
            if (enablePooling)
            {
                // Try reuse a pooled instance that matches the prefab name
                for (int i = _pool.Count - 1; i >= 0; i--)
                {
                    if (_pool[i] != null && _pool[i].gameObject.name.Contains(prefab != null ? prefab.name : string.Empty))
                    {
                        instance = _pool[i];
                        _pool.RemoveAt(i);
                        break;
                    }
                }
            }

            GameObject popupGO = null;
#if UNITY_ADDRESSABLES
            if (instance == null && _addressablePrefab != null && prefab == defaultPrefab)
            {
                popupGO = Instantiate(_addressablePrefab, parent);
            }
            else
#endif
            if (instance == null)
            {
                popupGO = Instantiate(prefab.gameObject, parent);
            }

            if (instance != null)
            {
                _currentPopup = instance;
                _currentPopup.transform.SetParent(parent, false);
                _currentPopup.gameObject.SetActive(true);
            }
            else
            {
                _currentPopup = popupGO.GetComponent<UIPopup>();
n                if (_currentPopup == null)
                {
                    Logger.LogError("UI", "Instantiated prefab does not have UIPopup component");
#if UNITY_ADDRESSABLESn                    if (popupGO != null) Destroy(popupGO);
#else
                    Destroy(popupGO);
#endif
                    RestoreFocus();
                    ProcessQueue();
                    return;
                }
            }

            // Setup popup based on request type
            if (request.Config != null)
            {
                // Use config, but allow overrides
                _currentPopup.Setup(request.Config, WrapCallback(request.OnResult));

                // Apply overrides if specified
                if (!string.IsNullOrEmpty(request.TitleOverride) || !string.IsNullOrEmpty(request.MessageOverride))
                {
                    // For overrides, we need to re-setup with the override values
                    // This is a simplified approach; in a full implementation you might want
                    // to allow partial overrides without re-creating buttons
                }
            }
            else
            {
                // Use runtime strings
                _currentPopup.Setup(
                    request.TitleOverride ?? string.Empty,
                    request.MessageOverride ?? string.Empty,
                    request.ButtonLabels ?? new[] { "OK" },
                    WrapCallback(request.OnResult)
                );
            }

            // Show the popup
            _currentPopup.Show();

            if (debug)
            {
                Logger.Log("UI", "Popup shown");
            }
        }

        private Action<int> WrapCallback(Action<int> originalCallback)
        {
            return buttonIndex =>
            {
                // Invoke original callback first
                originalCallback?.Invoke(buttonIndex);

                // Clean up
                if (_currentPopup != null)
                {
                    if (enablePooling)
                        ReturnToPool(_currentPopup);
                    else
                        Destroy(_currentPopup.gameObject);
                    _currentPopup = null;
                }

                // Restore focus
                RestoreFocus();

                // Process next in queue
                ProcessQueue();
            };
        }

#if UNITY_ADDRESSABLES
        private void Start()
        {
            if (popupAddressable != null)
            {
                popupAddressable.LoadAssetAsync<GameObject>().Completed += handle =>
                {
                    if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                        _addressablePrefab = handle.Result;
                    else
                        Logger.LogWarning("UI", $"[UIPopupService] Failed loading addressable popup prefab on '{name}'");
                };
            }
        }
#endif

        private void ReturnToPool(UIPopup popup)
        {
            if (popup == null) return;
            popup.gameObject.SetActive(false);
            popup.transform.SetParent(transform, false);
            _pool.Add(popup);
            if (debug) Logger.LogVerbose("UI", $"Returned popup to pool: {popup.name}");
        }

        private UIPopup GetPrefab(PopupRequest request)
        {
            // Priority: request custom prefab > config custom prefab > default prefab
            if (request.CustomPrefab != null)
            {
                return request.CustomPrefab;
            }

            if (request.Config != null && request.Config.customPrefab != null)
            {
                return request.Config.customPrefab;
            }

            return defaultPrefab;
        }

        private void SaveFocus()
        {
            if (EventSystem.current != null)
            {
                _savedSelection = EventSystem.current.currentSelectedGameObject;

                if (debug && _savedSelection != null)
                {
                    Logger.LogVerbose("UI", $"Saved focus: {_savedSelection.name}");
                }
            }
        }

        private void RestoreFocus()
        {
            if (EventSystem.current != null && _savedSelection != null)
            {
                // Only restore if the saved object is still valid and active
                if (_savedSelection.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(_savedSelection);

                    if (debug)
                    {
                        Logger.LogVerbose("UI", $"Restored focus: {_savedSelection.name}");
                    }
                }

                _savedSelection = null;
            }
        }

        #endregion
    }
}