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
            ChatEntryView view = Object.Instantiate(_prefab, _poolRoot);
            view.gameObject.SetActive(false);
            _stack.Push(view);
        }
    }

    public ChatEntryView Spawn(Transform parent)
    {
        ChatEntryView view = _stack.Count > 0 ?
            _stack.Pop() 
            : Object.Instantiate(_prefab, _poolRoot);
        
        view.transform.SetParent(parent, false);
        view.gameObject.SetActive(true);
        return view;
    }

    public void Despawn(ChatEntryView view)
    {
        if (!view) 
            return;
        
        view.gameObject.SetActive(false);
        view.transform.SetParent(_poolRoot, false);
        _stack.Push(view);
    }
}