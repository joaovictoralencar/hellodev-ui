using UnityEngine;
using TMPro;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPColorBinder : BaseColorBinder
    {        [SerializeField] private TextMeshProUGUI target;
        protected override void ApplyColor(Color color)
        {            if (target == null) target = GetComponent<TextMeshProUGUI>();            if (target != null) target.color = color;        }    }
}
