// ChatRail.cs (Updated)
// View Layer: ChatEntryData를 받아서 UI에 표시만 담당
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatRail : MonoBehaviour
{
    [Header("Bind (or assign via LiveUIRoot)")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Pooling")]
    [SerializeField] private ChatEntryView entryPrefab;
    [SerializeField] private int maxEntries = 10;

    private readonly Stack<ChatEntryView> pool = new();
    private readonly Queue<ChatEntryView> activeChat = new();

    /// <summary>
    /// ChatEntryData를 받아서 UI에 표시
    /// </summary>
    public void Push(in ChatEntryData data, bool autoScrollToBottom = true)
    {
        ChatEntryView view = Acquire();
        view.Present(data);

        activeChat.Enqueue(view);

        while (activeChat.Count > maxEntries)
            Release(activeChat.Dequeue());

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    /// <summary>
    /// 여러 개 한꺼번에 Push
    /// </summary>
    public void PushMultiple(ChatEntryData[] dataArray, bool autoScrollToBottom = true)
    {
        if (dataArray == null || dataArray.Length == 0)
            return;

        foreach (var data in dataArray)
        {
            Push(data, autoScrollToBottom: false);
        }

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    private ChatEntryView Acquire()
    {
        ChatEntryView chat = pool.Count > 0 ?
            pool.Pop() 
            : CreateNew();

        chat.gameObject.SetActive(true);
        chat.transform.SetParent(content, worldPositionStays: false);
        chat.transform.SetAsLastSibling();
        return chat;
    }

    private ChatEntryView CreateNew()
    {
        ChatEntryView chat = Instantiate(entryPrefab, transform);
        chat.gameObject.SetActive(false);
        return chat;
    }

    private void Release(ChatEntryView chatView)
    {
        if (!chatView)
            return;

        chatView.gameObject.SetActive(false);
        chatView.transform.SetParent(transform, worldPositionStays: false);
        pool.Push(chatView);
    }

    public void Clear()
    {
        while (activeChat.Count > 0)
            Release(activeChat.Dequeue());

        if (scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void ScrollToBottom()
    {
        if (!scrollRect) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    // ========== Legacy Helpers (하위 호환) ==========
    // 기존 코드와의 호환성 유지용
    // 나중에 제거 권장

    public void PushChat(string name, string body, bool isMy)
    {
        var data = new ChatEntryData
        {
            type = ChatEntryType.Chat,
            side = isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            chatName = name,
            chatBody = body,
        };
        Push(data);
    }

    public void PushDonation(string name, int amount, string bodyOrEmpty, bool isMy)
    {
        var data = new ChatEntryData
        {
            type = ChatEntryType.Donation,
            side = isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            chatName = name,
            chatBody = bodyOrEmpty,
            donationAmount = amount,
        };
        Push(data);
    }

    public void PushIdol(string body)
    {
        var data = new ChatEntryData
        {
            type = ChatEntryType.Idol,
            side = ChatEntrySide.Other,
            chatName = "",
            chatBody = body,
        };
        Push(data);
    }
}