using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Service that handles showing, queuing, pooling, and closing popups.
    /// </summary>
    public class UIPopupService : MonoBehaviour, IUIPopupService
    {
        [SerializeField] private GameObject defaultPrefab;
        [SerializeField] private AssetReferenceGameObject defaultPrefabReference;
        [SerializeField] private RectTransform popupContainer;
        [SerializeField] private bool reusePopup = true;

        // Queuing
        private readonly Queue<PopupRequest> _queue = new();
        private readonly List<IUIPopup> _activePopups = new();
        private bool _isProcessing;

        // Handle tracking per popup instance
        private readonly Dictionary<IUIPopup, PopupHandle> _handles = new();

        // Pooling: maps popup ID to a stack of inactive instances
        private readonly Dictionary<string, Stack<IUIPopup>> _popUpPools = new();

        // Caches to resolve a popup ID from a prefab/asset reference before instantiation
        private readonly Dictionary<GameObject, string> _spawnedPrefabs = new();
        private readonly Dictionary<AssetReferenceGameObject, string> _spawnedReferences = new();

        #region Public API (IUIPopupService)

        public bool HasActivePopup => _activePopups.Count > 0;
        public int PopupCount => _activePopups.Count;
        public IUIPopup CurrentPopup => _activePopups.Count > 0 ? _activePopups[^1] : null;

        public async Task<IUIPopup> ShowPopup(PopupRequest request)
        {
            _queue.Enqueue(request);
            await ProcessQueue();
            return CurrentPopup;
        }

        public Task<IUIPopup> ShowPopup(Popup_SO popupSO, Action<IUIPopup>[] callbacks = null)
        {
            return ShowPopup(popupSO.Request);
        }

        public async Task<T> ShowPopup<T>(PopupRequest request) where T : Component, IUIPopup
        {
            return (T)await ShowPopup(request);
        }

        public void ClosePopup(IUIPopup popup)
        {
            popup?.ClosePopUp();
        }

        public void ClosePopup(string popupId)
        {
            var internalActiveList = new List<IUIPopup>(_activePopups);
            foreach (var p in internalActiveList)
            {
                if (p.Id == popupId)
                {
                    p.ClosePopUp();
                    break;
                }
            }
        }

        public void CloseTopPopup()
        {
            CurrentPopup?.ClosePopUp();
        }

        public void CloseAll()
        {
            var internalActiveList = new List<IUIPopup>(_activePopups);
            foreach (var p in internalActiveList)
            {
                p.ClosePopUp();
            }
        }

        public void HandleCancelInput()
        {
            CurrentPopup?.HandleCancel();
        }

        #endregion

        #region Queue Processing

        private async Task ProcessQueue()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            while (_queue.Count > 0)
            {
                var request = _queue.Dequeue();
                Logging.Logger.LogVerbose("UI.PopUp", $"Processing queued popup request: {request.Title ?? request.LocalizedTitle?.GetLocalizedString()}", this);

                var popup = await GetOrCreatePopupAsync(request);
                if (popup == null)
                {
                    Logging.Logger.LogWarning("UI.PopUp", "Failed to create popup, skipping request", this);
                    continue;
                }

                _activePopups.Add(popup);
                Logging.Logger.Log("UI.PopUp", $"Popup '{popup.Id}' added, active count: {_activePopups.Count}", this);
            }

            _isProcessing = false;
        }

        #endregion

        #region Popup Creation / Pooling

        private async Task<IUIPopup> GetOrCreatePopupAsync(PopupRequest request)
        {
            if (reusePopup)
            {
                // 1. Attempt to resolve the ID from previous allocations to check the pool
                string targetId = null;

                if (request.Prefab != null)
                    _spawnedPrefabs.TryGetValue(request.Prefab, out targetId);
                else if (request.PrefabReference != null)
                    _spawnedReferences.TryGetValue(request.PrefabReference, out targetId);
                else if (defaultPrefabReference != null)
                    _spawnedReferences.TryGetValue(defaultPrefabReference, out targetId);
                else if (defaultPrefab != null)
                    _spawnedPrefabs.TryGetValue(defaultPrefab, out targetId);

                // 2. Try picking up an available instance from the pool
                if (!string.IsNullOrEmpty(targetId) &&
                    _popUpPools.TryGetValue(targetId, out var poolStack) &&
                    poolStack.Count > 0)
                {
                    var pooledPopup = poolStack.Pop();
                    Logging.Logger.LogVerbose("UI.PopUp", $"Reusing pooled popup '{targetId}'", this);

                    await InitializePopUp(request, pooledPopup);
                    return pooledPopup;
                }
            }

            // 3. Fallback: instantiate a new copy
            Logging.Logger.LogVerbose("UI.PopUp", "Creating new popup instance", this);

            var handle = new PopupHandle();
            GameObject prefab = null;

            if (request.Prefab != null)
            {
                prefab = request.Prefab;
            }
            else if (request.PrefabReference != null)
            {
                var op = Addressables.LoadAssetAsync<GameObject>(request.PrefabReference);
                handle.AddressableHandle = op;
                prefab = await op.Task;

                if (op.Status != AsyncOperationStatus.Succeeded)
                {
                    Logging.Logger.LogError("UI.PopUp", "Failed to load addressable prefab", this);
                    return null;
                }
            }
            else if (defaultPrefabReference != null)
            {
                var op = Addressables.LoadAssetAsync<GameObject>(defaultPrefabReference);
                handle.AddressableHandle = op;
                prefab = await op.Task;

                if (op.Status != AsyncOperationStatus.Succeeded)
                {
                    Logging.Logger.LogWarning("UI.PopUp", "Default addressable prefab failed, falling back to defaultPrefab", this);
                    prefab = defaultPrefab;
                }
            }
            else
            {
                prefab = defaultPrefab;
            }

            if (prefab == null)
            {
                Logging.Logger.LogError("UI.PopUp", "No prefab available to instantiate popup", this);
                return null;
            }

            var go = Instantiate(prefab, popupContainer);
            var popup = go.GetComponent<IUIPopup>();

            if (popup == null)
            {
                Logging.Logger.LogError("UI.PopUp", "Popup prefab does not implement IUIPopup interface", this);
                Destroy(go);
                return null;
            }

            await InitializePopUp(request, popup);

            handle.PopupId = popup.Id;
            handle.PrefabInstance = go;
            popup.InjectHandle(handle);
            _handles[popup] = handle;

            // Cache the resolved ID for future pooling lookups
            if (request.Prefab != null)
                _spawnedPrefabs[request.Prefab] = popup.Id;
            else if (request.PrefabReference != null)
                _spawnedReferences[request.PrefabReference] = popup.Id;
            else
            {
                if (defaultPrefab != null) _spawnedPrefabs[defaultPrefab] = popup.Id;
                if (defaultPrefabReference != null) _spawnedReferences[defaultPrefabReference] = popup.Id;
            }

            return popup;
        }

        private async Task InitializePopUp(PopupRequest request, IUIPopup popup)
        {
            await popup.Initialize(request);
            popup.Closed += OnPopupClosed;
            popup.Show();
        }

        #endregion

        #region Event Handlers

        private void OnPopupClosed(IUIPopup popup)
        {
            popup.Closed -= OnPopupClosed;
            _activePopups.Remove(popup);
            Logging.Logger.Log("UI.PopUp", $"Popup '{popup.Id}' closed, remaining active: {_activePopups.Count}", this);

            if (!reusePopup)
            {
                Destroy(popup.Container.gameObject);
                return;
            }

            // Pool or destroy based on whether we already have a pooled instance
            if (!_popUpPools.TryGetValue(popup.Id, out var poolStack))
            {
                poolStack = new Stack<IUIPopup>();
                _popUpPools[popup.Id] = poolStack;
            }

            if (poolStack.Count > 0)
            {
                // We already have one in the pool – destroy this extra copy
                if (_handles.TryGetValue(popup, out var handle))
                {
                    handle.Release();
                    _handles.Remove(popup);
                }

                if (popup.Container != null && popup.Container.gameObject != null)
                {
                    Destroy(popup.Container.gameObject);
                }
            }
            else
            {
                // Otherwise, hide it and push it into the pool
                _popUpPools[popup.Id].Push(popup);
                Logging.Logger.LogVerbose("UI.PopUp", $"Popup '{popup.Id}' pooled for reuse", this);
            }
        }

        #endregion
    }
}