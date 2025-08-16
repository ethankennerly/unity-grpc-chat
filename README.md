# Unity gRPC Chat
Minimal chat app with Unity 6 client and .NET backend over gRPC + Protobuf + SQLite.

## Quick Start

1. **Backend**
   See [Backend/README.md](Backend/README.md) for setup and scripts.

2. **Unity Client**
   - Open repo in Unity 6.
   - Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/tag/v4.5.0).
   - Use it to add:
     - `Grpc.Net.Client`
     - `Grpc.Net.Client.Web`
     - `Google.Protobuf`
     - `Grpc.Core.Api`
   - Enter Play Mode to test chat between clients.

## Functional Requirements
- Enter display name before sending messages.
- Type messages via keyboard (ASCII only, max 1024 chars).
- Shared history shows last 1024 messages with sender and timestamp.
- Messages reach all clients within 15s.
- Late joiners get recent history.

## Technical Requirements
- Unity required gRPC-Web over HTTP/1.1, Protobuf payloads.
- Backend persists to SQLite, keeps last 1024 rows.
- No auth, encryption, or scaling beyond 2 clients (local dev only).
- NUnit automated tests included.

## Architecture
- .NET 8 minimal gRPC server, SQLite storage.
- Unity client consumes gRPC stream, appends to scrollable UI.
- Clients dedupe & order by `id`.

### Message Flow

#### Connect & Subscribe
```mermaid
sequenceDiagram
    autonumber
    participant Client as Unity Client
    participant Server as .NET Backend
    participant DB as SQLite

    Client->>Server: Open gRPC channel
    Client->>Server: SubscribeMessages(sinceId)
    Server->>DB: SELECT messages WHERE id > sinceId ORDER BY id LIMIT 1024
    DB-->>Server: Backlog rows
    Server-->>Client: Stream backlog
    note over Server,Client: Stream remains open
    Server->>Client: Push live messages
```

#### Send & Broadcast
```mermaid
sequenceDiagram
    autonumber
    participant A as Client A
    participant B as Client B
    participant Server as .NET Backend
    participant DB as SQLite

    A->>Server: SendMessage(sender, text)
    Server->>Server: Validate ASCII, ≤1024 chars
    Server->>DB: INSERT row
    DB-->>Server: OK (id, createdAt)
    Server->>DB: DELETE old rows (retain 1024)
    Server-->>A: SendAck(id, createdAt)
    Server-->>A: Stream message
    Server-->>B: Stream message
```

#### Late Joiner
```mermaid
sequenceDiagram
    autonumber
    participant Client as New Client
    participant Server as .NET Backend
    participant DB as SQLite

    Client->>Server: SubscribeMessages(sinceId=0 or last seen)
    Server->>DB: SELECT last 1024 ORDER BY id
    DB-->>Server: Backlog rows
    Server-->>Client: Stream backlog
    Server-->>Client: Continue live
```
