using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUISpriteLoader
{
    bool TryGetCached(string address, out Sprite sprite);
    IEnumerator Load(string address, Action<Sprite> onLoaded, Action onFailed = null);
}

public sealed class UISpritePatcher
{
    private readonly IUISpriteLoader _loader;
    private readonly bool _enableTrace;

    public UISpritePatcher(IUISpriteLoader loader, bool enableTrace = false)
    {
        _loader = loader;
        _enableTrace = enableTrace;
    }

    public IEnumerator Apply(IUISpritePortProvider ui, List<SpritePortBinding> bindings)
    {
        if (ui == null)
        {
            Debug.LogError("[UISpritePatcher] ui is null.");
            yield break;
        }

        if (bindings == null || bindings.Count == 0)
        {
            if (_enableTrace) Debug.Log("[UISpritePatcher] No bindings to apply.");
            yield break;
        }

        // address -> list of portIds (same sprite used by multiple ports)
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (string.IsNullOrEmpty(b.Address) || string.IsNullOrEmpty(b.PortId))
                continue;

            if (!map.TryGetValue(b.Address, out var ports))
            {
                ports = new List<string>(4);
                map.Add(b.Address, ports);
            }
            ports.Add(b.PortId);
        }

        if (_enableTrace)
            Debug.Log($"[UISpritePatcher] Applying {bindings.Count} bindings (unique addresses: {map.Count})...");

        int successCount = 0;
        int failedCount = 0;

        foreach (var kv in map)
        {
            string address = kv.Key;
            List<string> ports = kv.Value;

            Sprite sprite = null;
            bool loadedOk = false;

            // 1) cache
            if (_loader.TryGetCached(address, out var cached) && cached)
            {
                sprite = cached;
                loadedOk = true;
                if (_enableTrace) Debug.Log($"  [cached] {address}");
            }
            else
            {
                // 2) load (contract: this coroutine returns AFTER callback fired exactly once)
                Sprite loadedSprite = null;
                bool loadDone = false;
                bool loadFailed = false;

                yield return _loader.Load(
                    address,
                    onLoaded: s =>
                    {
                        loadedSprite = s;
                        loadDone = true;
                    },
                    onFailed: () =>
                    {
                        loadFailed = true;
                        loadDone = true;
                    });

                // 안전장치: Loader 구현이 계약을 어겼을 때를 대비
                if (!loadDone)
                {
                    Debug.LogWarning($"[UISpritePatcher] Loader returned before completing callbacks: {address}");
                }

                if (!loadFailed && loadedSprite)
                {
                    sprite = loadedSprite;
                    loadedOk = true;
                    if (_enableTrace) Debug.Log($"  [loaded] {address}");
                }
                else
                {
                    //Debug.LogWarning($"[UISpritePatcher] Load failed: {address}");
                }
            }

            // 3) apply to all ports that use this address
            if (loadedOk && sprite)
            {
                for (int i = 0; i < ports.Count; i++)
                {
                    string portId = ports[i];
                    if (ui.TrySetSprite(portId, sprite))
                    {
                        successCount++;
                        if (_enableTrace) Debug.Log($"    + {portId} ← {address}");
                    }
                    else
                    {
                        failedCount++;
                        Debug.LogWarning($"[UISpritePatcher] Failed to set sprite on port: {portId}");
                    }
                }
            }
            else
            {
                failedCount += ports.Count;
            }
        }

        if (_enableTrace)
            Debug.Log($"[UISpritePatcher] Complete: {successCount} success, {failedCount} failed.");
    }
}