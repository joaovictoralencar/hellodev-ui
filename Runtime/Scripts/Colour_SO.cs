using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Colour", fileName = "Colour")]
    public class Colour_SO : ScriptableObject
    {
        [SerializeField] private Color _colour = new Color(1, 1, 1, 1);
        public Color Colour => _colour;
    }
}