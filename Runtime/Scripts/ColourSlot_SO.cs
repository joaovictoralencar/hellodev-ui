using System;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Colour Slot", fileName = "ColourSlot")]
    public class ColourSlot_SO : ScriptableObject
    {
        [HideInInspector] [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [SerializeField] private string tag;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private Colour_SO defaultColour;

        public string Id { get { EnsureId(); return id; } }
        public string DisplayName => displayName;
        public string Role => role;
        public string Tag => tag;
        public string Description => description;
        public Colour_SO DefaultColour => defaultColour;

        public void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString("N");
        }

        private void OnEnable() => EnsureId();

#if UNITY_EDITOR
        public void EditorSetup(string newDisplayName, string newRole, string newTag, string newDescription, Colour_SO newDefaultColour)
        {
            displayName   = newDisplayName;
            role          = newRole;
            tag           = newTag;
            description   = newDescription;
            defaultColour = newDefaultColour;
            EnsureId();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
