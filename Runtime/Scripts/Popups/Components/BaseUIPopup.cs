using System;
using System.Threading.Tasks;
using HelloDev.UI.Default;
using UnityEngine;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Base class for all popups, providing common lifecycle and event handling.
    /// </summary>
    public abstract class BaseUIPopup : MonoBehaviour, IUIPopup
    {
        /// <summary>
        /// Unique identifier for this popup instance (used internally for pooling).
        /// </summary>
        public string Id { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The <see cref="UIContainer"/> that controls the popup's visibility.
        /// </summary>
        public UIContainer Container { get; protected set; }

        /// <summary>
        /// Event raised when the popup is closed.
        /// </summary>
        public event Action<IUIPopup> Closed;

        protected PopupRequest Request;
        protected PopupHandle Handle;

        /// <summary>
        /// Gets the <see cref="UIContainer"/> component on Awake.
        /// </summary>
        protected virtual void Awake()
        {
            Container = GetComponent<UIContainer>();
        }

        /// <summary>
        /// Initialises the popup with the given request data.
        /// </summary>
        public virtual Task Initialize(PopupRequest request)
        {
            Request = request;
            Logging.Logger.LogVerbose("UI.PopUp", "Base initialisation completed", this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Injects the handle that holds the addressable reference for cleanup.
        /// </summary>
        public virtual void InjectHandle(PopupHandle handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Shows the popup and registers the close listener.
        /// </summary>
        public virtual void Show()
        {
            Container.ShowContainer();
            Container.onHide.AddListener(OnClose);
        }

        /// <summary>
        /// Called when the container finishes hiding. Invokes the <see cref="Closed"/> event.
        /// </summary>
        public virtual void OnClose()
        {
            Container.onHide.RemoveListener(OnClose);
            Closed?.Invoke(this);
        }

        /// <summary>
        /// Closes the popup by hiding the container.
        /// </summary>
        public void ClosePopUp()
        {
            Container.HideContainer();
        }

        /// <summary>
        /// Handles cancellation input (e.g. Escape key) – default implementation closes the popup.
        /// </summary>
        public virtual void HandleCancel()
        {
            ClosePopUp();
        }
    }
}