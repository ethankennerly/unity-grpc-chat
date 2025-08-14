namespace MinimalChat
{
    /// <summary>
    /// Factory isolates how services are created so the presenter stays testable.
    /// </summary>
    public interface IChatServiceFactory
    {
        IChatService Create(bool useLoopback);
    }
}
