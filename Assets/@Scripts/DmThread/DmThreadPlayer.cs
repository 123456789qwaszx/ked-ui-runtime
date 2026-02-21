using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DmThreadPlayer
{
    public enum State : byte
    {
        Idle = 0,
        Playing = 1,
        WaitingTap = 2,
        WaitingChoice = 3,
        Completed = 4,
    }

    public State CurrentState => _state;

    public bool AutoEnabled => _autoEnabled;
    public float AutoDelaySeconds
    {
        get => _autoDelaySeconds;
        set => _autoDelaySeconds = Mathf.Max(0f, value);
    }

    public event Action<DmResultEvent> OnResult; // Completed / ChoiceSelected

    private readonly DmThreadPanel _panel;

    private DmScript _script;
    private readonly Dictionary<string, int> _eventIndexById = new();

    private int _cursor;
    private State _state;

    private bool _inputBlocked;

    // Waiting for typing completion
    private DmEntryView _currentTypingView;
    private bool _lineFullyShown;

    // Waiting for auto advance time
    private bool _autoEnabled;
    private float _autoDelaySeconds = 1.2f;
    private float _autoCountdown;

    // Current choice context
    private DmChoice _activeChoice;
    private string _activeEventId; // for results/logging

    public DmThreadPlayer(DmThreadPanel panel)
    {
        _panel = panel;
    }

    public void SetInputBlocked(bool blocked)
    {
        _inputBlocked = blocked;
        _panel.SetInputBlocked(blocked);
    }

    public void SetAutoEnabled(bool enabled)
    {
        _autoEnabled = enabled;

        // auto 켜면 즉시 타이머 갱신(현재 상태에 따라)
        if (_autoEnabled)
            _autoCountdown = _autoDelaySeconds;
    }

    public void Start(in DmScript script, bool clearUi = true)
    {
        _script = script;

        BuildIndex();

        _cursor = 0;
        _state = State.Playing;

        _lineFullyShown = true;
        _currentTypingView = null;

        _activeChoice = default;
        _activeEventId = null;

        _autoCountdown = _autoDelaySeconds;

        if (clearUi)
            _panel.ClearThread();

        _panel.Show(true);
        _panel.ShowChoices(false, null, null);

        // 첫 이벤트 즉시 실행
        Step();
    }

    public void Tick(float unscaledDeltaTime)
    {
        if (_state == State.Completed || _state == State.Idle)
            return;

        if (!_autoEnabled || _inputBlocked)
            return;

        // auto는 "대기 상태"에서만 의미 있음
        if (_state == State.WaitingTap)
        {
            _autoCountdown -= unscaledDeltaTime;
            if (_autoCountdown <= 0f)
            {
                _autoCountdown = _autoDelaySeconds;
                RequestAdvance();
            }
        }
    }

    /// <summary>
    /// 사용자 탭/다음 입력
    /// </summary>
    public void RequestAdvance()
    {
        if (_inputBlocked) return;

        switch (_state)
        {
            case State.WaitingTap:
                // 라인이 아직 타이핑 중이면 즉시 완료
                if (!_lineFullyShown && _currentTypingView)
                {
                    _currentTypingView.ForceCompleteTyping();
                    OnLineFullyShownInternal();
                    return;
                }

                // 다음으로 진행
                _state = State.Playing;
                Step();
                break;

            case State.Playing:
                // Play중에 들어온 탭은 "현재 라인 타이핑 완료"로 처리할 수 있음
                if (!_lineFullyShown && _currentTypingView)
                {
                    _currentTypingView.ForceCompleteTyping();
                    OnLineFullyShownInternal();
                }
                break;

            case State.WaitingChoice:
                // 선택지 상태에서는 탭은 무시(선택 버튼 사용)
                break;
        }
    }

    /// <summary>
    /// 패널 선택지 버튼 입력(0..N-1)
    /// </summary>
    public void ChooseOption(int index)
    {
        if (_inputBlocked) return;
        if (_state != State.WaitingChoice) return;

        var opts = _activeChoice.options;
        if (opts == null || (uint)index >= (uint)opts.Length) return;

        var opt = opts[index];
        OnResult?.Invoke(new DmResultEvent(_script.threadId, DmResultKind.ChoiceSelected, _activeEventId, opt.id));

        _panel.ShowChoices(false, null, null);

        // 분기
        if (!string.IsNullOrEmpty(opt.gotoEventId) && _eventIndexById.TryGetValue(opt.gotoEventId, out int target))
            _cursor = target;
        else
            _cursor++; // 다음 이벤트로

        _state = State.Playing;
        Step();
    }

    // --------------------
    // Internals
    // --------------------

    private void BuildIndex()
    {
        _eventIndexById.Clear();

        if (_script.events == null) return;

        for (int i = 0; i < _script.events.Length; i++)
        {
            var e = _script.events[i];
            if (string.IsNullOrEmpty(e.id)) continue;
            if (_eventIndexById.ContainsKey(e.id)) continue; // 중복은 무시
            _eventIndexById.Add(e.id, i);
        }
    }

    private void Step()
    {
        if (_script.events == null || _cursor >= _script.events.Length)
        {
            Complete();
            return;
        }

        var e = _script.events[_cursor];
        _activeEventId = e.id;

        switch (e.kind)
        {
            case DmEventKind.Line:
                PlayLine(e.id, e.line);
                break;

            case DmEventKind.Marker:
                PlayMarker(e.id, e.marker);
                break;

            case DmEventKind.Choice:
                ShowChoice(e.id, e.choice);
                break;

            default:
                // 알 수 없는 이벤트는 스킵
                _cursor++;
                Step();
                break;
        }
    }

    private void PlayMarker(string eventId, in DmMarker marker)
    {
        var text = marker.label ?? "";
        var model = new DmEntryModel(DmEntryKind.System, "", text);

        _panel.AppendEntry(model);
        _panel.ScrollToBottom(immediate: false);

        // marker는 기본적으로 탭 대기 없이 진행(원하면 wait 정책 추가 가능)
        _cursor++;
        Step();
    }

    private void PlayLine(string eventId, in DmLine line)
    {
        var kind = ConvertKind(line.side);
        var name = line.speaker ?? "";
        var text = line.text ?? "";

        var model = new DmEntryModel(kind, name, text);
        var view = _panel.AppendEntry(model);

        _panel.ScrollToBottom(immediate: false);

        _currentTypingView = view;
        _lineFullyShown = true;

        float typing = Mathf.Max(0f, line.typingSeconds);
        bool waitForTap = line.waitForTap;

        if (view && typing > 0f)
        {
            _lineFullyShown = false;

            // 타이핑 완료 신호를 받아서 상태 전환
            view.PlayTyping(typing, OnLineFullyShownInternal);
        }

        // 라인이 즉시 표시되었거나, 타이핑 완료 후에는 wait 정책 적용
        if (_lineFullyShown)
        {
            if (waitForTap)
            {
                _state = State.WaitingTap;
                _autoCountdown = _autoDelaySeconds;
            }
            else
            {
                _cursor++;
                _state = State.Playing;
                Step();
            }
        }
        else
        {
            // 타이핑 중에는 일단 탭대기 상태로 둠 (탭하면 즉시 완료 처리)
            _state = State.WaitingTap;
            _autoCountdown = _autoDelaySeconds;
        }
    }

    private void OnLineFullyShownInternal()
    {
        _lineFullyShown = true;

        // 타이핑이 끝났는데, 라인이 waitForTap=false였으면 자동으로 진행하고 싶을 수 있음
        // 여기서는 "현재 이벤트를 다시 읽어서 정책 적용" 대신,
        // 가장 안전한 방식으로: 현재 라인이 waitForTap=true라는 가정(기본) + 탭/오토로 넘어가게 둔다.
        // 만약 waitForTap=false 라인도 쓰고 싶으면, 아래처럼 정책을 추가할 수 있음:
        //
        // - DmLine.waitForTap=false이면 타이핑 완료 즉시 다음 이벤트로 진행
        //
        // 이를 위해선 현재 line의 waitForTap을 저장해야 함.
        //
        // 지금은 기본 정책(대부분 waitForTap=true)으로 단순 유지.
    }

    private void ShowChoice(string eventId, in DmChoice choice)
    {
        _activeChoice = choice;

        string prompt = choice.prompt;
        string[] opts = BuildChoiceTexts(choice.options);

        _panel.ShowChoices(true, prompt, opts);
        _panel.ScrollToBottom(immediate: false);

        _state = State.WaitingChoice;
    }

    private static string[] BuildChoiceTexts(DmChoiceOption[] options)
    {
        if (options == null) return Array.Empty<string>();

        int n = Mathf.Min(options.Length, 5);
        var arr = new string[n];
        for (int i = 0; i < n; i++)
            arr[i] = options[i].text ?? "";
        return arr;
    }

    private void Complete()
    {
        _state = State.Completed;

        _panel.ShowChoices(false, null, null);

        OnResult?.Invoke(new DmResultEvent(_script.threadId, DmResultKind.Completed, _activeEventId ?? "", ""));
    }

    private static DmEntryKind ConvertKind(DmSide side)
    {
        switch (side)
        {
            case DmSide.Idol: return DmEntryKind.Incoming;
            case DmSide.Player: return DmEntryKind.Outgoing;
            case DmSide.System: return DmEntryKind.System;
            default: return DmEntryKind.System;
        }
    }
}