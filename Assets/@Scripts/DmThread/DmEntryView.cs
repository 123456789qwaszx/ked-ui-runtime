using TMPro;
using UnityEngine;

public sealed class DmEntryView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;

    public DmEntryKind Kind { get; set; }
    
    public void Present(DmEntryModel model)
    {
        Kind = model.Kind;

        if (nameText) nameText.text = model.Name ?? "";
        if (bodyText) bodyText.text = model.Text ?? "";
    }
}