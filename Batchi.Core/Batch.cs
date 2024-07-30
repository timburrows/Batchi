using System.Collections;

namespace Batchi.Core;

public class Batch<TMessage>() : IBatch<TMessage>
    where TMessage : class
{
    public List<JobContext<TMessage>> Contexts = new();


    public JobContext<TMessage> this[int index] => Contexts[index];
    public int Length => Contexts.Count;
    
    public IEnumerator<JobContext<TMessage>> GetEnumerator()
    {
        return Contexts.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public interface IBatch<TMessage> : IEnumerable<JobContext<TMessage>>
    where TMessage : class
{
    public JobContext<TMessage> this[int index] { get; }
    public int Length { get; }
}