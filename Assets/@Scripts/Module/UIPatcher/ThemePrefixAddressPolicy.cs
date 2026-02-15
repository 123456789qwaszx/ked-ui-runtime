using UnityEngine;

/// <summary>
/// Theme 기반 Sprite 주소 생성 정책.
/// "ui/{theme}/{portId}" 패턴 사용.
/// </summary>
public sealed class ThemePrefixAddressPolicy
{
    private readonly string _basePrefix;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="basePrefix">기본 prefix. 예: "ui" → "ui/{theme}/{portId}"</param>
    public ThemePrefixAddressPolicy(string basePrefix = "ui")
    {
        _basePrefix = basePrefix ?? "ui";
    }

    public string GetAddress(string portId, in UIContext ctx)
    {
        if (string.IsNullOrEmpty(portId))
        {
            Debug.LogWarning("[ThemePrefixAddressPolicy] portId is null or empty.");
            return null;
        }

        string themeId = ctx.ThemeId ?? "default";
        
        // "ui/dark/AnalysisIcon_Image"
        return $"{_basePrefix}/{themeId.ToLower()}/{portId}";
    }
}
