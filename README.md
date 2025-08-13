# Unity Minimal Chat
Two clients chat in realtime using Unity 6.

## Installation
Unity 6 [Version](ProjectSettings/ProjectVersion.txt)

**Dependencies**

- **gRPC for Unity** — via Unity Package Manager (UPM) if available, or install `Grpc.Net.Client` and `Grpc.Tools` via NuGet for backend/.NET components.
- **Google.Protobuf** — included with gRPC packages, or install separately via NuGet.
- **SQLite** — use `Microsoft.Data.Sqlite` NuGet package for backend server.
- **NUnit** — Unity Test Framework package for client tests; NUnit NuGet for backend tests.

**Backend NuGet Installation**
```
dotnet add package Grpc.Net.Client
dotnet add package Grpc.Tools
dotnet add package Google.Protobuf
dotnet add package Microsoft.Data.Sqlite
dotnet add package NUnit
```

## Functional Requirements

### User Interaction
- Each client selects a display name before sending messages.
- Messages are entered via keyboard.
- ASCII text only; maximum 1024 characters per message.

### Network Behavior
- Minimum network speed supported: 64 kbps.
- Messages are delivered to other connected clients within 15 seconds.
- Late joiners receive recent chat history upon connect.

### Message History
- A shared message history is visible to all connected clients.
- Only the last 1024 chat entries are stored.
- Each message identifies the sender.

## Technical Requirements

### Transport & Serialization
- gRPC over HTTP/2.
- Protobuf for all request/response payloads.

### Persistent Storage
- Server persists messages to an SQL database (**SQLite**).
- Retains only the most recent **1024** messages.
  On each insert, delete all rows where `id <= MAX(id) - 1024`.

### Implementation Scope
- Authentication is out of scope.
- End-to-end encryption beyond TLS is out of scope (assumes local development).
- More than two clients is out of scope for the initial implementation.
- Server runs locally for development and testing.

### Code Quality
- Professional, production-ready C#.
- NUnit for automated testing.
- Consistent, self-documenting C# style.
- At most 100 characters per line where practical.

### Client Efficiency
- Minimize garbage allocation at runtime.

## Architecture

### Connect & Subscribe (backlog + live stream)
``` mermaid
sequenceDiagram
    autonumber
    participant FE as Frontend: Client
    participant BE as Backend: Server
    participant DB as Backend: SQLite DB

    FE->>BE: Open gRPC channel
    FE->>BE: SubscribeMessages(sinceId)
    BE->>DB: SELECT messages WHERE id > sinceId ORDER BY id LIMIT 256
    DB-->>BE: Backlog messages (≤ 1024 retained)
    BE-->>FE: Stream backlog messages
    note over BE,FE: Stream remains open for live updates
    BE->>FE: On new insert → stream Message
```

### Send Message (unary) and broadcast
``` mermaid
sequenceDiagram
    autonumber
    participant FEa as Frontend: Client A
    participant FEb as Frontend: Client B
    participant BE as Backend: Server
    participant DB as Backend: SQLite DB

    FEa->>BE: SendMessage(sender, text)
    BE->>BE: Validate ASCII, length ≤ 1024
    BE->>DB: INSERT message(sender, text, createdAt)
    DB-->>BE: Insert OK (id)
    BE->>DB: DELETE FROM messages WHERE id <= MAX(id) - 1024
    BE-->>FEa: SendAck(id, createdAt)
    BE-->>FEb: Stream Message(id, sender, text, createdAt)
    FEa->>FEa: Append to scroll (optional echo via stream)
    FEb->>FEb: Append to scroll
```

### Late Joiner (history replay on connect)
``` mermaid
sequenceDiagram
    autonumber
    participant FE as Frontend: New Client
    participant BE as Backend: Server
    participant DB as Backend: SQLite DB

    FE->>BE: SubscribeMessages(sinceId = 0 or last seen)
    BE->>DB: SELECT last ≤ 1024 messages ORDER BY id
    DB-->>BE: Backlog
    BE-->>FE: Stream backlog then continue live
```

## Service Contracts (overview)
- **Chat.SendMessage** — Unary RPC
  **Input:** `{ sender, text }`
  **Output:** `{ id, createdAt }`
  - `id: int64` (monotonic primary key, defines total order).
  - `createdAt: string` in ISO-8601 UTC (`2025-08-13T12:34:56Z`).
  - Server validates ASCII only and `text.Length <= 1024`; rejects with `INVALID_ARGUMENT`.

- **Chat.SubscribeMessages** — Server-streaming RPC
  **Input:** `{ sinceId }`
  **Output (stream):** `{ id, sender, text, createdAt }`
  - Returns backlog `id > sinceId` in ascending order (max 256 per batch).
  - Continues as a live stream; delivery is **at-least-once**.
  - Clients must ignore duplicates and out-of-order IDs.

## Data Model (SQLite)
- `messages(id INTEGER PK AUTOINCREMENT, createdAt TEXT, sender TEXT, text TEXT)`
- Retain only the latest 1024 rows; purge older records on insert.
- Index on `id` (implicit PK) and optionally on `createdAt`.

## Frontend Notes (Unity)
- Maintain `lastReceivedId`; on connect call `SubscribeMessages(lastReceivedId)`.
- Consume the gRPC stream on a background task; dispatch to main thread for UI updates.
- Append messages to a scrollable view; format like: `[HH:mm] sender: text`.
- On stream error, retry with exponential backoff (500 ms → 5 s) using last received `id`.

## Backend Notes (.NET)
- .NET 8 minimal gRPC server; SQLite via `Microsoft.Data.Sqlite` (+ Dapper).
- SQLite PRAGMAs: WAL mode, `busy_timeout`, `synchronous=NORMAL`.
- Broadcast new inserts to all active subscribers.
- Provide in-memory SQLite mode for automated tests (backlog + live stream integration).
