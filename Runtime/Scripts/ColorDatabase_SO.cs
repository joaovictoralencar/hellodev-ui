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

            public void EnsureId()
            {
                if (string.IsNullOrEmpty(Id))
                    Id = Guid.NewGuid().ToString("N");
            }
        }

        [Serializable]
        public class ColorValue
        {
            public string SlotId;
            public Color Color = Color.white;
            public string Hex; // HTML hex without '#'

            public void SyncHex()
            {
                Hex = ColorUtility.ToHtmlStringRGBA(Color);
            }
        }

        [Serializable]
        public class ColorTheme
        {
            public string Id;
            public string DisplayName;
            public List<ColorValue> Values = new List<ColorValue>();

            public void EnsureId()
            {
                if (string.IsNullOrEmpty(Id))
                    Id = Guid.NewGuid().ToString("N");
            }

            public Color? GetColor(string slotId)
            {
                var v = Values.Find(x => x.SlotId == slotId);
                if (v != null) return v.Color;
                return null;
            }
        }

        [SerializeField] private List<ColorSlot> slots = new List<ColorSlot>();
        [SerializeField] private List<ColorTheme> themes = new List<ColorTheme>();
        [SerializeField] private string activeThemeId;

        public IReadOnlyList<ColorSlot> Slots => slots;
        public IReadOnlyList<ColorTheme> Themes => themes;

        public string ActiveThemeId
        {
            get
            {
                if (string.IsNullOrEmpty(activeThemeId) && themes.Count > 0)
                    return themes[0].Id;
                return activeThemeId;
            }
            set => activeThemeId = value;
        }

        private void OnValidate()
        {
            EnsureIds();
            SyncHexes();
        }

        private void EnsureIds()
        {
            foreach (var s in slots) s.EnsureId();
            foreach (var t in themes) t.EnsureId();
        }

        private void SyncHexes()
        {
            foreach (var t in themes)
            {
                foreach (var v in t.Values)
                    v.SyncHex();
            }
        }

        public Color GetColorForSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return Color.white;

            ColorTheme theme = null;
            if (!string.IsNullOrEmpty(ActiveThemeId))
                theme = themes.Find(t => t.Id == ActiveThemeId);

            if (theme == null && themes.Count > 0)
                theme = themes[0];

            if (theme != null)
            {
                var val = theme.Values.Find(v => v.SlotId == slotId);
                if (val != null) return val.Color;
            }

            var slot = slots.Find(s => s.Id == slotId);
            if (slot != null && slot.ColourSO != null)
                return slot.ColourSO.Colour;

            return Color.white;
        }

    }
}