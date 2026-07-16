using System;
using System.Threading.Tasks;
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Interface for any popup UI element managed by the popup service.
    /// </summary>
    public interface IUIPopup
    {
        /// <summary>Unique identifier for this popup instance.</summary>
        string Id { get; }

        /// <summary>The <see cref="UIContainer"/> that controls the popup's visibility.</summary>
        UIContainer Container { get; }

        /// <summary>Event raised when the popup is closed.</summary>
        event Action<IUIPopup> Closed;

        /// <summary>Initialises the popup with the given request data.</summary>
        Task Initialize(PopupRequest request);

        /// <summary>Injects the handle that holds the addressable reference for cleanup.</summary>
        void InjectHandle(PopupHandle handle);

        /// <summary>Shows the popup.</summary>
        void Show();

        /// <summary>Closes the popup.</summary>
        void ClosePopUp();

        /// <summary>Handles cancellation input (e.g. Escape key).</summary>
        void HandleCancel();
    }

    /// <summary>
    /// Holds references to a popup instance and its associated addressable handle
    /// so that the asset can be properly released when no longer needed.
    /// </summary>
    public class PopupHandle
    {
        /// <summary>The ID of the popup that owns this handle.</summary>
        public string PopupId;

        /// <summary>The addressable operation handle for the popup's prefab, if loaded via Addressables.</summary>
        public AsyncOperationHandle<GameObject>? AddressableHandle;

        /// <summary>
        /// Releases the addressable asset if it is still valid.
        /// </summary>
        public void Release()
        {
            if (AddressableHandle.HasValue && AddressableHandle.Value.IsValid())
            {
                Logging.Logger.LogVerbose("UI.PopUp", $"Releasing addressable handle for popup '{PopupId}'");
                UnityEngine.AddressableAssets.Addressables.Release(AddressableHandle.Value);
            }
        }
    }
}