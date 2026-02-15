using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ResourceManager를 IUISpriteLoader로 감싸는 어댑터.
/// Addressables 의존성을 격리.
/// </summary>
public sealed class AddressablesSpriteLoader : IUISpriteLoader
{
    private readonly ResourceManager _resourceManager;

    public AddressablesSpriteLoader(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public bool TryGetCached(string address, out Sprite sprite)
    {
        return _resourceManager.TryGetResource(address, out sprite);
    }

    public IEnumerator Load(string address, Action<Sprite> onLoaded, Action onFailed = null)
    {
        // ResourceManager는 이미 비동기 로딩이 끝난 상태라고 가정
        // (LoadScopeAssets로 미리 로드됨)
        
        if (_resourceManager.TryGetResource(address, out Sprite sprite))
        {
            onLoaded?.Invoke(sprite);
        }
        else
        {
            //Debug.LogWarning($"[AddressablesSpriteLoader] Sprite not found: {address}");
            onFailed?.Invoke();
        }
        
        yield break;
    }
}