using System.Collections;
using TMPro;
using UnityEngine;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    /// <summary>
    /// Binds a TextMeshProUGUI's font and size to a TextStyle_SO, resolving the font via
    /// the active UITheme. Reacts to theme changes automatically.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPFontBinder : MonoBehaviour
    {
        [SerializeField] private TextStyle_SO style;
        [SerializeField] private TextMeshProUGUI target;

        private UIThemeRuntime runtime;
        private Coroutine resolveCoroutine;

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApply();
                return;
            }
#endif
            resolveCoroutine = StartCoroutine(ResolveAndApply());
        }

        protected virtual void OnDisable()
        {
            if (resolveCoroutine != null)
                StopCoroutine(resolveCoroutine);
            if (runtime != null)
                runtime.OnThemeChanged -= HandleThemeChanged;
        }

        private IEnumerator ResolveAndApply()
        {
            bool ready = false;
            int frames = 0;
            UIThemeService.WhenReady(rt => { runtime = rt; ready = true; });
            while (!ready)
            {
                if (++frames >= 300)
                {
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[TMPFontBinder '{name}'] No UIThemeRuntime found after 300 frames.");
                    yield break;
                }

                yield return null;
            }

            Apply(runtime.Database);
            runtime.OnThemeChanged += HandleThemeChanged;
        }

        private void HandleThemeChanged(UITheme_SO theme)
        {
            if (runtime != null) Apply(runtime.Database);
        }

        private void Apply(UIDatabase_SO database)
        {
            if (target == null) target = GetComponent<TextMeshProUGUI>();
            if (target == null || style == null) return;
            style.ApplyTo(target, database);
            Logger.LogVerbose(HelloDev.Logging.UIConstants.System, $"[TMPFontBinder '<color=#AAAAAA>{name}</color>'] Applied style '{style.name}'");
        }

#if UNITY_EDITOR
        private void EditorApply()
        {
            if (target == null) target = GetComponent<TextMeshProUGUI>();
            if (target == null || style == null) return;
            var db = UIDatabase_SO.FindBestInProject();
            style.ApplyTo(target, db);
        }

        private void OnValidate() => EditorApply();
#endif
    }
}

