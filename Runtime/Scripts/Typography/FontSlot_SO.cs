using System;
using TMPro;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Font Slot", fileName = "FontSlot")]
    public class FontSlot_SO : ScriptableObject
    {
        [HideInInspector] [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private TMP_FontAsset defaultFont;

        public string Id { get { EnsureId(); return id; } }
        public string DisplayName => displayName;
        public string Role => role;
        public string Description => description;
        public TMP_FontAsset DefaultFont => defaultFont;

        public void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString("N");
        }

        private void OnEnable() => EnsureId();

#if UNITY_EDITOR
        public void EditorSetup(string newDisplayName, string newRole, string newDescription, TMP_FontAsset newDefaultFont)
        {
            displayName = newDisplayName;
            role        = newRole;
            description = newDescription;
            defaultFont = newDefaultFont;
            EnsureId();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
