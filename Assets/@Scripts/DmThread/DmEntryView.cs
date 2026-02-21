using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class DmEntryView : UIBase<DmEntryView.Refs>
{
    public enum Refs
    {
        Root_Root,

        Bubble_Root,         // optional (system이면 없을 수도)
        NameText_Text,       // optional
        BodyText_Text,       // required
        TimeText_Text,       // optional
    }

    private bool _valid;

    private RectTransform _root;
    private RectTransform _bubble;
    private TMP_Text _name;
    private TMP_Text _body;
    private TMP_Text _time;

    private int _typingToken;
    private Coroutine _typingCo;

    protected override void Initialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
        StopTypingInternal();
    }

    private void CacheRefs()
    {
        _root = View.Rect(Refs.Root_Root);
        _bubble = View.Rect(Refs.Bubble_Root);

        _name = View.Text(Refs.NameText_Text);
        _body = View.Text(Refs.BodyText_Text);
        _time = View.Text(Refs.TimeText_Text);
    }

    public void SetActive(bool active)
    {
        if (_root) _root.gameObject.SetActive(active);
        else gameObject.SetActive(active);
    }

    public void SetModel(in DmEntryModel model, string timeText = null)
    {
        if (!_valid) return;

        StopTypingInternal();

        if (_name)
        {
            bool hasName = !string.IsNullOrEmpty(model.Name);
            _name.gameObject.SetActive(hasName);
            _name.text = hasName ? model.Name : "";
        }

        if (_time)
        {
            bool hasTime = !string.IsNullOrEmpty(timeText);
            _time.gameObject.SetActive(hasTime);
            _time.text = hasTime ? timeText : "";
        }

        if (_body)
        {
            _body.text = model.Text ?? "";
            _body.maxVisibleCharacters = int.MaxValue;
        }
    }

    /// <summary>
    /// typingSeconds > 0이면 타이핑 연출. 완료 시 onDone 호출.
    /// </summary>
    public void PlayTyping(float typingSeconds, Action onDone)
    {
        if (!_valid)
        {
            onDone?.Invoke();
            return;
        }

        StopTypingInternal();

        if (!_body || typingSeconds <= 0f)
        {
            if (_body) _body.maxVisibleCharacters = int.MaxValue;
            onDone?.Invoke();
            return;
        }

        _typingToken++;
        int token = _typingToken;

        _typingCo = StartCoroutine(CoType(token, typingSeconds, onDone));
    }

    public void ForceCompleteTyping()
    {
        if (!_valid) return;
        if (_body) _body.maxVisibleCharacters = int.MaxValue;
        StopTypingInternal();
    }

    private IEnumerator CoType(int token, float seconds, Action onDone)
    {
        // TMP가 텍스트 정보 업데이트를 위해 한 프레임 필요할 수 있음
        yield return null;

        if (!_body)
        {
            onDone?.Invoke();
            yield break;
        }

        _body.ForceMeshUpdate();

        int total = _body.textInfo.characterCount;
        if (total <= 0)
        {
            _body.maxVisibleCharacters = int.MaxValue;
            onDone?.Invoke();
            yield break;
        }

        _body.maxVisibleCharacters = 0;

        float t = 0f;
        while (t < seconds)
        {
            if (token != _typingToken) yield break;

            t += Time.unscaledDeltaTime;
            float p = seconds > 0f ? Mathf.Clamp01(t / seconds) : 1f;

            int visible = Mathf.Clamp(Mathf.RoundToInt(total * p), 0, total);
            _body.maxVisibleCharacters = visible;

            yield return null;
        }

        _body.maxVisibleCharacters = int.MaxValue;

        if (token == _typingToken)
            onDone?.Invoke();
    }

    private void StopTypingInternal()
    {
        _typingToken++;
        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }
    }

    private bool ValidateRefs()
    {
        string missing = "";

        // Root는 optional로 봐도 되지만, 있으면 좋음
        // AppendMissing(ref missing, _root, Refs.Root_Root);

        AppendMissing(ref missing, _body, Refs.BodyText_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DmEntryView] Missing refs:\n{missing}", this);
            return false;
        }
        return true;
    }
}