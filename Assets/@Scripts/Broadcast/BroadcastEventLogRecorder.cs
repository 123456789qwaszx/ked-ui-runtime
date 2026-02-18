using System;
using System.Collections.Generic;
using UnityEngine;

// BroadcastEvent 동안 발생한 관측을 Phase/Event 단위로 집계해
// CloseRecording 시점에 BroadcastEventLog(확정본)를 완성한다.
public sealed class BroadcastEventLogRecorder
{
    private BroadcastEventLog _log;
    private readonly List<PhaseLog> _phases = new List<PhaseLog>(capacity: 4);

    private bool _eventActive;
    private bool _phaseActive;

    private PhaseLog _currentPhase;

    public bool IsEventActive => _eventActive;
    public bool IsPhaseActive => _phaseActive;

    // ---------- Lifecycle ----------
    public void OpenRecording(string runId, string eventId, int eventIndex, double startedAtSec)
    {
        // 중복 시작시 초기화
        if (_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] OpenRecording called while recording is active. Resetting previous state.");
            ResetInternal();
        }

        _log = BroadcastEventLog.CreateNew(runId, eventId, eventIndex, startedAtSec);

        _phases.Clear();
        _eventActive = true;
        _phaseActive = false;
        _currentPhase = default;
    }

    public void CloseRecording(double endedAtSec)
    {
        if (!_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] CloseRecording called but recording is not active.");
            return;
        }

        // Phase가 열려있으면 자동 마감(안전장치)
        if (_phaseActive)
            ClosePhase(endedAtSec);

        _log.endedAtSec = endedAtSec;
        _log.phases = _phases.ToArray();
        _log.dominantTag = ComputeDominantTag(_log.instinctCountTotal, _log.analysisCountTotal, _log.chaosCountTotal);

        _eventActive = false;
    }

    public void OpenPhase(int phaseIndex, string phaseId, string profileKeyAtEnter, double startedAtSec)
    {
        EnsureEventActive();

        if (_phaseActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] OpenPhase called while phase is active. Auto-closing previous phase.");
            ClosePhase(startedAtSec);
        }

        _currentPhase = PhaseLog.CreateNew(phaseIndex, phaseId, profileKeyAtEnter, startedAtSec);
        _phaseActive = true;
    }

    public void ClosePhase(double endedAtSec)
    {
        if (!_phaseActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] ClosePhase called but phase is not active.");
            return;
        }

        _currentPhase.endedAtSec = endedAtSec;
        _phases.Add(_currentPhase);

        _phaseActive = false;
        _currentPhase = default;
    }

    // ---------- Recording API ----------
    public void RecordDonation(int amount)
    {
        EnsurePhaseActive();

        if (amount <= 0)
        {
            Debug.LogWarning($"[BroadcastEventRecorder] RecordDonation ignored: amount <= 0 ({amount})");
            return;
        }

        // Phase
        _currentPhase.donationCount += 1;
        _currentPhase.donationSum += amount;

        // Event totals
        _log.donationCountTotal += 1;
        _log.donationSumTotal += amount;

        // big donation 임계치 (P0)
        if (amount >= 10000)
            _log.flags |= BroadcastFlags.BigDonationOccurred;
    }

    public void RecordEmoji(int emojiId)
    {
        EnsurePhaseActive();

        _currentPhase.emojiCount += 1;
        _log.emojiCountTotal += 1;

        // P1에서 종류별 통계가 필요해질 때 사용
        _ = emojiId;
    }

    public void RecordChat(ChatTag tag, string optionId, IdolReaction reaction)
    {
        EnsurePhaseActive();

        _currentPhase.chatLineCount += 1;
        _log.chatLineCountTotal += 1;

        ApplyTag(tag);
        ApplyReaction(reaction);

        // P1에서 “문구별 통계/리플레이” 필요해질 때 사용
        _ = optionId;
    }

    public void RecordDecision(PhaseDecisionKind kind, string optionId, bool accepted)
    {
        EnsurePhaseActive();

        if (_currentPhase.hasDecision)
        {
            Debug.LogWarning("[BroadcastEventRecorder] RecordDecision called multiple times in one phase. Overwriting previous decision.");
        }

        _currentPhase.hasDecision = true;
        _currentPhase.decision = new PhaseDecisionLog
        {
            kind = kind,
            optionId = optionId,
            accepted = accepted
        };
    }

    public void RecordOperatorWarning()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.OperatorWarning;
        _log.operatorWarningCount += 1;
    }

    public void RecordRestrictionTriggered()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.RestrictionTriggered;
        _log.restrictionTriggeredCount += 1;
    }

    public void RecordVoteSplit()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.VoteSplit;
        _log.voteSplitCount += 1;
    }

    public void RecordClipSeeded()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.ClipSeeded;
        _log.clipSeededCount += 1;
    }

    public void RecordMyMsgPinned()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.MyMsgPinned;
        _log.myMsgPinnedCount += 1;
    }

    public void RecordIdolDirectRequest()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.IdolDirectRequest;
        _log.idolDirectRequestCount += 1;
    }

    public void RecordPromiseAccepted()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.PromiseAccepted;
        _log.promiseAcceptedCount += 1;
    }

    public void RecordPromiseDodged()
    {
        EnsureEventActive();
        _log.flags |= BroadcastFlags.PromiseDodged;
        _log.promiseDodgedCount += 1;
    }

    // ---------- Output ----------
    public BroadcastEventLog GetFinalLogOrNull()
    {
        if (_log == null) return null;

        if (_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] GetFinalLogOrNull called while recording is active. Call CloseRecording first.");
            return null;
        }

        return _log;
    }

    // ---------- Internals ----------
    private void ApplyTag(ChatTag tag)
    {
        switch (tag)
        {
            case ChatTag.Instinct:
                _currentPhase.instinctCount += 1;
                _log.instinctCountTotal += 1;
                break;
            case ChatTag.Analysis:
                _currentPhase.analysisCount += 1;
                _log.analysisCountTotal += 1;
                break;
            case ChatTag.Chaos:
                _currentPhase.chaosCount += 1;
                _log.chaosCountTotal += 1;
                break;
            default:
                Debug.LogWarning($"[BroadcastEventRecorder] Unknown ChatTag: {tag}");
                break;
        }
    }

    private void ApplyReaction(IdolReaction reaction)
    {
        switch (reaction)
        {
            case IdolReaction.Positive:
                _currentPhase.idolPositiveReact += 1;
                _log.idolPositiveReactTotal += 1;
                break;
            case IdolReaction.Negative:
                _currentPhase.idolNegativeReact += 1;
                _log.idolNegativeReactTotal += 1;
                break;
            case IdolReaction.Neutral:
                _currentPhase.idolNeutralReact += 1;
                _log.idolNeutralReactTotal += 1;
                break;
            default:
                Debug.LogWarning($"[BroadcastEventRecorder] Unknown IdolReaction: {reaction}");
                break;
        }
    }

    private void EnsureEventActive()
    {
        if (!_eventActive)
            throw new InvalidOperationException("[BroadcastEventRecorder] Recording is not active. Call OpenRecording first.");
    }

    private void EnsurePhaseActive()
    {
        EnsureEventActive();
        if (!_phaseActive)
            throw new InvalidOperationException("[BroadcastEventRecorder] Phase is not active. Call OpenPhase first.");
    }

    private void ResetInternal()
    {
        _log = null;
        _phases.Clear();
        _eventActive = false;
        _phaseActive = false;
        _currentPhase = default;
    }

    private static ChatTag ComputeDominantTag(int instinct, int analysis, int chaos)
    {
        if (instinct >= analysis && instinct >= chaos) return ChatTag.Instinct;
        if (analysis >= chaos) return ChatTag.Analysis;
        return ChatTag.Chaos;
    }
}