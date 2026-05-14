using System;
using UnityEngine;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Defines per-state visual styling for any UISelectable (Button, Toggle, InputField, etc.).
    /// Assign to UISelectable.style — no separate binder components needed.
    ///
    /// Background and Text map to ColourSlot_SOs; the active theme in the UIDatabase resolves
    /// the actual colour at runtime.
    ///
    /// Note: if a Unity Button/Toggle component is present, set its Transition to None so
    /// the built-in color block does not override the style system.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/UI/Selectable Style", fileName = "SelectableStyle")]
    public class UISelectableStyle_SO : ScriptableObject
    {
        [Serializable]
        public class StateStyle
        {
            [Tooltip("Colour slot for the background graphic (Image, etc.)")]
            public ColourSlot_SO Background;

            [Tooltip("Colour slot for the label text (TMP_Text).")]
            public ColourSlot_SO Text;
        }

        [Header("States")]
        [SerializeField] private StateStyle normal      = new StateStyle();
        [SerializeField] private StateStyle highlighted = new StateStyle();
        [SerializeField] private StateStyle pressed     = new StateStyle();
        [SerializeField] private StateStyle selected    = new StateStyle();
        [SerializeField] private StateStyle disabled    = new StateStyle();

        [Header("Animation")]
        [SerializeField] private bool  scaleOnSelect = false;
        [SerializeField] private float scaledSize    = 1.05f;
        [SerializeField] private float scaleTime     = 0.15f;

        public StateStyle Normal      => normal;
        public StateStyle Highlighted => highlighted;
        public StateStyle Pressed     => pressed;
        public StateStyle Selected    => selected;
        public StateStyle Disabled    => disabled;

        public bool  ScaleOnSelect => scaleOnSelect;
        public float ScaledSize    => scaledSize;
        public float ScaleTime     => scaleTime;

        public StateStyle GetStateStyle(UISelectable.SelectableState state)
        {
            return state switch
            {
                UISelectable.SelectableState.Normal      => normal,
                UISelectable.SelectableState.Highlighted => highlighted,
                UISelectable.SelectableState.Pressed     => pressed,
                UISelectable.SelectableState.Selected    => selected,
                UISelectable.SelectableState.Disabled    => disabled,
                _                                        => normal
            };
        }
    }
}
