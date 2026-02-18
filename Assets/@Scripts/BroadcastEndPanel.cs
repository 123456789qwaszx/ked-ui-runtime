using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class BroadcastEndPanel : UIPanel<BroadcastEndPanel.Refs>
{
    // UI에서 “정산 확인 → 밤 사건으로 진행”을 누르면 eventKey를 넘겨주는 식
    public event Action OnCloseRequested;
    public event Action<string> OnContinueRequested; // nightEventKey

    #region Refs

    public enum Refs
    {
        // ---- Root ----
        PanelRoot_Root,

        // ---- Header ----
        TitleText_Text,
        CloseButton_Button,

        // ---- Recap ----
        RecapRoot_Root,
        SummaryText_Text,

        ChangeRow0_Root,
        ChangeRow0_Label_Text,
        ChangeRow0_Delta_Text,
        ChangeRow0_Cause_Text,

        ChangeRow1_Root,
        ChangeRow1_Label_Text,
        ChangeRow1_Delta_Text,
        ChangeRow1_Cause_Text,

        ChangeRow2_Root,
        ChangeRow2_Label_Text,
        ChangeRow2_Delta_Text,
        ChangeRow2_Cause_Text,

        // ---- Evaluation ----
        EvaluationRoot_Root,
        GradeText_Text,
        NoteText_Text,

        // ---- Settlement ----
        SettlementRoot_Root,
        DeltaZoneText_Text,
        DeltaRiskText_Text,
        DeltaPromiseText_Text,
        TokensText_Text,
        LocksText_Text,

        // ---- Night Event ----
        NightRoot_Root,
        NightTitleText_Text,
        NightTeaserText_Text,

        // ---- Next Contract ----
        ContractRoot_Root,
        ContractTitleText_Text,
        ContractGoalsText_Text,
        ContractHintText_Text,

        // ---- Footer ----
        ContinueButton_Button,
    }

    private RectTransform panelRoot;

    private TMP_Text titleText;
    private Button closeButton;

    private RectTransform recapRoot;
    private TMP_Text summaryText;
    private ChangeRow[] changeRows;

    private RectTransform evaluationRoot;
    private TMP_Text gradeText;
    private TMP_Text noteText;

    private RectTransform settlementRoot;
    private TMP_Text deltaZoneText;
    private TMP_Text deltaRiskText;
    private TMP_Text deltaPromiseText;
    private TMP_Text tokensText;
    private TMP_Text locksText;

    private RectTransform nightRoot;
    private TMP_Text nightTitleText;
    private TMP_Text nightTeaserText;

    private RectTransform contractRoot;
    private TMP_Text contractTitleText;
    private TMP_Text contractGoalsText;
    private TMP_Text contractHintText;

    private Button continueButton;

    #endregion

    private bool valid;

    // 마지막으로 세팅한 결과(버튼 콜백에서 nightKey를 넘길 때 사용)
    private BroadcastEndResult _current;

    protected override void Initialize()
    {
        panelRoot = View.Rect(Refs.PanelRoot_Root);

        titleText = GetTmp(Refs.TitleText_Text);
        closeButton = View.Button(Refs.CloseButton_Button);

        recapRoot = View.Rect(Refs.RecapRoot_Root);
        summaryText = GetTmp(Refs.SummaryText_Text);

        changeRows = new[]
        {
            new ChangeRow(
                View.Rect(Refs.ChangeRow0_Root),
                GetTmp(Refs.ChangeRow0_Label_Text),
                GetTmp(Refs.ChangeRow0_Delta_Text),
                GetTmp(Refs.ChangeRow0_Cause_Text)
            ),
            new ChangeRow(
                View.Rect(Refs.ChangeRow1_Root),
                GetTmp(Refs.ChangeRow1_Label_Text),
                GetTmp(Refs.ChangeRow1_Delta_Text),
                GetTmp(Refs.ChangeRow1_Cause_Text)
            ),
            new ChangeRow(
                View.Rect(Refs.ChangeRow2_Root),
                GetTmp(Refs.ChangeRow2_Label_Text),
                GetTmp(Refs.ChangeRow2_Delta_Text),
                GetTmp(Refs.ChangeRow2_Cause_Text)
            ),
        };

        evaluationRoot = View.Rect(Refs.EvaluationRoot_Root);
        gradeText = GetTmp(Refs.GradeText_Text);
        noteText = GetTmp(Refs.NoteText_Text);

        settlementRoot = View.Rect(Refs.SettlementRoot_Root);
        deltaZoneText = GetTmp(Refs.DeltaZoneText_Text);
        deltaRiskText = GetTmp(Refs.DeltaRiskText_Text);
        deltaPromiseText = GetTmp(Refs.DeltaPromiseText_Text);
        tokensText = GetTmp(Refs.TokensText_Text);
        locksText = GetTmp(Refs.LocksText_Text);

        nightRoot = View.Rect(Refs.NightRoot_Root);
        nightTitleText = GetTmp(Refs.NightTitleText_Text);
        nightTeaserText = GetTmp(Refs.NightTeaserText_Text);

        contractRoot = View.Rect(Refs.ContractRoot_Root);
        contractTitleText = GetTmp(Refs.ContractTitleText_Text);
        contractGoalsText = GetTmp(Refs.ContractGoalsText_Text);
        contractHintText = GetTmp(Refs.ContractHintText_Text);

        continueButton = View.Button(Refs.ContinueButton_Button);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        valid = ValidateRefs();
        if (!valid) return;
#else
        valid = true;
#endif

        // 기본은 숨김(원하면 여기서 true로)
        SetVisible(false);

        BindEvent(closeButton, _ => OnCloseRequested?.Invoke());
        BindEvent(continueButton, _ =>
        {
            // night event key로 다음 진행을 위임
            string nightKey = _current != null ? _current.nightEvent.eventKey : "none";
            OnContinueRequested?.Invoke(nightKey);
        });
    }

    public void SetVisible(bool visible)
    {
        if (!valid) return;
        if (panelRoot) panelRoot.gameObject.SetActive(visible);
    }

    public void Show(BroadcastEndResult result)
    {
        if (!valid) return;

        _current = result;

        // 방어
        if (result == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // ----- Header -----
        SetText(titleText, "방송 정산");

        // ----- Recap -----
        bool hasRecap = result.recap.changes != null && result.recap.changes.Length > 0;
        SetActive(recapRoot, hasRecap);

        SetText(summaryText, result.recap.summaryText);

        // changes: 최대 3줄 고정(너 빌더가 3줄로 생성하니까)
        for (int i = 0; i < changeRows.Length; i++)
        {
            if (result.recap.changes == null || i >= result.recap.changes.Length)
            {
                changeRows[i].SetActive(false);
                continue;
            }

            var c = result.recap.changes[i];
            changeRows[i].SetActive(true);
            changeRows[i].Set(c.label, c.delta, c.causeText);
        }

        // ----- Evaluation -----
        bool hasEval = !string.IsNullOrEmpty(result.evaluation.noteText) || result.evaluation.grade != EvalGrade.Success;
        SetActive(evaluationRoot, true);

        SetText(gradeText, $"Grade: {result.evaluation.grade}");
        SetText(noteText, result.evaluation.noteText);

        // ----- Settlement -----
        SetActive(settlementRoot, true);

        SetText(deltaZoneText, $"Zone {FormatSigned(result.settlementPayload.zoneDelta)}");
        SetText(deltaRiskText, $"Risk {FormatSigned(result.settlementPayload.riskDelta)}");
        SetText(deltaPromiseText, $"Promise {FormatSigned(result.settlementPayload.promiseDelta)}");

        SetText(tokensText, BuildTokensText(result.settlementPayload.tokenDeltas));
        SetText(locksText, BuildLocksText(result.settlementPayload.locksAdded, result.settlementPayload.locksRemoved));

        // ----- Night -----
        bool hasNight = result.nightEvent.kind != NightEventKind.None && !string.IsNullOrEmpty(result.nightEvent.eventKey);
        SetActive(nightRoot, hasNight);

        if (hasNight)
        {
            SetText(nightTitleText, result.nightEvent.titleText);
            SetText(nightTeaserText, result.nightEvent.teaserText);
        }

        // ----- Contract -----
        bool hasContract = !string.IsNullOrEmpty(result.nextContract.contractId);
        SetActive(contractRoot, hasContract);

        if (hasContract)
        {
            SetText(contractTitleText, result.nextContract.titleText);
            SetText(contractGoalsText, BuildGoalsText(result.nextContract.goalsText));
            SetText(contractHintText, result.nextContract.hintText);
        }
    }

    // ---------- Helpers ----------

    private TMP_Text GetTmp(Refs r)
    {
        var rt = View.Rect(r);
        return rt ? rt.GetComponent<TMP_Text>() : null;
    }

    private static void SetText(TMP_Text t, string s)
    {
        if (!t) return;
        t.text = s ?? string.Empty;
    }

    private static void SetActive(RectTransform rt, bool active)
    {
        if (!rt) return;
        rt.gameObject.SetActive(active);
    }

    private static string FormatSigned(int v)
    {
        if (v > 0) return $"+{v}";
        return v.ToString();
    }

    private static string BuildGoalsText(string[] goals)
    {
        if (goals == null || goals.Length <= 0) return string.Empty;

        // P0: 1~2개만이지만 안전하게 처리
        if (goals.Length == 1) return $"• {goals[0]}";
        return $"• {goals[0]}\n• {goals[1]}";
    }

    private static string BuildTokensText(TokenDelta[] tokenDeltas)
    {
        if (tokenDeltas == null || tokenDeltas.Length <= 0)
            return "Tokens: (없음)";

        // effectText 1줄 요약을 그대로 쓰는게 네 설계 의도에 맞음
        // (필요하면 kind/tokenId도 붙이면 됨)
        var lines = new System.Text.StringBuilder(128);
        lines.Append("Tokens:\n");
        int count = Mathf.Min(tokenDeltas.Length, 4); // P0: 너무 길어지면 컷
        for (int i = 0; i < count; i++)
        {
            string text = tokenDeltas[i].effectText;
            if (string.IsNullOrEmpty(text))
                text = $"{tokenDeltas[i].kind} {tokenDeltas[i].tokenId} ({FormatSigned(tokenDeltas[i].stackDelta)})";

            lines.Append("• ").Append(text).Append('\n');
        }
        return lines.ToString().TrimEnd();
    }

    private static string BuildLocksText(LockFlags added, LockFlags removed)
    {
        // removed는 P0에선 None이겠지만 준비는 해둠
        if (added == LockFlags.None && removed == LockFlags.None)
            return "Locks: (변화 없음)";

        if (removed == LockFlags.None)
            return $"Locks Added: {added}";

        if (added == LockFlags.None)
            return $"Locks Removed: {removed}";

        return $"Locks Added: {added}\nLocks Removed: {removed}";
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, panelRoot, Refs.PanelRoot_Root);

        AppendMissing(ref missing, titleText, Refs.TitleText_Text);
        AppendMissing(ref missing, closeButton, Refs.CloseButton_Button);

        AppendMissing(ref missing, recapRoot, Refs.RecapRoot_Root);
        AppendMissing(ref missing, summaryText, Refs.SummaryText_Text);

        // for (int i = 0; i < changeRows.Length; i++)
        // {
        //     changeRows[i].AppendMissing(ref missing, i);
        // }

        AppendMissing(ref missing, evaluationRoot, Refs.EvaluationRoot_Root);
        AppendMissing(ref missing, gradeText, Refs.GradeText_Text);
        AppendMissing(ref missing, noteText, Refs.NoteText_Text);

        AppendMissing(ref missing, settlementRoot, Refs.SettlementRoot_Root);
        AppendMissing(ref missing, deltaZoneText, Refs.DeltaZoneText_Text);
        AppendMissing(ref missing, deltaRiskText, Refs.DeltaRiskText_Text);
        AppendMissing(ref missing, deltaPromiseText, Refs.DeltaPromiseText_Text);
        AppendMissing(ref missing, tokensText, Refs.TokensText_Text);
        AppendMissing(ref missing, locksText, Refs.LocksText_Text);

        // night/contract는 “없어도” 게임이 멈추진 않지만, UI 자체는 있길 권장
        AppendMissing(ref missing, nightRoot, Refs.NightRoot_Root);
        AppendMissing(ref missing, nightTitleText, Refs.NightTitleText_Text);
        AppendMissing(ref missing, nightTeaserText, Refs.NightTeaserText_Text);

        AppendMissing(ref missing, contractRoot, Refs.ContractRoot_Root);
        AppendMissing(ref missing, contractTitleText, Refs.ContractTitleText_Text);
        AppendMissing(ref missing, contractGoalsText, Refs.ContractGoalsText_Text);
        AppendMissing(ref missing, contractHintText, Refs.ContractHintText_Text);

        AppendMissing(ref missing, continueButton, Refs.ContinueButton_Button);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[BroadcastEndPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }

    private readonly struct ChangeRow
    {
        private readonly RectTransform _root;
        private readonly TMP_Text _label;
        private readonly TMP_Text _delta;
        private readonly TMP_Text _cause;

        public ChangeRow(RectTransform root, TMP_Text label, TMP_Text delta, TMP_Text cause)
        {
            _root = root;
            _label = label;
            _delta = delta;
            _cause = cause;
        }

        public void SetActive(bool active)
        {
            if (_root) _root.gameObject.SetActive(active);
        }

        public void Set(string label, int delta, string cause)
        {
            if (_label) _label.text = label ?? "";
            if (_delta) _delta.text = FormatSigned(delta);
            if (_cause) _cause.text = cause ?? "";
        }
    }
}
