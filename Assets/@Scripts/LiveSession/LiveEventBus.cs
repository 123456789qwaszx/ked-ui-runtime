using System;
using UnityEngine;

public sealed class LiveEventBus : MonoBehaviour
{
    // 싱글톤 (간단 버전)
    public static LiveEventBus Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== Beat/Bar 이벤트 ==========
    
    /// <summary>
    /// 비트/바 틱 (BeatTimesheetRunner가 발행)
    /// </summary>
    public event Action<int, float> OnBeat;
    
    public void RaiseBeat(int bar, float beat)
    {
        OnBeat?.Invoke(bar, beat);
    }

    // ========== Idol 이벤트 ==========
    
    /// <summary>
    /// 아이돌 대사 발생 (lineId, tone, isCheckIn)
    /// </summary>
    public event Action<string, ToneStage, bool> OnIdolLine;
    
    public void RaiseIdolLine(string lineId, ToneStage tone, bool isCheckIn)
    {
        OnIdolLine?.Invoke(lineId, tone, isCheckIn);
    }

    // ========== Donation 이벤트 ==========
    
    /// <summary>
    /// 후원 제출 (amount, message)
    /// </summary>
    public event Action<int, string> OnDonationSubmitted;
    
    public void RaiseDonationSubmitted(int amount, string message)
    {
        OnDonationSubmitted?.Invoke(amount, message);
    }

    // ========== Choice 이벤트 ==========
    
    /// <summary>
    /// 선택지 표시
    /// </summary>
    public event Action<string> OnChoiceShown;
    
    /// <summary>
    /// 선택지 선택 (choiceId, optionIndex)
    /// </summary>
    public event Action<string, int> OnChoicePicked;
    
    public void RaiseChoiceShown(string choiceId)
    {
        OnChoiceShown?.Invoke(choiceId);
    }
    
    public void RaiseChoicePicked(string choiceId, int optionIndex)
    {
        OnChoicePicked?.Invoke(choiceId, optionIndex);
    }

    // ========== Chat 요청 이벤트 (핵심!) ==========
    
    /// <summary>
    /// ChatWave 발생 요청 (preset, count)
    /// ChatSystem이 구독하여 처리
    /// </summary>
    public event Action<ChatPreset, int> RequestChatWave;
    
    /// <summary>
    /// ChatRefrain 발생 요청 (refrainId)
    /// ChatSystem이 구독하여 처리
    /// </summary>
    public event Action<string> RequestChatRefrain;
    
    /// <summary>
    /// MyMsg(내 메시지) 발생 요청 (message, amount)
    /// ChatSystem이 구독하여 처리
    /// </summary>
    public event Action<string, int> RequestMyMsg;
    
    public void RaiseChatWave(ChatPreset preset, int count)
    {
        RequestChatWave?.Invoke(preset, count);
    }
    
    public void RaiseChatRefrain(string refrainId)
    {
        RequestChatRefrain?.Invoke(refrainId);
    }
    
    public void RaiseMyMsg(string message, int amount)
    {
        RequestMyMsg?.Invoke(message, amount);
    }

    // ========== Preset 전환 이벤트 ==========
    
    /// <summary>
    /// 프리셋 전환 요청
    /// </summary>
    public event Action<ChatPreset> RequestPresetChange;
    
    public void RaisePresetChange(ChatPreset preset)
    {
        RequestPresetChange?.Invoke(preset);
    }
}

// ========== Enums ==========

public enum ToneStage
{
    Calm,    // 평온
    Bright,  // 밝음
    Intense, // 고조
}

public enum ChatPreset
{
    A, // 평온 (CHEER 중심)
    B, // 후원 집중 (MYMSG 중심)
    C, // 분위기 고조 (TOXIC 허용)
}