using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatEngine : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private ChatRuleProfileSO profile;

    [Header("Content")]
    [SerializeField] private ChatContentDBSO contentDb;

    [Header("UI")]
    [SerializeField] private ChatRail rail;

    [Header("Debug")]
    [SerializeField] private bool autoStart = true;

    private ChatEngineCore _core;
    private ChatEngineRuntime _rt;
    private Queue<ChatEvent> _queue;

    private IChatRng _rng;
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

        if (rail)
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
        // 즉시 boost 주입 (여러 번 와도 최대 1로 clamp)
        if (signals.Has(ChatSignalFlags.IdolSpoke))
            _rt.idolSpokeBoost = 1f;

        if (signals.Has(ChatSignalFlags.DonationHappened))
            _rt.donationBoost = 1f;

        if (signals.Has(ChatSignalFlags.BigDonationHappened))
            _rt.bigDonationBoost = 1f;

        if (signals.Has(ChatSignalFlags.SystemNotice))
            _rt.systemBoost = 1f;

        if (signals.Has(ChatSignalFlags.ISpoke))
            _rt.myMsgBoost = 1f;

        // donation amount 같은 건 sampler가 참고할 수 있게 별도 저장해도 됨
    }

    private void Update()
    {
        if (!_running || !profile) return;
        _core.Tick(profile, _rt, Time.deltaTime, _queue);
    }
}
