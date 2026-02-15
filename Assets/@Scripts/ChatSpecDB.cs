// ChatSpecDB.cs
// 채팅 스펙 데이터베이스 (쿼리 API 제공)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatSpecDB", menuName = "Live/Chat Spec DB", order = 201)]
public sealed class ChatSpecDB : ScriptableObject
{
    [Header("Database")]
    [Tooltip("전체 채팅 스펙 목록")]
    public ChatSpecSO[] specs;

    [Header("Runtime Cache")]
    [Tooltip("자동으로 인덱싱됨 (Play 모드에서만)")]
    [SerializeField] private bool cacheBuilt = false;

    private Dictionary<string, ChatSpecSO> idCache;
    private Dictionary<ChatEntryKind, List<ChatSpecSO>> kindCache;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        if (cacheBuilt && idCache != null && kindCache != null)
            return;

        idCache = new Dictionary<string, ChatSpecSO>();
        kindCache = new Dictionary<ChatEntryKind, List<ChatSpecSO>>();

        foreach (var spec in specs)
        {
            if (!spec) continue;

            // ID 캐시
            if (!string.IsNullOrEmpty(spec.id))
                idCache[spec.id] = spec;

            // Kind 캐시
            if (!kindCache.TryGetValue(spec.kind, out var list))
            {
                list = new List<ChatSpecSO>();
                kindCache[spec.kind] = list;
            }
            list.Add(spec);
        }

        cacheBuilt = true;
    }

    /// <summary>
    /// ID로 단일 스펙 찾기
    /// </summary>
    public ChatSpecSO GetById(string id)
    {
        BuildCache();
        return idCache.TryGetValue(id, out var spec) ? spec : null;
    }

    /// <summary>
    /// Kind로 필터링
    /// </summary>
    public List<ChatSpecSO> QueryByKind(ChatEntryKind kind)
    {
        BuildCache();
        return kindCache.TryGetValue(kind, out var list) ? list : new List<ChatSpecSO>();
    }

    /// <summary>
    /// 조건 태그로 필터링
    /// </summary>
    public List<ChatSpecSO> QueryByConditions(params string[] requiredConditions)
    {
        BuildCache();

        if (requiredConditions == null || requiredConditions.Length == 0)
            return specs.ToList();

        var results = new List<ChatSpecSO>();
        foreach (var spec in specs)
        {
            if (!spec) continue;

            // 모든 조건이 충족되면 추가
            bool allMatch = true;
            foreach (var required in requiredConditions)
            {
                if (spec.conditions == null || !spec.conditions.Contains(required))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
                results.Add(spec);
        }

        return results;
    }

    /// <summary>
    /// Kind + 조건 복합 쿼리
    /// </summary>
    public List<ChatSpecSO> Query(ChatEntryKind? kind = null, params string[] conditions)
    {
        BuildCache();

        var results = specs.AsEnumerable();

        // Kind 필터
        if (kind.HasValue)
            results = results.Where(s => s && s.kind == kind.Value);

        // 조건 필터
        if (conditions != null && conditions.Length > 0)
        {
            results = results.Where(s =>
            {
                if (!s || s.conditions == null) return false;
                foreach (var cond in conditions)
                {
                    if (!s.conditions.Contains(cond))
                        return false;
                }
                return true;
            });
        }

        return results.ToList();
    }

    /// <summary>
    /// 전체 스펙 개수
    /// </summary>
    public int Count => specs?.Length ?? 0;

#if UNITY_EDITOR
    [ContextMenu("Rebuild Cache")]
    private void RebuildCache()
    {
        cacheBuilt = false;
        BuildCache();
        Debug.Log($"[ChatSpecDB] Cache rebuilt: {Count} specs", this);
    }
#endif
}