// ChatEntryFactory.cs
// ChatSpecSO → ChatEntryData 변환 팩토리
using UnityEngine;

public static class ChatEntryFactory
{
    /// <summary>
    /// Spec으로부터 ChatEntryData 생성
    /// </summary>
    public static ChatEntryData Create(ChatSpecSO spec)
    {
        if (!spec)
        {
            Debug.LogError("[ChatEntryFactory] Spec is null.");
            return default;
        }

        var data = new ChatEntryData
        {
            kind = spec.kind,
            side = spec.isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            name = ResolveName(spec),
            body = spec.body,
            donationAmount = spec.donationAmount,
        };

        return data;
    }

    /// <summary>
    /// 여러 Spec을 Data 배열로 변환
    /// </summary>
    public static ChatEntryData[] CreateMultiple(ChatSpecSO[] specs)
    {
        if (specs == null || specs.Length == 0)
            return new ChatEntryData[0];

        var results = new ChatEntryData[specs.Length];
        for (int i = 0; i < specs.Length; i++)
        {
            results[i] = Create(specs[i]);
        }

        return results;
    }

    /// <summary>
    /// 이름 처리 (비어있으면 랜덤 생성)
    /// </summary>
    private static string ResolveName(ChatSpecSO spec)
    {
        // Idol/System은 이름 비워두기
        if (spec.kind == ChatEntryKind.Idol || spec.kind == ChatEntryKind.System)
            return "";

        // 이름이 지정되어 있으면 그대로 사용
        if (!string.IsNullOrEmpty(spec.chatName))
            return spec.chatName;

        // 이름이 없으면 랜덤 생성
        return GenerateRandomName();
    }

    /// <summary>
    /// 랜덤 시청자 이름 생성
    /// </summary>
    private static string GenerateRandomName()
    {
        // 간단한 랜덤 이름 풀 (나중에 확장 가능)
        string[] prefixes = { "viewer", "user", "fan", "guest" };
        string prefix = prefixes[Random.Range(0, prefixes.Length)];
        int number = Random.Range(1, 9999);
        return $"{prefix}_{number}";
    }

    /// <summary>
    /// Validation (데이터 검증)
    /// </summary>
    public static bool Validate(ChatSpecSO spec, out string errorMessage)
    {
        errorMessage = null;

        if (!spec)
        {
            errorMessage = "Spec is null";
            return false;
        }

        // Donation은 금액 필수
        if (spec.kind == ChatEntryKind.Donation && spec.donationAmount <= 0)
        {
            errorMessage = $"Donation spec [{spec.id}] must have donationAmount > 0";
            return false;
        }

        // Body 필수 (System 제외)
        if (spec.kind != ChatEntryKind.System && string.IsNullOrEmpty(spec.body))
        {
            errorMessage = $"Spec [{spec.id}] must have body text";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 디버그: Data 내용 출력
    /// </summary>
    public static void PrintData(ChatEntryData data)
    {
        Debug.Log($"[ChatEntryFactory] Data: kind={data.kind}, side={data.side}, name={data.name}, body={data.body}, amount={data.donationAmount}");
    }
}