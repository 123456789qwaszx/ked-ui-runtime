// LiveUIRoot.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class LiveUIRoot : UIRoot<LiveUIRoot.Refs>
{
    public event Action OnExitRequested;
    public event Action OnDonateRequested;
    public event Action OnSendEmojiRequested;
    public event Action OnSendChatRequested;

    #region Refs

    public enum Refs
    {
        // ---- Phone (center) ----
        PhoneRoot_Root,

        ChatRailHost_Root,          // ChatRail 컴포넌트가 붙은 오브젝트
        ChatScrollRect_ScrollRect,  // ScrollRect가 붙은 오브젝트(Transform 이름)
        ChatContent_Root,           // Content Root (RectTransform)

        // ---- Minimal UI (top-left) ----
        TopLeft_Root,
        ExitButton_Button,
        DonateButton_Button,
        SendEmojiButton_Button,
        SendChatButton_Button,

        // ---- Optional: overlay dim ----
        OverlayDim_Image,
    }

    // ===== Cache =====
    private RectTransform phoneRoot;

    private ChatRail chatRail;
    
    private ScrollRect chatScroll;
    private RectTransform chatContent;

    private Button exitButton;
    private Button donateButton;
    private Button sendEmojiButton;
    private Button sendChatButton;

    private Image overlayDim;

    #endregion

    private bool valid;

    protected override void Initialize()
    {
        phoneRoot = View.Rect(Refs.PhoneRoot_Root);

        var chatRailHost = View.Rect(Refs.ChatRailHost_Root);
        chatRail = chatRailHost ? chatRailHost.GetComponent<ChatRail>() : null;

        var chatScrollRt = View.Rect(Refs.ChatScrollRect_ScrollRect);
        chatScroll = chatScrollRt ? chatScrollRt.GetComponent<ScrollRect>() : null;

        chatContent = View.Rect(Refs.ChatContent_Root);

        exitButton = View.Button(Refs.ExitButton_Button);
        donateButton = View.Button(Refs.DonateButton_Button);
        sendEmojiButton = View.Button(Refs.SendEmojiButton_Button);
        sendChatButton  = View.Button(Refs.SendChatButton_Button);

        overlayDim = View.Image(Refs.OverlayDim_Image);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        valid = ValidateRefs();
        if (!valid) return;
#else
        valid = true;
#endif

        SetOverlayDimVisible(false);

        BindEvent(exitButton, _ => OnExitRequested?.Invoke());
        BindEvent(donateButton, _ => OnDonateRequested?.Invoke());
        BindEvent(sendEmojiButton, _ => OnSendEmojiRequested?.Invoke());
        BindEvent(sendChatButton, _ => OnSendChatRequested?.Invoke());
    }

    public ChatRail GetChatRail() => chatRail;

    public void SetOverlayDimVisible(bool visible)
    {
        if (!valid) return;
        if (overlayDim) overlayDim.gameObject.SetActive(visible);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (!valid) return;
        if (exitButton) exitButton.interactable = interactable;
        if (donateButton) donateButton.interactable = interactable;
        if (sendEmojiButton) sendEmojiButton.interactable = interactable;
        if (sendChatButton) sendChatButton.interactable = interactable;
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, phoneRoot, Refs.PhoneRoot_Root);

        AppendMissing(ref missing, chatRail, Refs.ChatRailHost_Root);
        AppendMissing(ref missing, chatScroll, Refs.ChatScrollRect_ScrollRect);
        AppendMissing(ref missing, chatContent, Refs.ChatContent_Root);

        AppendMissing(ref missing, exitButton, Refs.ExitButton_Button);
        AppendMissing(ref missing, donateButton, Refs.DonateButton_Button);
        AppendMissing(ref missing, sendEmojiButton, Refs.SendEmojiButton_Button);
        AppendMissing(ref missing, sendChatButton, Refs.SendChatButton_Button);

        // OverlayDim은 옵션이면 체크에서 빼도 됨.
        // AppendMissing(ref missing, overlayDim, Refs.OverlayDim_Image);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[LiveUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
