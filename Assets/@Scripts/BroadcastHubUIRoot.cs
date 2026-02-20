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

        // --------------------
        // Left: Phone shell
        // --------------------
        HubPhone_Root,
        HubPhoneBG_Root,
        HubPhoneBG_Image,

        HubPhoneTop_Root,
        PhoneTopTime_Text,
        PhoneTopNotch_Image,
        PhoneTopBattery_Image,

        // --------------------
        // Left: Pages (only one visible at a time)
        // --------------------
        HubPhonePages_Root,

        PhonePageSns_Root,
        PageSnsScrollRect_ScrollRect,
        PageSnsContent_Root,

        PhonePagePoll_Root,
        PollHeaderText_Text,
        PollOptionBar_Root,

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

        PhonePageDm_Root,
        DmScrollRect_ScrollRect,
        DmContent_Root,

        // Bottom tab bar
        PhoneTabBar_Root,
        TabBarBg_Image,

        TabBarSns_Root,
        TabBarSnsIcon_Image,
        TabBarSnsLabel_Text,
        TabBarSns_Button,

        TabBarPoll_Root,
        TabBarPollIcon_Image,
        TabBarPollLabel_Text,
        TabBarPoll_Button,

        TabBarDm_Root,
        TabBarDmIcon_Image,
        TabBarDmLabel_Text,
        TabBarDm_Button,

        // --------------------
        // Right: Inspector (Ravi info panel)
        // --------------------
        InspectorRoot_Root,
        HeroRoot_Root,
        Hero_Image,                 // always on
        HeroStatusText_Text,        // e.g., "DAY 7 | OFF AIR"
        HeroMoodText_Text,          // optional small line

        // 우측 상세 패널(별도 Panel)
        PollDetailPanel_Widget,
        DmThreadPanel_Widget,
    }
    #endregion

    // Tab enum (internal convenience)
    public enum TabKind : byte
    {
        Sns = 0,
        Poll = 1,
        Dm = 2,
    }

    // ===== Cache =====
    private bool _valid;

    // Top
    private Button _backButton;

    // Tabs
    private Button _tabSnsBtn;
    private Button _tabPollBtn;
    private Button _tabDmBtn;

    // Pages (CanvasGroup)
    private CanvasGroup _pageSnsCg;
    private CanvasGroup _pagePollCg;
    private CanvasGroup _pageDmCg;

    // Poll options
    private Button[] _pollOptionBtns;
    private TMP_Text[] _pollOptionLabels;

    // Inspector header
    private Image _heroImage;
    private TMP_Text _heroStatusText;
    private TMP_Text _heroMoodText;

    // Inspector widgets
    private PollDetailPanel _pollDetailPanel;
    private DmThreadPanel _dmThreadPanel;

    // State
    private int _selectedPollIndex = -1;

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
        ShowTab(TabKind.Sns);
        SetPollSelected(-1);

        // Right panels default
        if (_pollDetailPanel) _pollDetailPanel.Show(false);
        if (_dmThreadPanel) _dmThreadPanel.Show(false);
    }

    private void CacheRefs()
    {
        // Top
        _backButton = View.Button(Refs.BackButton_Button);

        // Tabs
        _tabSnsBtn = View.Button(Refs.TabBarSns_Button);
        _tabPollBtn = View.Button(Refs.TabBarPoll_Button);
        _tabDmBtn = View.Button(Refs.TabBarDm_Button);

        // Pages (CanvasGroup for smooth switching)
        _pageSnsCg = View.CanvasGroup(Refs.PhonePageSns_Root);
        _pagePollCg = View.CanvasGroup(Refs.PhonePagePoll_Root);
        _pageDmCg = View.CanvasGroup(Refs.PhonePageDm_Root);

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

        // Inspector header
        _heroImage = View.Image(Refs.Hero_Image);
        _heroStatusText = View.Text(Refs.HeroStatusText_Text);
        _heroMoodText = View.Text(Refs.HeroMoodText_Text);

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

        // Poll confirm is owned by right panel (B policy)
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

        SetCanvasGroupVisible(_pageSnsCg, tab == TabKind.Sns);
        SetCanvasGroupVisible(_pagePollCg, tab == TabKind.Poll);
        SetCanvasGroupVisible(_pageDmCg, tab == TabKind.Dm);

        // 우측 상세 패널 기본 정책 (Presenter가 원하면 오버라이드)
        if (_pollDetailPanel) _pollDetailPanel.Show(tab == TabKind.Poll);
        if (_dmThreadPanel) _dmThreadPanel.Show(tab == TabKind.Dm);
    }

    public void SetHeroSprite(Sprite sprite)
    {
        if (!_valid) return;
        if (_heroImage) _heroImage.sprite = sprite;
    }

    public void SetHeroStatus(string statusLine)
    {
        if (!_valid) return;
        if (_heroStatusText) _heroStatusText.text = statusLine ?? "";
    }

    public void SetHeroMood(string moodLine)
    {
        if (!_valid) return;
        if (_heroMoodText) _heroMoodText.text = moodLine ?? "";
    }

    /// <summary>
    /// Provide poll option labels. Null/empty entries will hide/disable that slot.
    /// </summary>
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
        // 우측 패널 confirm도 잠금
        if (_pollDetailPanel) _pollDetailPanel.SetConfirmInteractable(false);
    }

    public void SetPollSelected(int index)
    {
        if (!_valid) return;

        _selectedPollIndex = index;

        // 라벨 하이라이트(간단 버전)
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

        // 우측 패널 confirm 정책 동기화
        if (_pollDetailPanel)
            _pollDetailPanel.SetConfirmInteractable(_selectedPollIndex >= 0);
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

        // Top (optional이지만, Back은 보통 필수)
        AppendMissing(ref missing, _backButton, Refs.BackButton_Button);

        // Tabs required
        AppendMissing(ref missing, _tabSnsBtn, Refs.TabBarSns_Button);
        AppendMissing(ref missing, _tabPollBtn, Refs.TabBarPoll_Button);
        AppendMissing(ref missing, _tabDmBtn, Refs.TabBarDm_Button);

        // Pages required
        AppendMissing(ref missing, _pageSnsCg, Refs.PhonePageSns_Root);
        AppendMissing(ref missing, _pagePollCg, Refs.PhonePagePoll_Root);
        AppendMissing(ref missing, _pageDmCg, Refs.PhonePageDm_Root);

        // Poll: at least 3 options required
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

        // Inspector header core recommended
        AppendMissing(ref missing, _heroImage, Refs.Hero_Image);
        AppendMissing(ref missing, _heroStatusText, Refs.HeroStatusText_Text);
        // Mood는 optional이면 주석 가능
        // AppendMissing(ref missing, _heroMoodText, Refs.HeroMoodText_Text);

        // Widgets are optional: 없어도 허브는 돌아가게 하고 싶으면 체크 제외
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