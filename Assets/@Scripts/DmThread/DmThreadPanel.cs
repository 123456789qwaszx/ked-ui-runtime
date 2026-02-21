using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class DmThreadPanel : UIBase<DmThreadPanel.Refs>, IUIPanel
{
    #region Refs

    public enum Refs
    {
        Pnl_DmThread,
        ThreadScrollRect_ScrollRect,
        ThreadContent_Root,
    }

    private CanvasGroup _rootCg;
    private ScrollRect _scroll;
    private RectTransform _content;

    
    protected override void Initialize()
    {
        _rootCg = View.CanvasGroup(Refs.Pnl_DmThread);
        
        RectTransform scrollRt = View.Rect(Refs.ThreadScrollRect_ScrollRect);
        _scroll = scrollRt.GetComponent<ScrollRect>();
        
        _content = View.Rect(Refs.ThreadContent_Root);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
    }

    private bool ValidateRefs()
    {
        string missing = "";
        AppendMissing(ref missing, _rootCg,  Refs.Pnl_DmThread);
        AppendMissing(ref missing, _scroll,  Refs.ThreadScrollRect_ScrollRect);
        AppendMissing(ref missing, _content, Refs.ThreadContent_Root);

        if (missing.Length > 0)
            Debug.LogWarning($"[DmThreadPanel] Missing refs:\n{missing}", this);

        return missing.Length == 0;
    }
    
    #endregion
    
    [SerializeField] private DmEntryView tplIncoming;
    [SerializeField] private DmEntryView tplOutgoing;
    [SerializeField] private DmEntryView tplSystem;

    private readonly List<DmEntryView> _active = new();
    private bool _valid;
    
    public void AppendEntry(DmEntryModel model)
    {
        if (!_valid) 
            return;

        DmEntryView template = model.Kind switch
        {
            DmEntryKind.Incoming => tplIncoming,
            DmEntryKind.Outgoing => tplOutgoing,
            DmEntryKind.System   => tplSystem,
            _                    => tplIncoming,
        };

        GameObject go = Instantiate(template.gameObject);
        DmEntryView view = go.GetComponent<DmEntryView>();

        view.transform.SetParent(_content, worldPositionStays: false);

        view.Kind = model.Kind;

        _active.Add(view);
    }

    public void ClearThread()
    {
        if (!_valid) return;

        for (int i = 0; i < _active.Count; i++)
        {
            var entry = _active[i];
            if (entry) Destroy(entry.gameObject);
        }

        _active.Clear();
        ScrollToBottom();
    }
    
    public void Show(bool visible)
    {
        if (!_valid)
            return;

        _rootCg.alpha = visible ? 1f : 0f;
        _rootCg.interactable = visible;
        _rootCg.blocksRaycasts = visible;
    }

    public void ScrollToBottom()
    {
        if (!_valid)
            return;

        Canvas.ForceUpdateCanvases();
        _scroll.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}