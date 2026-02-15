// Editor/LiveScenarioGenerator.cs
// Unity 에디터 전용: 시나리오 자동 생성 유틸
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LiveScenarioGenerator
{
    [MenuItem("Tools/Dev/Generate Stress Test Scenario (30 lines)")]
    public static void GenerateStressTestScenario()
    {
        var scenario = ScriptableObject.CreateInstance<LiveTestScenarioSO>();
        scenario.steps = new LiveTestScenarioSO.Step[30];
        scenario.loop = false;
        scenario.speedMultiplier = 1f;
        scenario.randomSeed = 12345;

        for (int i = 0; i < 30; i++)
        {
            scenario.steps[i] = new LiveTestScenarioSO.Step
            {
                time = i * 0.33f,
                kind = ChatEntryKind.Chat,
                name = $"viewer_{i % 7}",
                body = $"테스트 채팅 {i}",
                isMy = false,
            };
        }

        // Assets/Dev/Scenarios/ 폴더 확인/생성
        string folderPath = "Assets/Dev/Scenarios";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dev"))
                AssetDatabase.CreateFolder("Assets", "Dev");
            AssetDatabase.CreateFolder("Assets/Dev", "Scenarios");
        }

        string path = $"{folderPath}/Stress_30Lines10Sec.asset";
        AssetDatabase.CreateAsset(scenario, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = scenario;

        Debug.Log($"[LiveScenarioGenerator] Created: {path}");
    }

    [MenuItem("Tools/Dev/Generate Donation Test Scenario")]
    public static void GenerateDonationTestScenario()
    {
        var scenario = ScriptableObject.CreateInstance<LiveTestScenarioSO>();
        scenario.steps = new LiveTestScenarioSO.Step[6];
        scenario.loop = false;
        scenario.speedMultiplier = 1f;
        scenario.randomSeed = 0;

        scenario.steps[0] = new LiveTestScenarioSO.Step
        {
            time = 0f,
            kind = ChatEntryKind.Chat,
            name = "viewer_1",
            body = "안녕하세요~",
            isMy = false,
        };

        scenario.steps[1] = new LiveTestScenarioSO.Step
        {
            time = 1f,
            kind = ChatEntryKind.Donation,
            name = "나",
            donationAmount = 1000,
            isMy = true,
        };

        scenario.steps[2] = new LiveTestScenarioSO.Step
        {
            time = 3f,
            kind = ChatEntryKind.Donation,
            name = "viewer_2",
            donationAmount = 10000,
            isMy = false,
        };

        scenario.steps[3] = new LiveTestScenarioSO.Step
        {
            time = 5f,
            kind = ChatEntryKind.Chat,
            name = "viewer_3",
            body = "후원 감사합니다!",
            isMy = false,
        };

        scenario.steps[4] = new LiveTestScenarioSO.Step
        {
            time = 7f,
            kind = ChatEntryKind.Idol,
            body = "오늘도 함께해줘서 고마워!",
        };

        scenario.steps[5] = new LiveTestScenarioSO.Step
        {
            time = 9f,
            kind = ChatEntryKind.System,
            body = "테스트 종료",
        };

        string folderPath = "Assets/Dev/Scenarios";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dev"))
                AssetDatabase.CreateFolder("Assets", "Dev");
            AssetDatabase.CreateFolder("Assets/Dev", "Scenarios");
        }

        string path = $"{folderPath}/Donation_Reaction.asset";
        AssetDatabase.CreateAsset(scenario, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = scenario;

        Debug.Log($"[LiveScenarioGenerator] Created: {path}");
    }
}
#endif