// ChatEntryView.cs (Single-root version)
// - Left/Right 분리 제거
// - side에 따라 정렬(좌/우)만 바꿈
// - kind에 따라 배지/금액/이름 노출 제어
//
// 전제:
// - Content 쪽에 VerticalLayoutGroup이 있고
// - 각 엔트리(프리팹 루트)에 LayoutElement(선택)가 붙어 있으면 더 안정적
//
// 주의:
// - 정렬은 "EntryRoot의 앵커/피벗 + 내부 정렬 그룹"으로 처리.
// - 가장 안전한 방식은 EntryRoot 안에 AlignmentRoot를 두고, 그 RectTransform을 좌/우로 붙이는 것.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public enum ChatEntryKind : byte
{
    Chat = 0,
    Donation = 1,
    Idol = 2,
    System = 3,
}

public enum ChatEntrySide : byte
{
    Other = 0,
    My = 1,
}

[Serializable]
public struct ChatEntryData
{
    public ChatEntryKind kind;
    public ChatEntrySide side;

    public string name;
    public string body;

    public int donationAmount;
    public Sprite emoteSprite;
    public bool bigEmoteOnly;
}

/// <summary>
/// 채팅 엔트리 UI (풀링 전제) - 단일 루트.
/// - side에 따라 좌/우 정렬만 변경
/// - Donation/Idol/System 등 kind에 따른 노출 제어
/// - 큰 이모티콘(옵션) 지원
/// </summary>
public sealed class ChatEntryView : UIBase<ChatEntryView.Refs>
{
    #region Refs

    public enum Refs
    {
        // ---- Roots ----
        EntryRoot_Root,          // 프리팹 루트(또는 하위 루트) RectTransform
        AlignRoot_Root,          // 좌/우 정렬을 위해 움직일 컨테이너(권장)

        // ---- Visual ----
        BubbleBG_Image,

        NameText_Text,
        BodyText_Text,

        Badge_Image,             // Donation badge (optional)
        Amount_Text,             // Donation amount (optional)

        BigEmote_Image,          // Big emote (optional)
    }

    // ===== Cache (lowerCamelCase, no leading underscore, no _Image/_Text suffix) =====

    private RectTransform entryRoot;
    private RectTransform alignRoot;

    private Image bubbleBG;

    private TMP_Text nameText;
    private TMP_Text bodyText;

    private Image badge;
    private TMP_Text amount;

    private Image bigEmote;

    #endregion

    public ChatEntryData Entry { get; private set; }
    private bool valid;

    protected override void Initialize()
    {
        entryRoot = View.Rect(Refs.EntryRoot_Root);
        alignRoot = View.Rect(Refs.AlignRoot_Root);

        bubbleBG = View.Image(Refs.BubbleBG_Image);

        nameText = View.Text(Refs.NameText_Text);
        bodyText = View.Text(Refs.BodyText_Text);

        badge = View.Image(Refs.Badge_Image);
        amount = View.Text(Refs.Amount_Text);

        bigEmote = View.Image(Refs.BigEmote_Image);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        valid = ValidateRefs();
        if (!valid) return;
#else
        valid = true;
#endif

        SetDonationUIVisible(false);
        SetBigEmoteVisible(false);
    }

    public void Present(in ChatEntryData data)
    {
        if (!valid) return;

        Entry = data;

        // Idol/System은 기본적으로 Other(방송자/시스템) 취급
        ChatEntrySide side =
            (data.kind == ChatEntryKind.Idol || data.kind == ChatEntryKind.System)
                ? ChatEntrySide.Other
                : data.side;

        ApplySideAlignment(side);

        if (nameText) nameText.text = data.name ?? "";
        if (bodyText) bodyText.text = data.body ?? "";

        // Donation UI
        bool isDonation = data.kind == ChatEntryKind.Donation;
        SetDonationUIVisible(isDonation);
        if (isDonation && amount) amount.text = data.donationAmount.ToString();

        // Big emote
        if (data.emoteSprite && bigEmote)
        {
            bigEmote.sprite = data.emoteSprite;
            SetBigEmoteVisible(true);

            if (bodyText) bodyText.gameObject.SetActive(!data.bigEmoteOnly);
        }
        else
        {
            SetBigEmoteVisible(false);
            if (bodyText) bodyText.gameObject.SetActive(true);
        }

        // Idol/System은 이름 숨김
        if (data.kind == ChatEntryKind.Idol || data.kind == ChatEntryKind.System)
        {
            if (nameText) nameText.gameObject.SetActive(false);
        }
        else
        {
            if (nameText) nameText.gameObject.SetActive(true);
        }
    }

    private void ApplySideAlignment(ChatEntrySide side)
    {
        // 핵심:
        // - AlignRoot를 "좌측 정렬" 또는 "우측 정렬" 위치로 붙인다.
        // - 이 방식이 LayoutGroup과도 충돌이 적고, 프리팹 제어가 쉽다.

        if (!alignRoot) return;

        bool isMy = side == ChatEntrySide.My;

        // anchor/pivot을 좌/우로 스냅
        // (alignRoot가 entryRoot 안에서 stretch가 아닌 상태를 권장)
        // alignRoot.anchorMin = isMy ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        // alignRoot.anchorMax = isMy ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        // alignRoot.pivot     = isMy ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);

        // 위치는 0으로(붙이기)
        alignRoot.anchoredPosition = Vector2.zero;

        // (선택) 텍스트 정렬도 맞추고 싶으면 여기서:
        if (bodyText) bodyText.alignment = isMy ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        if (nameText) nameText.alignment = isMy ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
    }

    private void SetDonationUIVisible(bool visible)
    {
        if (badge) badge.gameObject.SetActive(visible);
        if (amount) amount.gameObject.SetActive(visible);
    }

    private void SetBigEmoteVisible(bool visible)
    {
        if (bigEmote) bigEmote.gameObject.SetActive(visible);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, entryRoot, Refs.EntryRoot_Root);
        AppendMissing(ref missing, alignRoot, Refs.AlignRoot_Root);

        AppendMissing(ref missing, nameText, Refs.NameText_Text);
        AppendMissing(ref missing, bodyText, Refs.BodyText_Text);

        // 아래는 옵션이면 체크에서 빼도 됨.
        // AppendMissing(ref missing, bubbleBG, Refs.BubbleBG_Image);
        // AppendMissing(ref missing, badge, Refs.Badge_Image);
        // AppendMissing(ref missing, amount, Refs.Amount_Text);
        // AppendMissing(ref missing, bigEmote, Refs.BigEmote_Image);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[ChatEntryView] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
