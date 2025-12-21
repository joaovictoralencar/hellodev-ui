using HelloDev.Tweening;
using UnityEngine;

namespace HelloDev.UI.Tweening
{
    /// <summary>
    /// Initializes the TweenService with the PrimeTweenProvider on Awake.
    /// Add this component to a GameObject in your first scene to set up the tween system.
    /// </summary>
    public class TweenServiceInitializer : MonoBehaviour
    {
        [Tooltip("If true, this GameObject won't be destroyed when loading a new scene.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (TweenService.IsConfigured)
            {
                // Already configured, destroy this duplicate
                Destroy(gameObject);
                return;
            }

            TweenService.SetProvider(new PrimeTweenProvider());

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Only clear if we're the active initializer
            if (TweenService.Provider is PrimeTweenProvider)
            {
                TweenService.ClearProvider();
            }
        }
    }
}
