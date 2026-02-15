using System.Collections.Generic;
using System.Text;
using UnityEngine;

// UI의 포트 목록을 획득하고, Policy를 적용해서 바인딩 생성.
public sealed class SpritePatchResolver
{
    private readonly ThemePrefixAddressPolicy _policy;
    private readonly bool _enableTrace;

    public SpritePatchResolver(
        ThemePrefixAddressPolicy policy,
        bool enableTrace = false)
    {
        _policy = policy;
        _enableTrace = enableTrace;
    }

    public List<SpritePortBinding> BuildBindings(
        IUISpritePortProvider ui,
        in UIContext ctx)
    {
        var bindings = new List<SpritePortBinding>();
        var portIds = ui.GetSpritePortIds();

        if (portIds == null || portIds.Count == 0)
        {
            if (_enableTrace)
                Debug.Log($"[SpritePatchResolver] No sprite ports found for UI.");
            
            return bindings;
        }

        StringBuilder trace = _enableTrace ? new StringBuilder(256) : null;
        trace?.AppendLine($"[SpritePatchResolver] Building bindings for {portIds.Count} ports");
        trace?.AppendLine($"  ctx.theme={ctx.ThemeId}, ctx.locale={ctx.LocaleId}");

        foreach (var portId in portIds)
        {
            // Policy로 주소 생성
            string address = _policy.GetAddress(portId, ctx);

            // 검증
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[SpritePatchResolver] Policy returned null/empty address for port: {portId}");
                continue;
            }

            bindings.Add(new SpritePortBinding(portId, address));
            trace?.AppendLine($"  +binding: {portId} → {address}");
        }

        if (_enableTrace && trace != null)
        {
            Debug.Log(trace.ToString());
        }

        return bindings;
    }
}