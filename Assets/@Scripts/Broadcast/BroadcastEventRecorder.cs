using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BroadcastEventRecorder
{
    private BroadcastEventLog _log;
    private readonly List<PhaseLog> _phases = new List<PhaseLog>(capacity: 4);

    private bool _eventActive;
    private bool _phaseActive;

    private int _currentPhaseIndex = -1;
    private PhaseLog _currentPhase;

    public bool IsEventActive => _eventActive;
    public bool IsPhaseActive => _phaseActive;

    public void BeginEvent(string runId, string eventId, int eventIndex, double startedAtSec)
    {
        // 중복 시작 방지
        if (_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] BeginEvent called while event is active. Resetting previous state.");
            ResetInternal();
        }

        _log = new BroadcastEventLog
        {
            runId = runId,
            eventId = eventId,
            eventIndex = eventIndex,
            startedAtSec = startedAtSec,
            endedAtSec = double.NaN,
            phases = null,
            indicesAtEnd = default,
            
            flags = BroadcastFlags.None,

            operatorWarningCount = 0,
            restrictionTriggeredCount = 0,
            voteSplitCount = 0,
            clipSeededCount = 0,
            myMsgPinnedCount = 0,
            idolDirectRequestCount = 0,
            promiseAcceptedCount = 0,
            promiseDodgedCount = 0,

            dominantTag = ChatTag.Instinct, // 기본값
        };

        _phases.Clear();
        _eventActive = true;
        _phaseActive = false;
        _currentPhaseIndex = -1;
    }

    public void EndEvent(double endedAtSec)
    {
        if (!_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] EndEvent called but event is not active.");
            return;
        }

        // Phase가 열려있으면 자동 마감(안전장치)
        if (_phaseActive)
        {
            EndPhase(endedAtSec);
        }

        _log.endedAtSec = endedAtSec;
        _log.phases = _phases.ToArray();
        
        _log.dominantTag = ComputeDominantTag(_log.instinctCountTotal, _log.analysisCountTotal, _log.chaosCountTotal);

        _eventActive = false;
    }

    public void BeginPhase(int phaseIndex, string phaseId, string profileKeyAtEnter, double startedAtSec)
    {
        EnsureEventActive();

        if (_phaseActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] BeginPhase called while phase is active. Auto-ending previous phase.");
            EndPhase(startedAtSec);
        }

        _currentPhaseIndex = phaseIndex;
        _currentPhase = new PhaseLog
        {
            phaseIndex = phaseIndex,
            phaseId = phaseId,
            profileKeyAtEnter = profileKeyAtEnter,
            startedAtSec = startedAtSec,
            endedAtSec = double.NaN,

            donationCount = 0,
            donationSum = 0,

            emojiCount = 0,
            chatLineCount = 0,

            instinctCount = 0,
            analysisCount = 0,
            chaosCount = 0,

            idolPositiveReact = 0,
            idolNegativeReact = 0,
            idolNeutralReact = 0,

            hasDecision = false,
            decision = default
        };

        _phaseActive = true;
    }

    public void EndPhase(double endedAtSec)
    {
        if (!_phaseActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] EndPhase called but phase is not active.");
            return;
        }

        _currentPhase.endedAtSec = endedAtSec;
        _phases.Add(_currentPhase);

        _phaseActive = false;
        _currentPhaseIndex = -1;
        _currentPhase = default;
    }

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
        
        // big donation 임계치
        if (amount >= 10000)
            _log.flags |= BroadcastFlags.BigDonationOccurred;
    }

    public void RecordEmoji(int emojiId)
    {
        EnsurePhaseActive();

        // P0: 종류별 카운트는 필요 없고 total만 증가
        _currentPhase.emojiCount += 1;
        _log.emojiCountTotal += 1;

        // emojiId는 P1에서 “종류별 통계” 필요해질 때 사용
        _ = emojiId;
    }

    public void RecordChat(ChatTag tag, string optionId, IdolReaction reaction)
    {
        EnsurePhaseActive();

        _currentPhase.chatLineCount += 1;
        _log.chatLineCountTotal += 1;

        // Tag
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

        // Reaction
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

        // optionId는 P1에서 “문구별 통계/리플레이” 필요해질 때 사용
        _ = optionId;
    }

    public void RecordDecision(PhaseDecisionKind kind, string optionId, bool accepted)
    {
        EnsurePhaseActive();

        // Phase당 0~1회 규칙 (두 번 들어오면 덮어씀 + 경고)
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

    public BroadcastEventLog BuildFinalLogOrNull()
    {
        if (_log == null) return null;

        // EndEvent가 호출되어야만 확정본이다.
        if (_eventActive)
        {
            Debug.LogWarning("[BroadcastEventRecorder] BuildFinalLogOrNull called while event is active. Call EndEvent first.");
            return null;
        }

        return _log;
    }

    private void EnsureEventActive()
    {
        if (!_eventActive)
            throw new InvalidOperationException("[BroadcastEventRecorder] Event is not active. Call BeginEvent first.");
    }

    private void EnsurePhaseActive()
    {
        EnsureEventActive();
        if (!_phaseActive)
            throw new InvalidOperationException("[BroadcastEventRecorder] Phase is not active. Call BeginPhase first.");
    }

    private void ResetInternal()
    {
        _log = null;
        _phases.Clear();
        _eventActive = false;
        _phaseActive = false;
        _currentPhaseIndex = -1;
        _currentPhase = default;
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
    
    private static ChatTag ComputeDominantTag(int instinct, int analysis, int chaos)
    {
        // 동점 규칙(P0): Instinct > Analysis > Chaos 우선순위로 안정적으로 고정
        // (원하면 나중에 Reaction 가중치 등으로 바꿀 수 있음)
        if (instinct >= analysis && instinct >= chaos) return ChatTag.Instinct;
        if (analysis >= chaos) return ChatTag.Analysis;
        return ChatTag.Chaos;
    }
}