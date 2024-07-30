namespace Batchi.Core;

public class JobContext<TMessage>
{
    public int JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public required TMessage Message { get; set; }
}