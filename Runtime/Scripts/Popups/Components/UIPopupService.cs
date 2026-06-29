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
        [Tooltip("Fallback prefab used if no other prefab is provided.")]
        [SerializeField] private GameObject defaultPrefab;
        [Tooltip("Fallback addressable prefab used if no other reference is provided.")]
        [SerializeField] private AssetReferenceGameObject defaultPrefabReference;
        [Tooltip("Parent RectTransform under which popups are instantiated.")]
        [SerializeField] private RectTransform popupContainer;
        [Tooltip("If true, popups are pooled instead of destroyed.")]
        [SerializeField] private bool reusePopup = true;

        // Queuing
        private readonly Queue<PopupRequest> _queue = new();
        private readonly List<IUIPopup> _activePopups = new();
        private bool _isProcessing;

        // Handle tracking per popup instance
        private readonly Dictionary<IUIPopup, PopupHandle> _handles = new();

        // Pooling: maps popup ID (GUID) to a stack of inactive instances
        private readonly Dictionary<string, Stack<IUIPopup>> _popUpPools = new();

        // Caches to resolve a popup ID from a prefab/asset reference before instantiation
        private readonly Dictionary<GameObject, string> _spawnedPrefabs = new();
        private readonly Dictionary<string, string> _spawnedReferences = new(); // key = addressable runtime key string, value = popup ID

        #region Public API (IUIPopupService)

        /// <summary>
        /// Whether any popup is currently active (visible).
        /// </summary>
        public bool HasActivePopup => _activePopups.Count > 0;

        /// <summary>
        /// Number of currently active popups.
        /// </summary>
        public int PopupCount => _activePopups.Count;

        /// <summary>
        /// The topmost (most recently shown) active popup.
        /// </summary>
        public IUIPopup CurrentPopup => _activePopups.Count > 0 ? _activePopups[^1] : null;

        /// <summary>
        /// Shows a popup from a custom <see cref="PopupRequest"/>.
        /// </summary>
        public async Task<IUIPopup> ShowPopup(PopupRequest request)
        {
            _queue.Enqueue(request);
            await ProcessQueue();
            return CurrentPopup;
        }

        /// <summary>
        /// Shows a popup defined by a <see cref="Popup_SO"/> asset.
        /// </summary>
        public Task<IUIPopup> ShowPopup(Popup_SO popupSO, Action<IUIPopup>[] callbacks = null)
        {
            return ShowPopup(popupSO.Request);
        }

        /// <summary>
        /// Shows a popup and casts the result to a specific type.
        /// </summary>
        public async Task<T> ShowPopup<T>(PopupRequest request) where T : Component, IUIPopup
        {
            return (T)await ShowPopup(request);
        }

        /// <summary>
        /// Closes a specific popup instance.
        /// </summary>
        public void ClosePopup(IUIPopup popup) => popup?.ClosePopUp();

        /// <summary>
        /// Closes the first active popup with the given ID.
        /// </summary>
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

        /// <summary>
        /// Closes the topmost active popup.
        /// </summary>
        public void CloseTopPopup() => CurrentPopup?.ClosePopUp();

        /// <summary>
        /// Closes all active popups.
        /// </summary>
        public void CloseAll()
        {
            var internalActiveList = new List<IUIPopup>(_activePopups);
            foreach (var p in internalActiveList) p.ClosePopUp();
        }

        /// <summary>
        /// Forwards a cancel input to the current popup.
        /// </summary>
        public void HandleCancelInput() => CurrentPopup?.HandleCancel();

        #endregion

        #region Queue Processing

        /// <summary>
        /// Processes the queue of popup requests sequentially.
        /// </summary>
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
                string instanceName = popup.Container != null ? popup.Container.gameObject.name : "unknown";
                Logging.Logger.Log("UI.PopUp", $"Popup instance '{instanceName}' added, active count: {_activePopups.Count}", popup.Container.gameObject);
            }

            _isProcessing = false;
        }

        #endregion

        #region Popup Creation / Pooling

        /// <summary>
        /// Orchestrates the retrieval of a popup – either from the pool or by creating a new one.
        /// </summary>
        private async Task<IUIPopup> GetOrCreatePopupAsync(PopupRequest request)
        {
            // 1. Attempt to get a pooled instance
            if (TryGetFromPool(request, out var pooledPopup))
            {
                await InitializePopUp(request, pooledPopup);
                return pooledPopup;
            }

            // 2. No pooled instance available – create a new one
            Logging.Logger.LogVerbose("UI.PopUp", "Creating new popup instance", this);

            var handle = new PopupHandle();

            // 3. Load the prefab (handles direct, addressable, and fallbacks)
            GameObject prefab = await LoadPopupPrefabAsync(request, handle);
            if (prefab == null)
            {
                Logging.Logger.LogError("UI.PopUp", "No prefab available to instantiate popup", this);
                return null;
            }

            // 4. Instantiate and validate the popup
            (IUIPopup popup, GameObject instance) = InstantiatePopupInstance(prefab, popupContainer);
            if (popup == null || instance == null) return null;

            Logging.Logger.LogVerbose("UI.PopUp", $"New popup instance created: '{instance.name}'", instance);

            // 5. Initialise, store the handle, and cache the ID
            await InitializePopUp(request, popup);

            handle.PopupId = popup.Id;
            popup.InjectHandle(handle);
            _handles[popup] = handle;

            CachePopupId(request, popup);

            return popup;
        }

        /// <summary>
        /// Tries to retrieve a popup from the pool using the request's resolved ID.
        /// </summary>
        private bool TryGetFromPool(PopupRequest request, out IUIPopup popup)
        {
            popup = null;
            if (!reusePopup) return false;

            string targetId = ResolvePopupId(request);
            if (string.IsNullOrEmpty(targetId)) return false;

            if (_popUpPools.TryGetValue(targetId, out var poolStack) && poolStack.Count > 0)
            {
                popup = poolStack.Pop();
                string instanceName = popup.Container != null ? popup.Container.gameObject.name : "unknown";
                Logging.Logger.LogVerbose("UI.PopUp", $"Reusing pooled popup instance: '{instanceName}'", popup.Container.gameObject);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads the prefab for the popup, handling direct references, addressables, and fallbacks.
        /// Also stores the addressable handle in the provided <see cref="PopupHandle"/>.
        /// </summary>
        private async Task<GameObject> LoadPopupPrefabAsync(PopupRequest request, PopupHandle handle)
        {
            GameObject prefab = null;

            try
            {
                if (request.Prefab != null)
                {
                    prefab = request.Prefab;
                }
                else if (request.PrefabReference.IsValid())
                {
                    Logging.Logger.LogVerbose("UI.PopUp", "Loading popup prefab from addressables", this);
                    var op = Addressables.LoadAssetAsync<GameObject>(request.PrefabReference);
                    handle.AddressableHandle = op;
                    prefab = await op.Task;

                    if (op.Status != AsyncOperationStatus.Succeeded)
                    {
                        Logging.Logger.LogError("UI.PopUp", "Failed to load addressable prefab", this);
                        return null;
                    }
                }
                else if (defaultPrefabReference.IsValid())
                {
                    Logging.Logger.LogVerbose("UI.PopUp", "Loading default addressable prefab", this);
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
            }
            catch (Exception ex)
            {
                Logging.Logger.LogError("UI.PopUp", $"Exception while loading popup prefab: {ex.Message}", this);
                return null;
            }

            return prefab;
        }

        /// <summary>
        /// Instantiates the prefab, validates that it implements <see cref="IUIPopup"/>,
        /// and returns both the interface and the GameObject instance.
        /// </summary>
        private (IUIPopup popup, GameObject instance) InstantiatePopupInstance(GameObject prefab, Transform parent)
        {
            if (prefab == null) return (null, null);

            GameObject go = Instantiate(prefab, parent);
            IUIPopup popup = go.GetComponent<IUIPopup>();

            if (popup == null)
            {
                Logging.Logger.LogError("UI.PopUp", "Popup prefab does not implement IUIPopup interface", go);
                Destroy(go);
                return (null, null);
            }

            return (popup, go);
        }

        /// <summary>
        /// Caches the popup's ID against its prefab/addressable source so we can
        /// later resolve it for pooling.
        /// </summary>
        private void CachePopupId(PopupRequest request, IUIPopup popup)
        {
            if (request.Prefab != null)
                _spawnedPrefabs[request.Prefab] = popup.Id;
            else if (request.PrefabReference.IsValid())
                _spawnedReferences[request.PrefabReference.RuntimeKey.ToString()] = popup.Id;
            else
            {
                if (defaultPrefab != null) _spawnedPrefabs[defaultPrefab] = popup.Id;
                if (defaultPrefabReference.IsValid())
                    _spawnedReferences[defaultPrefabReference.RuntimeKey.ToString()] = popup.Id;
            }
        }

        /// <summary>
        /// Resolves the cached popup ID from any of the request’s sources
        /// (direct prefab, addressable reference, or default fallbacks).
        /// </summary>
        private string ResolvePopupId(PopupRequest request)
        {
            if (request.Prefab != null && _spawnedPrefabs.TryGetValue(request.Prefab, out string id))
                return id;

            if (request.PrefabReference.IsValid())
            {
                string key = request.PrefabReference.RuntimeKey.ToString();
                if (_spawnedReferences.TryGetValue(key, out string idFromRef))
                    return idFromRef;
            }

            if (defaultPrefabReference.IsValid())
            {
                string defaultKey = defaultPrefabReference.RuntimeKey.ToString();
                if (_spawnedReferences.TryGetValue(defaultKey, out string idFromDefaultRef))
                    return idFromDefaultRef;
            }

            if (defaultPrefab != null && _spawnedPrefabs.TryGetValue(defaultPrefab, out string idFromDefaultPrefab))
                return idFromDefaultPrefab;

            return null;
        }

        /// <summary>
        /// Initialises the popup, subscribes to its Closed event, and shows it.
        /// </summary>
        private async Task InitializePopUp(PopupRequest request, IUIPopup popup)
        {
            await popup.Initialize(request);
            popup.Closed += OnPopupClosed;
            popup.Show();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles popup closure – either pools or destroys the instance.
        /// </summary>
        private void OnPopupClosed(IUIPopup popup)
        {
            popup.Closed -= OnPopupClosed;
            _activePopups.Remove(popup);

            string instanceName = popup.Container != null ? popup.Container.gameObject.name : "unknown";
            Logging.Logger.Log("UI.PopUp", $"Popup instance '{instanceName}' closed, remaining active: {_activePopups.Count}", popup.Container.gameObject);

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
                    Destroy(popup.Container.gameObject);
            }
            else
            {
                // Otherwise, hide it and push it into the pool
                _popUpPools[popup.Id].Push(popup);
                Logging.Logger.LogVerbose("UI.PopUp", $"Popup instance '{instanceName}' pooled for reuse", popup.Container.gameObject);
            }
        }

        #endregion
    }
}