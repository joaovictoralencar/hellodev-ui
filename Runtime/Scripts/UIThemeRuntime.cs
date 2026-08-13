using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using HelloDev.Loader;
using Logger = HelloDev.Logging.Logger;

namespace HelloDev.UI.Default
{
    public class UIThemeRuntime : MonoBehaviour
    {
        public enum LoadMode { Direct, Resources, Addressables }

        [SerializeField] private LoadMode loadMode = LoadMode.Direct;
        [SerializeField] private UIDatabase_SO database;
        [SerializeField] private string resourcesPath;
#if UNITY_ADDRESSABLES
        [SerializeField] private AssetReferenceT<UIDatabase_SO> addressableReference;
#endif

        public event Action<UITheme_SO> OnThemeChanged;

        public UIDatabase_SO Database => database;

        private void Awake()
        {
            StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            if (loadMode == LoadMode.Direct)
            {
                if (database == null)
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Direct mode but database is null on '{name}'");
                Register();
                yield break;
            }

            if (loadMode == LoadMode.Addressables)
            {
#if UNITY_ADDRESSABLES
                if (addressableReference == null || !addressableReference.RuntimeKeyIsValid())
                {
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Addressable reference is not set on '{name}'");
                }
                else
                {
                    HelloDev.Loader.Loader.LoadAssetAsync<UIDatabase_SO>(addressableReference)
                        .OnComplete(result =>
                        {
                            if (result != null)
                                database = result;
                            else
                                Logger.LogError(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Failed to load addressable on '{name}'");
                            Register();
                        }).Forget();
                    yield break;
                }
#else
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Addressables mode selected on '{name}', but com.unity.addressables is not available.");
#endif

                Register();
                yield break;
            }

            if (loadMode == LoadMode.Resources)
            {
                if (string.IsNullOrEmpty(resourcesPath))
                {
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Resources path is empty on '{name}'");
                }
                else
                {
                    var r = Resources.LoadAsync<UIDatabase_SO>(resourcesPath);
                    yield return r;
                    database = r.asset as UIDatabase_SO;
                }

                Register();
                yield break;
            }

            Register();
        }

        private void Register()
        {
            if (database == null)
            {
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] Registered but database is null on '{name}'");
                UIThemeService.SetDefault(this);
                return;
            }

            UIThemeLocator.Register(database.name, database, this);
            UIThemeService.SetDefault(this);

            Logger.Log(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] '<color=#80C0F0>{database.name}</color>' ready — " +
                $"{database.ColourSlots.Count} colour slot(s) | {database.FontSlots.Count} font slot(s) | " +
                $"{database.Themes.Count} theme(s) | " +
                $"active theme: '<color=#F0D080>{database.ActiveTheme?.name ?? "none"}</color>'");
        }

        public Color GetColour(ColourSlot_SO slot)
        {
            if (database == null)
            {
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] GetColour('{slot?.DisplayName}') called but database is null");
                return Color.white;
            }

            return database.GetColourForSlot(slot);
        }

        public TMP_FontAsset GetFont(FontSlot_SO slot)
        {
            if (database == null) return slot?.DefaultFont;
            return database.GetFontForSlot(slot);
        }

        public void SetActiveTheme(UITheme_SO theme)
        {
            if (database == null || theme == null) return;
            database.ActiveTheme = theme;
            Logger.Log(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] '<color=#80C0F0>{database.name}</color>' theme -> '<color=#F0D080>{theme.name}</color>'");
            OnThemeChanged?.Invoke(theme);
        }

        public void SetActiveTheme(string themeName)
        {
            if (database == null) return;
            foreach (var t in database.Themes)
            {
                if (t != null && t.name == themeName)
                {
                    SetActiveTheme(t);
                    return;
                }
            }

            Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] No theme named '{themeName}' in '{database.name}'");
        }

        private void OnDestroy()
        {
            if (database != null)
                UIThemeLocator.Unregister(database.name, database, this);
            UIThemeService.ClearDefault(this);
            Logger.Log(HelloDev.Logging.UIConstants.System, $"[UIThemeRuntime] '{database?.name ?? "database"}' unregistered");
        }
    }

    public static class UIThemeLocator
    {
        private static readonly Dictionary<string, UIThemeRuntime> _byKey = new Dictionary<string, UIThemeRuntime>();
        private static readonly Dictionary<UIDatabase_SO, UIThemeRuntime> _bySO = new Dictionary<UIDatabase_SO, UIThemeRuntime>();

        public static void Register(string key, UIDatabase_SO so, UIThemeRuntime service)
        {
            if (!string.IsNullOrEmpty(key)) _byKey[key] = service;
            if (so != null) _bySO[so] = service;
        }

        public static void Unregister(string key, UIDatabase_SO so, UIThemeRuntime service)
        {
            if (!string.IsNullOrEmpty(key) && _byKey.TryGetValue(key, out var keyedService) && keyedService == service)
                _byKey.Remove(key);
            if (so != null && _bySO.TryGetValue(so, out var soService) && soService == service)
                _bySO.Remove(so);
        }

        public static UIThemeRuntime Get(UIDatabase_SO so)
        {
            if (so == null) return null;
            _bySO.TryGetValue(so, out var service);
            return service;
        }

        public static UIThemeRuntime Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            _byKey.TryGetValue(key, out var service);
            return service;
        }

        public static UIThemeRuntime FindAny()
        {
            foreach (var value in _bySO.Values)
                if (value != null) return value;
            foreach (var value in _byKey.Values)
                if (value != null) return value;
            return null;
        }
    }

    /// <summary>
    /// Provides a scene-wide default UIThemeRuntime that all UI components can discover
    /// without needing an explicit inspector reference.
    /// </summary>
    public static class UIThemeService
    {
        private static UIThemeRuntime _default;
        private static readonly List<Action<UIThemeRuntime>> _waiters = new List<Action<UIThemeRuntime>>();

        public static UIThemeRuntime Default => _default;
        public static bool IsReady => _default != null;

        public static void WhenReady(Action<UIThemeRuntime> callback)
        {
            if (callback == null) return;
            if (_default != null) callback(_default);
            else _waiters.Add(callback);
        }

        internal static void SetDefault(UIThemeRuntime runtime)
        {
            if (runtime == null || _default != null) return;
            _default = runtime;
            foreach (var waiter in _waiters) waiter?.Invoke(runtime);
            _waiters.Clear();
        }

        internal static void ClearDefault(UIThemeRuntime runtime)
        {
            if (_default == runtime) _default = null;
        }
    }
}

