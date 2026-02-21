using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class DmChoiceBar : UIBase<DmChoiceBar.Refs>, IUIPanel
{
    public event Action<int> OnOptionSelected;

    public enum Refs
    {
        Root_Root,

        PromptText_Text,          // optional

        Option0_Root,
        Option0_LabelText,
        Option0_Button,

        Option1_Root,
        Option1_LabelText,
        Option1_Button,

        Option2_Root,
        Option2_LabelText,
        Option2_Button,

        Option3_Root,
        Option3_LabelText,
        Option3_Button,

        Option4_Root,
        Option4_LabelText,
        Option4_Button,
    }

    private bool _valid;

    private CanvasGroup _rootCg;

    private TMP_Text _prompt;

    private RectTransform[] _optRoots;
    private TMP_Text[] _optLabels;
    private Button[] _optBtns;

    protected override void Initialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
        BindHandlers();

        Show(false);
        SetPrompt(null);
        SetOptions(null);
    }

    private void CacheRefs()
    {
        _rootCg = View.CanvasGroup(Refs.Root_Root);

        _prompt = View.Text(Refs.PromptText_Text);

        _optRoots = new[]
        {
            View.Rect(Refs.Option0_Root),
            View.Rect(Refs.Option1_Root),
            View.Rect(Refs.Option2_Root),
            View.Rect(Refs.Option3_Root),
            View.Rect(Refs.Option4_Root),
        };

        _optLabels = new[]
        {
            View.Text(Refs.Option0_LabelText),
            View.Text(Refs.Option1_LabelText),
            View.Text(Refs.Option2_LabelText),
            View.Text(Refs.Option3_LabelText),
            View.Text(Refs.Option4_LabelText),
        };

        _optBtns = new[]
        {
            View.Button(Refs.Option0_Button),
            View.Button(Refs.Option1_Button),
            View.Button(Refs.Option2_Button),
            View.Button(Refs.Option3_Button),
            View.Button(Refs.Option4_Button),
        };
    }

    private void BindHandlers()
    {
        for (int i = 0; i < _optBtns.Length; i++)
        {
            int idx = i;
            var btn = _optBtns[i];
            if (!btn) continue;

            BindEvent(btn, _ =>
            {
                if (!btn.interactable) return;
                OnOptionSelected?.Invoke(idx);
            });
        }
    }

    public void Show(bool visible)
    {
        if (!_valid) return;
        SetCanvasGroupVisible(_rootCg, visible);
    }

    public void SetPrompt(string prompt)
    {
        if (!_valid) return;
        if (_prompt)
        {
            bool has = !string.IsNullOrEmpty(prompt);
            _prompt.gameObject.SetActive(has);
            _prompt.text = has ? prompt : "";
        }
    }

    public void SetOptions(string[] options)
    {
        if (!_valid) return;

        int count = options != null ? options.Length : 0;
        for (int i = 0; i < _optBtns.Length; i++)
        {
            bool has = i < count && !string.IsNullOrEmpty(options[i]);

            if (_optRoots[i]) _optRoots[i].gameObject.SetActive(has);
            if (_optBtns[i]) _optBtns[i].interactable = has;

            if (_optLabels[i])
                _optLabels[i].text = has ? options[i] : "";
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (!_valid) return;

        for (int i = 0; i < _optBtns.Length; i++)
        {
            if (_optBtns[i] && _optBtns[i].gameObject.activeSelf)
                _optBtns[i].interactable = interactable;
        }
    }

    private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _rootCg, Refs.Root_Root);

        // 최소 3개는 권장
        if (_optBtns == null || _optBtns.Length < 3)
        {
            if (missing.Length > 0) missing += "\n";
            missing += "- option buttons array not built";
        }
        else
        {
            AppendMissing(ref missing, _optBtns[0], Refs.Option0_Button);
            AppendMissing(ref missing, _optBtns[1], Refs.Option1_Button);
            AppendMissing(ref missing, _optBtns[2], Refs.Option2_Button);
        }

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DmChoiceBar] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}