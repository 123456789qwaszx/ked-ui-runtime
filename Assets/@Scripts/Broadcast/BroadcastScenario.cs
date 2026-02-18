using System;
using UnityEngine;

[Serializable]
public sealed class BroadcastScenario
{
    [Tooltip("시나리오 ID (예: live_ravi_01)")]
    public string scenarioId;

    [Tooltip("방송을 구성하는 Phase 목록")]
    public PhaseSpec[] phases;

    public int PhaseCount => phases != null ? phases.Length : 0;

    public bool TryGetPhase(int index, out PhaseSpec phase)
    {
        if (phases == null || index < 0 || index >= phases.Length)
        {
            phase = default;
            return false;
        }

        phase = phases[index];
        return true;
    }

    public static BroadcastScenario CreateTestDefault()
    {
        return new BroadcastScenario
        {
            scenarioId = "live_test_01",
            phases = new[]
            {
                new PhaseSpec("perf_01", "Opening", 10f, hasIntermission: false),
                new PhaseSpec("perf_02", "Talk", 12f, hasIntermission: true),
                new PhaseSpec("perf_03", "DonationDrive", 10f, hasIntermission: true),
                new PhaseSpec("perf_04", "Crisis", 10f, hasIntermission: true),
                new PhaseSpec("perf_05", "Ending", 8f, hasIntermission: false),
            }
        };
    }
}

[Serializable]
public struct PhaseSpec
{
    [Tooltip("Phase 식별자 (예: perf_01)")]
    public string phaseId;

    [Tooltip("Phase 진입 시 기본 프로필 키")]
    public string baseProfileKey;

    [Tooltip("테스트용 지속 시간(초). 0 이하이면 자동 진행 없음.")]
    public float durationSec;

    [Tooltip("Phase 종료 후 선택(인터미션)이 있는지")]
    public bool hasIntermission;

    public PhaseSpec(string phaseId, string baseProfileKey, float durationSec, bool hasIntermission)
    {
        this.phaseId = phaseId;
        this.baseProfileKey = baseProfileKey;
        this.durationSec = durationSec;
        this.hasIntermission = hasIntermission;
    }
}