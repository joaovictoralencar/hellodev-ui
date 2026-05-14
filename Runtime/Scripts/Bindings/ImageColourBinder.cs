using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Image))]
    public class ImageColourBinder : BaseColourBinder
    {
        [SerializeField] private Image target;

        protected override void ApplyColour(Color colour)
        {
            if (target == null) target = GetComponent<Image>();
            if (target != null) target.color = colour;
        }
    }
}
