using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatContentDB", menuName = "Live/Chat Content DB", order = 210)]
public sealed class ChatContentDBSO : ScriptableObject
{
    [Header("Names")]
    public string[] viewerNames;

    [Header("Emotes")]
    public Sprite[] emotes;

    [Header("Texts")]
    public TextBucket[] textBuckets;

    [Header("Donation Amounts (weighted)")]
    public DonationAmount[] donationAmounts;

    [Serializable]
    public struct TextBucket
    {
        public ChatEventKind kind;
        public CrowdFlavor flavor; // Crowd일 때만 의미, 아니면 None 사용
        public string[] texts;
    }

    [Serializable]
    public struct DonationAmount
    {
        public int amount;
        [Range(0f, 10f)] public float weight;
    }

    public bool TryGetTexts(ChatEventKind kind, CrowdFlavor flavor, out string[] texts)
    {
        if (textBuckets != null)
        {
            for (int i = 0; i < textBuckets.Length; i++)
            {
                var b = textBuckets[i];
                if (b.kind == kind && b.flavor == flavor && b.texts != null && b.texts.Length > 0)
                {
                    texts = b.texts;
                    return true;
                }
            }

            // fallback: flavor 무시하고 None으로 찾기
            for (int i = 0; i < textBuckets.Length; i++)
            {
                var b = textBuckets[i];
                if (b.kind == kind && b.flavor == CrowdFlavor.None && b.texts != null && b.texts.Length > 0)
                {
                    texts = b.texts;
                    return true;
                }
            }
        }

        texts = null;
        return false;
    }
}