using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HelloDev.Loader;
using Logger = HelloDev.Logging.Logger;
#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace HelloDev.UI.Default
{
    public class ColorDatabaseRuntime : MonoBehaviour
    {
        public enum LoadMode { Direct, Resources, Addressables }

n        [SerializeField] private LoadMode loadMode = LoadMode.Direct;
        [SerializeField] private ColorDatabase_SO database;
        [SerializeField] private string resourcesPath;
        [SerializeField] private string addressableKey;
        [SerializeField] private string databaseKey = "ColorDatabase";

n        public event Action<string> OnThemeChanged;
        public event Action<string> OnSlotColorChanged;

n        public ColorDatabase_SO Database => database;
        public string DatabaseKey => databaseKey;

n        private void Awake()
        {
            StartCoroutine(LoadCoroutine());
        }

n        private IEnumerator LoadCoroutine()create
        {
            if (loadMode == LoadMode.Direct)
            {
                if (database == null)
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[ColorDatabaseRuntime] Direct mode but database is null on {name}");

n                Register();
                yield break;
            }

n#if USE_ADDRESSABLES
            if (loadMode == LoadMode.Addressables)
            {
                if (string.IsNullOrEmpty(addressableKey))
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[ColorDatabaseRuntime] Addressables key is empty on {name}");
                else
                {
                    HelloDev.Loader.Loader.LoadAssetAsync<ColorDatabase_SO>(addressableKey)
                        .OnComplete(result =>
                        {
                            if (result != null)
                                database = result;
                            else
                                Logger.LogError(HelloDev.Logging.UIConstants.System, $"[ColorDatabaseRuntime] Failed to load addressable '{addressableKey}'");
                            Register();
                        });
                    yield break;
                }

n                Register();
                yield break;
            }
#endif
n            if (loadMode == LoadMode.Resources)
            {
                if (string.IsNullOrEmpty(resourcesPath))
                    Logger.LogWarning(HelloDev.Logging.UIConstants.System, $"[ColorDatabaseRuntime] Resources path is empty on {name}");
                else
                {
                    var r = Resources.LoadAsync<ColorDatabase_SO>(resourcesPath);
                    yield return r;
                    database = r.asset as ColorDatabase_SO;
                }

n                Register();
                yield break;
            }

n            Register();
        }

n        private void Register()
        {
            ColorDatabaseLocator.Register(databaseKey, this);
        }

n        public Color GetColor(string slotId)
        {
            if (database == null)
            {
                Logger.LogWarning(HelloDev.Logging.UIConstants.System, "[ColorDatabaseRuntime] No database loaded.");
                return Color.white;
            }

n            return database.GetColorForSlot(slotId);
        }

n        public void SetActiveTheme(string themeId)
        {
            if (database == null) return;
            database.ActiveThemeId = themeId;
            OnThemeChanged?.Invoke(themeId);
        }

n        private void OnDestroy()
        {
            ColorDatabaseLocator.Unregister(databaseKey, this);
        }
    }

n    public static class ColorDatabaseLocator
    {
        private static readonly Dictionary<string, ColorDatabaseRuntime> _services = new Dictionary<string, ColorDatabaseRuntime>();

n        public static void Register(string key, ColorDatabaseRuntime service)
        {
            if (string.IsNullOrEmpty(key)) key = "ColorDatabase";
            _services[key] = service;
        }

n        public static void Unregister(string key, ColorDatabaseRuntime service)
        {
            if (_services.TryGetValue(key, out var existing) && existing == service)
            {
                _services.Remove(key);
            }
        }

n        public static ColorDatabaseRuntime Get(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "ColorDatabase";
            _services.TryGetValue(key, out var service);
            return service;
        }
    }
}

