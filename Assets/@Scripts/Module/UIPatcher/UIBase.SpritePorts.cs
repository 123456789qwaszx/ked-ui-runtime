using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IUISpritePortProvider
{
    // View.Image(Refs.)
    IReadOnlyList<string> GetSpritePortIds();
    
    // 실제로 스프라이트 연결법
    bool TrySetSprite(string portId, Sprite sprite);
}

/// <summary>
/// UIBase<TRefs>에 IUISpritePortProvider 구현 추가.
/// Refs(Enum)를 스캔해서 "_Image"로 끝나는 것을 자동으로 수집 및, Sprite 포트로 인식.
/// </summary>
public abstract partial class UIBase<TRefs> : IUISpritePortProvider
    where TRefs : struct, Enum
{
    private List<string> _cachedSpritePortIds;

    /// <summary>
    /// Sprite가 필요한 포트 목록 반환.
    /// "_Image"로 끝나는 Enum 값들을 자동으로 수집.
    /// </summary>
    public IReadOnlyList<string> GetSpritePortIds()
    {
        if (_cachedSpritePortIds != null)
            return _cachedSpritePortIds;

        _cachedSpritePortIds = new List<string>();

        foreach (TRefs enumValue in Enum.GetValues(typeof(TRefs)))
        {
            string name = enumValue.ToString();

            // "_Image"로 끝나는 것만 Sprite 포트로 간주
            if (name.EndsWith("_Image", StringComparison.Ordinal))
            {
                _cachedSpritePortIds.Add(name);
            }
        }

        return _cachedSpritePortIds;
    }

    // portId에 해당하는 Image 컴포넌트에 Sprite 설정.
    public bool TrySetSprite(string portId, Sprite sprite)
    {
        if (string.IsNullOrEmpty(portId))
            return false;

        // portId를 Enum으로 파싱
        if (!Enum.TryParse<TRefs>(portId, ignoreCase: false, out var enumKey))
        {
            Debug.LogWarning($"[UIBase] Invalid portId (not in Refs enum): {portId}");
            return false;
        }

        // View.Image로 캐시된 Image 컴포넌트 획득
        Image image = View.Image(enumKey);
        if (image == null)
        {
            Debug.LogWarning($"[UIBase] Image component not found for port: {portId}");
            return false;
        }

        // Sprite 적용
        image.sprite = sprite;
        return true;
    }
}