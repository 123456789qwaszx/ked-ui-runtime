using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class PollDetailPanel : UIPanel<PollDetailPanel.Refs>
{
    public event Action OnConfirmRequested;

    public enum Refs
    {
        PollDetailTitle_Root,
        TitleText_Text,
        SubtitleText_Text,
        DescriptionText_Text,

        RewardHeader_Root,
        RewardHeader_Image,
        RewardHeader_Text,

        RewardList_Root,

        Reward0Root,
        Reward0Icon_Image,
        Reward0Amount_Text,

        Reward1Root,
        Reward1Icon_Image,
        Reward1Amount_Text,

        Reward2Root,
        Reward2Icon_Image,
        Reward2Amount_Text,

        Reward3Root,
        Reward3Icon_Image,
        Reward3Amount_Text,

        ConfirmButton_Root,
        ConfirmButton_Image,
        ConfirmButton_Text,
        ConfirmButton_Button
    }

    // Cache
    private bool _valid;

    private CanvasGroup _rootCg;

    // Title
    private RectTransform _titleRoot;
    private TMP_Text _title;
    private TMP_Text _subtitle;
    private TMP_Text _desc;

    // Reward header + list
    private RectTransform _rewardHeaderRoot;
    private Image _rewardHeaderImage;
    private TMP_Text _rewardHeaderText;

    private RectTransform _rewardListRoot;

    private RectTransform[] _rewardRoots;
    private Image[] _rewardIcons;
    private TMP_Text[] _rewardAmounts;

    // Confirm
    private RectTransform _confirmRoot;
    private Image _confirmImage;
    private TMP_Text _confirmText;
    private Button _confirmBtn;

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

        // Default
        SetConfirmInteractable(false);
        SetRewardCount(0);
    }

    private void CacheRefs()
    {
        // 루트 CanvasGroup: enum에 Root 키가 없으므로 컴포넌트에서 직접 확보
        _rootCg = GetComponent<CanvasGroup>();
        if (!_rootCg)
            _rootCg = gameObject.AddComponent<CanvasGroup>();

        // Title
        _titleRoot = View.Rect(Refs.PollDetailTitle_Root);
        _title = View.Text(Refs.TitleText_Text);
        _subtitle = View.Text(Refs.SubtitleText_Text);
        _desc = View.Text(Refs.DescriptionText_Text);

        // Reward header
        _rewardHeaderRoot = View.Rect(Refs.RewardHeader_Root);
        _rewardHeaderImage = View.Image(Refs.RewardHeader_Image);
        _rewardHeaderText = View.Text(Refs.RewardHeader_Text);

        // Reward list
        _rewardListRoot = View.Rect(Refs.RewardList_Root);

        _rewardRoots = new[]
        {
            View.Rect(Refs.Reward0Root),
            View.Rect(Refs.Reward1Root),
            View.Rect(Refs.Reward2Root),
            View.Rect(Refs.Reward3Root),
        };

        _rewardIcons = new[]
        {
            View.Image(Refs.Reward0Icon_Image),
            View.Image(Refs.Reward1Icon_Image),
            View.Image(Refs.Reward2Icon_Image),
            View.Image(Refs.Reward3Icon_Image),
        };

        _rewardAmounts = new[]
        {
            View.Text(Refs.Reward0Amount_Text),
            View.Text(Refs.Reward1Amount_Text),
            View.Text(Refs.Reward2Amount_Text),
            View.Text(Refs.Reward3Amount_Text),
        };

        // Confirm
        _confirmRoot = View.Rect(Refs.ConfirmButton_Root);
        _confirmImage = View.Image(Refs.ConfirmButton_Image);
        _confirmText = View.Text(Refs.ConfirmButton_Text);
        _confirmBtn = View.Button(Refs.ConfirmButton_Button);
    }

    private void BindHandlers()
    {
        BindEvent(_confirmBtn, _ => OnConfirmRequested?.Invoke());
    }

    // --------------------
    // Public API
    // --------------------

    public void Show(bool visible)
    {
        if (!_valid) return;

        if (visible) UIManager.Instance.PushPanelPatched<PollDetailPanel>();
        else
        {
            UIManager.Instance.PopPanel();
        }
        //SetCanvasGroupVisible(_rootCg, visible);
    }

    public void SetTexts(string title, string subtitle, string desc)
    {
        if (!_valid) return;
        if (_title) _title.text = title ?? "";
        if (_subtitle) _subtitle.text = subtitle ?? "";
        if (_desc) _desc.text = desc ?? "";
    }

    public void SetRewardHeader(string headerText)
    {
        if (!_valid) return;
        if (_rewardHeaderText) _rewardHeaderText.text = headerText ?? "";
    }

    public void SetRewardCount(int count)
    {
        if (!_valid) return;

        count = Mathf.Clamp(count, 0, _rewardRoots.Length);

        if (_rewardListRoot)
            _rewardListRoot.gameObject.SetActive(count > 0);

        for (int i = 0; i < _rewardRoots.Length; i++)
        {
            if (_rewardRoots[i])
                _rewardRoots[i].gameObject.SetActive(i < count);
        }
    }

    public void SetReward(int index, Sprite icon, string amountText)
    {
        if (!_valid) return;
        if ((uint)index >= (uint)_rewardRoots.Length) return;

        if (_rewardIcons[index]) _rewardIcons[index].sprite = icon;
        if (_rewardAmounts[index]) _rewardAmounts[index].text = amountText ?? "";
    }

    public void SetConfirmInteractable(bool interactable, string buttonText = null)
    {
        if (!_valid) return;

        if (_confirmBtn) _confirmBtn.interactable = interactable;

        if (_confirmText)
        {
            if (!string.IsNullOrEmpty(buttonText)) _confirmText.text = buttonText;
            else _confirmText.text = interactable ? "이걸로 방송 켤게" : "선택해줘";
        }
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

        // Root cg is always ensured (auto-added), so no strict check here.

        AppendMissing(ref missing, _titleRoot, Refs.PollDetailTitle_Root);
        AppendMissing(ref missing, _title, Refs.TitleText_Text);
        AppendMissing(ref missing, _desc, Refs.DescriptionText_Text);

        AppendMissing(ref missing, _rewardListRoot, Refs.RewardList_Root);

        // Confirm is essential
        AppendMissing(ref missing, _confirmBtn, Refs.ConfirmButton_Button);
        AppendMissing(ref missing, _confirmText, Refs.ConfirmButton_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[PollDetailPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}