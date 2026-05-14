using UnityEngine;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    [RequireComponent(typeof(Button))]
    public class SampleThemeSwitcher : MonoBehaviour
    {
        private int currentIndex;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(SwitchTheme);

            var db = UIThemeService.Default?.Database;
            if (db != null)
                Logger.Log("UI", $"[SampleThemeSwitcher '<color=#AAAAAA>{name}</color>'] Ready — {db.Themes.Count} theme(s) in '<color=#80C0F0>{db.name}</color>'");
            else
                Logger.LogWarning("UI", $"[SampleThemeSwitcher '{name}'] UIThemeService not ready yet — themes will be unavailable until database loads");
        }

        private void SwitchTheme()
        {
            var runtime = UIThemeService.Default;
            if (runtime == null)
            {
                Logger.LogWarning("UI", $"[SampleThemeSwitcher '{name}'] No database registered yet");
                return;
            }

            var db = runtime.Database;
            currentIndex = (currentIndex + 1) % db.Themes.Count;
            runtime.SetActiveTheme(db.Themes[currentIndex]);
        }
    }
}