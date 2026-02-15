using UnityEngine;

public sealed class ChatRuleState
{
    // ========== Refrain Window ==========
    
    /// <summary>
    /// 리프레인 윈도우 종료 시각 (Time.time 기준)
    /// </summary>
    public double refrainWindowUntil;
    
    /// <summary>
    /// 마지막 리프레인 발생 시각
    /// </summary>
    public double lastRefrainAt;
    
    /// <summary>
    /// 현재 윈도우 내에서 리프레인 사용 여부
    /// </summary>
    public bool refrainUsedInWindow;

    // ========== Current Preset ==========
    
    /// <summary>
    /// 현재 활성 프리셋
    /// </summary>
    public ChatPreset currentPreset = ChatPreset.A;

    // ========== Wave Cooldown ==========
    
    /// <summary>
    /// 마지막 Wave 발생 시각
    /// </summary>
    public double lastWaveAt;
    
    /// <summary>
    /// Wave 최소 간격 (초)
    /// </summary>
    public float minWaveInterval = 0.5f;

    // ========== Refrain Window API ==========

    /// <summary>
    /// 리프레인 윈도우 열기
    /// </summary>
    /// <param name="duration">윈도우 지속 시간 (초)</param>
    public void OpenRefrainWindow(float duration = 5f)
    {
        refrainWindowUntil = Time.time + duration;
        refrainUsedInWindow = false;
        
        Debug.Log($"[ChatRuleState] Refrain window opened for {duration}s (until {refrainWindowUntil:F2})");
    }

    /// <summary>
    /// 리프레인 발생 가능 여부
    /// </summary>
    public bool CanEmitRefrain()
    {
        bool inWindow = Time.time < refrainWindowUntil;
        bool notUsed = !refrainUsedInWindow;
        
        return inWindow && notUsed;
    }

    /// <summary>
    /// 리프레인 소비 (1회 제한)
    /// </summary>
    public void ConsumeRefrain()
    {
        refrainUsedInWindow = true;
        lastRefrainAt = Time.time;
        
        Debug.Log($"[ChatRuleState] Refrain consumed at {lastRefrainAt:F2}");
    }

    /// <summary>
    /// 리프레인 윈도우 강제 종료
    /// </summary>
    public void CloseRefrainWindow()
    {
        refrainWindowUntil = 0;
        refrainUsedInWindow = true;
    }

    // ========== Wave Cooldown API ==========

    /// <summary>
    /// Wave 발생 가능 여부 (쿨다운 체크)
    /// </summary>
    public bool CanEmitWave()
    {
        return Time.time - lastWaveAt >= minWaveInterval;
    }

    /// <summary>
    /// Wave 소비 (쿨다운 갱신)
    /// </summary>
    public void ConsumeWave()
    {
        lastWaveAt = Time.time;
    }

    // ========== Debug Helpers ==========

    /// <summary>
    /// 현재 상태 로그 출력
    /// </summary>
    public void PrintState()
    {
        Debug.Log($"[ChatRuleState] Preset: {currentPreset}, " +
                  $"RefrainWindow: {(CanEmitRefrain() ? "OPEN" : "CLOSED")}, " +
                  $"WaveCooldown: {(CanEmitWave() ? "READY" : "COOLING")}");
    }
}