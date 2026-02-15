using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public enum SoundType { Bgm, Sfx }

// 싱글씬 고정이면 Scene은 굳이 포함하지 않아도 됨.
// 남기고 싶으면 남겨도 되지만, 의미가 흐려지긴 함.
public enum LoadType
{
    DataParse,
    Resources,
    Init,
    Setting
}

public sealed class ResourceManager
{
    private readonly Dictionary<string, Object> resources = new();

    private readonly Dictionary<string, List<HandleRecord>> scopeRecords = new();

    // Scope -> (Type -> Keys)
    // 외부 노출은 IReadOnlyList이므로 내부는 HashSet으로 관리하면 dedupe가 빠름
    private readonly Dictionary<string, Dictionary<Type, HashSet<string>>> scopeKeysByType = new();

    // prevent duplicate load
    private readonly HashSet<string> loadingScopes = new();
    private readonly HashSet<string> loadedScopes = new();

    private sealed class HandleRecord
    {
        public AsyncOperationHandle Handle;
        public List<string> Keys;
        public Type AssetType;
    }

    // ----------------------------
    // Public API
    // ----------------------------

    public bool IsScopeLoaded(string scopeId) => !string.IsNullOrEmpty(scopeId) && loadedScopes.Contains(scopeId);
    public bool IsScopeLoading(string scopeId) => !string.IsNullOrEmpty(scopeId) && loadingScopes.Contains(scopeId);

    /// <summary>
    /// One-time Addressables init + load resident scope.
    /// </summary>
    public async Task Initialize()
    {
        var handle = Addressables.InitializeAsync();
        await handle.Task;

        await LoadScopeAssets(
            scopeId: "resident",
            labelFront: StringAdrLabelFront.EntryScene,
            onProgress: null);
    }

    /// <summary>
    /// Load (preload) assets by scope and your labelFront rule.
    /// 싱글씬 고정 정책: Scene 로드는 여기 책임이 아님.
    /// </summary>
    public async Task LoadScopeAssets(
        string scopeId,
        string labelFront,
        Action<float> onProgress = null,
        float timeoutSeconds = 10f)
    {
        if (string.IsNullOrEmpty(scopeId))
            throw new ArgumentException("scopeId is null/empty.", nameof(scopeId));

        if (loadedScopes.Contains(scopeId))
            return;

        // 이미 로딩 중이면 "그 로딩이 끝날 때까지" 기다리는 게 안전함
        if (loadingScopes.Contains(scopeId))
        {
            while (loadingScopes.Contains(scopeId))
                await Task.Yield();

            return;
        }

        loadingScopes.Add(scopeId);

        try
        {
            await LoadAssetsByLabel<GameObject>(scopeId, labelFront + StringAdrLabelBack.Object, timeoutSeconds, onProgress);
            await LoadAssetsByLabel<Sprite>(scopeId, labelFront + StringAdrLabelBack.Sprite, timeoutSeconds, onProgress);
            await LoadAssetsByLabel<AudioClip>(scopeId, labelFront + StringAdrLabelBack.AudioClip, timeoutSeconds, onProgress);

            loadedScopes.Add(scopeId);
        }
        finally
        {
            loadingScopes.Remove(scopeId);
        }
    }

    /// <summary>
    /// Unload a scope: releases handles + removes cached assets for that scope.
    /// IMPORTANT: caller must release instantiated instances BEFORE calling this.
    /// </summary>
    public async Task UnloadScope(string scopeId)
    {
        if (string.IsNullOrEmpty(scopeId))
            return;

        if (!scopeRecords.TryGetValue(scopeId, out var records))
            return;

        for (int r = 0; r < records.Count; r++)
        {
            var rec = records[r];
            if (!rec.Handle.IsValid())
                continue;

            if (rec.Keys != null)
            {
                for (int i = 0; i < rec.Keys.Count; i++)
                {
                    var key = rec.Keys[i];
                    resources.Remove(key);
                }
            }

            Addressables.Release(rec.Handle);
        }

        scopeRecords.Remove(scopeId);
        scopeKeysByType.Remove(scopeId);
        loadedScopes.Remove(scopeId);

        await Task.Yield();
    }

    public async Task UnloadAllScopesExcept(params string[] keepScopes)
    {
        var keep = new HashSet<string>(keepScopes ?? Array.Empty<string>());

        var all = scopeRecords.Keys.ToList();
        for (int i = 0; i < all.Count; i++)
        {
            if (keep.Contains(all[i]))
                continue;

            await UnloadScope(all[i]);
        }
    }

