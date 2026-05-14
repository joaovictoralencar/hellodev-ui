using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Optional add-on component for any UISelectable (or standalone use).
    /// Owns the style/database/graphic/label references so the base UISelectable
    /// stays lean and style-agnostic.
    ///
    /// Quick setup:
    ///   1. Add this component next to UIButton / UIToggle / UIInputField.
    ///   2. Assign a UISelectableStyle_SO to <see cref="style"/>.
    ///   3. Right-click → "Auto-Discover References" to fill in graphic + label.
    ///   4. No database reference needed — the scene's UIThemeRuntime is discovered automatically.
    /// </summary>
    [AddComponentMenu("HelloDev UI/Colour Style")]
    public class UIColourStyle : MonoBehaviour
    {
        [SerializeField] private UISelectableStyle_SO style;

        [Tooltip("The graphic whose colour is driven by the Background slot. Auto-wired if left empty.")]
        [SerializeField] private Graphic backgroundGraphic;

        [Tooltip("The label text driven by the Text slot. Auto-wired if left empty.")]
        [SerializeField] private TMP_Text labelText;

        public UISelectableStyle_SO Style => style;

        public void Apply(UISelectable.SelectableState state)
        {
            if (style == null || runtime == null) return;
            var stateStyle = style.GetStateStyle(state);
            if (stateStyle != null)
                ApplyWithRuntime(stateStyle, runtime);
        }

        private UIThemeRuntime runtime;
        private UISelectable selectable;
        private Coroutine resolveCoroutine;
        private System.Action<UITheme_SO> themeChangedHandler;

        private void Awake()
        {
            if (backgroundGraphic == null)
                backgroundGraphic = GetComponent<Graphic>();
            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(includeInactive: true);
            selectable = GetComponent<UISelectable>();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                resolveCoroutine = StartCoroutine(ResolveDatabase());
        }

        private void OnDisable()
        {
            if (resolveCoroutine != null)
            {
                StopCoroutine(resolveCoroutine);
                resolveCoroutine = null;
            }

            if (runtime != null && themeChangedHandler != null)
            {
                runtime.OnThemeChanged -= themeChangedHandler;
                runtime = null;
            }
        }

        private IEnumerator ResolveDatabase()
        {
            bool ready = false;
            int frames = 0;
            UIThemeService.WhenReady(rt => { runtime = rt; ready = true; });
            while (!ready)
            {
                if (++frames >= 300)
                {
                    Logger.LogWarning("UI", $"[UIColourStyle '{name}'] No UIThemeRuntime found after 300 frames — add a UIThemeRuntime to the scene.");
                    yield break;
                }

                yield return null;
            }

            themeChangedHandler = _ => ApplyCurrentState();
            runtime.OnThemeChanged += themeChangedHandler;
            resolveCoroutine = null;
            ApplyCurrentState();
        }

        private void ApplyCurrentState()
        {
            Apply(selectable != null ? selectable.CurrentState : UISelectable.SelectableState.Normal);
        }

        private void ApplyWithRuntime(UISelectableStyle_SO.StateStyle stateStyle, UIThemeRuntime rt)
        {
            if (backgroundGraphic != null && stateStyle.Background != null)
            {
                var colour = rt.GetColour(stateStyle.Background);
                backgroundGraphic.color = colour;
                var selectableGraphic = GetComponent<UnityEngine.UI.Selectable>();
                if (selectableGraphic != null)
                {
                    var colourBlock = selectableGraphic.colors;
                    colourBlock.normalColor = colourBlock.highlightedColor = colourBlock.selectedColor = colourBlock.pressedColor = colour;
                    selectableGraphic.colors = colourBlock;
                    backgroundGraphic.CrossFadeColor(colour, 0f, true, true);
                }
            }

            if (labelText != null && stateStyle.Text != null)
                labelText.color = rt.GetColour(stateStyle.Text);
        }

        private void ApplyWithDatabase(UISelectableStyle_SO.StateStyle stateStyle, UIDatabase_SO db,
            UITheme_SO themeOverride = null)
        {
            Color Resolve(ColourSlot_SO slot)
            {
                if (themeOverride != null)
                {
                    var colour = themeOverride.GetColour(slot);
                    if (colour.HasValue) return colour.Value;
                    return slot.DefaultColour != null ? slot.DefaultColour.Colour : Color.white;
                }

                return db.GetColourForSlot(slot);
            }

            if (backgroundGraphic != null && stateStyle.Background != null)
                backgroundGraphic.color = Resolve(stateStyle.Background);
            if (labelText != null && stateStyle.Text != null)
                labelText.color = Resolve(stateStyle.Text);
        }

#if UNITY_EDITOR
        [HideInInspector] [SerializeField] private UIDatabase_SO editorPreviewDatabase;

        private UIDatabase_SO EditorResolveDatabase() =>
            editorPreviewDatabase != null ? editorPreviewDatabase : UIDatabase_SO.FindBestInProject();

        public void EditorPreview(UISelectable.SelectableState state, UITheme_SO themeOverride = null)
        {
            if (backgroundGraphic == null)
                backgroundGraphic = GetComponent<Graphic>() ?? GetComponentInChildren<Graphic>();
            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (style == null) return;
            var db = EditorResolveDatabase();
            if (db == null) return;

            var stateStyle = style.GetStateStyle(state);
            if (stateStyle != null) ApplyWithDatabase(stateStyle, db, themeOverride);

            if (backgroundGraphic != null) UnityEditor.EditorUtility.SetDirty(backgroundGraphic);
            if (labelText != null) UnityEditor.EditorUtility.SetDirty(labelText);
        }

        public void EditorGetColours(out Color? bgColour, out Color? textColour)
        {
            bgColour = backgroundGraphic != null ? (Color?)backgroundGraphic.color : null;
            textColour = labelText != null ? (Color?)labelText.color : null;
        }

        public void EditorSetColours(Color? bgColour, Color? textColour)
        {
            if (bgColour.HasValue && backgroundGraphic != null)
            {
                backgroundGraphic.color = bgColour.Value;
                UnityEditor.EditorUtility.SetDirty(backgroundGraphic);
            }

            if (textColour.HasValue && labelText != null)
            {
                labelText.color = textColour.Value;
                UnityEditor.EditorUtility.SetDirty(labelText);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying || style == null) return;

            var db = EditorResolveDatabase();
            if (db == null) return;

            if (selectable == null) selectable = GetComponent<UISelectable>();
            var state = selectable != null ? selectable.CurrentState : UISelectable.SelectableState.Normal;
            var stateStyle = style.GetStateStyle(state);
            if (stateStyle != null)
                ApplyWithDatabase(stateStyle, db);
        }

        [ContextMenu("Auto-Discover References")]
        private void AutoDiscoverReferences()
        {
            if (backgroundGraphic == null)
                backgroundGraphic = GetComponent<Graphic>() ?? GetComponentInChildren<Graphic>();
            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(includeInactive: true);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
