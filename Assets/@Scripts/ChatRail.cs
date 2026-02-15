// ChatRail.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ChatRail - 풀링 기반 스크롤 리스트.
/// - 30줄/10초에서도 GC/깨짐 없이 동작 목표
/// - PushChat / PushDonation / PushIdol 로 한 곳으로 통합
/// </summary>
public sealed class ChatRail : MonoBehaviour
{
    [Header("Bind (or assign via LiveUIRoot)")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Pooling")]
    [SerializeField] private ChatEntryView entryPrefab;
    [SerializeField] private int warmupCount = 8;
    [SerializeField] private int maxEntries = 10;

    private readonly Stack<ChatEntryView> pool = new();
    private readonly Queue<ChatEntryView> active = new();
    private bool initialized;

    public void Bind(ScrollRect sr, RectTransform ct)
    {
        scrollRect = sr;
        content = ct;
        TryInitialize();
    }

    private void Awake() => TryInitialize();

    private void TryInitialize()
    {
        if (initialized) return;
        if (!scrollRect || !content || !entryPrefab) return;

        Warmup(warmupCount);
        initialized = true;
    }

    private void Warmup(int count)
    {
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            var v = CreateNew();
            Release(v);
        }
    }

    public void Clear()
    {
        if (!initialized) return;

        while (active.Count > 0)
            Release(active.Dequeue());

        if (scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    public void Push(in ChatEntryData data, bool autoScrollToBottom = true)
    {
        if (!initialized) return;

        var v = Acquire();
        v.Present(data);

        active.Enqueue(v);

        while (active.Count > maxEntries)
            Release(active.Dequeue());

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    public void PushChat(string name, string body, bool isMy)
    {
        var d = new ChatEntryData
        {
            kind = ChatEntryKind.Chat,
            side = isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            name = name,
            body = body,
        };
        Push(d, autoScrollToBottom: true);
    }

    public void PushDonation(string name, int amount, string bodyOrEmpty, bool isMy)
    {
        var d = new ChatEntryData
        {
            kind = ChatEntryKind.Donation,
            side = isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            name = name,
            body = bodyOrEmpty,
            donationAmount = amount,
        };
        Push(d, autoScrollToBottom: true);
    }

    public void PushIdol(string body)
    {
        var d = new ChatEntryData
        {
            kind = ChatEntryKind.Idol,
            side = ChatEntrySide.Other,
            name = "",
            body = body,
        };
        Push(d, autoScrollToBottom: true);
    }

    private ChatEntryView Acquire()
    {
        ChatEntryView v = pool.Count > 0 ? pool.Pop() : CreateNew();

        v.gameObject.SetActive(true);
        v.transform.SetParent(content, worldPositionStays: false);
        v.transform.SetAsLastSibling();
        return v;
    }

    private void Release(ChatEntryView v)
    {
        if (!v) return;

        v.gameObject.SetActive(false);
        v.transform.SetParent(transform, worldPositionStays: false);
        pool.Push(v);
    }

    private ChatEntryView CreateNew()
    {
        var v = Instantiate(entryPrefab, transform);
        v.gameObject.SetActive(false);
        return v;
    }

    private void ScrollToBottom()
    {
        if (!scrollRect) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
