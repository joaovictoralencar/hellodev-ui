using HelloDev.Events;
using UnityEngine;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// GameEvent for requesting popups without direct service reference.
    /// Any script can raise this event to show a popup.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/UI/Events/Popup Request Event")]
    public class PopupRequestEvent : GameEvent_SO<PopupRequest>
    {
    }
}
