using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class PollDetailPanel : UIBase<PollDetailPanel.Refs>, IUIPanel
{
    public event Action OnConfirmRequested;

    public enum Refs
    {
        Root_Root,
        Header_Root,

        TitleText_Text,
        SubtitleText_Text,
        DescText_Text,

        TagRow_Root,
        Tag0_Text,
        Tag1_Text,
        Tag2_Text,

        RewardHeader_Root,
        RewardHeaderText_Text,
        RewardList_Root,

        Reward0_Root,
        Reward0_Icon_Image,
        Reward0_AmountText_Text,

        Reward1_Root,
        Reward1_Icon_Image,
        Reward1_AmountText_Text,

        Reward2_Root,
        Reward2_Icon_Image,
        Reward2_AmountText_Text,

        Requirement_Root,
        RequirementText_Text,

        Confirm_Button,
        ConfirmText_Text,
    }

    // Cache
    private bool _valid;

    private CanvasGroup _rootCg;

    private TMP_Text _title;
    private TMP_Text _subtitle;
    private TMP_Text _desc;

    private RectTransform _tagRow;
    private TMP_Text[] _tags;

    private TMP_Text _rewardHeader;
    private RectTransform _rewardList;

    private RectTransform[] _rewardRoots;
    private Image[] _rewardIcons;
    private TMP_Text[] _rewardAmounts;

    private RectTransform _reqRoot;
    private TMP_Text _reqText;

    private Button _confirmBtn;
    private TMP_Text _confirmText;

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

        // Default closed (UIBase PreInitialize에서 이미 CloseByCanvasGroup 처리됨)
        SetConfirmInteractable(false);
        SetRequirementVisible(false);
        SetRewardCount(0);
        SetTags(null);
    }

    private void CacheRefs()
    {
        _rootCg = View.CanvasGroup(Refs.Root_Root);

        _title = View.Text(Refs.TitleText_Text);
        _subtitle = View.Text(Refs.SubtitleText_Text);
        _desc = View.Text(Refs.DescText_Text);

        _tagRow = View.Rect(Refs.TagRow_Root);
        _tags = new[]
        {
            View.Text(Refs.Tag0_Text),
            View.Text(Refs.Tag1_Text),
            View.Text(Refs.Tag2_Text),
        };

        _rewardHeader = View.Text(Refs.RewardHeaderText_Text);
        _rewardList = View.Rect(Refs.RewardList_Root);

        _rewardRoots = new[]
        {
            View.Rect(Refs.Reward0_Root),
            View.Rect(Refs.Reward1_Root),
            View.Rect(Refs.Reward2_Root),
        };

        _rewardIcons = new[]
        {
            View.Image(Refs.Reward0_Icon_Image),
            View.Image(Refs.Reward1_Icon_Image),
            View.Image(Refs.Reward2_Icon_Image),
        };

        _rewardAmounts = new[]
        {
            View.Text(Refs.Reward0_AmountText_Text),
            View.Text(Refs.Reward1_AmountText_Text),
            View.Text(Refs.Reward2_AmountText_Text),
        };

        _reqRoot = View.Rect(Refs.Requirement_Root);
        _reqText = View.Text(Refs.RequirementText_Text);

        _confirmBtn = View.Button(Refs.Confirm_Button);
        _confirmText = View.Text(Refs.ConfirmText_Text);
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
        SetCanvasGroupVisible(_rootCg, visible);
    }

    public void SetTexts(string title, string subtitle, string desc)
    {
        if (!_valid) return;
        if (_title) _title.text = title ?? "";
        if (_subtitle) _subtitle.text = subtitle ?? "";
        if (_desc) _desc.text = desc ?? "";
    }

    public void SetTags(string[] tags)
    {
        if (!_valid) return;

        bool any = tags != null && tags.Length > 0;
        if (_tagRow) _tagRow.gameObject.SetActive(any);

        for (int i = 0; i < _tags.Length; i++)
        {
            if (!_tags[i]) continue;
            string t = (tags != null && i < tags.Length) ? tags[i] : null;
            _tags[i].gameObject.SetActive(!string.IsNullOrEmpty(t));
            _tags[i].text = t ?? "";
        }
    }

    public void SetRewardCount(int count)
    {
        if (!_valid) return;

        count = Mathf.Clamp(count, 0, _rewardRoots.Length);

        if (_rewardList) _rewardList.gameObject.SetActive(count > 0);

        for (int i = 0; i < _rewardRoots.Length; i++)
        {
            if (_rewardRoots[i]) _rewardRoots[i].gameObject.SetActive(i < count);
        }
    }

    public void SetReward(int index, Sprite icon, string amountText)
    {
        if (!_valid) return;
        if ((uint)index >= (uint)_rewardRoots.Length) return;

        if (_rewardIcons[index]) _rewardIcons[index].sprite = icon;
        if (_rewardAmounts[index]) _rewardAmounts[index].text = amountText ?? "";
    }

    public void SetRequirementVisible(bool visible, string text = null)
    {
        if (!_valid) return;
        if (_reqRoot) _reqRoot.gameObject.SetActive(visible);
        if (visible && _reqText) _reqText.text = text ?? "";
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

        AppendMissing(ref missing, _rootCg, Refs.Root_Root);

        AppendMissing(ref missing, _title, Refs.TitleText_Text);
        AppendMissing(ref missing, _desc, Refs.DescText_Text);

        AppendMissing(ref missing, _confirmBtn, Refs.Confirm_Button);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[PollDetailPanel] Missing refs:\n{missing}", this);
            return false;
        }
        return true;
    }
}