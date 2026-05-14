using TMPro;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPColourBinder : BaseColourBinder
    {
        [SerializeField] private TextMeshProUGUI target;

        protected override void ApplyColour(Color colour)
        {
            if (target == null) target = GetComponent<TextMeshProUGUI>();
            if (target != null) target.color = colour;
        }
    }
}
