using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatEntryView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image emoteImage;

    //[Header("Optional")]
    //[SerializeField] private GameObject amountRoot;

    public void Bind(in ChatRenderModel m)
    {
        if (nameText) nameText.text = m.nameText ?? string.Empty;
        if (bodyText) bodyText.text = m.bodyText ?? string.Empty;

        //if (amountRoot)
        //    amountRoot.SetActive(m.amount > 0);

        if (amountText)
            amountText.text = m.amount > 0 ? $"₩{m.amount:N0}" : string.Empty;

        if (emoteImage)
        {
            emoteImage.gameObject.SetActive(m.emoteSprite != null);
            emoteImage.sprite = m.emoteSprite;
        }
    }
}