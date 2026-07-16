using UnityEngine;

namespace HelloDev.UI.Popups
{
    /// <summary>
    /// ScriptableObject asset that holds a <see cref="PopupRequest"/> for reuse.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/UI/Popup")]
    public class Popup_SO : ScriptableObject
    {
        [Tooltip("The popup configuration.")]
        public PopupRequest Request;
    }
}