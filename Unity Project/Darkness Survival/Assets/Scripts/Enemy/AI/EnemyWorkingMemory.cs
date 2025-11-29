using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(30)]
public class EnemyWorkingMemory : MonoBehaviour
{
    [Serializable]
    public struct MemoryEntry
    {
        public SituationState state;
        public EnemyAction action;
        public float reward;
        public double timestamp;
    }

    [SerializeField, Range(4, 128)] int capacity = 32;

    readonly Queue<MemoryEntry> entries = new Queue<MemoryEntry>();

    public event Action<MemoryEntry> EntryRecorded;

    public IReadOnlyCollection<MemoryEntry> Entries => entries;

    public void PushObservation(in SituationState state, in EnemyAction action, float reward)
    {
        MemoryEntry entry = new MemoryEntry
        {
            state = state,
            action = action,
            reward = reward,
            timestamp = Time.timeAsDouble
        };

        entries.Enqueue(entry);
        TrimToCapacity();
        EntryRecorded?.Invoke(entry);
    }

    public int GetSequence(MemoryEntry[] buffer)
    {
        if (buffer == null)
        {
            return 0;
        }

        int count = Mathf.Min(buffer.Length, entries.Count);
        if (count <= 0)
        {
            return 0;
        }

        MemoryEntry[] snapshot = entries.ToArray();
        Array.Copy(snapshot, snapshot.Length - count, buffer, 0, count);
        return count;
    }

    public MemoryEntry? LastEntry
    {
        get
        {
            if (entries.Count == 0)
            {
                return null;
            }

            MemoryEntry[] snapshot = entries.ToArray();
            return snapshot[snapshot.Length - 1];
        }
    }

    public void Clear()
    {
        entries.Clear();
    }

    void TrimToCapacity()
    {
        while (entries.Count > capacity)
        {
            entries.Dequeue();
        }
    }
}
