namespace MinimalChat
{
    /// <summary>
    /// UI surface used by the presenter. Implemented by a tiny MonoBehaviour adapter.
    /// </summary>
    public interface IChatView
    {
        string GetDisplayName();

        string GetMessageInput();

        void SetDisplayName(string value);

        void ClearMessageInput();

        bool IsLoopbackEnabled();

        void SetMessages(string text);

        event System.Action SendClicked;

        event System.Action LoopbackChanged;
    }
}
