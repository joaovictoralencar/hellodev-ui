using System.Collections;
using UnityEngine;

namespace HelloDev.UI.Default
{
    public abstract class BaseColorBinder : MonoBehaviour
    {
        [SerializeField] protected string databaseKey = "ColorDatabase";
        [SerializeField] protected string slotId;
        [SerializeField] protected Colour_SO directColour;
        [SerializeField] protected bool useDirectColour;
        [SerializeField] protected Color fallbackColor = Color.white;
        [SerializeField] protected bool applyInEditor = true;

        protected ColorDatabaseRuntime runtime;
        private Coroutine resolveCoroutine;

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && applyInEditor) ApplyEditorColor();
#endif
            if (Application.isPlaying)
            {
                resolveCoroutine = StartCoroutine(ResolveAndApply());
            }
        }

#if UNITY_EDITOR
        protected virtual void ApplyEditorColor()
        {
            if (useDirectColour && directColour != null) ApplyColor(directColour.Colour);
            else
            {
                var runtimeTemp = ColorDatabaseLocator.Get(databaseKey);
                if (runtimeTemp != null && !string.IsNullOrEmpty(slotId)) ApplyColor(runtimeTemp.GetColor(slotId));
            }
        }
#endif

        protected IEnumerator ResolveAndApply()
        {
            while (!useDirectColour && ColorDatabaseLocator.Get(databaseKey) == null)
            {
                yield return null;
            }

            runtime = ColorDatabaseLocator.Get(databaseKey);

            if (useDirectColour && directColour != null)
                ApplyColor(directColour.Colour);
            else if (runtime != null && !string.IsNullOrEmpty(slotId))
            {
                ApplyColor(runtime.GetColor(slotId));
                runtime.OnThemeChanged += HandleThemeChanged;
            }
            else
                ApplyColor(fallbackColor);
        }

        protected virtual void HandleThemeChanged(string themeId)
        {
            if (runtime != null && !string.IsNullOrEmpty(slotId))
                ApplyColor(runtime.GetColor(slotId));
        }

        protected abstract void ApplyColor(Color color);

        protected virtual void OnDisable()
        {
            if (resolveCoroutine != null) StopCoroutine(resolveCoroutine);
            if (runtime != null) runtime.OnThemeChanged -= HandleThemeChanged;
        }
    }
}
