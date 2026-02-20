using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class BroadcastHubUIRoot : UIRoot<BroadcastHubUIRoot.Refs>
{
    // ====== Events ======
    public event Action OnBackRequested;

    public event Action OnTabSnsRequested;
    public event Action OnTabPollRequested;
    public event Action OnTabDmRequested;

    public event Action<int> OnPollOptionSelected; // 0..N-1
    public event Action OnPollConfirmRequested;

    #region Refs
    public enum Refs
    {
        // --------------------
        // Root / Top controls (optional)
        // --------------------
        BackButton_Button,
        OverlayDim_Image, // ✅ 코드에서 사용중이므로 추가

        // --------------------
        // Left: Phone shell
        // --------------------
        HubPhone_Root,
        HubPhoneBG_Root,
        HubPhoneBG_Image,

        HubPhoneTop_Root,
        HubPhoneTopTime_Text,
        HubPhoneTopNotch_Image,
        HubPhoneTopBattery_Image,

        // Bottom tab bar
        TabBar_Root,
        TabSns_Button,
        TabPoll_Button,
        TabDm_Button,

        TabSns_LabelText,           // optional
        TabPoll_LabelText,          // optional
        TabDm_LabelText,            // optional

        // --------------------
        // Left: Pages (only one visible at a time)
        // --------------------
        PageSns_Root,
        PagePoll_Root,
        PageDm_Root,

        // --------------------
        // Page: SNS
        // --------------------
        SnsScrollRect_ScrollRect,   // optional
        SnsContent_Root,            // optional (feed content)

        // --------------------
        // Page: Poll (broadcast selection)
        // --------------------
        PollHeaderText_Text,        // optional
        PollOptionBar_Root,         // row for option buttons

        PollOption0_Button,
        PollOption1_Button,
        PollOption2_Button,
        PollOption3_Button,         // optional
        PollOption4_Button,         // optional

        PollOption0_LabelText,      // optional
        PollOption1_LabelText,      // optional
        PollOption2_LabelText,      // optional
        PollOption3_LabelText,      // optional
        PollOption4_LabelText,      // optional

        PollConfirm_Button,
        PollConfirm_LabelText,      // optional

        // --------------------
        // Left: DM (or Gifts) - placeholder
        // --------------------
        DmScrollRect_ScrollRect,    // optional
        DmContent_Root,             // optional

        // --------------------
        // Right: Inspector (Ravi info panel)
        // --------------------
        InspectorRoot_Root,

        Hero_Image,                 // always on
        HeroStatusText_Text,        // e.g., "DAY 7 | OFF AIR"
        HeroMoodText_Text,          // optional small line

        // ✅ 코드에서 사용중이므로 추가
        ContextTitleText_Text,
        ContextBodyText_Text,

        DeltaRow_Root,
        DeltaText_Text,

        // ✅ 우측 상세 패널(별도 Panel)
        PollDetailPanel_Widget,
        DmThreadPanel_Widget,
    }

    // ===== Cache =====
    private bool _valid;

    // Root / optional
    private Button _backButton;
    private Image _overlayDim;

    // Tabs
    private Button _tabSnsBtn;
    private Button _tabPollBtn;
    private Button _tabDmBtn;

    // Pages
    private CanvasGroup _pageSnsCg;
    private CanvasGroup _pagePollCg;
    private CanvasGroup _pageDmCg;

    // Poll options
    private Button[] _pollOptionBtns;
    private TMP_Text[] _pollOptionLabels;
    private Button _pollConfirmBtn;
    private TMP_Text _pollConfirmLabel;

    // Inspector (header)
    private Image _raviBurst;
    private TMP_Text _raviStatusText;
    private TMP_Text _raviMoodText;

    private TMP_Text _contextTitle;
    private TMP_Text _contextBody;

    private CanvasGroup _deltaRowCg;
    private TMP_Text _deltaText;

    // Inspector widgets
    private PollDetailPanel _pollDetailPanel;
    private DmThreadPanel _dmThreadPanel;

    // State
    private int _selectedPollIndex = -1;

    #endregion

    // Tab enum (internal convenience)
    public enum TabKind : byte
    {
        Sns = 0,
        Poll = 1,
        Dm = 2,
    }

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

        // Default state
        SetOverlayDimVisible(false);
        ShowTab(TabKind.Sns);
        SetPollSelected(-1);
        SetPollConfirmInteractable(false);

        SetDeltaVisible(false);

        // Right panels default
        if (_pollDetailPanel) _pollDetailPanel.Show(false);
        if (_dmThreadPanel) _dmThreadPanel.Show(false);
    }

    private void CacheRefs()
    {
        _backButton = View.Button(Refs.BackButton_Button);
        _overlayDim = View.Image(Refs.OverlayDim_Image);

        _tabSnsBtn = View.Button(Refs.TabSns_Button);
        _tabPollBtn = View.Button(Refs.TabPoll_Button);
        _tabDmBtn = View.Button(Refs.TabDm_Button);

        // Pages are best controlled by CanvasGroup for smooth switching
        _pageSnsCg = View.CanvasGroup(Refs.PageSns_Root);
        _pagePollCg = View.CanvasGroup(Refs.PagePoll_Root);
        _pageDmCg = View.CanvasGroup(Refs.PageDm_Root);

        // Poll
        _pollOptionBtns = new[]
        {
            View.Button(Refs.PollOption0_Button),
            View.Button(Refs.PollOption1_Button),
            View.Button(Refs.PollOption2_Button),
            View.Button(Refs.PollOption3_Button), // optional
            View.Button(Refs.PollOption4_Button), // optional
        };

        _pollOptionLabels = new[]
        {
            View.Text(Refs.PollOption0_LabelText),
            View.Text(Refs.PollOption1_LabelText),
            View.Text(Refs.PollOption2_LabelText),
            View.Text(Refs.PollOption3_LabelText),
            View.Text(Refs.PollOption4_LabelText),
        };

        _pollConfirmBtn = View.Button(Refs.PollConfirm_Button);
        _pollConfirmLabel = View.Text(Refs.PollConfirm_LabelText);

        // Inspector header
        _raviBurst = View.Image(Refs.Hero_Image);
        _raviStatusText = View.Text(Refs.HeroStatusText_Text);
        _raviMoodText = View.Text(Refs.HeroMoodText_Text);

        _contextTitle = View.Text(Refs.ContextTitleText_Text);
        _contextBody = View.Text(Refs.ContextBodyText_Text);

        _deltaRowCg = View.CanvasGroup(Refs.DeltaRow_Root);
        _deltaText = View.Text(Refs.DeltaText_Text);

        // Inspector widgets
        _pollDetailPanel = View.Widget<PollDetailPanel>(Refs.PollDetailPanel_Widget);
        _dmThreadPanel = View.Widget<DmThreadPanel>(Refs.DmThreadPanel_Widget);
    }

    private void BindHandlers()
    {
        // Back
        BindEvent(_backButton, _ => OnBackRequested?.Invoke());

        // Tabs
        BindEvent(_tabSnsBtn, _ =>
        {
            ShowTab(TabKind.Sns);
            OnTabSnsRequested?.Invoke();
        });

        BindEvent(_tabPollBtn, _ =>
        {
            ShowTab(TabKind.Poll);
            OnTabPollRequested?.Invoke();
        });

        BindEvent(_tabDmBtn, _ =>
        {
            ShowTab(TabKind.Dm);
            OnTabDmRequested?.Invoke();
        });

        // Poll options
        for (int i = 0; i < _pollOptionBtns.Length; i++)
        {
            int idx = i;
            var btn = _pollOptionBtns[i];
            if (!btn) continue;

            BindEvent(btn, _ =>
            {
                if (!btn.interactable) return;

                SetPollSelected(idx);
                OnPollOptionSelected?.Invoke(idx);
            });
        }

        // Poll confirm (left)
        BindEvent(_pollConfirmBtn, _ =>
        {
            if (_selectedPollIndex < 0) return;
            OnPollConfirmRequested?.Invoke();
        });

        // Poll confirm (right panel)
        if (_pollDetailPanel)
        {
            _pollDetailPanel.OnConfirmRequested -= HandlePollPanelConfirm;
            _pollDetailPanel.OnConfirmRequested += HandlePollPanelConfirm;
        }
    }

    private void HandlePollPanelConfirm()
    {
        if (_selectedPollIndex < 0) return;
        OnPollConfirmRequested?.Invoke();
    }

    // --------------------
    // Public API
    // --------------------

    public PollDetailPanel GetPollDetailPanel() => _pollDetailPanel;
    public DmThreadPanel GetDmThreadPanel() => _dmThreadPanel;

    public void ShowTab(TabKind tab)
    {
        if (!_valid) return;

        SetPageVisible(_pageSnsCg, tab == TabKind.Sns);
        SetPageVisible(_pagePollCg, tab == TabKind.Poll);
        SetPageVisible(_pageDmCg, tab == TabKind.Dm);

        // 탭 전환 시 우측 상세 패널 기본 정책 (원하면 Presenter가 오버라이드)
        if (_pollDetailPanel) _pollDetailPanel.Show(tab == TabKind.Poll);
        if (_dmThreadPanel) _dmThreadPanel.Show(tab == TabKind.Dm);
    }

    public void SetOverlayDimVisible(bool visible)
    {
        if (!_valid) return;
        if (_overlayDim) _overlayDim.gameObject.SetActive(visible);
    }

    public void SetRaviBurst(Sprite sprite)
    {
        if (!_valid) return;
        if (_raviBurst) _raviBurst.sprite = sprite;
    }

    public void SetRaviStatus(string statusLine)
    {
        if (!_valid) return;
        if (_raviStatusText) _raviStatusText.text = statusLine ?? "";
    }

    public void SetRaviMood(string moodLine)
    {
        if (!_valid) return;
        if (_raviMoodText) _raviMoodText.text = moodLine ?? "";
    }

    public void SetContext(string title, string body)
    {
        if (!_valid) return;
        if (_contextTitle) _contextTitle.text = title ?? "";
        if (_contextBody) _contextBody.text = body ?? "";
    }

    public void SetDeltaVisible(bool visible)
    {
        if (!_valid) return;
        SetCanvasGroupVisible(_deltaRowCg, visible);
    }

    public void SetDeltaText(string text)
    {
        if (!_valid) return;
        if (_deltaText) _deltaText.text = text ?? "";
    }

    public void SetPollOptions(params string[] options)
    {
        if (!_valid) return;

        int count = options != null ? options.Length : 0;

        for (int i = 0; i < _pollOptionBtns.Length; i++)
        {
            var btn = _pollOptionBtns[i];
            var label = _pollOptionLabels[i];

            bool has = i < count && !string.IsNullOrEmpty(options[i]);

            if (btn)
            {
                btn.gameObject.SetActive(has);
                btn.interactable = has;
            }

            if (label)
                label.text = has ? options[i] : "";
        }

        SetPollSelected(-1);
        SetPollConfirmInteractable(false);
    }

    public void SetPollSelected(int index)
    {
        if (!_valid) return;

        _selectedPollIndex = index;
        SetPollConfirmInteractable(_selectedPollIndex >= 0);

        for (int i = 0; i < _pollOptionLabels.Length; i++)
        {
            var t = _pollOptionLabels[i];
            if (!t) continue;

            bool isActive = _pollOptionBtns[i] && _pollOptionBtns[i].gameObject.activeSelf;
            if (!isActive) continue;

            string raw = t.text;
            raw = raw != null ? raw.Replace("▶ ", "").TrimStart() : "";

            t.text = (i == _selectedPollIndex) ? $"▶ {raw}" : raw;
        }
    }

    public void SetPollConfirmInteractable(bool interactable)
    {
        if (!_valid) return;
        if (_pollConfirmBtn) _pollConfirmBtn.interactable = interactable;

        if (_pollConfirmLabel)
            _pollConfirmLabel.text = interactable ? "이걸로 방송 켤게" : "선택해줘";

        // 우측 패널도 동일 정책이면 같이 갱신
        if (_pollDetailPanel)
            _pollDetailPanel.SetConfirmInteractable(interactable);
    }

    // --------------------
    // Internals
    // --------------------

    private static void SetPageVisible(CanvasGroup cg, bool visible)
    {
        SetCanvasGroupVisible(cg, visible);
    }

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

        AppendMissing(ref missing, _tabSnsBtn, Refs.TabSns_Button);
        AppendMissing(ref missing, _tabPollBtn, Refs.TabPoll_Button);
        AppendMissing(ref missing, _tabDmBtn, Refs.TabDm_Button);

        AppendMissing(ref missing, _pageSnsCg, Refs.PageSns_Root);
        AppendMissing(ref missing, _pagePollCg, Refs.PagePoll_Root);
        AppendMissing(ref missing, _pageDmCg, Refs.PageDm_Root);

        if (_pollOptionBtns == null || _pollOptionBtns.Length < 3)
        {
            if (missing.Length > 0) missing += "\n";
            missing += "- Poll option buttons array not built";
        }
        else
        {
            AppendMissing(ref missing, _pollOptionBtns[0], Refs.PollOption0_Button);
            AppendMissing(ref missing, _pollOptionBtns[1], Refs.PollOption1_Button);
            AppendMissing(ref missing, _pollOptionBtns[2], Refs.PollOption2_Button);
        }

        AppendMissing(ref missing, _pollConfirmBtn, Refs.PollConfirm_Button);

        AppendMissing(ref missing, _raviBurst, Refs.Hero_Image);
        AppendMissing(ref missing, _contextTitle, Refs.ContextTitleText_Text);
        AppendMissing(ref missing, _contextBody, Refs.ContextBodyText_Text);

        // Optional widgets: 있으면 좋고 없어도 돌아가게 하고 싶으면 주석처리 가능
        // AppendMissing(ref missing, _pollDetailPanel, Refs.PollDetailPanel_Widget);
        // AppendMissing(ref missing, _dmThreadPanel, Refs.DmThreadPanel_Widget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[BroadcastHubUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}