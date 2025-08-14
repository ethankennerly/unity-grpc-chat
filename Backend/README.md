# Backend (gRPC + Protobuf + SQLite)

Minimal backend service for **unity-minimal-chat**.  
Implements message persistence (SQLite) and real-time streaming (gRPC server).

---

## Quick Start

### 1. Install Dependencies
Run the [installation script](scripts/install-backend.sh) to install NuGet dependencies.

```
./scripts/install-backend.sh
```

### 2. Build & Run
Run the [build script](scripts/build-backend.sh) to compile the backend.

```
./scripts/build-backend.sh
```

Run the [start script](scripts/start-backend.sh) to launch the backend server locally.

```
./scripts/start-backend.sh
```

---

## Structure

- [`src/`](src) — Backend C# source code (gRPC services, SQLite persistence)
- [`protos/`](protos) — Protobuf definitions for chat service
- [`scripts/`](scripts) — Install/build/run scripts
- [`tests/`](tests) — NUnit backend test suite

---

## Notes

- Uses **.NET 8** minimal gRPC server.
- SQLite in **WAL mode** for write concurrency.
- Retains **last 1024 messages** only.
- All details and configurations are in the respective folder's README or script comments.
