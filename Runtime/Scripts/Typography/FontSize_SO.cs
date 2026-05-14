using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Font Size", fileName = "FontSize")]
    public class FontSize_SO : ScriptableObject
    {
        [SerializeField] private float size = 14f;
        public float Size => size;
    }
}
