using TMPro;
using UnityEngine;

public sealed class DmEntryView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform root;     // optional
    [SerializeField] private TMP_Text nameText;      // optional
    [SerializeField] private TMP_Text bodyText;      // required
    [SerializeField] private TMP_Text timeText;      // optional

    public DmEntryKind Kind { get; set; }
}