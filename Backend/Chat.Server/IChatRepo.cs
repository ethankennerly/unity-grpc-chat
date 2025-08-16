using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chat.Server
{
    /// <summary>
    /// Minimal persistence contract: insert, read backlog, and a live insert event.
    /// </summary>
    public interface IChatRepo
    {
        /// <summary>
        /// Insert a message and return its id and created-at (epoch ms).
        /// </summary>
        Task<(long id, long createdAt)> InsertAsync(
            string sender,
            string text,
            CancellationToken ct);

        /// <summary>
        /// Read all rows with id &gt; sinceId in ascending id order.
        /// </summary>
        Task<List<RepoMessage>> ReadBacklogAsync(
            long sinceId,
            CancellationToken ct);

        /// <summary>
        /// Raised after a successful insert. Subscribers get the full row payload.
        /// </summary>
        event Action<RepoMessage>? OnInserted;
    }

    /// <summary>
    /// Plain DTO the repo returns/raises.
    /// </summary>
    public sealed class RepoMessage
    {
        public long Id { get; set; }
        public long CreatedAt { get; set; } // epoch ms
        public string Sender { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
