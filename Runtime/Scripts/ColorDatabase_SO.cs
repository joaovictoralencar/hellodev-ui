using System;
using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.UI.Default
{
    [CreateAssetMenu(menuName = "HelloDev/UI/Color Database", fileName = "ColorDatabase")]
    public class ColorDatabase_SO : ScriptableObject
    {
        [Serializable]
        public class ColorSlot
        {
            public string Id;
            public string Name;
            public string Role;
            public string Tag;
            [TextArea] public string Description;
            public Colour_SO ColourSO;
n            public void EnsureId()
            {
                if (string.IsNullOrEmpty(Id))
                    Id = Guid.NewGuid().ToString("N");
            }
        }
n        [Serializable]
        public class ColorValue
        {
            public string SlotId;
            public Color Color = Color.white;
            public string Hex; // HTML hex without '#'
n            public void SyncHex()
            {
                Hex = ColorUtility.ToHtmlStringRGBA(Color);
            }
        }
n        [Serializable]
        public class ColorTheme
        {
            public string Id;
            public string DisplayName;
            public List<ColorValue> Values = new List<ColorValue>();
n            public void EnsureId()
            {
                if (string.IsNullOrEmpty(Id))
                    Id = Guid.NewGuid().ToString("N");
            }
n            public Color? GetColor(string slotId)
            {
                var v = Values.Find(x => x.SlotId == slotId);
                if (v != null) return v.Color;
                return null;
            }
        }
n        [SerializeField] private List<ColorSlot> slots = new List<ColorSlot>();
        [SerializeField] private List<ColorTheme> themes = new List<ColorTheme>();
        [SerializeField] private string activeThemeId;
n        public IReadOnlyList<ColorSlot> Slots => slots;
        public IReadOnlyList<ColorTheme> Themes => themes;
n        public string ActiveThemeId
        {
            get
            {
                if (string.IsNullOrEmpty(activeThemeId) && themes.Count > 0)
                    return themes[0].Id;
                return activeThemeId;
            }
            set => activeThemeId = value;
        }
n        private void OnValidate()
        {
            EnsureIds();
            SyncHexes();
        }
n        private void EnsureIds()
        {
            foreach (var s in slots) s.EnsureId();
            foreach (var t in themes) t.EnsureId();
        }
n        private void SyncHexes()
        {
            foreach (var t in themes)
            {
                foreach (var v in t.Values)
                    v.SyncHex();
            }
        }
n        public Color GetColorForSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return Color.white;
n            ColorTheme theme = null;
            if (!string.IsNullOrEmpty(ActiveThemeId))
                theme = themes.Find(t => t.Id == ActiveThemeId);
n            if (theme == null && themes.Count > 0)
                theme = themes[0];
n            if (theme != null)
            {
                var val = theme.Values.Find(v => v.SlotId == slotId);
                if (val != null) return val.Color;
            }
n            var slot = slots.Find(s => s.Id == slotId);
            if (slot != null && slot.ColourSO != null)
                return slot.ColourSO.Colour;
n            return Color.white;
        }
n#if UNITY_EDITOR
        [ContextMenu("Generate Constants")]
        public void GenerateConstants()
        {n            HelloDev.UI.Default.Editor.ColorIdGenerator.GenerateForDatabase(this);
        }
#endif
    }
}
