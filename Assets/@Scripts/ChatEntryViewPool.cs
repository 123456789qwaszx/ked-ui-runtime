using System.Collections.Generic;
using UnityEngine;

public sealed class ChatEntryViewPool
{
    private readonly ChatEntryView _prefab;
    private readonly Transform _poolRoot;
    private readonly Stack<ChatEntryView> _stack = new Stack<ChatEntryView>(64);

    public ChatEntryViewPool(ChatEntryView prefab, Transform poolRoot, int preload)
    {
        _prefab = prefab;
        _poolRoot = poolRoot;
        Preload(preload);
    }

    private void Preload(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var v = Object.Instantiate(_prefab, _poolRoot);
            v.gameObject.SetActive(false);
            _stack.Push(v);
        }
    }

    public ChatEntryView Spawn(Transform parent)
    {
        ChatEntryView v = _stack.Count > 0 ? _stack.Pop() : Object.Instantiate(_prefab, _poolRoot);
        v.transform.SetParent(parent, false);
        v.gameObject.SetActive(true);
        return v;
    }

    public void Despawn(ChatEntryView v)
    {
        if (!v) return;
        v.gameObject.SetActive(false);
        v.transform.SetParent(_poolRoot, false);
        _stack.Push(v);
    }
}