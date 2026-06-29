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
        public string Id { get; private set; } = Guid.NewGuid().ToString();

        public UIContainer Container { get; protected set; }

        public event Action<IUIPopup> Closed;

        protected PopupRequest Request;
        protected PopupHandle Handle;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            Container = GetComponent<UIContainer>();
        }

        #endregion

        #region Public API (IUIPopup)

        public virtual Task Initialize(PopupRequest request)
        {
            Request = request;
            Logging.Logger.LogVerbose("UI.PopUp", $"Base initialisation for popup '{Id}'", this);
            return Task.CompletedTask;
        }

        public virtual void InjectHandle(PopupHandle handle)
        {
            Handle = handle;
        }

        public virtual void Show()
        {
            Container.ShowContainer();
            Container.onHide.AddListener(OnClose);
        }

        public virtual void OnClose()
        {
            Container.onHide.RemoveListener(OnClose);
            Closed?.Invoke(this);
        }

        public void ClosePopUp()
        {
            Container.HideContainer();
        }

        public virtual void HandleCancel()
        {
            ClosePopUp();
        }

        #endregion
    }
}