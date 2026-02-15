using System.Collections.Generic;
using UnityEngine;

public sealed class ChatRail : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Transform poolRoot;

    [Header("Prefabs")]
    [SerializeField] private ChatEntryView entryPrefab;

    [Header("Limits")]
    [SerializeField] private int maxEntries = 120;
    [SerializeField] private int consumeBudgetPerFrame = 6;
    [SerializeField] private int preloadPool = 40;

    private ChatEntryViewPool _pool;
    private readonly List<ChatEntryView> _alive = new List<ChatEntryView>(256);

    private IChatRenderResolver _resolver;
    private Queue<ChatEvent> _sourceQueue;

    public void BindSource(Queue<ChatEvent> sourceQueue, IChatRenderResolver resolver)
    {
        _sourceQueue = sourceQueue;
        _resolver = resolver;
    }

    private void Awake()
    {
        if (!contentRoot) contentRoot = transform;
        if (!poolRoot) poolRoot = transform;

        if (entryPrefab)
            _pool = new ChatEntryViewPool(entryPrefab, poolRoot, preloadPool);
    }

    private void Update()
    {
        if (_sourceQueue == null || _resolver == null || _pool == null || contentRoot == null)
            return;

        int budget = Mathf.Max(1, consumeBudgetPerFrame);

        while (budget-- > 0 && _sourceQueue.Count > 0)
        {
            ChatEvent evt = _sourceQueue.Dequeue();
            ChatRenderModel model = _resolver.Resolve(in evt);

            ChatEntryView view = _pool.Spawn(contentRoot);
            view.Bind(in model);

            _alive.Add(view);

            TrimIfNeeded();
        }
    }

    private void TrimIfNeeded()
    {
        int over = _alive.Count - Mathf.Max(1, maxEntries);
        if (over <= 0) return;

        // 오래된 것부터 제거
        for (int i = 0; i < over; i++)
        {
            var v = _alive[i];
            _pool.Despawn(v);
        }

        _alive.RemoveRange(0, over);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (_pool == null) return;

        for (int i = 0; i < _alive.Count; i++)
            _pool.Despawn(_alive[i]);

        _alive.Clear();

        if (_sourceQueue != null)
            _sourceQueue.Clear();
    }
}
