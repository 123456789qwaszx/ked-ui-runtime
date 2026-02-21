using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct SnsPostModel
{
    public readonly Sprite Icon;      // [1]
    public readonly string HeadText;  // [2] (이름/자기소개/주소 한 줄)
    public readonly string SnsText;   // [3]
    public readonly Sprite Media;     // [4] optional

    public SnsPostModel(Sprite icon, string headText, string snsText, Sprite media = null)
    {
        Icon = icon;
        HeadText = headText;
        SnsText = snsText;
        Media = media;
    }
}

public sealed class SnsPostLineView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;     // [1] required
    [SerializeField] private TMP_Text headText;   // [2] required
    [SerializeField] private TMP_Text snsText;    // [3] required
    [SerializeField] private Image snsImage;      // [4] optional (Sprite가 없으면 숨김)

    public void Present(in SnsPostModel model)
    {
        // [1] Icon
        if (iconImage)
        {
            iconImage.sprite = model.Icon;
            iconImage.enabled = model.Icon != null;
        }

        // [2] Head
        if (headText)
            headText.text = model.HeadText ?? "";

        // [3] Body
        if (snsText)
            snsText.text = model.SnsText ?? "";

        // [4] Media (optional)
        if (snsImage)
        {
            bool hasMedia = model.Media != null;
            snsImage.gameObject.SetActive(hasMedia);
            if (hasMedia)
                snsImage.sprite = model.Media;
        }
    }
}