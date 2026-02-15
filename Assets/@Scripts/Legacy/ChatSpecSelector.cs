// // ChatSpecSelector.cs
// // 가중치 기반 랜덤 선택 서비스
// using System.Collections.Generic;
// using UnityEngine;
//
// public static class ChatSpecSelector
// {
//     /// <summary>
//     /// 가중치 기반 랜덤 선택 (Weighted Random)
//     /// </summary>
//     public static ChatSpecSO SelectRandom(List<ChatSpecSO> specs)
//     {
//         if (specs == null || specs.Count == 0)
//             return null;
//
//         // 1개면 바로 반환
//         if (specs.Count == 1)
//             return specs[0];
//
//         // 전체 가중치 합 계산
//         float totalWeight = 0f;
//         foreach (var spec in specs)
//         {
//             if (spec)
//                 totalWeight += spec.weight;
//         }
//
//         if (totalWeight <= 0f)
//         {
//             // 가중치가 모두 0이면 균등 랜덤
//             return specs[Random.Range(0, specs.Count)];
//         }
//
//         // 가중치 랜덤 선택
//         float roll = Random.Range(0f, totalWeight);
//         float cumulative = 0f;
//
//         foreach (var spec in specs)
//         {
//             if (!spec) continue;
//
//             cumulative += spec.weight;
//             if (roll < cumulative)
//                 return spec;
//         }
//
//         // Fallback (부동소수점 오차 대비)
//         return specs[specs.Count - 1];
//     }
//
//     /// <summary>
//     /// 여러 개 선택 (중복 허용)
//     /// </summary>
//     public static List<ChatSpecSO> SelectRandomMultiple(List<ChatSpecSO> specs, int count)
//     {
//         if (specs == null || specs.Count == 0 || count <= 0)
//             return new List<ChatSpecSO>();
//
//         var results = new List<ChatSpecSO>(count);
//         for (int i = 0; i < count; i++)
//         {
//             var selected = SelectRandom(specs);
//             if (selected)
//                 results.Add(selected);
//         }
//
//         return results;
//     }
//
//     /// <summary>
//     /// 여러 개 선택 (중복 불허)
//     /// </summary>
//     public static List<ChatSpecSO> SelectRandomUnique(List<ChatSpecSO> specs, int count)
//     {
//         if (specs == null || specs.Count == 0 || count <= 0)
//             return new List<ChatSpecSO>();
//
//         // 요청 개수가 전체보다 많으면 전체 반환
//         if (count >= specs.Count)
//             return new List<ChatSpecSO>(specs);
//
//         var pool = new List<ChatSpecSO>(specs);
//         var results = new List<ChatSpecSO>(count);
//
//         for (int i = 0; i < count; i++)
//         {
//             if (pool.Count == 0)
//                 break;
//
//             var selected = SelectRandom(pool);
//             if (selected)
//             {
//                 results.Add(selected);
//                 pool.Remove(selected);
//             }
//         }
//
//         return results;
//     }
//
//     /// <summary>
//     /// 디버그: 가중치 분포 출력
//     /// </summary>
//     public static void PrintWeightDistribution(List<ChatSpecSO> specs)
//     {
//         if (specs == null || specs.Count == 0)
//         {
//             Debug.Log("[ChatSpecSelector] No specs to analyze.");
//             return;
//         }
//
//         float totalWeight = 0f;
//         foreach (var spec in specs)
//         {
//             if (spec)
//                 totalWeight += spec.weight;
//         }
//
//         Debug.Log($"[ChatSpecSelector] Total specs: {specs.Count}, Total weight: {totalWeight:F2}");
//         foreach (var spec in specs)
//         {
//             if (!spec) continue;
//             float percentage = totalWeight > 0 ? (spec.weight / totalWeight) * 100f : 0f;
//             Debug.Log($"  [{spec.id}] {spec.chatName}: {spec.weight:F2} ({percentage:F1}%)");
//         }
//     }
// }