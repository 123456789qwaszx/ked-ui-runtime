using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BroadcastHubSnsPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BroadcastHubUIRoot hub;

    [Header("SNS UI")]
    [SerializeField] private RectTransform snsContentRoot;      // Refs.PageSnsContent_Root
    [SerializeField] private SnsPostLineView tplPostLine;       // 한 줄 템플릿

    private readonly List<GameObject> _active = new();
    private bool _bound;

    private void Awake()
    {
        if (!hub)
        {
            Debug.LogError("[BroadcastHubSnsPresenter] hub is null.", this);
            enabled = false;
            return;
        }

        if (!snsContentRoot)
        {
            Debug.LogError("[BroadcastHubSnsPresenter] snsContentRoot is null. Assign PageSnsContent_Root.", this);
            enabled = false;
            return;
        }

        if (!tplPostLine)
        {
            Debug.LogError("[BroadcastHubSnsPresenter] tplPostLine is null. Assign a SnsPostLineView template.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (_bound) return;
        _bound = true;

        hub.OnTabSnsRequested += HandleTabSns;
    }

    private void OnDisable()
    {
        if (!_bound) return;
        _bound = false;

        hub.OnTabSnsRequested -= HandleTabSns;
    }

    private void HandleTabSns()
    {
        Clear();

        Append(new SnsPostModel(
            icon: null,
            headText: "라비@ravi_official",
            snsText: "오늘 방송 끝! 다들 고마워 ",
            media: null
        ));

        Append(new SnsPostModel(
            icon: null,
            headText: "익명@anon_122",
            snsText: "오늘 텐션 미쳤다ㅋㅋㅋㅋ",
            media: null
        ));
    }

    private void Append(in SnsPostModel model)
    {
        var go = Instantiate(tplPostLine.gameObject);
        go.transform.SetParent(snsContentRoot, false);

        var view = go.GetComponent<SnsPostLineView>();
        if (view) view.Present(model);

        _active.Add(go);
    }

    private void Clear()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i]) Destroy(_active[i]);
        }
        _active.Clear();
    }
}