// ChatEngineState.cs
// 채팅 엔진 내부 상태 (누적 시간, 버스트, 쿨다운, 히스토리)
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatEngineState
{
    // ========== Accumulator (시간 누적) ==========
    
    /// <summary>
    /// 다음 발행까지 남은 시간 (초)
    /// </summary>
    public float timeUntilNextEmit;

    // ========== Burst 상태 ==========
    
    /// <summary>
    /// 현재 버스트 중인가?
    /// </summary>
    public bool isBursting;
    
    /// <summary>
    /// 버스트 남은 개수
    /// </summary>
    public int burstRemaining;
    
    /// <summary>
    /// 버스트 중 다음 발행까지 시간
    /// </summary>
    public float burstTimeUntilNext;

    // ========== Kind 히스토리 (Hygiene) ==========
    
    /// <summary>
    /// 최근 생성된 Kind 리스트 (streak 체크용)
    /// </summary>
    public readonly List<ChatEventKind> recentKinds = new List<ChatEventKind>();
    
    /// <summary>
    /// Kind별 마지막 발생 시각 (쿨다운 체크용)
    /// </summary>
    public readonly Dictionary<ChatEventKind, float> kindLastEmitTime = new Dictionary<ChatEventKind, float>();

    // ========== Text 히스토리 (중복 방지) ==========
    
    /// <summary>
    /// 최근 생성된 텍스트 (중복 방지용)
    /// </summary>
    public readonly Queue<string> recentTexts = new Queue<string>();

    // ========== Donation 빈도 (특수 제한) ==========
    
    /// <summary>
    /// 마지막 Donation 발생 시각
    /// </summary>
    public float lastDonationTime;

    // ========== Helper API ==========

    /// <summary>
    /// Kind 발생 기록
    /// </summary>
    public void RecordKind(ChatEventKind kind)
    {
        recentKinds.Add(kind);
        
        // 최근 20개만 유지 (메모리 관리)
        if (recentKinds.Count > 20)
            recentKinds.RemoveAt(0);
        
        kindLastEmitTime[kind] = Time.time;
    }

    /// <summary>
    /// 텍스트 발생 기록
    /// </summary>
    public void RecordText(string text, int windowSize)
    {
        if (string.IsNullOrEmpty(text))
            return;

        recentTexts.Enqueue(text);
        
        // windowSize만 유지
        while (recentTexts.Count > windowSize)
            recentTexts.Dequeue();
    }

    /// <summary>
    /// 최근 N개 중 같은 Kind 연속 횟수 (streak)
    /// </summary>
    public int GetCurrentStreak(ChatEventKind kind)
    {
        int streak = 0;
        for (int i = recentKinds.Count - 1; i >= 0; i--)
        {
            if (recentKinds[i] == kind)
                streak++;
            else
                break;
        }
        return streak;
    }

    /// <summary>
    /// Kind 쿨다운 체크
    /// </summary>
    public bool IsKindInCooldown(ChatEventKind kind, float cooldownSeconds)
    {
        if (cooldownSeconds <= 0f)
            return false;

        if (!kindLastEmitTime.TryGetValue(kind, out float lastTime))
            return false;

        return Time.time - lastTime < cooldownSeconds;
    }

    /// <summary>
    /// 텍스트 중복 체크
    /// </summary>
    public bool IsTextRecent(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var recent in recentTexts)
        {
            if (recent == text)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Donation 빈도 체크
    /// </summary>
    public bool CanEmitDonation(float maxFrequency)
    {
        if (maxFrequency <= 0f)
            return true; // 제한 없음

        float minInterval = 1f / maxFrequency;
        return Time.time - lastDonationTime >= minInterval;
    }

    /// <summary>
    /// 버스트 시작
    /// </summary>
    public void StartBurst(int length, float initialInterval)
    {
        isBursting = true;
        burstRemaining = length;
        burstTimeUntilNext = initialInterval;
    }

    /// <summary>
    /// 버스트 소비
    /// </summary>
    public void ConsumeBurst()
    {
        burstRemaining--;
        if (burstRemaining <= 0)
            isBursting = false;
    }

    /// <summary>
    /// 상태 리셋 (프로필 교체 시)
    /// </summary>
    public void Reset()
    {
        timeUntilNextEmit = 0f;
        isBursting = false;
        burstRemaining = 0;
        burstTimeUntilNext = 0f;
        
        recentKinds.Clear();
        kindLastEmitTime.Clear();
        recentTexts.Clear();
        
        lastDonationTime = 0f;
    }
}