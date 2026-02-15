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
        // 최신 신호를 OR로 누적 (한 틱에서 여러 이벤트 들어올 수 있으니)
        _signals = new ChatSignals(_signals.flags | signals.flags, signals.donationAmount);
    }

    private void Update()
    {
        if (!_running || !profile) return;

        // Tick: signals를 넘기고, tick 끝나면 소비
        _core.Tick(profile, _rt, Time.deltaTime, _queue /*, _signals */);
        _signals = default;
    }
}
