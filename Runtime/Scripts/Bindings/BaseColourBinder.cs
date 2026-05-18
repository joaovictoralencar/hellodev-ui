using System.Collections;
using UnityEngine;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    public abstract class BaseColourBinder : MonoBehaviour
    {
        [Header("Colour Source")]
        [Tooltip("The colour slot from the active theme. Used when 'Use Direct Colour' is off.")]
        [SerializeField] protected ColourSlot_SO slot;

        [Tooltip("When enabled, ignores the slot and applies 'Direct Colour' directly — bypassing the theme system.")]
        [SerializeField] protected bool useDirectColour;

        [Tooltip("A fixed Colour_SO to apply when 'Use Direct Colour' is on.")]
        [SerializeField] protected Colour_SO directColour;

        [Header("Fallback")]
        [Tooltip("Colour applied if the slot is missing and no DefaultColour is set on the slot asset.")]
        [SerializeField] protected Color fallbackColour = Color.white;

        [Header("Editor")]
        [Tooltip("Automatically applies the slot colour in edit mode so the scene view matches the theme.")]
        [SerializeField] protected bool applyInEditor = true;

        protected UIThemeRuntime runtime;
        private Coroutine resolveCoroutine;

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && applyInEditor)
                ApplyEditorColour();
#endif
            if (Application.isPlaying)
                resolveCoroutine = StartCoroutine(ResolveAndApply());
        }

        protected virtual void OnDisable()
        {
            if (resolveCoroutine != null)
                StopCoroutine(resolveCoroutine);

            if (runtime != null)
                runtime.OnThemeChanged -= HandleThemeChanged;
        }

#if UNITY_EDITOR
        [HideInInspector] [SerializeField] private UIDatabase_SO editorPreviewDatabase;

        public void EditorApplyPreview(UITheme_SO themeOverride = null) => ApplyEditorColour(themeOverride);

        protected virtual void ApplyEditorColour(UITheme_SO themeOverride = null)
        {
            if (useDirectColour && directColour != null)
            {
                ApplyColour(directColour.Colour);
                return;
            }

            if (slot == null) return;

            var db = editorPreviewDatabase != null ? editorPreviewDatabase : UIDatabase_SO.FindBestInProject();
            if (db != null)
                ApplyColour(db.GetColourForSlot(slot, themeOverride));
        }
#endif

        private IEnumerator ResolveAndApply()
        {
            if (useDirectColour)
            {
                if (directColour != null)
                {
                    ApplyColour(directColour.Colour);
                    Logger.LogVerbose(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '<color=#AAAAAA>{name}</color>'] " +
                        $"Direct colour '<color=#F0D080>{directColour.name}</color>' -> {HexTag(directColour.Colour)}");
                }
                else
                {
                    ApplyColour(fallbackColour);
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '{name}'] useDirectColour=true but directColour is null — using fallback");
                }

                yield break;
            }

            {
                bool ready = false;
                int frames = 0;
                UIThemeService.WhenReady(rt => { runtime = rt; ready = true; });
                while (!ready)
                {
                    if (++frames >= 300)
                    {
                        Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '{name}'] No UIThemeRuntime found after 300 frames — add a UIThemeRuntime to the scene.");
                        yield break;
                    }

                    yield return null;
                }
            }

            if (slot == null)
            {
                ApplyColour(fallbackColour);
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '{name}'] slot is not assigned — using fallback");
                yield break;
            }

            var colour = runtime.GetColour(slot);
            ApplyColour(colour);
            Logger.LogVerbose(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '<color=#AAAAAA>{name}</color>'] " +
                $"Slot '<color=#F0D080>{slot.DisplayName}</color>' -> {HexTag(colour)} " +
                $"(db: '<color=#80C0F0>{runtime.Database?.name}</color>')");
            runtime.OnThemeChanged += HandleThemeChanged;
        }

        protected virtual void HandleThemeChanged(UITheme_SO theme)
        {
            if (runtime == null || slot == null) return;
            var colour = runtime.GetColour(slot);
            ApplyColour(colour);
            Logger.LogVerbose(HelloDev.Logging.UIConstants.System, $"[{GetType().Name} '<color=#AAAAAA>{name}</color>'] " +
                $"Theme changed -> slot '<color=#F0D080>{slot.DisplayName}</color>' reapplied as {HexTag(colour)}");
        }

        protected abstract void ApplyColour(Color colour);

        private static string HexTag(Color colour)
        {
            var hex = ColorUtility.ToHtmlStringRGB(colour);
            return $"<color=#{hex}>#{hex}</color>";
        }
    }
}

