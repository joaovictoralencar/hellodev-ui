using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Image))]
    public class ImageColorBinder : BaseColorBinder
    {        [SerializeField] private Image target;
        protected override void ApplyColor(Color color)
        {            if (target == null) target = GetComponent<Image>();            if (target != null) target.color = color;        }    }
}
