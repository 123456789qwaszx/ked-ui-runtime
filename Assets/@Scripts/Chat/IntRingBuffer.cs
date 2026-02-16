using UnityEngine;

public sealed class IntRingBuffer
{
    private int[] _buf;
    private int _count;
    private int _head; // next write index

    public IntRingBuffer(int capacity)
    {
        _buf = capacity > 0 ? new int[capacity] : null;
        _count = 0;
        _head = 0;
    }

    public void Resize(int capacity)
    {
        if (capacity <= 0)
        {
            _buf = null;
            _count = 0;
            _head = 0;
            return;
        }

        var next = new int[capacity];
        int copy = Mathf.Min(_count, capacity);
        for (int i = 0; i < copy; i++)
        {
            next[i] = GetFromNewest(i);
        }
        // GetFromNewest(0)=newest 이므로 역순 복사 필요
        // 여기서는 단순하게 newest부터 담고 head를 맞춰줌
        for (int i = 0; i < copy / 2; i++)
        {
            int t = next[i];
            next[i] = next[copy - 1 - i];
            next[copy - 1 - i] = t;
        }

        _buf = next;
        _count = copy;
        _head = copy % capacity;
    }

    // newestIndex: 0=가장 최근, 1=그 전...
    private int GetFromNewest(int newestIndex)
    {
        int idx = _head - 1 - newestIndex;
        if (_buf == null) return 0;
        int cap = _buf.Length;
        while (idx < 0) idx += cap;
        return _buf[idx];
    }

    public void Clear()
    {
        _count = 0;
        _head = 0;
    }

    public void Push(int v)
    {
        if (_buf == null || _buf.Length == 0) return;

        _buf[_head] = v;
        _head = (_head + 1) % _buf.Length;
        if (_count < _buf.Length) _count++;
    }

    public bool Contains(int v)
    {
        if (_buf == null) return false;

        int cap = _buf.Length;
        int n = _count;
        int idx = _head - 1;
        while (idx < 0) idx += cap;

        for (int i = 0; i < n; i++)
        {
            if (_buf[idx] == v) return true;
            idx--;
            if (idx < 0) idx += cap;
        }
        return false;
    }
}