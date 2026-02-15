using UnityEngine;

public static class ChatEntryFactory
{
    public static ChatEntryData Create(ChatSpecSO spec)
    {
        if (!spec)
        {
            Debug.LogError("[ChatEntryFactory] Spec is null.");
            return default;
        }

        var data = new ChatEntryData
        {
            type = spec.entryType,
            crowdKind = spec.crowdKind,

            side = spec.isMy ? ChatEntrySide.My : ChatEntrySide.Other,
            chatName = spec.chatName,
            chatBody = spec.body,

            donationAmount = spec.donationAmount,
        };

        return data;
    }

}
