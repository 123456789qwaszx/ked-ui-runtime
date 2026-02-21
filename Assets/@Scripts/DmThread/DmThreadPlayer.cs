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

    public event Action<DmResultEvent> OnResult; // Completed / ChoiceSelected

    private readonly DmThreadPanel _panel;

    private DmScript _script;
    private readonly Dictionary<string, int> _eventIndexById = new();

    private int _cursor;
    private State _state;

    private bool _inputBlocked;

    // Auto
    private bool _autoEnabled;
    private float _autoDelaySeconds = 1.2f;
    private float _autoCountdown;

    // Choice
    private DmChoice _activeChoice;
    private string _activeEventId;

    public DmThreadPlayer(DmThreadPanel panel)
    {
        _panel = panel;
    }

    public void StartDm(DmScript script, bool clearUi = true)
    {
        _script = script;

        _cursor = 0;
        _state = State.Playing;

        _activeChoice = default;
        _activeEventId = null;

        _autoCountdown = _autoDelaySeconds;

        BuildIndex();

        if (clearUi)
            _panel.ClearThread();

        _panel.Show(true);

        Step();
    }

    public void Tick(float unscaledDeltaTime)
    {
        if (_state == State.Completed || _state == State.Idle)
            return;

        if (!_autoEnabled || _inputBlocked)
            return;

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

    public void RequestAdvance()
    {
        if (_inputBlocked) return;

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
                // 선택지 상태에서는 탭 무시
                break;
        }
    }

    public void ChooseOption(int index)
    {
        if (_inputBlocked) return;
        if (_state != State.WaitingChoice) return;

        var opts = _activeChoice.options;
        if (opts == null || (uint)index >= (uint)opts.Length) return;

        var opt = opts[index];
        OnResult?.Invoke(new DmResultEvent(_script.threadId, DmResultKind.ChoiceSelected, _activeEventId, opt.id));

        if (!string.IsNullOrEmpty(opt.gotoEventId) && _eventIndexById.TryGetValue(opt.gotoEventId, out int target))
            _cursor = target;
        else
            _cursor++;

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
            if (_eventIndexById.ContainsKey(e.id)) continue; // 중복 방지
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

        var dmEvent = _script.events[_cursor];
        _activeEventId = dmEvent.id;

        switch (dmEvent.kind)
        {
            case DmEventKind.Line:
                PlayLine(dmEvent.id, dmEvent.line);
                break;

            case DmEventKind.Marker:
                PlayMarker(dmEvent.id, dmEvent.marker);
                break;

            case DmEventKind.Choice:
                ShowChoice(dmEvent.id, dmEvent.choice);
                break;

            default:
                _cursor++;
                Step();
                break;
        }
    }

    private void PlayMarker(string eventId, in DmMarker marker)
    {
        string text = marker.label ?? "";
        var model = new DmEntryModel(DmEntryKind.System, "", text);

        _panel.AppendEntry(model);
        _panel.ScrollToBottom();

        _cursor++;
        Step();
    }

    private void PlayLine(string eventId, in DmLine line)
    {
        var model = new DmEntryModel(
            line.kind,
            line.speaker ?? "",
            line.text ?? ""
        );

        _panel.AppendEntry(model);
        _panel.ScrollToBottom();

        if (line.waitForTap)
        {
            _state = State.WaitingTap;
            _autoCountdown = _autoDelaySeconds;
            return;
        }

        _cursor++;
        _state = State.Playing;
        Step();
    }

    private void ShowChoice(string eventId, in DmChoice choice)
    {
        _activeChoice = choice;

        string prompt = choice.prompt;
        string[] opts = BuildChoiceTexts(choice.options);

        _panel.ScrollToBottom();

        _state = State.WaitingChoice;
    }

    private static string[] BuildChoiceTexts(DmChoiceOption[] options)
    {
        if (options == null)
            return Array.Empty<string>();

        int n = Mathf.Min(options.Length, 5);
        var arr = new string[n];
        for (int i = 0; i < n; i++)
            arr[i] = options[i].text ?? "";
        return arr;
    }

    private void Complete()
    {
        _state = State.Completed;

        OnResult?.Invoke(new DmResultEvent(_script.threadId, DmResultKind.Completed, _activeEventId ?? "", ""));
    }
}