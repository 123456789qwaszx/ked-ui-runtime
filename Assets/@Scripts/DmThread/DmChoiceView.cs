using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DmChoiceView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text promptText;      // optional
    [SerializeField] private Button[] optionButtons;   // required (2~5)
    [SerializeField] private TMP_Text[] optionTexts;   // optional (same size as buttons)

    private Action<int> _onPick;

    public void Present(in DmChoice choice, Action<int> onPick)
    {
        _onPick = onPick;

        promptText.text = choice.prompt;

        DmChoiceOption[] options = choice.options;

        int optionCount = Mathf.Min(options.Length, optionButtons.Length);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = i < optionCount;
            Button btn = optionButtons[i];

            btn.gameObject.SetActive(active);
            btn.onClick.RemoveAllListeners();

            if (!active)
                continue;

            int captured = i;
            btn.onClick.AddListener(() => _onPick?.Invoke(captured));
            
            optionTexts[i].text = options[i].text ?? "";
        }
    }
}