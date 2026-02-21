using System;
using System.Collections.Generic;
using UnityEngine;

public enum DmResultKind : byte
{
    Completed = 0,
    ChoiceSelected = 1,
}

public readonly struct DmResultEvent
{
    public readonly string ThreadId;
    public readonly DmResultKind Kind;
    public readonly string EventId;
    public readonly string ChoiceId;

    public DmResultEvent(string threadId, DmResultKind kind, string eventId, string choiceId)
    {
        ThreadId = threadId;
        Kind = kind;
        EventId = eventId;
        ChoiceId = choiceId;
    }
}

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

    public event Action<DmResultEvent> OnResult; // Completed / ChoiceSelected

    private DmThreadPanel _panel;

    private DmScript _script;
    private readonly Dictionary<string, int> _eventIndexById = new();

    private int _cursor;
    private State _state;

    private DmChoice _activeChoice;
    private string _activeEventId;
    
    private DmChoiceView _activeChoiceView;

    private bool _init;
    public void Initialize(DmThreadPanel panel)
    {
        if (_init)
            return;
        
        _panel = panel;

        _init = true;
    }

    public void StartDm(DmScript script, bool clearUi = true)
    {
        _script = script;

        _cursor = 0;
        _state = State.Playing;

        _activeChoice = default;
        _activeEventId = null;

        _eventIndexById.Clear();

        for (int i = 0; i < _script.events.Length; i++)
        {
            DmEvent dmEvent = _script.events[i];
            
            if (string.IsNullOrEmpty(dmEvent.id))
                continue;
            
            if (_eventIndexById.ContainsKey(dmEvent.id))
                continue;
            
            _eventIndexById.Add(dmEvent.id, i);
        }

        if (clearUi)
            _panel.ClearThread();

        _panel.Show(true);

        Step();
    }
    public void RequestAdvance()
    {
        switch (_state)
        {
            case State.WaitingTap:
                _state = State.Playing;
                Step();
                break;

            case State.Playing:
                // 진행 중 탭은 무시(원하면 여기서 "빠른 넘김" 정책을 추가)
                break;

            case State.WaitingChoice:
                break;
        }
    }

    public void ChooseOption(int index)
    {
        if (_state != State.WaitingChoice)
            return;

        DmChoiceOption[] options = _activeChoice.options;
        DmChoiceOption opt = options[index];
        
        if (_activeChoiceView)
            _activeChoiceView.Lock();
        _activeChoiceView = null;
        
        OnResult?.Invoke(
            new DmResultEvent(
                _script.threadId, // 어떤 Dm스레드인지
                DmResultKind.ChoiceSelected, // 이게 선택인지, 완료인지 구분
                _activeEventId, // 선택이 발생한 좌표: choiceId 내에 yes/no, 세이브/리플레이 시 어느 이벤트에서 선택했는지
                opt.id)); // 실제로 어떤 옵션을 골랐는지, 토큰 부여/플래그/분기 결정의 입력값

        if (!string.IsNullOrEmpty(opt.gotoEventId) && _eventIndexById.TryGetValue(opt.gotoEventId, out int target))
            _cursor = target; // 지정된 이벤트로 점프
        else
            _cursor++; // 다음 이벤트로 진행

        _state = State.Playing;
        Step();
    }

    
    private void Step()
    {
        while (_cursor < _script.events.Length)
        {
            DmEvent dmEvent = _script.events[_cursor];
            _activeEventId = dmEvent.id;

            switch (dmEvent.kind)
            {
                case DmEventKind.Line:
                    _cursor++;
                    PlayLine(dmEvent.line);
                    return;
                
                case DmEventKind.Marker:
                    _cursor++;
                    PlayMarker(dmEvent.marker);
                    continue;
                
                case DmEventKind.Choice:
                    _cursor++;
                    ShowChoice(dmEvent.choice);
                    return;
                case DmEventKind.Emoji:
                    _cursor++;
                    PlayEmoji(dmEvent.emoji);
                    return;
                
                default:
                    _cursor++;
                    continue;
            }
        }
        
        _state = State.Completed;
        OnResult?.Invoke(new DmResultEvent(_script.threadId, DmResultKind.Completed, _activeEventId ?? "", ""));
    }

    private void PlayMarker(DmMarker marker)
    {
        string text = marker.label ?? "";
        var model = new DmEntryModel(DmEntryKind.System, "", text);

        _panel.AppendEntry(model);
        _panel.ScrollToBottom();
    }

    private void PlayLine(DmLine line)
    {
        var model = new DmEntryModel(
            line.kind,
            line.speaker ?? "",
            line.text ?? ""
        );

        _panel.AppendEntry(model);
        _panel.ScrollToBottom();

        _state = State.WaitingTap;
    }
    
    private void PlayEmoji(DmEmoji emoji)
    {
        string name = emoji.speaker ?? "";
        string text = string.IsNullOrEmpty(emoji.emojiId) ? "" : $":{emoji.emojiId}:";

        var model = new DmEntryModel(
            emoji.kind,
            name,
            text
        );

        _panel.AppendEntry(model);
        _panel.ScrollToBottom();

        _state = State.WaitingTap;
    }

    private void ShowChoice(DmChoice choice)
    {
        _activeChoice = choice;

        _activeChoiceView = _panel.AppendChoice(choice, ChooseOption);
        _panel.ScrollToBottom();

        _state = State.WaitingChoice;
    }
}