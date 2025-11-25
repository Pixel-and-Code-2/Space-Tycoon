using System.Collections.Generic;
using UnityEditor.Search;

public class PriorityQueue<T>
{
    private class QueueItem
    {
        public T Item { get; set; }
        public float Priority { get; set; }
    }

    private List<QueueItem> items = new List<QueueItem>();
    public int Count => items.Count;

    public void Enqueue(T item, float priority)
    {
        items.Add(new QueueItem { Item = item, Priority = priority });
        int i = items.Count - 1;

        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (items[parent].Priority <= items[i].Priority) break;

            var temp = items[i];
            items[i] = items[parent];
            items[parent] = temp;

            i = parent;
        }
    }


    public T Dequeue()
    {
        if (items.Count == 0) return default(T);

        T result = items[0].Item;
        items[0] = items[items.Count - 1];
        items.RemoveAt(items.Count - 1);

        int i = 0;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < items.Count && items[left].Priority < items[smallest].Priority)
            {
                smallest = left;
            }
            if (right < items.Count && items[right].Priority < items[smallest].Priority)
            {
                smallest = right;
            }

            if (smallest == i)
            {
                break;
            }

            var temp = items[i];
            items[i] = items[smallest];
            items[smallest] = temp;

            i = smallest;
        }
        return result;
    }

    public bool Contains(T item)
    {
        foreach (var queueItem in items)
        {
            if(queueItem.Item.Equals(item))
                return true; 
        }
        return false;
    }

    public void Clear()
    {
        items.Clear();
    }
}