    public bool TryGetResource<T>(string key, out T asset) where T : Object
    {
        asset = null;

        if (!resources.TryGetValue(key, out var obj) || obj == null)
            return false;

        asset = obj as T;
        return asset != null;
    }

    public T GetResource<T>(string key) where T : Object
    {
        if (!TryGetResource<T>(key, out var asset))
        {
            Debug.LogWarning($"[ResourceManager] Missing or type mismatch. Key='{key}', Type={typeof(T).Name}");
            return null;
        }
        return asset;
    }

    public IReadOnlyList<string> GetLoadedKeysByType<T>(string scopeId) where T : Object
    {
        if (string.IsNullOrEmpty(scopeId))
            return Array.Empty<string>();

        if (!scopeKeysByType.TryGetValue(scopeId, out var map))
            return Array.Empty<string>();

        if (!map.TryGetValue(typeof(T), out var set) || set == null || set.Count == 0)
            return Array.Empty<string>();

        // IReadOnlyList를 원하니 그때만 배열로 스냅샷(호출 빈도 낮다면 OK)
        return set.ToArray();
    }

    private T GetScriptableObject<T>(string key) where T : ScriptableObject => GetResource<T>(key);

    // ----------------------------
    // Internals
    // ----------------------------

    private async Task LoadAssetsByLabel<T>(
        string scopeId,
        string label,
        float timeoutSeconds,
        Action<float> onProgress) where T : Object
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(label);
        await locationHandle.Task;

        var locations = locationHandle.Result
            .Where(loc => loc.ResourceType == typeof(T))
            .ToList();

        Addressables.Release(locationHandle);

        if (locations.Count <= 0)
            return;

        var assetHandle = Addressables.LoadAssetsAsync<T>(locations, null);

        bool success = await LoadWithTimeoutAndProgress(assetHandle, timeoutSeconds, onProgress);

        if (!success)
        {
            Debug.LogError($"[ResourceManager] LoadAssetsByLabel<{typeof(T).Name}> failed: {label}");
            if (assetHandle.IsValid())
                Addressables.Release(assetHandle);
            return;
        }

        var primaryKeys = new List<string>(locations.Count);

        for (int i = 0; i < assetHandle.Result.Count; i++)
        {
            var asset = assetHandle.Result[i];
            if (asset == null) continue;

            var address = locations[i].PrimaryKey;
            primaryKeys.Add(address);

            if (!resources.ContainsKey(address))
                resources[address] = asset;
        }

        AddScopeRecord(scopeId, assetHandle, primaryKeys, typeof(T));
        AddScopeKeysByType(scopeId, typeof(T), primaryKeys);

        Debug.Log($"[ResourceManager] Loaded {typeof(T).Name} ({primaryKeys.Count}) for label '{label}'");
    }

    private void AddScopeRecord(string scopeId, AsyncOperationHandle handle, List<string> keys, Type assetType)
    {
        if (!scopeRecords.TryGetValue(scopeId, out var list))
        {
            list = new List<HandleRecord>();
            scopeRecords[scopeId] = list;
        }

        list.Add(new HandleRecord
        {
            Handle = handle,
            Keys = keys,
            AssetType = assetType
        });
    }

    private void AddScopeKeysByType(string scopeId, Type type, List<string> keys)
    {
        if (!scopeKeysByType.TryGetValue(scopeId, out var map))
        {
            map = new Dictionary<Type, HashSet<string>>();
            scopeKeysByType[scopeId] = map;
        }

        if (!map.TryGetValue(type, out var set))
        {
            set = new HashSet<string>();
            map[type] = set;
        }

        for (int i = 0; i < keys.Count; i++)
            set.Add(keys[i]);
    }

    private async Task<bool> LoadWithTimeoutAndProgress<T>(
        AsyncOperationHandle<T> handle,
        float timeoutSeconds,
        Action<float> onProgress)
    {
        var timeoutTask = Task.Delay((int)(timeoutSeconds * 1000));
        var progressTask = WaitUntilDoneWithGUI(handle, onProgress);

        var completedTask = await Task.WhenAny(progressTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.LogError("[ResourceManager] Load Resource Timeout");
            return false;
        }

        return handle.Status == AsyncOperationStatus.Succeeded;
    }

    private async Task WaitUntilDoneWithGUI<T>(AsyncOperationHandle<T> handle, Action<float> onProgress)
    {
        float lastProgress = -1f;

        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            if (onProgress != null && Mathf.Abs(progress - lastProgress) > 0.01f)
            {
                lastProgress = progress;
                onProgress(progress);
            }
            await Task.Yield();
        }

        onProgress?.Invoke(1f);
    }
}