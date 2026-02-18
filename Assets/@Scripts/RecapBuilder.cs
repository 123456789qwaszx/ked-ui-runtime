using UnityEngine;

public static class RecapBuilder
{
    public static RecapLine Build(BroadcastEventLog log, Deltas deltas, EvaluationResult eval, TokenDelta[] tokenDeltas, LockFlags locksAdded)
    {
        // 1) 요약 문장 선택(간단 버전): 가장 큰 절대 델타 축으로
        string summary = PickSummary(deltas, eval, log);

        // 2) 핵심 변화 3줄 고정
        var changes = new RecapChangeLine[3];

        changes[0] = new RecapChangeLine
        {
            label = "Zone",
            delta = deltas.zone,
            causeText = BuildZoneCause(log)
        };

        changes[1] = new RecapChangeLine
        {
            label = "Risk",
            delta = deltas.risk,
            causeText = BuildRiskCause(log, eval)
        };

        // 3번째 줄: Promise 우선, 없으면 Token/Lock
        if (deltas.promise != 0)
        {
            changes[2] = new RecapChangeLine
            {
                label = "Promise",
                delta = deltas.promise,
                causeText = BuildPromiseCause(log)
            };
        }
        else if (locksAdded != LockFlags.None)
        {
            changes[2] = new RecapChangeLine
            {
                label = "Lock",
                delta = 0,
                causeText = $"(원인: 불길함 누적) {locksAdded}"
            };
        }
        else if (tokenDeltas != null && tokenDeltas.Length > 0)
        {
            changes[2] = new RecapChangeLine
            {
                label = "Token",
                delta = 0,
                causeText = tokenDeltas[0].effectText
            };
        }
        else
        {
            changes[2] = new RecapChangeLine
            {
                label = "Note",
                delta = 0,
                causeText = "(원인: 없음)"
            };
        }

        return new RecapLine { summaryText = summary, changes = changes };
    }

    private static string PickSummary(Deltas deltas, EvaluationResult eval, BroadcastEventLog log)
    {
        // Critical 우선
        if (eval.grade == EvalGrade.Critical)
            return "괜찮아! …근데 이제, 규칙이 바뀔 거야.";

        int az = Mathf.Abs(deltas.zone);
        int ar = Mathf.Abs(deltas.risk);
        int ap = Mathf.Abs(deltas.promise);

        if (ap >= az && ap >= ar)
            return "고마워! …그럼 약속, 더 해줄 수 있지?";
        if (ar >= az && ar >= ap)
            return "오늘도 즐거웠지? …다들 ‘그 얘기’만 하더라.";
        return "오늘 무대는 최고였어! …그러니까, 지켜줘야 해.";
    }

    private static string BuildZoneCause(BroadcastEventLog log)
    {
        if ((log.flags & BroadcastFlags.BigDonationOccurred) != 0) return "(원인: 고액 후원)";
        if (log.myMsgPinnedCount > 0) return "(원인: 내 메시지 하이라이트)";
        return "(원인: 방송 성과)";
    }

    private static string BuildRiskCause(BroadcastEventLog log, EvaluationResult eval)
    {
        if (log.operatorWarningCount > 0) return "(원인: 운영 경고)";
        if (log.clipSeededCount > 0) return "(원인: 클립/밈 확산)";
        if (eval.grade == EvalGrade.Breach) return "(원인: 임계치 근접)";
        return "(원인: 채팅 과열)";
    }

    private static string BuildPromiseCause(BroadcastEventLog log)
    {
        if (log.promiseAcceptedCount > 0) return "(원인: 약속 수락)";
        if (log.idolDirectRequestCount > 0) return "(원인: 직접 요청)";
        return "(원인: 관계 압박)";
    }
}
