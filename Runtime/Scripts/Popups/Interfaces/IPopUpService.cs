using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// Service that manages the lifecycle of popups, including showing, closing,
    /// and pooling them.
    /// </summary>
    public interface IUIPopupService
    {
        /// <summary>Whether any popup is currently active (visible).</summary>
        bool HasActivePopup { get; }

        /// <summary>Number of currently active popups.</summary>
        int PopupCount { get; }

        /// <summary>The topmost (most recently shown) active popup.</summary>
        IUIPopup CurrentPopup { get; }

        /// <summary>Shows a popup defined by a <see cref="Popup_SO"/> asset.</summary>
        Task<IUIPopup> ShowPopup(Popup_SO popupSO, Action<IUIPopup>[] callbacks = null);

        /// <summary>Shows a popup from a custom <see cref="PopupRequest"/>.</summary>
        Task<IUIPopup> ShowPopup(PopupRequest request);

        /// <summary>Shows a popup and casts the result to a specific type.</summary>
        Task<T> ShowPopup<T>(PopupRequest request) where T : Component, IUIPopup;

        /// <summary>Closes a specific popup instance.</summary>
        void ClosePopup(IUIPopup popup);

        /// <summary>Closes the first active popup with the given ID.</summary>
        void ClosePopup(string popupId);

        /// <summary>Closes the topmost active popup.</summary>
        void CloseTopPopup();

        /// <summary>Closes all active popups.</summary>
        void CloseAll();

        /// <summary>Forwards a cancel input to the current popup.</summary>
        void HandleCancelInput();
    }
}