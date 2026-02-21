using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class DmThreadPanel : UIBase<DmThreadPanel.Refs>, IUIPanel
{
    // Panel -> External events (선택지 눌렀을 때 Player로 전달용)
    public event Action<int> OnChoiceOptionSelected;

    public enum Refs
    {
        Root_Root,

        Header_Root,
        HeaderTitle_Text,          // optional

        ThreadScrollRect_ScrollRect,
        ThreadViewport_Root,       // optional
        ThreadContent_Root,

        // Entry templates (inactive recommended)
        EntryIncomingTemplate_Widget,
        EntryOutgoingTemplate_Widget,
        EntrySystemTemplate_Widget,

        // Choice bar widget (optional but recommended)
        ChoiceBar_Widget,
    }

    private bool _valid;

    private CanvasGroup _rootCg;

    private TMP_Text _headerTitle;

    private ScrollRect _scroll;
    private RectTransform _content;

    private DmEntryView _tplIncoming;
    private DmEntryView _tplOutgoing;
    private DmEntryView _tplSystem;

    private DmChoiceBar _choiceBar;

    // Pool per kind
    private readonly Stack<DmEntryView> _poolIncoming = new();
    private readonly Stack<DmEntryView> _poolOutgoing = new();
    private readonly Stack<DmEntryView> _poolSystem = new();

    // Active list (for release)
    private readonly List<DmEntryView> _active = new();

    private bool _inputBlocked;

    protected override void Initialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        BindHandlers();

        Show(false);
        ShowChoices(false, null, null);

        // 템플릿은 꺼둔 상태 권장
        if (_tplIncoming) _tplIncoming.SetActive(false);
        if (_tplOutgoing) _tplOutgoing.SetActive(false);
        if (_tplSystem) _tplSystem.SetActive(false);
    }

    private void CacheRefs()
    {
        _rootCg = View.CanvasGroup(Refs.Root_Root);

        _headerTitle = View.Text(Refs.HeaderTitle_Text);

        var scrollRt = View.Rect(Refs.ThreadScrollRect_ScrollRect);
        _scroll = scrollRt ? scrollRt.GetComponent<ScrollRect>() : null;

        _content = View.Rect(Refs.ThreadContent_Root);

        _tplIncoming = View.Widget<DmEntryView>(Refs.EntryIncomingTemplate_Widget);
        _tplOutgoing = View.Widget<DmEntryView>(Refs.EntryOutgoingTemplate_Widget);
        _tplSystem = View.Widget<DmEntryView>(Refs.EntrySystemTemplate_Widget);

        _choiceBar = View.Widget<DmChoiceBar>(Refs.ChoiceBar_Widget);
    }

    private void BindHandlers()
    {
        if (_choiceBar)
        {
            _choiceBar.OnOptionSelected -= HandleChoiceSelected;
            _choiceBar.OnOptionSelected += HandleChoiceSelected;
        }
    }

    private void HandleChoiceSelected(int index)
    {
        if (_inputBlocked) return;
        OnChoiceOptionSelected?.Invoke(index);
    }

    // --------------------
    // Public API
    // --------------------

    public void Show(bool visible)
    {
        if (!_valid) return;
        SetCanvasGroupVisible(_rootCg, visible);
    }

    public void SetHeaderTitle(string text)
    {
        if (!_valid) return;
        if (_headerTitle)
        {
            bool has = !string.IsNullOrEmpty(text);
            _headerTitle.gameObject.SetActive(has);
            _headerTitle.text = has ? text : "";
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        if (!_valid) return;

        _inputBlocked = blocked;

        if (_scroll) _scroll.enabled = !blocked;
        if (_choiceBar) _choiceBar.SetInteractable(!blocked);
    }

    public void ClearThread()
    {
        if (!_valid) return;

        // Release active entries to pool
        for (int i = 0; i < _active.Count; i++)
            Release(_active[i]);

        _active.Clear();

        // Also clear content children if something went wrong (safety)
        // (선택) 강하게 정리하고 싶으면 아래를 켜도 됨.
        // for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);

        ScrollToBottom(immediate: true);
    }

    /// <summary>
    /// Append one entry. Returns the spawned view so Player가 타이핑 제어 가능.
    /// </summary>
    public DmEntryView AppendEntry(in DmEntryModel model, string timeText = null)
    {
        if (!_valid) return null;

        var view = Acquire(model.Kind);
        if (!view) return null;

        view.transform.SetParent(_content, worldPositionStays: false);
        view.SetActive(true);
        view.SetModel(model, timeText);

        _active.Add(view);

        return view;
    }

    public void ScrollToBottom(bool immediate)
    {
        if (!_valid) return;
        if (!_scroll) return;

        if (immediate)
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            // end-of-frame 한번 미루기
            StartCoroutine(CoScrollToBottom());
        }
    }

    private System.Collections.IEnumerator CoScrollToBottom()
    {
        yield return null;
        if (_scroll)
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Show/hide choices with prompt and option strings.
    /// </summary>
    public void ShowChoices(bool visible, string prompt, string[] options)
    {
        if (!_valid) return;

        if (!_choiceBar)
            return;

        _choiceBar.SetPrompt(prompt);
        _choiceBar.SetOptions(options);

        _choiceBar.Show(visible);
    }

    // --------------------
    // Pool
    // --------------------

    private DmEntryView Acquire(DmEntryKind kind)
    {
        switch (kind)
        {
            case DmEntryKind.Incoming:
                return AcquireFrom(_poolIncoming, _tplIncoming);
            case DmEntryKind.Outgoing:
                return AcquireFrom(_poolOutgoing, _tplOutgoing);
            case DmEntryKind.System:
                return AcquireFrom(_poolSystem, _tplSystem);
            default:
                return null;
        }
    }

    private static DmEntryView AcquireFrom(Stack<DmEntryView> pool, DmEntryView template)
    {
        if (pool.Count > 0)
            return pool.Pop();

        if (!template)
            return null;

        // template을 content에 두지 말고, panel 하위 어딘가에 비활성으로 두는 걸 권장
        var go = UnityEngine.Object.Instantiate(template.gameObject);
        var v = go.GetComponent<DmEntryView>();
        return v;
    }

    private void Release(DmEntryView view)
    {
        if (!view) return;

        view.ForceCompleteTyping();
        view.SetActive(false);
        view.transform.SetParent(transform, worldPositionStays: false); // panel 밑으로 회수(정리용)

        // kind 판별: 템플릿 참조 비교로 간단히 처리 (권장: view에 Kind 필드 두는 방식도 가능)
        // 여기서는 템플릿 프리팹 이름으로 대충 분류하지 않고, 3개 풀에 “균등 push”하는 대신
        // 더 안전하게 하려면 view에 kind를 저장하는 필드를 추가해도 됨.
        // 지금은 가장 간단하게: outgoing/incoming/system을 view의 이름 prefix로 구분하자(선택).
        string n = view.name ?? "";
        if (n.Contains("Incoming"))
            _poolIncoming.Push(view);
        else if (n.Contains("Outgoing"))
            _poolOutgoing.Push(view);
        else if (n.Contains("System"))
            _poolSystem.Push(view);
        else
            _poolIncoming.Push(view); // fallback
    }

    // --------------------
    // Internals
    // --------------------

    private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _rootCg, Refs.Root_Root);

        AppendMissing(ref missing, _scroll, Refs.ThreadScrollRect_ScrollRect);
        AppendMissing(ref missing, _content, Refs.ThreadContent_Root);

        // 템플릿 3종은 사실상 필수 (풀링/표현을 위해)
        AppendMissing(ref missing, _tplIncoming, Refs.EntryIncomingTemplate_Widget);
        AppendMissing(ref missing, _tplOutgoing, Refs.EntryOutgoingTemplate_Widget);
        AppendMissing(ref missing, _tplSystem, Refs.EntrySystemTemplate_Widget);

        // ChoiceBar는 DM에 선택지가 없다면 없어도 되지만, 대부분 필요
        // AppendMissing(ref missing, _choiceBar, Refs.ChoiceBar_Widget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DmThreadPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}