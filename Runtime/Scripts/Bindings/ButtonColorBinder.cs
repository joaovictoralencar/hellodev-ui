using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Button))]
    public class ButtonColorBinder : BaseColorBinder
    {        [SerializeField] private Button target;        [SerializeField] private bool tintAllStates = true;        [SerializeField] private float pressedMultiplier = 0.9f;        protected override void ApplyColor(Color color)        {            if (target == null) target = GetComponent<Button>();            if (target != null)            {                var cb = target.colors;                if (tintAllStates)                {                    cb.normalColor = color;                    cb.highlightedColor = color;                    cb.pressedColor = color * pressedMultiplier;                    cb.selectedColor = color;                }                else                {                    cb.normalColor = color;                }                target.colors = cb;            }        }    }
}
