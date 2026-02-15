using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatEngine : MonoBehaviour
{
    [Header("Profile")] [SerializeField] private ChatRuleProfileSO profile;
    [Header("Content")] [SerializeField] private ChatContentDBSO contentDb;
    [Header("UI")] [SerializeField] private ChatRail rail;
    [Header("Debug")] [SerializeField] private bool autoStart = true;

    private ChatEngineCore _core;
    private ChatEngineRuntime _rt;
    private Queue<ChatEvent> _queue;

    private UnityChatRng _rng;
    private IChatPayloadSampler _sampler;
    private IChatRenderResolver _resolver;

    private ChatSignals _signals;
    private bool _running;

    private void Awake()
    {
        int kindCount = Enum.GetValues(typeof(ChatEventKind)).Length;

        _queue = new Queue<ChatEvent>(256);

        _rng = new UnityChatRng();
        _sampler = new DbChatPayloadSampler(contentDb, _rng);
        _resolver = new DbChatRenderResolver(contentDb);

        _core = new ChatEngineCore(kindCount, _sampler, _rng);
        _rt = new ChatEngineRuntime(kindCount, profile ? profile.noRepeatTextWindowN : 0);

        rail.BindSource(_queue, _resolver);

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
        _rt.Reset(kindCount, profile.noRepeatTextWindowN);

        // 시작 즉시 1개를 원하면 0, 조금 기다리면 NextNormalInterval로
        _rt.timeUntilNextEmit = 0f;

        _running = true;
    }

    public void StopEngine() => _running = false;

    public void SetProfile(ChatRuleProfileSO newProfile)
    {
        profile = newProfile;
        if (!profile) return;

        int kindCount = Enum.GetValues(typeof(ChatEventKind)).Length;
        _rt.Reset(kindCount, profile.noRepeatTextWindowN);
    }

    public void PushSignals(ChatSignals signals)
    {
        if (!profile) return; // holdSeconds 읽기 위해

        float hold = Mathf.Max(0f, profile.signalHoldSeconds);
        float until = _rt.now + hold;

        if (signals.Has(ChatSignalFlags.IdolSpoke))
        {
            _rt.idolSpokeBoost = 1f;
            _rt.idolSpokeHoldUntil = Mathf.Max(_rt.idolSpokeHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.DonationHappened))
        {
            _rt.donationBoost = 1f;
            _rt.donationHoldUntil = Mathf.Max(_rt.donationHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.BigDonationHappened))
        {
            _rt.bigDonationBoost = 1f;
            _rt.bigDonationHoldUntil = Mathf.Max(_rt.bigDonationHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.SystemNotice))
        {
            _rt.systemBoost = 1f;
            _rt.systemHoldUntil = Mathf.Max(_rt.systemHoldUntil, until);
        }

        if (signals.Has(ChatSignalFlags.ISpoke))
        {
            _rt.myMsgBoost = 1f;
            _rt.myMsgHoldUntil = Mathf.Max(_rt.myMsgHoldUntil, until);
        }
    }

    private void Update()
    {
        if (!_running || !profile) return;
        _core.Tick(profile, _rt, Time.deltaTime, _queue);
    }
}