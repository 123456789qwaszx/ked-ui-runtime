using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatEngine : MonoBehaviour
{
    [SerializeField] private ChatRuleProfileSO profile;
    [SerializeField] private ChatContentDBSO contentDb;
    [SerializeField] private ChatRail rail;
    [SerializeField] private bool autoStart = true;

    private ChatEngineCore _chatEngineCore;
    private ChatEngineRuntime _chatEngineRuntime;
    private Queue<ChatEvent> _chatQueue;

    private UnityChatRng _rng;
    private IChatPayloadSampler _chatPayloadSampler;
    private IChatRenderResolver _chatRenderResolver;
    
    // ==== BroadcastEvent ====
    private ChatEngineDeps _deps;
    private BroadcastEventSession _session;

    private bool _running;

    public void Initialize(ChatEngineDeps deps)
    {
        _deps = deps;
        
        int kindCount = Enum.GetValues(typeof(ChatEventKind)).Length;

        _chatQueue = new Queue<ChatEvent>(256);

        _rng = new UnityChatRng();
        _chatPayloadSampler = new DbChatPayloadSampler(contentDb, _rng);
        _chatRenderResolver = new DbChatRenderResolver(contentDb);

        _chatEngineCore = new ChatEngineCore(kindCount, _chatPayloadSampler, _rng);
        _chatEngineRuntime = new ChatEngineRuntime(kindCount, profile ? profile.noRepeatTextWindowN : 0);

        rail.BindSource(_chatQueue, _chatRenderResolver);

        if (autoStart)
            StartEngine();
    }

    public void StartEngine()
    {
        if (!profile)
        {
            Debug.LogError("[ChatEngine] profile is null.", this);
            return;
        }

        int kindCount = Enum.GetValues(typeof(ChatEventKind)).Length;
        _chatEngineRuntime.Reset(kindCount, profile.noRepeatTextWindowN);

        // 시작 즉시 1개를 원하면 0, 조금 기다리면 NextNormalInterval로
        _chatEngineRuntime.timeUntilNextEmit = 0f;

        _running = true;
    }

    public void StopEngine() => _running = false;

    public void SetProfile(ChatRuleProfileSO newProfile)
    {
        profile = newProfile;
        if (!profile) return;

        int kindCount = Enum.GetValues(typeof(ChatEventKind)).Length;
        _chatEngineRuntime.Reset(kindCount, profile.noRepeatTextWindowN);
    }

    public void PushSignals(ChatSignals signals)
    {
        if (!profile)
            return;

        float hold = Mathf.Max(0f, profile.signalHoldSeconds);
        float until = _chatEngineRuntime.now + hold;

        if (signals.Has(ChatSignalFlags.IdolSpoke))
        {
            _chatEngineRuntime.idolSpokeBoost = 1f;
            _chatEngineRuntime.idolSpokeHoldUntil = Mathf.Max(_chatEngineRuntime.idolSpokeHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.DonationHappened))
        {
            _chatEngineRuntime.donationBoost = 1f;
            _chatEngineRuntime.donationHoldUntil = Mathf.Max(_chatEngineRuntime.donationHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.BigDonationHappened))
        {
            _chatEngineRuntime.bigDonationBoost = 1f;
            _chatEngineRuntime.bigDonationHoldUntil = Mathf.Max(_chatEngineRuntime.bigDonationHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.SystemNotice))
        {
            _chatEngineRuntime.systemBoost = 1f;
            _chatEngineRuntime.systemHoldUntil = Mathf.Max(_chatEngineRuntime.systemHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.ISpoke))
        {
            _chatEngineRuntime.myMsgBoost = 1f;
            _chatEngineRuntime.myMsgHoldUntil = Mathf.Max(_chatEngineRuntime.myMsgHoldUntil, until);
        }
    }

    private void Update()
    {
        if (!_running || !profile)
            return;
        
        _chatEngineCore.Tick(profile, _chatEngineRuntime, Time.deltaTime, _chatQueue);
    }
    
    // ==== BroadcastEvent API====
    public void BeginEvent(string runId, string eventId, int eventIndex, double nowSec)
    {
        _session = new BroadcastEventSession(runId, eventId, eventIndex, _deps.recorder);
        _session.Begin(nowSec);
    }

    public void EndEvent(double nowSec)
    {
        if (_session == null)
            return;
        
        BroadcastEventLog log = _session.End(nowSec);
        _deps.repository.Add(log);
        _session = null;
    }

    public void RecordDonation(int amount) => _deps.recorder.RecordDonation(amount);
    public void RecordEmoji(int emojiId) => _deps.recorder.RecordEmoji(emojiId);

    public IdolReaction SubmitChat(ChatTag tag, string optionId)
    {
        IdolReaction reaction = _deps.idolReactor.React(tag, optionId);
        _deps.recorder.RecordChat(tag, optionId, reaction);
        return reaction;
    }

    public void BeginPhase(int phaseIndex, string phaseId, string profileKeyAtEnter, double nowSec)
        => _deps.recorder.BeginPhase(phaseIndex, phaseId, profileKeyAtEnter, nowSec);

    public void EndPhase(double nowSec) => _deps.recorder.EndPhase(nowSec);

    public void RecordDecision(PhaseDecisionKind kind, string optionId, bool accepted)
        => _deps.recorder.RecordDecision(kind, optionId, accepted);
    
    public BroadcastEventLog GetLastSavedEventLogOrNull()
    {
        return _deps.repository != null ? _deps.repository.GetLastOrNull() : null;
    }
}