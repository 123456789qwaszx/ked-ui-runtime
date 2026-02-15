// IdolSpeechQueue.cs
using System.Collections.Generic;
using UnityEngine;

public sealed class IdolSpeechQueue : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float minInterval = 1.2f;
    [SerializeField] private float maxInterval = 2.0f;

    private readonly Queue<string> queue = new();
    [SerializeField]private ChatRail chatRail;

    private Coroutine routine;
    private bool paused;

    public void Enqueue(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        queue.Enqueue(line.Trim());
    }

    public void Clear()
    {
        queue.Clear();
    }

    public void Play()
    {
        if (routine != null) return;
        routine = StartCoroutine(PlayRoutine());
    }

    public void Pause()
    {
        paused = true;
    }

    public void Resume()
    {
        paused = false;
    }

    private System.Collections.IEnumerator PlayRoutine()
    {
        while (queue.Count > 0)
        {
            while (paused)
                yield return null;

            if (!chatRail)
                break;

            string line = queue.Dequeue();
            chatRail.PushIdol(line);

            float dt = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(dt);
        }

        routine = null;
    }
}