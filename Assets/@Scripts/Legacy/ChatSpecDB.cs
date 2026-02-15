// using System.Collections.Generic;
// using UnityEngine;
//
// [CreateAssetMenu(fileName = "ChatSpecDB", menuName = "Live/Chat Spec DB", order = 201)]
// public sealed class ChatSpecDB : ScriptableObject
// {
//     public ChatSpecSO[] specs;
//
//     private Dictionary<string, ChatSpecSO> idCache;
//     private Dictionary<ChatCrowdKind, List<ChatSpecSO>> crowdCache;
//     private bool cacheBuilt;
//
//     private void OnEnable()
//     {
//         BuildCache(force: false);
//     }
//
// #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         cacheBuilt = false;
//     }
// #endif
//
//     public void BuildCache(bool force)
//     {
//         if (!force && cacheBuilt && idCache != null && crowdCache != null)
//             return;
//
//         idCache = new Dictionary<string, ChatSpecSO>(256);
//         crowdCache = new Dictionary<ChatCrowdKind, List<ChatSpecSO>>(64);
//
//         if (specs != null)
//         {
//             for (int i = 0; i < specs.Length; i++)
//             {
//                 var spec = specs[i];
//                 if (!spec) continue;
//
//                 if (!string.IsNullOrEmpty(spec.id))
//                     idCache[spec.id] = spec;
//
//                 if (!crowdCache.TryGetValue(spec.crowdKind, out var list))
//                 {
//                     list = new List<ChatSpecSO>(64);
//                     crowdCache.Add(spec.crowdKind, list);
//                 }
//
//                 list.Add(spec);
//             }
//         }
//
//         cacheBuilt = true;
//     }
//
//     // ✅ 기존 호환
//     public ChatSpecSO GetById(string id)
//     {
//         if (idCache == null || !cacheBuilt) BuildCache(force: false);
//         if (string.IsNullOrEmpty(id)) return null;
//
//         return idCache != null && idCache.TryGetValue(id, out var spec) ? spec : null;
//     }
//
//     // ✅ 기존 호환 (외부에서 리스트를 수정해도 DB 캐시가 망가지지 않게 복사본 반환)
//     public List<ChatSpecSO> GetByCrowdKind(ChatCrowdKind kind)
//     {
//         if (crowdCache == null || !cacheBuilt) BuildCache(force: false);
//
//         if (crowdCache != null && crowdCache.TryGetValue(kind, out var list) && list != null)
//             return new List<ChatSpecSO>(list);
//
//         return new List<ChatSpecSO>(0);
//     }
//
//     // ✅ 앞으로 “전체 풀” 필요할 때 안전하게 쓸 Query()
//     public List<ChatSpecSO> QueryAll()
//     {
//         var result = new List<ChatSpecSO>(specs != null ? specs.Length : 0);
//         if (specs == null) return result;
//
//         for (int i = 0; i < specs.Length; i++)
//         {
//             var s = specs[i];
//             if (s) result.Add(s);
//         }
//
//         return result;
//     }
//
//     public static bool MatchesAllConditions(ChatSpecSO spec, string[] required)
//     {
//         if (required == null || required.Length == 0)
//             return true;
//
//         if (spec.conditions == null || spec.conditions.Length == 0)
//             return false;
//
//         for (int i = 0; i < required.Length; i++)
//         {
//             string req = required[i];
//             bool found = false;
//
//             for (int j = 0; j < spec.conditions.Length; j++)
//             {
//                 if (spec.conditions[j] == req)
//                 {
//                     found = true;
//                     break;
//                 }
//             }
//
//             if (!found)
//                 return false;
//         }
//
//         return true;
//     }
//
// #if UNITY_EDITOR
//     [ContextMenu("Rebuild Cache")]
//     private void DebugRebuildCache()
//     {
//         BuildCache(force: true);
//         Debug.Log($"[ChatSpecDB] Cache rebuilt. specs={(specs != null ? specs.Length : 0)}", this);
//     }
// #endif
// }
