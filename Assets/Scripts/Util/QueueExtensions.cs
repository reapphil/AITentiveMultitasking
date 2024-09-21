using System.Collections.Generic;

public static class QueueExtensions
{
    public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize) 
    { 
        var result = new List<T>(); 

        for (var i = 0; i < chunkSize && queue.Count > 0; i++) 
        { 
            result.Add(queue.Dequeue()); 
        } 

        return result; 
    }

    public static IEnumerable<T> DequeueAll<T>(this Queue<T> queue)
    {
        return DequeueChunk(queue, queue.Count);
    }
}